using ClipOne.model;
using ClipOne.util;
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

namespace ClipOne.service
{
    class ClipService
    {
        /// <summary>
        /// 图片类型，通过在内容前面增加前缀来标识
        /// </summary>
        public   const string IMAGE_TYPE = "image";
        /// <summary>
        /// html类型，通过在内容前面增加前缀来标识
        /// </summary>
        public const string HTML_TYPE = "html";

       
        /// <summary>
        /// 文件类型，通过在内容前面增加前缀来标识
        /// </summary>
        public const string FILE_TYPE = "file";

        /// <summary>
        /// QQ富文本类型
        /// </summary>
        public const string QQ_RICH_TYPE = "QQ_Unicode_RichEdit_Format";

        /// <summary>
        /// Q文本类型
        /// </summary>
        public const string TEXT_TYPE = "text";

        /// <summary>
        /// 缓存目录
        /// </summary>
        public static string cacheDir = "cache";
        /// <summary>
        /// 设置开机启动
        /// </summary>
        public static void SetStartup(bool isAutoStartup)
        {

            RegistryKey reg = Registry.CurrentUser.CreateSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run");
            string exePath = System.Windows.Forms.Application.ExecutablePath;
            string exeName = System.Windows.Forms.Application.ProductName;
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
        public static void SaveData(List<ClipModel> resultList,string storePath)
        {
            string json = JsonConvert.SerializeObject(resultList);
            File.WriteAllText(storePath, json);
        }

        /// <summary>
        /// 保存图片到缓存目录
        /// </summary>
        /// <param name="bs"></param>
        /// <returns></returns>
        public static string SaveImage(BitmapSource bs)
        {
            string path = cacheDir + "/" + Guid.NewGuid().ToString() + ".bmp";
            BitmapEncoder encoder = new BmpBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bs));

            FileStream fs = new FileStream(path, FileMode.Create);
            encoder.Save(fs);
            fs.Close();
            return path;
        }
        /// <summary>
        /// 设置条目到剪切板
        /// </summary>
        /// <param name="result"></param>
        public static void SetValueToClip(ClipModel result)
        {
            if (result.Type==IMAGE_TYPE)
            {
                try
                {
                    
                    BitmapImage bitImg = new BitmapImage();
                    bitImg.BeginInit();
                    bitImg.UriSource = new Uri(result.ClipValue, UriKind.Relative);
                    bitImg.EndInit();
                    IDataObject data = new DataObject(DataFormats.Bitmap, bitImg);
                    Clipboard.SetDataObject(data, false);


                }
                catch {  return; }
            }
            else if (result.Type == HTML_TYPE)
            {
                
                IDataObject data = new DataObject(DataFormats.Html, result.ClipValue);
                Clipboard.SetDataObject(data, false);
            }
            else if (result.Type == QQ_RICH_TYPE)
            {

                
                MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(result.ClipValue));
                IDataObject data = new DataObject(QQ_RICH_TYPE, ms);
                Clipboard.SetDataObject(data, false);
            }
            else if (result.Type == FILE_TYPE)
            {
                string[] tmp = result.ClipValue.Split(',');
                 
                
                try
                {
                    IDataObject data = new DataObject(DataFormats.FileDrop, tmp);
                    MemoryStream memo = new MemoryStream(4);
                    byte[] bytes = new byte[] { (byte)(5), 0, 0, 0 };
                    memo.Write(bytes, 0, bytes.Length);
                    data.SetData("PreferredDropEffect", memo);
                    Clipboard.SetDataObject(data, false);
                }
                catch  {   return; }
            }
            else
            {
                try
                {
                    IDataObject data = new DataObject(DataFormats.Text, result.ClipValue);
                    System.Windows.Forms.Clipboard.SetDataObject(data, false);

                }
                catch   {   }
            }
        }

        

        /// <summary>
        /// 程序启动时清理缓存目录中的失效图片，如果缓存目录中的图片在持久化的图片信息中找不到对应记录，则删掉
        /// </summary>
        /// <param name="cacheDir">缓存目录</param>
        /// <param name="lastSaveImg">已经持久化的图片信息</param>
        public static void ClearExpireImage( object lastSaveImg)
        {


            List<string> imageList = Directory.EnumerateFiles(cacheDir).ToList();
            foreach (string str in imageList)
            {
                if (!lastSaveImg.ToString().Contains(str.Replace("\\", "/")))
                {
                    File.Delete(str);
                }
            }

        }

         
    }
}
