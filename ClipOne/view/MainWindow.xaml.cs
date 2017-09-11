using CefSharp;
using CefSharp.Wpf;
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
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml;
using ClipOne.service;
using static ClipOne.service.CommonService;
using HtmlAgilityPack;
using System.Windows.Resources;

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
        /// 浏览器
        /// </summary>
        ChromiumWebBrowser webView;

        /// <summary>
        /// 复制条目保存记录
        /// </summary>
        List<ClipModel> clipList = new List<ClipModel>(maxRecords + 2);


        /// <summary>
        /// 供浏览器JS回调的接口
        /// </summary>
        CallbackObjectForJs cbOjb;

        public static  bool isDevTools = false;

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
        /// 用于连续粘贴，Shift+鼠标选择 是否按下Shift键
        /// </summary>
        volatile bool isPressedShift = false;
        /// <summary>
        /// 用于连续粘贴，保存上一次选择的index
        /// </summary>
        private int lastSelectedIndex = -1;


        /// <summary>
        /// 退出时是否需要做处理
        /// </summary>
        private bool needCloseHandle = false;

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
            ExitWhenExists();


        }




        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.Hide();

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


            if (File.Exists(storePath))
            {
                InitStore();
            }


            //在这之后才需要对退出事件做处理
            needCloseHandle = true;

            InitWebView();

            InitialTray();

            wpfHwnd = new WindowInteropHelper(this).Handle;
            WinAPIHelper.AddClipboardFormatListener(wpfHwnd);
            var hWndSource = HwndSource.FromHwnd(wpfHwnd);
            //添加处理程序
            if (hWndSource != null)
            {
                hWndSource.AddHook(WndProc);
            }

            hotkeyAtom = HotKeyManager.GlobalAddAtom(hotkeyAtomStr);


            bool status = HotKeyManager.RegisterHotKey(wpfHwnd, hotkeyAtom, hotkeyModifier, hotkeyKey);

            if (!status)
            {
                Hotkey_Click(null, null);
            }

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
        private static void InitConfig()
        {
            //从持久化文件中读取设置项
            string json = File.ReadAllText(settingsPath);
            settingsMap = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            if (settingsMap.ContainsKey("startup"))
            {
                autoStartup = bool.Parse(settingsMap["startup"]);
                if (autoStartup)
                {
                    SetStartup(autoStartup);
                }
            }
            if (settingsMap.ContainsKey("skin"))
            {
                skinName = settingsMap["skin"];
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

        /// <summary>
        /// 初始化浏览器
        /// </summary>
        private void InitWebView()
        {
            ///初始化浏览器
            var setting = new CefSharp.CefSettings();
            setting.Locale = "zh-CN";
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

        private void BatchPasteTimer_Tick(object sender, EventArgs e)
        {
            SaveData(clipList, storePath);
        }

        /// <summary>
        /// 删除指定索引的数据
        /// </summary>
        /// <param name="index"></param>
        public void DeleteClip(int index)
        {
            ClipModel clip = clipList[index];
            clipList.RemoveAt(index);

       
            if (clip.Type == IMAGE_TYPE)
            {

                if (File.Exists(clip.DisplayValue))
                {
                    File.Delete(clip.DisplayValue);
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
            System.Windows.Forms.MenuItem startup = new System.Windows.Forms.MenuItem("开机自启");
            System.Windows.Forms.MenuItem devTools = new System.Windows.Forms.MenuItem("开发者工具");
            
            System.Windows.Forms.MenuItem separator1 = new System.Windows.Forms.MenuItem("-");
            System.Windows.Forms.MenuItem hotkey = new System.Windows.Forms.MenuItem("热键");

            System.Windows.Forms.MenuItem record = new System.Windows.Forms.MenuItem("记录数");
            System.Windows.Forms.MenuItem separator2 = new System.Windows.Forms.MenuItem("-");
            System.Windows.Forms.MenuItem skin = new System.Windows.Forms.MenuItem("皮肤");
            System.Windows.Forms.MenuItem second = new System.Windows.Forms.MenuItem("高亮第二条");
            System.Windows.Forms.MenuItem separator3 = new System.Windows.Forms.MenuItem("-");
            System.Windows.Forms.MenuItem clear = new System.Windows.Forms.MenuItem("清空");
            System.Windows.Forms.MenuItem reload = new System.Windows.Forms.MenuItem("刷新");


            devTools.Click += DevTools_Click;
            clear.Click += Clear_Click;
            reload.Click += new EventHandler(Reload);
            exit.Click += new EventHandler(Exit_Click);
            hotkey.Click += Hotkey_Click;
            startup.Click += Startup_Click;
            startup.Checked = autoStartup;

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
            if (Directory.Exists(cssDir))
            {
                List<string> fileList = Directory.EnumerateFiles(cssDir).ToList();

                foreach (string file in fileList)
                {

                    string fileName = Path.GetFileNameWithoutExtension(file);
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
            System.Windows.Forms.MenuItem[] childen = new System.Windows.Forms.MenuItem[] { clear, separator3, reload, skin, separator2, record, hotkey, separator1, devTools, startup, exit };
            notifyIcon.ContextMenu = new System.Windows.Forms.ContextMenu(childen);


        }

        private void DevTools_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.MenuItem item = (System.Windows.Forms.MenuItem)sender;
            if (!isDevTools) {
                item.Checked = true;
                isDevTools = true;
               
                webView?.GetBrowser()?.ShowDevTools();
                GetClipDataAndShowWindows();
                this.Left = 100;
            }
            else
            {
                item.Checked = false;
                webView?.GetBrowser()?.CloseDevTools();
                isDevTools = false;
                this.Hide();
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

            cssPath = cssPath.Replace("\\", "/").Replace("html/", "");
            string[] fileLines = File.ReadAllLines(defaultHtml);
            fileLines[fileLines.Length - 1] = " <link rel='stylesheet' type='text/css' href='" + cssPath + "'/>";
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



        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            //当为剪切板消息时，由于获取数据会有失败的情况，所以循环3次，尽量确保成功
            if (msg == WM_CLIPBOARDUPDATE)
            {

                

                ClipModel clip = new ClipModel();
 

                //处理剪切板QQ自定义格式
                if (Clipboard.ContainsData(QQ_RICH_TYPE))
                {
                    for (int i = 0; i < 3; i++)
                    {
                        try
                        {

                            MemoryStream stream = (MemoryStream)Clipboard.GetData(QQ_RICH_TYPE);
                            byte[] b = stream.ToArray();
                            string xmlStr = System.Text.Encoding.UTF8.GetString(b);
                            xmlStr = xmlStr.Substring(0, xmlStr.IndexOf("</QQRichEditFormat>") + "</QQRichEditFormat>".Length);

                            string htmlStr = Clipboard.GetData(DataFormats.Html).ToString();

                            //qq的html内容会固定出现在第14行。
                            htmlStr = htmlStr.Split("\r\n".ToCharArray())[14];
                            if (htmlStr.Contains("\"file:///\""))
                            {

                                XmlDocument document = new XmlDocument();
                                document.LoadXml(xmlStr);
                                foreach (XmlNode node in document.DocumentElement.ChildNodes)
                                {
                                    if (node.Name == "EditElement" && node.Attributes["type"].Value == "5") //图片类型
                                    {
                                        string filePath = node.Attributes["filepath"].Value;
                                        if (!htmlStr.Contains(Path.GetFileName(filePath)))
                                        {
                                            htmlStr = htmlStr.ReplaceFirst("\"file:///\"", "\"file:///" + filePath.Replace("\\", "/") + "\"");
                                        }
                                    }
                                }

                            }
                            clip.Type = QQ_RICH_TYPE;
                            clip.ClipValue = xmlStr;

                            if (htmlStr.IndexOf("%") >= 0)
                            {
                                HtmlDocument doc = new HtmlDocument();
                                doc.LoadHtml(htmlStr);
                                foreach (HtmlNode node in doc.DocumentNode.SelectNodes("//img"))
                                {
                                    string src = node.GetAttributeValue("src", string.Empty);
                                    if (src.IndexOf("%") >= 0)
                                    {
                                        src = src.Replace("%", "%25");
                                    }
                                    node.SetAttributeValue("src", src);
                                }
                                htmlStr = doc.DocumentNode.OuterHtml;
                            }
                            clip.DisplayValue = htmlStr;
                            clip.Height = 165;
                            break;



                        }
                        catch
                        {


                        }
                    }

                }
                //处理图片
                else if (Clipboard.ContainsImage() || Clipboard.ContainsData(DataFormats.Dib))
                {

                    for (int i = 0; i < 3; i++)
                    {
                        try
                        {


                            BitmapSource bs =  Clipboard.GetImage();

                            string path = SaveImage(bs);


                            clip.Type = IMAGE_TYPE;
                            clip.ClipValue = path;
                            clip.DisplayValue = path;
                            clip.Height = 165;
                            break;

                        }
                        catch
                        {


                        }
                    }

                }
                //处理HTML类型
                else if (Clipboard.ContainsData(DataFormats.Html))
                {
                    for (int i = 0; i < 3; i++)
                    {
                        try
                        {

                            string htmlStr = Clipboard.GetData(DataFormats.Html).ToString();
                            string textStr = Clipboard.GetText();
                           //  Console.WriteLine(htmlStr.Split("\r\n".ToCharArray())[16]);
                            // Console.WriteLine(textStr);
                            //文字类型和html类型中"img"出现的次数一样则说明可以以text类型来解析
                            if (textStr != null && (textStr.IndexOf("<img") == 0 || textStr.GetOccurTimes("img") == htmlStr.Split("\r\n".ToCharArray())[16].GetOccurTimes("img")))
                            {

                                clip.ClipValue = textStr;
                                clip.DisplayValue = textStr.Replace("<", "&lt;").Replace(">", "&gt;");
                                clip.Type = TEXT_TYPE;

                                string[] array = clip.DisplayValue.Split('\n');

                                string tempStr = array[0];
                                if (array.Length > 0)
                                {
                                    for (int j = 1; j < array.Length; j++)
                                    {
                                        if (j < 6)
                                        {
                                            tempStr += "<br>" + array[j];
                                        }
                                        else if (j == 6)
                                        {
                                            tempStr += "<br>...";
                                            break;
                                        }
                                    }
                                }

                                clip.DisplayValue = tempStr;


                                if (array.Length > 5)

                                {
                                    clip.Height = 6 * 22;
                                }
                                else if (array.Length > 0)
                                {
                                    clip.Height = (array.Length) * 22;
                                }
                                else
                                {
                                    clip.Height = 35;
                                }
                            }
                            else
                            {
                                clip.ClipValue = htmlStr;
                                //html内容会固定出现在第16行。
                                clip.DisplayValue = htmlStr.Split("\r\n".ToCharArray())[16];
                                clip.Type = HTML_TYPE;
                                clip.Height = 165;
                            }
                            break;
                        }
                        catch
                        {

                        }
                    }


                }

                //处理剪切板文件
                else if (Clipboard.ContainsText())
                {

                    for (int i = 0; i < 3; i++)
                    {
                        try
                        {

                            string str =  Clipboard.GetText();

                            clip.ClipValue = str;
                            clip.DisplayValue = str.Replace("<", "&lt;").Replace(">", "&gt;");
                            clip.Type = TEXT_TYPE;

                            string[] array = clip.DisplayValue.Split('\n');

                            string tempStr = array[0];
                            if (array.Length > 0)
                            {
                                for (int j = 1; j < array.Length; j++)
                                {
                                    if (j < 6)
                                    {
                                        tempStr += "<br>" + array[j];
                                    }
                                    else if (j == 6)
                                    {
                                        tempStr += "<br>...";
                                        break;
                                    }
                                }
                            }

                            clip.DisplayValue = tempStr;


                            if (array.Length > 5)

                            {
                                clip.Height = 6 * 22;
                            }
                            else if (array.Length > 0)
                            {
                                clip.Height = (array.Length) * 22;
                            }
                            else
                            {
                                clip.Height = 35;
                            }

                            break;

                        }
                        catch
                        {


                        }
                    }


                }
               
               

                //处理剪切板文件
                else if (Clipboard.ContainsFileDropList())
                {
                    for (int i = 0; i < 3; i++)
                    {
                        try
                        {
                            string[] files = (string[])Clipboard.GetData(DataFormats.FileDrop);

                            clip.Type = FILE_TYPE;
                            clip.ClipValue = string.Join(",", files);



                            //组装显示内容，按文件名分行
                            string displayStr = files.Length + " file";
                            if (files.Length > 1)
                            {
                                displayStr += "s";
                            }
                            int j = 0;
                            foreach (string str in files)
                            {
                                if (j < 5)
                                {
                                    displayStr += "<br>" + Path.GetFileName(str);
                                }
                                else if (j == 5)
                                {
                                    displayStr += "<br>...";
                                    break;
                                }
                                j++;
                            }


                            clip.DisplayValue = displayStr;

                            if (files.Length >= 5)

                            {
                                clip.Height = 6 * 22;
                            }
                            else
                            {
                                clip.Height = (files.Length + 1) * 22;
                            }

                            break;
                        }
                        catch
                        {


                        }
                    }

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

                EnQueue(clip);


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
        private async void EnQueue(ClipModel clip)
        {

            clipList.Insert(0, clip);

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
                if (clipList.Count > currentRecords)
                {
                    DeleteClip(currentRecords);
                }


            });

        }

        /// <summary>
        /// 显示窗口并列出所有条目
        /// </summary>
        private void GetClipDataAndShowWindows()
        {

            WinAPIHelper.POINT p = new WinAPIHelper.POINT();

            int displayHeight = 0;

            for (int i = 0; i < clipList.Count; i++)
            {

                displayHeight += clipList[i].Height;

            }
            string json = JsonConvert.SerializeObject(clipList, displayJsonSettings);

            json = HttpUtility.UrlEncode(json);

            webView.GetBrowser().MainFrame.ExecuteJavaScriptAsync("fun('" + json + "')");

            if (this.IsVisible)
                this.Hide();


            if (WinAPIHelper.GetCursorPos(out p))
            {
                if (clipList.Count == 0)
                {
                    this.Height = 100;
                }
                else
                {
                    this.Height = displayHeight + 25;


                }


                double x = SystemParameters.WorkArea.Width;//得到屏幕工作区域宽度
                double y = SystemParameters.WorkArea.Height;//得到屏幕工作区域高度
                double mx = CursorHelp.ConvertPixelsToDIPixels(p.X);
                double my = CursorHelp.ConvertPixelsToDIPixels(p.Y);

                if (mx > x - this.ActualWidth)
                {
                    this.Left = x - this.ActualWidth;
                }
                else
                {
                    this.Left = mx - 2;
                }
                if (my > y - this.ActualHeight)
                {
                    this.Top = y - this.ActualHeight;
                }
                else
                {
                    this.Top = my - 2;
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
                SaveData(clipList, storePath);
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
                    SetBatchPatse(id, lastSelectedIndex);

                    this.Dispatcher.Invoke(
                          new Action(
                        delegate
                        {

                            this.Hide();
                            preview.Hide();
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

                ClipModel result = clipList[id];
                clipList.RemoveAt(id);
                clipList.Insert(0, result);

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
            ClipModel result = clipList[id];
            if (result.Type == IMAGE_TYPE)
            {
                preview.ImgPath = result.ClipValue;

                preview.Show();

            }

        }
        /// <summary>
        /// 粘贴条目到活动窗口，单条粘贴时必须加延时，否则会出现粘贴不上的现象，可能是因为活动窗口还没完成激活时就已经发出ctrl+v按键消息导致的，事先通过API主动激活也不行，不知道为什么。
        /// </summary>
        /// <param name="result">需要粘贴的值</param>
        /// /// <param name="neadPause">是否需要延时，单条需要，批量不需要</param>
        private void SetValueToClip(ClipModel result, bool neadPause)
        {


            //设置剪切板前取消监听
            WinAPIHelper.RemoveClipboardFormatListener(wpfHwnd);

            CommonService.SetValueToClip(result);
            //设置剪切板后恢复监听
            WinAPIHelper.AddClipboardFormatListener(wpfHwnd);

            System.Windows.Forms.SendKeys.SendWait("^v");




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
            if(!isDevTools)
            this.Hide();

            lastSelectedIndex = -1;
            isPressedShift = false;
            if (preview != null)
            {
                preview.Hide();
            }
            


        }


        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
           

            if (clipList.Count > 0)
            {

                if (e.KeyboardDevice.Modifiers == ModifierKeys.Shift)  //处理基于SHIFT+数字的批量粘贴
                {

                    int keyNum = (int)e.Key - 34;

                    if (keyNum >= 0 && keyNum <= 35)
                    {
                        if (lastSelectedIndex == -1)
                        {
                            lastSelectedIndex = keyNum;
                        }
                        else
                        {
                            int currentKey = keyNum;

                            SetBatchPatse(currentKey, lastSelectedIndex);
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
                    if (e.Key == Key.F12)
                    {
                        
                            webView.GetBrowser().MainFrame.ViewSource();
                         
                        return;
                    }
                    int index = -1;
                    if (e.Key == Key.OemTilde)
                    {
                        index = 0;
                    }
                    else if (e.Key == Key.Space)
                    {
                        index = 1;
                    }
                    else
                    {
                        int keyNum = (int)e.Key - 34;
                        if (keyNum >= 0 && keyNum <= 35)
                        {
                            index = keyNum;

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
        private void SetBatchPatse(int nowIndex, int lastIndex)
        {
            batchPasteList.Clear();
            if (nowIndex > lastIndex)
            {
                for (int i = lastIndex; i <= nowIndex; i++)
                {
                    var result = clipList[i];
                    clipList.RemoveAt(i);
                    clipList.Insert(0, result);
                    batchPasteList.Add(result);


                }

            }
            else
            {
                for (int i = lastIndex; i >= nowIndex; i--)
                {
                    var result = clipList[lastIndex];
                    clipList.RemoveAt(lastIndex);
                    clipList.Insert(0, result);
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

        internal class MenuHandler : IContextMenuHandler
        {
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

