using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
 
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ClipPlus.service
{
    class CommonService
    {
        /// <summary>
        /// 图片类型，通过在内容前面增加前缀来标识
        /// </summary>
        private static string imageType = "``+image|";
        /// <summary>
        /// html类型，通过在内容前面增加前缀来标识
        /// </summary>
        private static string htmlType = "``+html|";
        /// <summary>
        /// 文件类型，通过在内容前面增加前缀来标识
        /// </summary>
        private static string fileType = "``+file|";
        /// <summary>
        /// 设置开机启动
        /// </summary>
        public static void SetStartup(bool isAutoStartup)
        {

            RegistryKey reg = Registry.CurrentUser.CreateSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run");
            string exePath = System.Windows.Forms.Application.ExecutablePath;
            string exeName = Path.GetFileName(exePath);
            if (!isAutoStartup)
            {
                if (reg.GetValue(exeName) != null)
                {

                    reg.DeleteValue(exeName);
                }
            }
            else
            {

                reg.SetValue(exeName, exePath);
            }

        }

        /// <summary>
        /// 持久化
        /// </summary>
        public static void SaveData(List<string> resultList,string storePath)
        {
            string json = JsonConvert.SerializeObject(resultList);
            File.WriteAllText(storePath, json);
        }


        /// <summary>
        /// 设置条目到剪切板
        /// </summary>
        /// <param name="result"></param>
        public static void SetValueToClip(string result)
        {
            if (result.StartsWith(imageType))
            {
                try
                {
                    result = result.Replace(imageType, "");
                    BitmapImage bitImg = new BitmapImage();
                    bitImg.BeginInit();
                    bitImg.UriSource = new Uri(result, UriKind.Relative);
                    bitImg.EndInit();
                    IDataObject data = new DataObject(DataFormats.Bitmap, bitImg);
                    Clipboard.SetDataObject(data, false);


                }
                catch {  return; }
            }
            else if (result.StartsWith(htmlType))
            {
                result = result.Replace(htmlType, "");
                IDataObject data = new DataObject(DataFormats.Html, result);
                Clipboard.SetDataObject(data, false);
            }
            else if (result.StartsWith(fileType))
            {
                string[] tmp = result.Split('\n');
                string[] files = new string[tmp.Length - 1];
                StringCollection coll = new StringCollection();

                for (int i = 0; i < tmp.Length; i++)
                {
                    if (i != 0)
                    {
                        coll.Add(tmp[i]);
                        files[i - 1] = tmp[i];

                    }
                }
                try
                {
                    IDataObject data = new DataObject(DataFormats.FileDrop, files);
                    MemoryStream memo = new MemoryStream(4);
                    byte[] bytes = new byte[] { (byte)(5), 0, 0, 0 };
                    memo.Write(bytes, 0, bytes.Length);
                    data.SetData("PreferredDropEffect", memo);
                    Clipboard.SetDataObject(data, false);
                }
                catch { return; }
            }
            else
            {
                try
                {
                    IDataObject data = new DataObject(DataFormats.Text, result);
                    System.Windows.Forms.Clipboard.SetDataObject(data, false);

                }
                catch   {   }
            }
        }

        /// <summary>
        /// 防止程序重复启动
        /// </summary>
        public static void ExitWhenExists()
        {

            Process[] pro = Process.GetProcesses();
            int n = pro.Where(p => p.ProcessName.ToLower().Equals("clipplus")).Count();
            if (n > 1)
            {
                Application.Current.Shutdown();
                return;
            }
        }

        /// <summary>
        /// 程序启动时清理缓存目录中的失效图片，如果缓存目录中的图片在持久化的图片信息中找不到对应记录，则删掉
        /// </summary>
        /// <param name="cacheDir">缓存目录</param>
        /// <param name="lastSaveImg">已经持久化的图片信息</param>
        public static void ClearExpireImage(string cacheDir,string lastSaveImg)
        {


            List<string> imageList = Directory.EnumerateFiles(cacheDir).ToList();
            foreach (string str in imageList)
            {
                if (!lastSaveImg.Contains(str.Replace("\\", "/")))
                {
                    File.Delete(str);
                }
            }

        }

        /// <summary>
        /// 发送ctrl+v按键消息，暂时废弃
        /// </summary>
        public static void SendPasteKey(IntPtr activeWnd)
        {
            try
            {
                //var SelfThreadId = WinAPIHelper.GetCurrentThreadId();//获取本身的线程ID
                //var ForeThreadId = WinAPIHelper.GetWindowThreadProcessId(activeWnd, IntPtr.Zero);//根据窗口句柄获取线程ID
                //WinAPIHelper.AttachThreadInput((IntPtr)ForeThreadId, SelfThreadId, 1);//附加线程
               // WinAPIHelper.SetForegroundWindow(activeWnd);
               // IntPtr foreWnd = WinAPIHelper.GetFocus();//获取具有输入焦点的窗口句柄

                //WinAPIHelper.AttachThreadInput((IntPtr)ForeThreadId, SelfThreadId, 0);//取消附加的线程


                //发送按键消息
                uint KEYEVENTF_KEYUP = 2;


                byte VK_CONTROL = 0x11;
 

                WinAPIHelper.keybd_event(VK_CONTROL, 0, 0, 0);
                WinAPIHelper.keybd_event(0x56, 0, 0, 0); //Send the C key (43 is "C") 56 is v
                WinAPIHelper.keybd_event(0x56, 0, KEYEVENTF_KEYUP, 0);

                WinAPIHelper.keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, 0);// 'Left Control Up


            }
            catch (Exception e) { Console.WriteLine(e.Message); return; }
        }

    }
}
