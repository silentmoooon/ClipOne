
using ClipOne.model;
using ClipOne.service;
using ClipOne.util;
using Microsoft.Toolkit.Win32.UI.Controls.Interop.WinRT;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Resources;
using static ClipOne.service.ClipService;

namespace ClipOne.view
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {

        /// <summary>
        /// 缓存目录
        /// </summary>
        public static string cacheDir = "cache";

        private static string webChche = System.IO.Directory.GetCurrentDirectory() + @"\webCache";
        /// <summary>
        /// 配置文件路径
        /// </summary>
        private static string settingsPath = "config\\settings.json";


        /// <summary>
        /// css目录
        /// </summary>
        private static string cssDir = "html\\css";

        /// <summary>
        /// 默认显示页面
        /// </summary>
        private static string defaultHtml = "html/index.html";

        /// <summary>
        /// 活动窗口句柄,在显示本窗口前,缓存当前活动窗口
        /// </summary>
        private IntPtr activeHwnd = IntPtr.Zero;

        /// <summary>
        /// 隐藏时将left设置为该值
        /// </summary>
        private int HideLeftValue = -10000;

        /// <summary>
        /// 透明度转换比例
        /// </summary>
        private double OpacityRatio = 0.06;


     private static   bool ctrlPress = false;

 

        public static bool isShow = false;

        /// <summary>
        /// 剪切板事件
        /// </summary>
        private static int WM_CLIPBOARDUPDATE = 0x031D;

        /// <summary>
        /// 注册快捷键全局原子字符串 
        /// </summary>
        private static string hotkeyAtomStr = "clipPlusAtom...";
        /// <summary>
        /// 快捷键全局原子
        /// </summary>
        private static int hotkeyAtom;

        /// <summary>
        /// 快捷键修饰键
        /// </summary>
        private static int hotkeyModifier = (int)HotKeyManager.KeyModifiers.Alt;
        /// <summary>
        /// 快捷键按键
        /// </summary>
        private static int hotkeyKey = (int)System.Windows.Forms.Keys.V;


        /// <summary>
        /// 是否开机启动
        /// </summary>
        private static bool autoStartup = false;

        /// <summary>
        /// 透明度
        /// </summary>
        private static double opacityValue = 1;

        /// <summary>
        /// 默认保存记录数
        /// </summary>
        private static int currentRecords = 100;

        /// <summary>
        /// 允许保存的最大记录数
        /// </summary>
        private static int maxRecords = 300;


        /// <summary>
        /// 默认皮肤
        /// </summary>
        private static string skinName = "stand";


        /// <summary>
        /// 默认支持格式
        /// </summary>
        public static ClipType supportFormat = ClipType.qq | ClipType.html | ClipType.image | ClipType.file | ClipType.text;

        /// <summary>
        /// 配置项map
        /// </summary>
        private static Dictionary<String, String> settingsMap = new Dictionary<string, string>();


        /// <summary>
        /// 托盘图标
        /// </summary>
        private System.Windows.Forms.NotifyIcon notifyIcon = null;

        /// <summary>
        /// 当前应用句柄
        /// </summary>
        private IntPtr wpfHwnd;


        /// <summary>
        /// 预览窗口
        /// </summary>
        private PreviewForm preview;



        public MainWindow()
        {
            InitializeComponent();

            System.IO.Directory.SetCurrentDirectory(System.Windows.Forms.Application.StartupPath);



        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {


            //如果配置文件存在则读取配置文件，否则按默认值设置
            if (File.Exists(settingsPath))
            {
                InitConfig();
            }
            if (!Directory.Exists(cacheDir))
            {
                Directory.CreateDirectory(cacheDir);
            }

            //初始化浏览器
            InitWebView();

 
            //初始化托盘图标
            InitialTray();




            //注册热键,如果注册热键失败则弹出热键设置界面
            hotkeyAtom = HotKeyManager.GlobalAddAtom(hotkeyAtomStr);
            bool status = HotKeyManager.RegisterHotKey(wpfHwnd, hotkeyAtom, hotkeyModifier, hotkeyKey);
            if (!status)
            {
                Hotkey_Click(null, null);
            }

            //初始化预览窗口
            InitPreviewForm();
 

        }





      
        /// <summary>
        /// 初始化预览窗口
        /// </summary>
        private void InitPreviewForm()
        {
            preview = new PreviewForm(this);
            preview.Focusable = false;
            preview.IsHitTestVisible = false;
            preview.IsTabStop = false;
            preview.ShowInTaskbar = false;
            preview.ShowActivated = false;
        }

        /// <summary>
        /// /加载设置项
        /// </summary>
        private static void InitConfig()
        {

            string json = File.ReadAllText(settingsPath);
            settingsMap = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            if (settingsMap.ContainsKey("startup"))  //自启动
            {
                autoStartup = bool.Parse(settingsMap["startup"]);
                if (autoStartup)
                {
                    SetStartup(autoStartup);
                }
            }
            if (settingsMap.ContainsKey("skin"))  //皮肤
            {
                skinName = settingsMap["skin"];
            }
            if (settingsMap.ContainsKey("record"))  //保存记录数
            {
                currentRecords = int.Parse(settingsMap["record"]);
            }
            if (settingsMap.ContainsKey("key"))   //快捷键
            {
                hotkeyKey = int.Parse(settingsMap["key"]);
                hotkeyModifier = int.Parse(settingsMap["modifier"]);

            }
            if (settingsMap.ContainsKey("format"))  //支持格式
            {
                supportFormat = (ClipType)int.Parse(settingsMap["format"]);
            }
            if (settingsMap.ContainsKey("opacity"))  //透明度
            {
                opacityValue = double.Parse(settingsMap["opacity"]);

            }
            if (settingsMap.ContainsKey("cache"))
            {
                cacheDir = settingsMap["cache"];
            }
            if (settingsMap.ContainsKey("webCache"))
            {
                webChche = settingsMap["webCache"];
            }

        }

        /// <summary>
        /// 初始化浏览器
        /// </summary>
        private void InitWebView()
        {
          
            webView1.IsJavaScriptEnabled = true;
            webView1.IsScriptNotifyAllowed = true;
       
            webView1.IsIndexedDBEnabled = true;
            webView1.ScriptNotify += WebView1_ScriptNotify;
            
            webView1.NavigateToLocal(defaultHtml);
             




        }
      
         
        private void WebView1_ScriptNotify(object sender, Microsoft.Toolkit.Win32.UI.Controls.Interop.WinRT.WebViewControlScriptNotifyEventArgs e)
        {
            string[] args = e.Value.Split(':');
         
            if (args[0] == "PasteValue")
            {
                 
                    PasteValue(args[1]);
                 
            }
            else if (args[0] == "PasteValueList")
            {
                 
                    PasteValueList(args[1]);
                
            }
            else if (args[0] == "DeleteImage")
            {
                new Thread(new ParameterizedThreadStart(DeleteFile)).Start(args[1]);
            }
            else if (args[0] == "ChangeWindowHeight")
            {
                ChangeWindowHeight(double.Parse(args[1]));
            }
            else if (args[0] == "Preview")
            {
                ShowPreviewForm(args[1]);
            }
            else if (args[0] == "HidePreview")
            {
                HidePreview();
            }
        }


        private void DeleteFile(object path)
        {
            for (int i = 0; i < 3; i++)
            {
                Thread.Sleep(i * 500);
                try
                {
                    File.Delete(path.ToString());
                    return;
                }
                catch
                {

                }
            }
        }

        /// <summary>
        /// 初始化托盘图标及菜单
        /// </summary>
        private void InitialTray()
        {
            string productName = System.Windows.Forms.Application.ProductName;
            //设置托盘图标
            notifyIcon = new System.Windows.Forms.NotifyIcon();
            notifyIcon.Text = productName;

            StreamResourceInfo info = Application.GetResourceStream(new Uri("/" + productName + ".ico", UriKind.Relative));
            Stream s = info.Stream;
            notifyIcon.Icon = new System.Drawing.Icon(s);
            notifyIcon.Visible = true;



            //设置菜单项
            System.Windows.Forms.MenuItem exit = new System.Windows.Forms.MenuItem("退出");
            System.Windows.Forms.MenuItem separator0 = new System.Windows.Forms.MenuItem("-");
            System.Windows.Forms.MenuItem startup = new System.Windows.Forms.MenuItem("开机自启");

            System.Windows.Forms.MenuItem separator1 = new System.Windows.Forms.MenuItem("-");
            System.Windows.Forms.MenuItem hotkey = new System.Windows.Forms.MenuItem("热键");

            System.Windows.Forms.MenuItem record = new System.Windows.Forms.MenuItem("记录数");
            System.Windows.Forms.MenuItem separator2 = new System.Windows.Forms.MenuItem("-");
            System.Windows.Forms.MenuItem opaSet = new System.Windows.Forms.MenuItem("透明度");
            System.Windows.Forms.MenuItem skin = new System.Windows.Forms.MenuItem("皮肤");
            System.Windows.Forms.MenuItem reload = new System.Windows.Forms.MenuItem("刷新");
            System.Windows.Forms.MenuItem separator3 = new System.Windows.Forms.MenuItem("-");
            System.Windows.Forms.MenuItem format = new System.Windows.Forms.MenuItem("格式");
            System.Windows.Forms.MenuItem clear = new System.Windows.Forms.MenuItem("清空");

            opaSet.Click += (sender, e) =>
            {
                
                OpacitySet os = new OpacitySet(this, (1 - opacityValue) / OpacityRatio);

                os.Topmost = true;
                os.ShowDialog();
                
            };

            clear.Click += Clear_Click;
            reload.Click += new EventHandler(Reload);
            exit.Click += new EventHandler(Exit_Click);
            hotkey.Click += Hotkey_Click;
            startup.Click += Startup_Click;
            startup.Checked = autoStartup;


            //增加记录数设置子菜单项
            for (int i = 100; i <= maxRecords; i += 100)
            {

                string recordsNum = i.ToString();
                System.Windows.Forms.MenuItem subRecord = new System.Windows.Forms.MenuItem(recordsNum);
                if (int.Parse(recordsNum) == currentRecords)
                {
                    subRecord.Checked = true;
                }
                subRecord.Click += RecordSet_Click;
                record.MenuItems.Add(subRecord);

            }

            //增加格式选择子菜单项
            foreach (ClipType type in Enum.GetValues(typeof(ClipType)))
            {

                System.Windows.Forms.MenuItem subFormat = new System.Windows.Forms.MenuItem(Enum.GetName(typeof(ClipType), type));
                subFormat.Tag = type;
                if ((supportFormat & type) != 0)
                {
                    subFormat.Checked = true;

                }
                if (type == ClipType.text)
                {
                    subFormat.Enabled = false;
                }
                else
                {
                    subFormat.Click += SubFormat_Click;
                }
                format.MenuItems.Add(subFormat);
            }

            //根据css文件创建皮肤菜单项
            if (Directory.Exists(cssDir))
            {
                string[] fileList = Directory.GetDirectories(cssDir);

                foreach (string file in fileList)
                {

                    string fileName = Path.GetFileName(file);
                    System.Windows.Forms.MenuItem subRecord = new System.Windows.Forms.MenuItem(fileName);
                    if (skinName.Equals(fileName.ToLower()))
                    {
                        subRecord.Checked = true;


                    }
                    subRecord.Tag = file;
                    skin.MenuItems.Add(subRecord);
                    subRecord.Click += SkinItem_Click;
                }
            }

            //关联菜单项至托盘
            System.Windows.Forms.MenuItem[] childen = new System.Windows.Forms.MenuItem[] { clear, format, separator3, reload, skin, opaSet, separator2, record, hotkey, separator1, startup, separator0, exit };
            notifyIcon.ContextMenu = new System.Windows.Forms.ContextMenu(childen);


        }

        /// <summary>
        /// 选择支持格式
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SubFormat_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.MenuItem item = (System.Windows.Forms.MenuItem)sender;
            if (item.Checked)
            {
                item.Checked = false;
                supportFormat = supportFormat & ~((ClipType)item.Tag);


            }
            else
            {

                item.Checked = true;
                supportFormat = supportFormat | ((ClipType)item.Tag);
            }
            settingsMap["format"] = ((int)supportFormat).ToString();
            SaveSettings();
        }



        private void SkinItem_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.MenuItem item = (System.Windows.Forms.MenuItem)sender;
            foreach (System.Windows.Forms.MenuItem i in item.Parent.MenuItems)
            {
                i.Checked = false;
            }
            item.Checked = true;
            settingsMap["skin"] = item.Text;
            SaveSettings();

            webView1.InvokeScript("saveData");
            string css = item.Tag.ToString();
            ChangeSkin(css);

        }


        /// <summary>
        /// 通过修改index.html中引入的样式文件来换肤
        /// </summary>
        /// <param name="cssPath"></param>
        private void ChangeSkin(string cssPath)
        {

            List<string> fileLines = File.ReadAllLines(defaultHtml).ToList();
            for (int i = 0; i < fileLines.Count; i++)
            {
                string str = fileLines.Last().Trim();
                if (str == "" || str.StartsWith("<link"))
                {
                    fileLines.RemoveAt(fileLines.Count - 1);
                }
                else
                {
                    break;
                }
            }
            string[] files = Directory.GetFiles(cssPath);

            foreach (string file in files)
            {

                string str = file.Replace("\\", "/").Replace("html/", "");
                fileLines.Add(" <link rel='stylesheet' type='text/css' href='" + str + "'/>");
            }
            File.WriteAllLines(defaultHtml, fileLines, Encoding.UTF8);

            webView1.Refresh();


        }

        /// <summary>
        /// 设置是否开机启动
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Startup_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.MenuItem item = (System.Windows.Forms.MenuItem)sender;
            item.Checked = !item.Checked;

            SetStartup(item.Checked);
            settingsMap["startup"] = item.Checked.ToString();
            SaveSettings();
        }

        /// <summary>
        /// 设置热键
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Hotkey_Click(object sender, EventArgs e)
        {
            SetHotKeyForm sethk = new SetHotKeyForm();
            sethk.HotkeyKey = hotkeyKey;
            sethk.HotkeyModifier = hotkeyModifier;
            sethk.WpfHwnd = wpfHwnd;
            sethk.HotkeyAtom = hotkeyAtom;
            if (sethk.ShowDialog() == true)
            {

                hotkeyKey = sethk.HotkeyKey;
                hotkeyModifier = sethk.HotkeyModifier;

                settingsMap["modifier"] = hotkeyModifier.ToString();
                settingsMap["key"] = hotkeyKey.ToString();
                SaveSettings();
            }
        }

        /// <summary>
        /// 保存设置
        /// </summary>
        private void SaveSettings()
        {
            string json = JsonConvert.SerializeObject(settingsMap);
            File.WriteAllText(settingsPath, json);
        }


        /// <summary>
        /// 修改透明度
        /// </summary>
        /// <param name="value"></param>
        public void ChangeOpacity(double value)
        {
            opacityValue = 1 - value * OpacityRatio;
            this.Opacity = opacityValue;
            settingsMap["opacity"] = opacityValue.ToString();
            SaveSettings();
        }

        /// <summary>
        /// 设置保存记录数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RecordSet_Click(object sender, EventArgs e)
        {

            System.Windows.Forms.MenuItem item = ((System.Windows.Forms.MenuItem)sender);
            foreach (System.Windows.Forms.MenuItem i in item.Parent.MenuItems)
            {
                i.Checked = false;
            }
            item.Checked = true;
            currentRecords = int.Parse(item.Text);
            settingsMap["record"] = item.Text;
            SaveSettings();


            webView1.InvokeScript("setMaxRecords", currentRecords.ToString());


        }

        /// <summary>
        /// 清空所有条目
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Clear_Click(object sender, EventArgs e)
        {
            string[] list = Directory.GetFiles(cacheDir);
            Parallel.ForEach(list, file =>
            {
                File.Delete(file);
            });

            webView1.InvokeScript("clear");
        }



        /// <summary>
        /// 刷新页面
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Reload(object sender, EventArgs e)
        {

            webView1.InvokeScript("saveData");
            webView1.Refresh();
        }

        private void Exit_Click(object sender, EventArgs e)
        {

            Application.Current.Shutdown();

        }


        /// <summary>
        /// 主要用来处理剪切板消息和热键
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="msg"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <param name="handled"></param>
        /// <returns></returns>
        public IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {

            //当为剪切板消息时，由于获取数据会有失败的情况，所以循环3次，尽量确保成功
            if (msg == WM_CLIPBOARDUPDATE)
            {


                ClipModel clip = new ClipModel();

                if (Clipboard.ContainsData(WECHAT_TYPE))
                {
                    HandleClipWeChat(clip);
                }

                //处理剪切板QQ自定义格式
                else if ((supportFormat & ClipType.qq) != 0 && Clipboard.ContainsData(QQ_RICH_TYPE))
                {
                    HandleClipQQ(clip);

                }
                //处理剪切板文件
                else if (Clipboard.ContainsText())
                {

                    HandClipText(clip);

                }
                //处理HTML类型
                else if ((supportFormat & ClipType.html) != 0 && Clipboard.ContainsData(DataFormats.Html))
                {
                    HandleClipHtml(clip);

                }
                //处理图片
                else if ((supportFormat & ClipType.image) != 0 && (Clipboard.ContainsImage() || Clipboard.ContainsData(DataFormats.Dib)))
                {
                    HandleClipImage(clip);

                }



                //处理剪切板文件
                else if ((supportFormat & ClipType.file) != 0 && Clipboard.ContainsFileDropList())
                {
                    HandleClipFile(clip);

                }

                else
                {

                    return IntPtr.Zero;
                }
                if (string.IsNullOrWhiteSpace(clip.ClipValue))
                {
                    return IntPtr.Zero;
                }

                EnQueue(clip);

                handled = true;
            }
            //触发显示界面快捷键
            else if (msg == HotKeyManager.WM_HOTKEY)
            {

                if (hotkeyAtom == wParam.ToInt32())
                {
                    
                    activeHwnd = WinAPIHelper.GetForegroundWindow();

                    this.Topmost = true;
                    this.Activate();
                    isShow = true;
 
                    WinAPIHelper.POINT point = new WinAPIHelper.POINT();
                    if (WinAPIHelper.GetCursorPos(out point))
                    {
                        double x = SystemParameters.WorkArea.Width;//得到屏幕工作区域宽度
                        double y = SystemParameters.WorkArea.Height;//得到屏幕工作区域高度
                        double mx = CursorHelp.ConvertPixelsToDIPixels(point.X);
                        double my = CursorHelp.ConvertPixelsToDIPixels(point.Y);

                        if (mx > x - this.ActualWidth)
                        {
                            this.Left = x - this.ActualWidth;
                        }
                        else
                        {
                            this.Left = mx;
                        }
                        if (my > y - this.ActualHeight)
                        {
                            this.Top = y - this.ActualHeight - 2;
                        }
                        else
                        {
                            this.Top = my - 2;
                        }


                    }

                    ShowWindowAndList();


                }
                handled = true;
            }
            return IntPtr.Zero;
        }




        /// <summary>
        /// 增加条目
        /// </summary>
        /// <param name="str"></param>
        private async void EnQueue(ClipModel clip)
        {

            string json = JsonConvert.SerializeObject(clip);

            json = HttpUtility.UrlEncode(json);
           
          
         await   webView1.InvokeScriptAsync("addData", json );


        }


        /// <summary>
        /// 显示窗口并列出所有条目
        /// </summary>
        private async void ShowWindowAndList()
        {

            await webView1.InvokeScriptAsync("showRecord");


        }

        /// <summary>
        /// 根据页面高度改变窗体高度
        /// </summary>
        /// <param name="height">页面高度</param>
        public void ChangeWindowHeight(double height)
        {
           
            this.Height = height + 21;

            WinAPIHelper.POINT point = new WinAPIHelper.POINT();
            if (WinAPIHelper.GetCursorPos(out point))
            {


                double y = SystemParameters.WorkArea.Height;//得到屏幕工作区域高度
                if (this.ActualHeight + this.Top > y)
                {
                    this.Top = y - this.ActualHeight - 2;
                }

            }

        }

       

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            webView1.InvokeScript("saveData");


            if (notifyIcon != null)
            {
                notifyIcon.Dispose();
            }

            webView1.Dispose();


            if (wpfHwnd != null)
            {
                WinAPIHelper.RemoveClipboardFormatListener(wpfHwnd);
                HotKeyManager.UnregisterHotKey(wpfHwnd, hotkeyAtom);
                HotKeyManager.GlobalDeleteAtom(hotkeyAtomStr);

            }

        }


        /// <summary>
        /// 根据索引粘贴条目到活动窗口
        /// </summary>
        /// <param name="id">索引</param>
        public void PasteValue(string clipStr)
        {
            DiyHide();
            preview.Hide();
            ClipModel clip = JsonConvert.DeserializeObject<ClipModel>(HttpUtility.UrlDecode(clipStr));
            

            //从显示列表中获取记录，并根据sourceId从对保存列表中的该条记录做相应处理

            if (clip.Type == FILE_TYPE)
            {
                string[] files = clip.ClipValue.Split(',');
                foreach (string str in files)
                {
                    if (!File.Exists(str))
                    {
                        MessageBox.Show("源文件缺失，粘贴失败！");
                        return;
                    }
                }
            }

            SinglePaste(clip);
          //  new Thread(new ParameterizedThreadStart(SinglePaste)).Start(clip);


        }

        /// <summary>
        /// 隐藏预览窗口
        /// </summary>
        public void HidePreview()
        {

            if (preview.IsVisible)
                preview.Hide();



        }

        /// <summary>
        /// 显示图片预览窗口
        /// </summary>
        /// <param name="id"></param>
        public void ShowPreviewForm(string path)
        {
            preview.Hide();

            preview.ImgPath = path;

            preview.Show();



        }
        /// <summary>
        /// 粘贴条目到活动窗口 
        /// </summary>
        /// <param name="result">需要粘贴的值</param>
        /// /// <param name="neadPause">是否需要延时，单条需要，批量不需要</param>
        private void SetValueToClip(ClipModel result, bool neadPause)
        {


            //设置剪切板前取消监听
            WinAPIHelper.RemoveClipboardFormatListener(wpfHwnd);
            ClipService.SetValueToClipboard(result);
            //this.Dispatcher.Invoke(new Action(delegate
            //{

            //}));

            //设置剪切板后恢复监听
            WinAPIHelper.AddClipboardFormatListener(wpfHwnd);
            if (neadPause)
            {
                Thread.Sleep(50);
            }
            System.Windows.Forms.SendKeys.SendWait("^v");
 
        }



        private void Window_Deactivated(object sender, EventArgs e)
        {

            WindowLostFocusHandle();
        }

        /// <summary>
        /// 当窗口失去焦点时的处理
        /// </summary>
        private void WindowLostFocusHandle()
        {


            if (preview != null)
            {
                preview.Hide();
            }
            
              DiyHide();
            


        }


        /// <summary>
        /// 根据给点起始、结束索引来设置批量粘贴条目
        /// </summary>
        /// <param name="nowIndex">结束索引</param>
        /// <param name="lastIndex">起始索引</param>
        public void PasteValueList(string clipListStr)
        {
            List<ClipModel> clipList = JsonConvert.DeserializeObject<List<ClipModel>>(HttpUtility.UrlDecode(clipListStr));
            DiyHide();
            preview.Hide();
            new Thread(new ParameterizedThreadStart(BatchPaste)).Start(clipList);
        }

        /// <summary>
        /// 单个粘贴
        /// </summary>
        /// <param name="clip"></param>
        private void SinglePaste(ClipModel clip)
        {
            SetValueToClip(clip, true);

        }
        /// <summary>
        /// 批量粘贴，由于循环太快、发送粘贴按键消息太慢，故延时200ms
        /// </summary>
        /// <param name="needPause"></param>
        private void BatchPaste(object clipList)
        {
            List<ClipModel> list = (List<ClipModel>)clipList;

            for (int i = 0; i < list.Count; i++)
            {

                ClipModel clip = list[i];
                if (i != list.Count - 1)
                {
                    clip.ClipValue = clip.ClipValue + "\n";
                }
                SetValueToClip(clip, false);
                Thread.Sleep(300);
            }


        }
 

        /// <summary>
        /// 添加剪切板监听， 更改窗体属性,不在alt+tab中显示
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_SourceInitialized(object sender, EventArgs e)
        {

            HwndSource source = PresentationSource.FromVisual(this) as HwndSource;
            source.AddHook(WndProc);
            wpfHwnd = (new WindowInteropHelper(this)).Handle;
            WinAPIHelper.AddClipboardFormatListener(wpfHwnd);

            int exStyle = (int)WinAPIHelper.GetWindowLong(wpfHwnd, -20);
            exStyle |= (int)0x00000080;
            WinAPIHelper.SetWindowLong(wpfHwnd, -20, exStyle);
 

        }

        //显示窗体,透明度为事先设置的值.
        private void DiyShow()
        {
            this.Topmost = true;
            this.Activate();
            isShow = true;

        }

        /// <summary>
        ///  
        /// </summary>
        private void DiyHide()
        {
            this.Topmost = false;
            if (isShow) {
               
                
                if (activeHwnd != IntPtr.Zero)
                {
                    WinAPIHelper.SetForegroundWindow(activeHwnd);
                }
                
              
                 isShow = false;
            }
            this.Left = HideLeftValue;

        }
        
        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                DiyHide();
            }
           
        }
    }



}

