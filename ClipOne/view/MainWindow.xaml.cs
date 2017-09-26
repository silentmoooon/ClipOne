using CefSharp;
using CefSharp.Wpf;
using ClipOne.model;
using ClipOne.service;
using ClipOne.util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        /// css目录
        /// </summary>
        private static string cssDir = "html\\css";

        /// <summary>
        /// 默认显示页面
        /// </summary>
        private static string defaultHtml = "html\\index.html";

        /// <summary>
        /// 活动窗口句柄,在显示本窗口前,缓存当前活动窗口
        /// </summary>
        private IntPtr activeHwnd = IntPtr.Zero;
        /// <summary>
        /// 浏览器
        /// </summary>
        ChromiumWebBrowser webView;

        /// <summary>
        /// 复制条目保存记录
        /// </summary>
        List<ClipModel> clipList = new List<ClipModel>(maxRecords + 2);

        /// <summary>
        /// 用于显示的记录列表
        /// </summary>
        List<ClipModel> displayList = new List<ClipModel>(maxRecords);


        /// <summary>
        /// 供浏览器JS回调的接口
        /// </summary>
        CallbackObjectForJs cbOjb;

        /// <summary>
        /// 是否不允许隐藏,在打开开发者工具和设置透明度使用
        /// </summary>
        public static bool isNotAllowHide = false;

        /// <summary>
        /// 剪切板事件
        /// </summary>
        private static int WM_CLIPBOARDUPDATE = 0x031D;

        /// <summary>
        /// JSON设置
        /// </summary>
        JsonSerializerSettings displayJsonSettings = new JsonSerializerSettings();

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
        /// 定时器，用于定时持久化条目
        /// </summary>
        System.Windows.Forms.Timer saveDataTimer = new System.Windows.Forms.Timer();


        /// <summary>
        /// 用于连续粘贴 ，连续粘贴条目列表
        /// </summary>
        List<ClipModel> batchPasteList = new List<ClipModel>();

 
        /// <summary>
        /// 预览窗口
        /// </summary>
        private PreviewForm preview;



        public MainWindow()
        {
            InitializeComponent();

            System.IO.Directory.SetCurrentDirectory(System.Windows.Forms.Application.StartupPath);
            //序列化到前端时只序列化需要的字段
            displayJsonSettings.ContractResolver = new LimitPropsContractResolver(new string[] { "Type", "DisplayValue" });



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

            //如果配置文件存在则读取配置文件，否则按默认值设置
            if (File.Exists(settingsPath))
            {
                InitConfig();
            }

            //如果存在持久化记录则加载
            if (File.Exists(storePath))
            {
                InitStore();
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
        /// 加载持久化数据
        /// </summary>
        private void InitStore()
        {
            string lastSaveImg = string.Empty;

            //从持久化文件中读取复制条目，并将图片类型的条目记录至lastSaveImag，供清除过期图片用
            string json = File.ReadAllText(storePath);

            List<ClipModel> list = JsonConvert.DeserializeObject<List<ClipModel>>(json);
            foreach (ClipModel clip in list)
            {
                clipList.Add(clip);
                if (clip.Type == IMAGE_TYPE)
                {
                    lastSaveImg += clip.ClipValue;
                }
            }
            saveDataTimer.Tick += BatchPasteTimer_Tick;
            saveDataTimer.Interval = 60000;
            saveDataTimer.Start();
            new Thread(new ParameterizedThreadStart(ClearExpireImage)).Start(lastSaveImg);

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
        }

        /// <summary>
        /// 初始化浏览器
        /// </summary>
        private void InitWebView()
        {
            ///初始化浏览器
            var setting = new CefSharp.CefSettings();
            setting.Locale = "zh-CN";
            setting.LogSeverity = LogSeverity.Disable;
            setting.WindowlessRenderingEnabled = true;
            setting.CefCommandLineArgs.Add("Cache-control", "no-cache");
            setting.CefCommandLineArgs.Add("Pragma", "no-cache");
            setting.CefCommandLineArgs.Add("expries", "-1");
            setting.CefCommandLineArgs.Add("disable-gpu", "1");
            CefSharp.Cef.Initialize(setting);
            webView = new ChromiumWebBrowser();
            webView.MenuHandler = new MenuHandler();
            BrowserSettings browserSetting = new BrowserSettings();
            browserSetting.ApplicationCache = CefState.Disabled;
            browserSetting.DefaultEncoding = "utf-8";
            webView.BrowserSettings = browserSetting;
            webView.Address = "file:///" + defaultHtml;

             cbOjb = new CallbackObjectForJs(this);
            webView.RegisterAsyncJsObject("callbackObj", cbOjb);

            mainGrid.Children.Add(webView);

        }

        /// <summary>
        /// 定时持久化数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BatchPasteTimer_Tick(object sender, EventArgs e)
        {
            SaveData(clipList, storePath);
        }

        /// <summary>
        /// 删除指定索引的数据
        /// </summary>
        /// <param name="index"></param>
        public void DeleteOverClipByIndex(int index)
        {

            clipList.RemoveAt(index);
           

        }
        /// <summary>
        /// 删除指定索引的数据
        /// </summary>
        /// <param name="index"></param>
        public void DeleteByIndex(int index)
        {


            //同时删除显示列表和保存列表中的条目
            ClipModel clip = displayList[index];
            displayList.RemoveAt(index);
            clipList.RemoveAt(clip.SourceId);
            //重新展示记录并保存当前结果
            ShowList(displayList, 0);
            SaveData(clipList, storePath);
           



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
            System.Windows.Forms.MenuItem devTools = new System.Windows.Forms.MenuItem("调试工具");

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
                isNotAllowHide = true;
                ShowWindowAndList();
                
                OpacitySet os = new OpacitySet(this, (1 - opacityValue) / 0.02);
               
                os.Topmost = true;
                os.ShowDialog();
                isNotAllowHide = false;
                DiyHide();
            };
            devTools.Click += DevTools_Click;
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
                List<string> fileList = Directory.EnumerateDirectories(cssDir).ToList();

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
            System.Windows.Forms.MenuItem[] childen = new System.Windows.Forms.MenuItem[] { clear, format, separator3, reload, skin, opaSet, separator2, record, hotkey, separator1, devTools, startup, separator0, exit };
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

        /// <summary>
        /// 进入开发者模式
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DevTools_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.MenuItem item = (System.Windows.Forms.MenuItem)sender;
            if (!isNotAllowHide)
            {
                item.Checked = true;
                isNotAllowHide = true;

                webView?.GetBrowser()?.ShowDevTools();
                ShowWindowAndList();
            }
            else
            {
                item.Checked = false;
                webView?.GetBrowser()?.CloseDevTools();
                isNotAllowHide = false;
                DiyHide();
            }
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
            string[] files = Directory.EnumerateFiles(cssPath).ToArray();
            foreach (string file in files)
            {
                string str = file.Replace("\\", "/").Replace("html/", "");
                fileLines.Add(" <link rel='stylesheet' type='text/css' href='" + str + "'/>");
            }
            File.WriteAllLines(defaultHtml, fileLines, Encoding.UTF8);
            webView.GetBrowser().Reload();


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
            opacityValue = 1 - value * 0.02;
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



        }

        /// <summary>
        /// 清空所有条目
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Clear_Click(object sender, EventArgs e)
        {
            clipList.Clear();
            SaveData(clipList, storePath);
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


                //处理剪切板QQ自定义格式
                if ((supportFormat & ClipType.qq) != 0 && Clipboard.ContainsData(QQ_RICH_TYPE))
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
                if (clipList.Count > 0 && clip.ClipValue == clipList[0].ClipValue)
                {
                    return IntPtr.Zero;
                }
                if (clip.Type == TEXT_TYPE)
                {
                    for (int i = 0; i < clipList.Count; i++)
                    {
                        if (clipList[i].Type == TEXT_TYPE && clipList[i].ClipValue == clip.ClipValue)
                        {

                            clipList.RemoveAt(i);
                            break;
                        }
                    }
                }

                EnQueue(clip);

                handled = true;
            }
            //触发显示界面快捷键
            else if (msg == HotKeyManager.WM_HOTKEY)
            {
                
                if (hotkeyAtom == wParam.ToInt32())
                {
                   
                    DiyHide();
                    activeHwnd = WinAPIHelper.GetForegroundWindow();
                    
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

            clipList.Insert(0, clip);

            await ClearRecord();



        }

        /// <summary>
        /// 清理多余条目，如果为图片类型则清理关联的图片
        /// </summary>
        /// <returns></returns>
        private Task ClearRecord()
        {
            return Task.Run(() =>
            {
                if (clipList.Count > currentRecords)
                {
                     
                    ClipModel model = clipList[currentRecords];
                    DeleteOverClipByIndex(currentRecords);
                    if (model.Type == IMAGE_TYPE)
                    {
                        File.Delete(model.ClipValue);
                    }
                     
                }


            });

        }

        /// <summary>
        /// 显示窗口并列出所有条目
        /// </summary>
        private void ShowWindowAndList()
        {
            displayList.Clear();

            for (int i = 0; i < clipList.Count; i++)
            {
                clipList[i].SourceId = i;
                displayList.Add(clipList[i]);

            }
           

            ShowList(displayList, 1);

           
            webView.Focus();

            DiyShow();
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
                    this.Top = y - this.ActualHeight;
                }
                else
                {
                    this.Top = my - 2;
                }


            }



        }

        /// <summary>
        /// 根据页面高度改变窗体高度
        /// </summary>
        /// <param name="height">页面高度</param>
        public void ChangeWindowHeight(double height)
        {
            this.Height = height + 25;

            WinAPIHelper.POINT point = new WinAPIHelper.POINT();
            if (WinAPIHelper.GetCursorPos(out point))
            {

                 
                double y = SystemParameters.WorkArea.Height;//得到屏幕工作区域高度
                if (this.ActualHeight + this.Top > y)
                {
                    this.Top = y - this.ActualHeight;
                }

            }

        }

        /// <summary>
        /// 展示list数组并将指定索引项高亮
        /// </summary>
        /// <param name="list"></param>
        /// <param name="index"></param>
        private void ShowList(List<ClipModel> list, int index)
        {

            string json = JsonConvert.SerializeObject(list, displayJsonSettings);

            json = HttpUtility.UrlEncode(json);


            webView.GetBrowser().MainFrame.ExecuteJavaScriptAsync("showList('" + json + "'," + index + ")");
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveData(clipList, storePath);

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
            if (id >= displayList.Count)
            {
                return;
            }

            this.Dispatcher.Invoke(
           new Action(
         delegate
         {
             DiyHide();

             preview.Hide();


            
         }));

            //从显示列表中获取记录，并根据sourceId从对保存列表中的该条记录做相应处理
            ClipModel result = displayList[id];

            clipList.RemoveAt(result.SourceId);

            if (result.Type == FILE_TYPE)
            {
                string[] files = result.ClipValue.Split(',');
                foreach (string str in files)
                {
                    if (!File.Exists(str))
                    {
                        this.Dispatcher.Invoke(
                     new Action(
                   delegate
                   {
                       MessageBox.Show("源文件缺失，粘贴失败！");
                   }));
                        return;
                    }
                }
            }

            clipList.Insert(0, result);

            new Thread(new ParameterizedThreadStart(SinglePaste)).Start(result);


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
                if (preview.IsVisible)
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
            ClipModel result = clipList[id];
            if (result.Type == IMAGE_TYPE)
            {
                preview.ImgPath = result.ClipValue;

                preview.Show();

            }

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
            if (!isNotAllowHide)
            {
                DiyHide();
            }


        }


        /// <summary>
        /// 根据给点起始、结束索引来设置批量粘贴条目
        /// </summary>
        /// <param name="nowIndex">结束索引</param>
        /// <param name="lastIndex">起始索引</param>
        public void PasteValueByRange(int lastIndex, int nowIndex)
        {
            batchPasteList.Clear();
            if (nowIndex > lastIndex)
            {
                for (int i = lastIndex; i <= nowIndex; i++)
                {
                    var result = displayList[i];

                    clipList.RemoveAt(result.SourceId);
                    if (result.Type == FILE_TYPE)
                    {
                        string[] files = result.ClipValue.Split(',');
                        foreach (string str in files)
                        {
                            if (!File.Exists(str))
                            {
                                this.Dispatcher.Invoke(
                             new Action(
                           delegate
                           {
                               MessageBox.Show("粘贴列表中部分源文件缺失，粘贴失败！");
                           }));
                                return;
                            }
                        }
                    }
                    clipList.Insert(0, result);
                    batchPasteList.Add(result);


                }

            }
            else if(nowIndex<lastIndex)
            {
                for (int i = lastIndex; i >= nowIndex; i--)
                {
                    var result = clipList[lastIndex];

                    clipList.RemoveAt(result.SourceId);
                    if (result.Type == FILE_TYPE)
                    {
                        string[] files = result.ClipValue.Split(',');
                        foreach (string str in files)
                        {
                            if (!File.Exists(str))
                            {
                                this.Dispatcher.Invoke(
                             new Action(
                           delegate
                           {
                               MessageBox.Show("粘贴列表中部分源文件缺失，粘贴失败！");
                           }));
                                return;
                            }
                        }
                    }
                    clipList.Insert(0, result);
                    batchPasteList.Add(result);
                }

            }
            else
            {
                PasteValueByIndex(nowIndex);
                return;
            }
            this.Dispatcher.Invoke(
                         new Action(
                       delegate
                       {

                           DiyHide();
                           preview.Hide();
                       }));
            new Thread(BatchPaste).Start();
        }

        /// <summary>
        /// 单个粘贴
        /// </summary>
        /// <param name="clip"></param>
        private void SinglePaste(object clip)
        {
            this.Dispatcher.Invoke(
                      new Action(
                    delegate
                    {

                        SetValueToClip((ClipModel)clip, true);
                    }));
          


        }
        /// <summary>
        /// 批量粘贴，由于循环太快、发送粘贴按键消息太慢，故延时200ms
        /// </summary>
        /// <param name="needPause"></param>
        private void BatchPaste()
        {

            for (int i = 0; i < batchPasteList.Count; i++)
            {

                this.Dispatcher.Invoke(
                      new Action(
                    delegate
                    {

                        ClipModel clip = batchPasteList[i];
                        if (i != batchPasteList.Count - 1)
                        {
                            clip.ClipValue = clip.ClipValue + "\n";
                        }
                        SetValueToClip(clip, false);
                    }));
                Thread.Sleep(200);
            }


        }

        /// <summary>
        /// 查找,如果value为""则显示全部且高亮第1项,如果有值则高亮第0项.
        /// </summary>
        /// <param name="value"></param>
        public void Search(string value)
        {
            displayList.Clear();
            value = value.ToLower();

            for (int i = 0; i < clipList.Count; i++)
            {
                if (clipList[i].Type == value.Trim() || clipList[i].ClipValue.ToLower().IndexOf(value) >= 0)
                {
                    clipList[i].SourceId = i;
                    displayList.Add(clipList[i]);
                }
            }
            ShowList(displayList, (value == "") ? 1 : 0);

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

            DiyHide();

        }

        //显示窗体,透明度为事先设置的值.
        private void DiyShow()
        {   
            this.Topmost = true;
            this.Activate();
            this.Opacity = opacityValue;


        }

        /// <summary>
        /// 通过把透明度设置为0来代替窗体隐藏，防止到窗体显示时才开始渲染
        /// </summary>
        private void DiyHide()
        {
            this.Topmost = false;
           
            WinAPIHelper.SetForegroundWindow(activeHwnd);
            webView?.GetBrowser()?.MainFrame.ExecuteJavaScriptAsync("hideSearch()");
            this.Opacity = 0;
        }



        internal class MenuHandler : IContextMenuHandler
        {
            /// <summary>
            /// 阻止默认的右键菜单
            /// </summary>
            /// <param name="browserControl"></param>
            /// <param name="browser"></param>
            /// <param name="frame"></param>
            /// <param name="parameters"></param>
            /// <param name="model"></param>
            public void OnBeforeContextMenu(IWebBrowser browserControl, IBrowser browser, IFrame frame, IContextMenuParams parameters, IMenuModel model)
            {
                model.Clear();
            }

            public bool OnContextMenuCommand(IWebBrowser browserControl, IBrowser browser, IFrame frame, IContextMenuParams parameters, CefMenuCommand commandId, CefEventFlags eventFlags)
            {
                return false;
            }

            public void OnContextMenuDismissed(IWebBrowser browserControl, IBrowser browser, IFrame frame)
            {

            }

            public bool RunContextMenu(IWebBrowser browserControl, IBrowser browser, IFrame frame, IContextMenuParams parameters, IMenuModel model, IRunContextMenuCallback callback)
            {
                return false;
            }
        }


    }


}

