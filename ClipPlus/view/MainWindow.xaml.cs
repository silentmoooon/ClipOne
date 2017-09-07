using CefSharp;
using CefSharp.Wpf;
using ClipPlus.service;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace ClipPlus
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {

        /// <summary>
        /// 持久化路径
        /// </summary>
        private static string storePath = "store\\clip.json";
        /// <summary>
        /// 配置文件持久化路径
        /// </summary>
        private static string settingsPath = "store\\settings.json";
        /// <summary>
        /// 持久化目录
        /// </summary>
        private static string storeDir = "store";
        /// <summary>
        /// 缓存目录
        /// </summary>
        private static string cacheDir = "cache";
        /// <summary>
        /// HTML目录
        /// </summary>
        private static string htmlDir = "html";

        /// <summary>
        /// 默认显示页面
        /// </summary>
        private static string defaultHtml = "html\\index.html";

        /// <summary>
        /// 托盘路径路径
        /// </summary>
        private static string iconPath = "icon\\clipPlus.ico";

        /// <summary>
        /// 浏览器
        /// </summary>
        ChromiumWebBrowser webView;

        /// <summary>
        /// 复制条目保存记录
        /// </summary>
        List<string> resultList = new List<string>(maxRecords + 2);

        /// <summary>
        /// 临时保存当前活动窗口
        /// </summary>
        IntPtr activeWnd;

        /// <summary>
        /// 供浏览器JS回调的接口
        /// </summary>
        CallbackObjectForJs cbOjb;

        /// <summary>
        /// 上次退出时保存的图片记录，用来清理缓存中的图片
        /// </summary>
        string lastSaveImg = string.Empty;

        /// <summary>
        /// 剪切板事件
        /// </summary>
        private static int WM_CLIPBOARDUPDATE = 0x031D;

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
        /// 默认保存记录数
        /// </summary>
        private static int currentRecords = 100;

        /// <summary>
        /// 允许保存的最大记录数
        /// </summary>
        private static int maxRecords = 500;

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
        /// 定时器，用于定时持久化条目
        /// </summary>
        System.Windows.Forms.Timer saveDataTimer = new System.Windows.Forms.Timer();

        /// <summary>
        /// 用于连续粘贴 ，连续粘贴条目列表
        /// </summary>
        List<string> batchPasteList = new List<string>();
        /// <summary>
        /// 用于连续粘贴，Shift+鼠标选择的械中是否按下Shift键
        /// </summary>
        volatile bool isPressedShift = false;
        /// <summary>
        /// 用于连续粘贴，保存上一次选择的index
        /// </summary>
        private int lastSelectedIndex = -1;


        public static bool isShowPreview = false;

        private bool needCloseHandle = false;

        /// <summary>
        /// 预览窗口
        /// </summary>
        private PreviewForm preview;

        public MainWindow()
        {
            InitializeComponent();
            System.IO.Directory.SetCurrentDirectory(System.Windows.Forms.Application.StartupPath);

            CommonService.ExitWhenExists();


        }




        private void Window_Loaded(object sender, RoutedEventArgs e)
        {




            if (!Directory.Exists(cacheDir))
            {
                Directory.CreateDirectory(cacheDir);
            }

            if (!Directory.Exists(storeDir))
            {
                Directory.CreateDirectory(storeDir);
            }
            else
            {
                //如果配置文件存在则读取配置文件，否则按默认值设置
                if (File.Exists(settingsPath))
                {
                    //从持久化文件中读取设置项
                    string json = File.ReadAllText(settingsPath);
                    settingsMap = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                    if (settingsMap.ContainsKey("startup"))
                    {
                        autoStartup = bool.Parse(settingsMap["startup"]);
                        if (autoStartup)
                        {
                            CommonService.SetStartup(autoStartup);
                        }
                    }
                    if (settingsMap.ContainsKey("record"))
                    {
                        currentRecords = int.Parse(settingsMap["record"]);
                    }
                    if (settingsMap.ContainsKey("key"))
                    {
                        hotkeyKey = int.Parse(settingsMap["key"]);
                        hotkeyModifier = int.Parse(settingsMap["modifier"]);

                    }
                }

                InitialTray();


                if (File.Exists(storePath))
                {
                    //从持久化文件中读取复制条目，并将图片类型的条目记录至lastSaveImag，供清除过期图片用
                    string json = File.ReadAllText(storePath);
                    List<string> list = JsonConvert.DeserializeObject<List<string>>(json);
                    foreach (string str in list)
                    {
                        resultList.Add(str);
                        if (str.StartsWith(imageType))
                        {
                            lastSaveImg += str;
                        }
                    }
                    Thread thread = new Thread(ClearExpireImage);
                    thread.Start();
                }
            }
            needCloseHandle = true;

            List<string> fileList = Directory.EnumerateFiles(htmlDir).ToList();

            //如果html目录没有任何页面， 程序将退出
            if (fileList.Count == 0)
            {
                MessageBox.Show("关键页面缺失，应用将退出！");
                Application.Current.Shutdown();
                return;
            }
            var setting = new CefSharp.CefSettings();

            // 设置语言
            setting.Locale = "zh-CN";
            setting.CefCommandLineArgs.Add("Cache-control", "no-cache");
            setting.CefCommandLineArgs.Add("Pragma", "no-cache");
            setting.CefCommandLineArgs.Add("expries", "-1");
            CefSharp.Cef.Initialize(setting);

            webView = new ChromiumWebBrowser();
            BrowserSettings browserSetting = new BrowserSettings();
            browserSetting.ApplicationCache = CefState.Disabled;
            browserSetting.DefaultEncoding = "utf-8";

            webView.BrowserSettings = browserSetting;
            //页面允许用户自定义，如果只有一个页面， 则设置该页面为启动页面，如果有多个页面， 则设置为非默认页面
            if (fileList.Count == 1)
            {
                webView.Address = "file:///" + fileList[0];
            }
            else
            {

                foreach (string str in fileList)
                {
                    if (!(str.ToLower().Equals(defaultHtml)))
                    {
                        webView.Address = "file:///" + str.ToLower();

                    }
                }
            }
            //
            cbOjb = new CallbackObjectForJs(this);
            webView.RegisterAsyncJsObject("callbackObj", cbOjb);

            mainGrid.Children.Add(webView);

            wpfHwnd = new WindowInteropHelper(this).Handle;
            WinAPIHelper.AddClipboardFormatListener(wpfHwnd);
            var hWndSource = HwndSource.FromHwnd(wpfHwnd);
            //添加处理程序
            if (hWndSource != null) hWndSource.AddHook(WndProc);


            hotkeyAtom = HotKeyManager.GlobalAddAtom(hotkeyAtomStr);

            saveDataTimer.Tick += BatchPasteTimer_Tick; ;
            saveDataTimer.Interval = 60000;
            saveDataTimer.Start();
            bool status = HotKeyManager.RegisterHotKey(wpfHwnd, hotkeyAtom, hotkeyModifier, hotkeyKey);

            if (!status)
            {
                Hotkey_Click(null, null);
            }
            preview = new PreviewForm(this);
            preview.Focusable = false;
            preview.IsHitTestVisible = false;
            preview.IsTabStop = false;
            preview.ShowInTaskbar = false;
            preview.ShowActivated = false;


        }

        private void BatchPasteTimer_Tick(object sender, EventArgs e)
        {
            CommonService.SaveData(resultList, storePath);
        }


        private void ClearExpireImage()
        {
            CommonService.ClearExpireImage(cacheDir, lastSaveImg);
        }




        /// <summary>
        /// 初始化托盘图标及菜单
        /// </summary>
        private void InitialTray()
        {

            //设置托盘图标
            notifyIcon = new System.Windows.Forms.NotifyIcon();
            notifyIcon.Text = "clipPlus";
            notifyIcon.Icon = new System.Drawing.Icon(iconPath);
            notifyIcon.Visible = true;



            //设置菜单项
            System.Windows.Forms.MenuItem exit = new System.Windows.Forms.MenuItem("退出");
            System.Windows.Forms.MenuItem startup = new System.Windows.Forms.MenuItem("开机自启");
            System.Windows.Forms.MenuItem hotkey = new System.Windows.Forms.MenuItem("热键");
            System.Windows.Forms.MenuItem record = new System.Windows.Forms.MenuItem("记录数");
            System.Windows.Forms.MenuItem clear = new System.Windows.Forms.MenuItem("清空");
            System.Windows.Forms.MenuItem reload = new System.Windows.Forms.MenuItem("刷新");
            clear.Click += Clear_Click;
            reload.Click += new EventHandler(Reload);
            exit.Click += new EventHandler(exit_Click);
            hotkey.Click += Hotkey_Click;
            startup.Click += Startup_Click;
            startup.Checked = autoStartup;

            for (int i = 0; i < 5; i++)
            {

                string recordsNum = ((i + 1) * 100).ToString();
                System.Windows.Forms.MenuItem subRecord = new System.Windows.Forms.MenuItem(recordsNum);
                if (int.Parse(recordsNum) == currentRecords)
                {
                    subRecord.Checked = true;
                }
                subRecord.Click += RecordSet_Click;
                record.MenuItems.Add(subRecord);

            }



            //关联菜单项至托盘
            System.Windows.Forms.MenuItem[] childen = new System.Windows.Forms.MenuItem[] { reload, clear, record, hotkey, startup, exit };
            notifyIcon.ContextMenu = new System.Windows.Forms.ContextMenu(childen);


        }

        private void Startup_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.MenuItem item = (System.Windows.Forms.MenuItem)sender;
            item.Checked = !item.Checked;

            CommonService.SetStartup(item.Checked);
            settingsMap["startup"] = item.Checked.ToString();
            saveSettings();
        }

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
                saveSettings();
            }
        }

        private void saveSettings()
        {
            string json = JsonConvert.SerializeObject(settingsMap);
            File.WriteAllText(settingsPath, json);
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
            saveSettings();



        }

        /// <summary>
        /// 清空所有条目
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Clear_Click(object sender, EventArgs e)
        {
            resultList.Clear();
            CommonService.SaveData(resultList, storePath);
        }

        /// <summary>
        /// 刷新页面
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Reload(object sender, EventArgs e)
        {
            webView.GetBrowser().Reload(true);
        }

        private void exit_Click(object sender, EventArgs e)
        {



            Application.Current.Shutdown();

        }


        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            //当为剪切板消息时
            if (msg == WM_CLIPBOARDUPDATE)
            {

                IDataObject iData = Clipboard.GetDataObject();

                string queueStr = string.Empty;
                //处理剪切板文字
                if (iData.GetDataPresent(DataFormats.Text))
                {
                    for (int i = 0; i < 3; i++)
                    {
                        try
                        {

                            queueStr = Clipboard.GetText();
                            break;

                        }
                        catch
                        {


                        }
                    }


                }
                //处理剪切板图形
                else if (iData.GetDataPresent(DataFormats.Bitmap) || iData.GetDataPresent(DataFormats.Dib))
                {

                    for (int i = 0; i < 3; i++)
                    {
                        try
                        {


                            BitmapSource bs = Clipboard.GetImage();

                            string path = cacheDir + "/" + Guid.NewGuid().ToString() + ".bmp";
                            BitmapEncoder encoder = new BmpBitmapEncoder();
                            encoder.Frames.Add(BitmapFrame.Create(bs));

                            FileStream fs = new FileStream(path, FileMode.Create);
                            encoder.Save(fs);
                            fs.Close();

                            queueStr = imageType + path;
                            break;

                        }
                        catch
                        {


                        }
                    }

                }
                //处理剪切板文件
                else if (iData.GetDataPresent(DataFormats.FileDrop))
                {
                    for (int i = 0; i < 3; i++)
                    {
                        try
                        {
                            StringCollection coll = Clipboard.GetFileDropList();
                            queueStr = fileType + coll.Count + " file";
                            if (coll.Count > 1)
                            {
                                queueStr += "s";
                            }

                            string[] array = new string[coll.Count];
                            coll.CopyTo(array, 0);

                            foreach (string str in array)
                            {
                                queueStr += "\n" + str;
                            }
                            break;
                        }
                        catch
                        {


                        }
                    }


                }
                //处理HTML类型
                else if (iData.GetDataPresent(DataFormats.Html))
                {
                    for (int i = 0; i < 3; i++)
                    {
                        try
                        {
                            queueStr = iData.GetData(DataFormats.Html).ToString();
                            break;
                        }
                        catch
                        {

                        }
                    }
                    queueStr = htmlType + queueStr;
                }

                else
                {

                    return IntPtr.Zero;
                }
                if (queueStr == string.Empty)
                {
                    return IntPtr.Zero;
                }
                if (resultList.Count > 0 && queueStr == resultList[0])
                {
                    return IntPtr.Zero;
                }

                EnQueue(queueStr);


            }
            //触发显示界面快捷键
            else if (msg == HotKeyManager.WM_HOTKEY)
            {
                if (hotkeyAtom == wParam.ToInt32())
                {
                    GetClipDataAndShowWindows();


                }

            }
            return IntPtr.Zero;
        }

        /// <summary>
        /// 增加条目
        /// </summary>
        /// <param name="str"></param>
        private async void EnQueue(String str)
        {

            resultList.Insert(0, str);

            await ClearImage();



        }

        /// <summary>
        /// 清理多余条目，如果为图片类型则清理关联的图片
        /// </summary>
        /// <returns></returns>
        private Task ClearImage()
        {
            return Task.Run(() =>
            {
                if (resultList.Count > currentRecords)
                {
                    string clearStr = resultList[currentRecords];
                    resultList.RemoveAt(currentRecords);


                    if (clearStr.StartsWith(imageType))
                    {
                        clearStr = clearStr.Replace(imageType, "");
                        if (File.Exists(clearStr))
                        {
                            File.Delete(clearStr);
                        }
                    }
                }


            });

        }

        /// <summary>
        /// 显示窗口并列出所有条目
        /// </summary>
        private void GetClipDataAndShowWindows()
        {
            activeWnd = WinAPIHelper.GetForegroundWindow();


            WinAPIHelper.POINT p = new WinAPIHelper.POINT();

            List<string> frontList = new List<string>(maxRecords);
            int imgCount = 0;





            for (int i = 0; i < resultList.Count; i++)
            {

                string str = resultList[i];

                if (!str.StartsWith(imageType) && !str.StartsWith(htmlType))
                {
                    if (str.StartsWith(fileType))
                    {
                        string[] tmp = str.Split(',');
                        str = tmp[0];
                        for (int j = 1; j < tmp.Length; j++)
                        {

                            str = str + ", " + System.IO.Path.GetFileName(tmp[j]);

                        }
                    }
                    if (str.Length - str.Replace("\n", "").Length > 5)
                    {
                        string[] tmp = str.Split('\n');
                        string tempStr = string.Empty;
                        for (int ii = 0; ii < 5; ii++)
                        {
                            tempStr += tmp[ii] + "\n";

                        }
                        str = tempStr + "...";
                    }
                    byte[] bb = Encoding.Default.GetBytes(str);


                }
                else
                {
                    imgCount++;
                }
                if (str.StartsWith(htmlType))
                {

                    string[] tmp = str.Split("\r\n".ToCharArray());

                    //html内容会固定出现在第17行。
                    str = htmlType + tmp[16];

                }
                frontList.Add(str);
            }
            string json = JsonConvert.SerializeObject(frontList);

            json = HttpUtility.UrlEncode(json);

            webView.GetBrowser().MainFrame.ExecuteJavaScriptAsync("fun('" + json + "')");

            if (this.IsVisible)
                this.Hide();
            
           
          
            
            if (WinAPIHelper.GetCursorPos(out p))
            {
                if (frontList.Count == 0)
                {
                    this.Height = 100;
                }
                else
                {
                    this.Height = 35 * frontList.Count + 10 + 165 * imgCount;


                }
                if (this.Height > 660)
                {
                    this.Height = 660;
                }

                double x = SystemParameters.WorkArea.Width;//得到屏幕工作区域宽度
                double y = SystemParameters.WorkArea.Height;//得到屏幕工作区域高度
                double mx = CursorHelp.ConvertPixelsToDIPixels(p.X);
                double my = CursorHelp.ConvertPixelsToDIPixels(p.Y);

                if (mx > x - this.Width)
                {
                    this.Left = x - this.Width;
                }
                else
                {
                    this.Left = mx;
                }
                if (my > y - this.Height)
                {
                    this.Top = y - this.Height;
                }
                else
                {
                    this.Top = my;
                }


                this.Show();
                this.Topmost = true;

                this.Activate();
                this.Focus();

            }


        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!needCloseHandle) { return; }
            if (notifyIcon != null)
            {
                notifyIcon.Dispose();
            }
            try
            {
                if (webView != null)
                {
                    webView.GetBrowser().CloseBrowser(true);
                    webView.Dispose();
                    Cef.Shutdown();
                }
            }
            catch { }

            if (wpfHwnd != null)
            {
                WinAPIHelper.RemoveClipboardFormatListener(wpfHwnd);
                HotKeyManager.UnregisterHotKey(wpfHwnd, hotkeyAtom);
                HotKeyManager.GlobalDeleteAtom(hotkeyAtomStr);
                CommonService.SaveData(resultList, storePath);
            }

        }

        /// <summary>
        /// 根据索引粘贴条目到活动窗口
        /// </summary>
        /// <param name="id">索引</param>
        public void PreviewByIndex(int id)
        {


            this.Dispatcher.Invoke(
           new Action(
         delegate
         {
             ShowPreviewForm(id);

         }));



        }

        /// <summary>
        /// 根据索引粘贴条目到活动窗口
        /// </summary>
        /// <param name="id">索引</param>
        public void PasteValueByIndex(int id)
        {

            //当按下shift键时，做批量处理判断
            if (isPressedShift)
            {
                if (lastSelectedIndex == -1)
                {
                    lastSelectedIndex = id;
                }
                else
                {
                    setBatchPatse(id, lastSelectedIndex);
                    this.Dispatcher.Invoke(
                          new Action(
                        delegate
                        {

                            this.Hide();
                        }));
                    new Thread(new ParameterizedThreadStart(BatchPaste)).Start(false);
                    lastSelectedIndex = -1;
                    isPressedShift = false;
                }
            }
            else  //单条处理
            {
                this.Dispatcher.Invoke(
               new Action(
             delegate
             {
                 this.Hide();
                 preview.Hide();


             }));

                string result = resultList[id];

                resultList.RemoveAt(id);
                resultList.Insert(0, result);
                batchPasteList.Clear();
                batchPasteList.Add(result);
                new Thread(new ParameterizedThreadStart(BatchPaste)).Start(true);

            }


        }

        /// <summary>
        /// 隐藏预览窗口
        /// </summary>
        public void HidePreview()
        {

            this.Dispatcher.Invoke(
              new Action(
            delegate
            {

                preview.Hide();
            }));



        }

        /// <summary>
        /// 显示图片预览窗口
        /// </summary>
        /// <param name="id"></param>
        private void ShowPreviewForm(int id)
        {
            preview.Hide();
            string result = resultList[id];
            if (result.StartsWith(imageType))
            {
                preview.ImgPath = result;

                preview.Show();

            }

        }
        /// <summary>
        /// 粘贴条目到活动窗口，单条粘贴时必须加延时，否则会出现粘贴不上的现象，可能是因为活动窗口还没完成激活时就已经发出ctrl+v按键消息导致的，事先通过API主动激活也不行，不知道为什么。
        /// </summary>
        /// <param name="result">需要粘贴的值</param>
        /// /// <param name="neadPause">是否需要延时，单条需要，批量不需要</param>
        private void SetValueToClip(string result, bool neadPause)
        {


            //设置剪切板前取消监听
            WinAPIHelper.RemoveClipboardFormatListener(wpfHwnd);

            CommonService.SetValueToClip(result);

            //设置剪切板后恢复监听
            WinAPIHelper.AddClipboardFormatListener(wpfHwnd);
            if (neadPause)
            {
                Thread.Sleep(100);
            }

            System.Windows.Forms.SendKeys.SendWait("^v");

            // CommonService.SendPasteKey(activeWnd);

            
        }



        private void Window_LostFocus(object sender, RoutedEventArgs e)
        {

            WindowLostFocusHandle();
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
            
            webView.GetBrowser().MainFrame.ExecuteJavaScriptAsync("scrollTop()");
            this.Hide();
            lastSelectedIndex = -1;
            isPressedShift = false;
            if (preview != null)
            {
                preview.Hide();
            }


        }

        private void DoHide()
        {
            webView.GetBrowser().MainFrame.ExecuteJavaScriptAsync("scrollTop()");
            this.Left = -1000;
            Thread.Sleep(50);
            this.Hide();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (resultList.Count > 0)
            {
                string key;
                int a;
                int b;
                if (e.KeyboardDevice.Modifiers == ModifierKeys.Shift)  //处理基于SHIFT+数字的批量粘贴
                {

                    key = e.Key.ToString();
                    a = key.CompareTo("D1");
                    b = key.CompareTo("D9");
                    if (a >= 0 && b <= 0)
                    {
                        if (lastSelectedIndex == -1)
                        {
                            lastSelectedIndex = int.Parse(key.Remove(0, 1)) - 1;
                        }
                        else
                        {
                            int currentKey = int.Parse(key.Remove(0, 1)) - 1;

                            setBatchPatse(currentKey, lastSelectedIndex);
                            this.Hide();
                            new Thread(new ParameterizedThreadStart(BatchPaste)).Start(false);
                            lastSelectedIndex = -1;

                        }
                        return;
                    }

                    isPressedShift = true;
                }

                else //处理单选粘贴
                {
                    int index = -1;
                    if (e.Key == Key.Space)
                    {
                        index = 1;
                    }
                    else
                    {
                        key = e.Key.ToString();
                        a = key.CompareTo("D1");
                        b = key.CompareTo("D9");
                        if (a >= 0 && b <= 0)
                        {
                            index = int.Parse(key.Remove(0, 1)) - 1;

                        }
                    }
                    if (index >= 0)
                    {

                        PasteValueByIndex(index);
                    }

                }
            }

        }


        /// <summary>
        /// 根据给点起始、结束索引来设置批量粘贴条目
        /// </summary>
        /// <param name="nowIndex">结束索引</param>
        /// <param name="lastIndex">起始索引</param>
        private void setBatchPatse(int nowIndex, int lastIndex)
        {
            batchPasteList.Clear();
            if (nowIndex > lastIndex)
            {
                for (int i = lastIndex; i <= nowIndex; i++)
                {
                    string result = resultList[i];
                    resultList.RemoveAt(i);
                    resultList.Insert(0, result);
                    batchPasteList.Add(result);


                }

            }
            else
            {
                for (int i = lastIndex; i >= nowIndex; i--)
                {
                    string result = resultList[lastIndex];
                    resultList.RemoveAt(lastIndex);
                    resultList.Insert(0, result);
                    batchPasteList.Add(result);
                }

            }
        }

        /// <summary>
        /// 批量粘贴，由于循环太快、发送粘贴按键消息太慢，故延时200ms
        /// </summary>
        /// <param name="needPause"></param>
        private void BatchPaste(object needPause)
        {
            for (int i = 0; i < batchPasteList.Count; i++)
            {



                this.Dispatcher.Invoke(
                       new Action(
                     delegate
                     {

                         SetValueToClip(batchPasteList[i], (bool)needPause);
                     }));
                Thread.Sleep(200);
            }
        }



        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyboardDevice.Modifiers == ModifierKeys.Shift)
            {
                //当shift键keyUp时，还原状态
                isPressedShift = false;
            }
        }



    }


}
