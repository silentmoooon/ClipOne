using ClipOne.model;
using ClipOne.service;
using ClipOne.util;
using Photino.NET;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ClipOne
{
    public class Program
    {
        private static Mutex? _mutex;
        private static PhotinoWindow? _window;
        private static IntPtr _hWnd = IntPtr.Zero;
        private static IntPtr _oldWndProc = IntPtr.Zero;
        private static WndProcDelegate? _wndProcDelegate;
        private static NativeMessageWindow? _msgWindow;

        private static ConfigService? _configService;
        private static Config? _config;
        private static StorageService? _storageService;
        private static ClipService? _clipService;
        private static TrayIconManager? _trayManager;

        private static IntPtr _activityWindow = IntPtr.Zero;
        private static volatile bool _watchStatus = true;

        private const string HotkeyAtomStr = "ClipOne_Global_Atom";
        private static short _hotkeyAtom;

        private const string DefaultHtml = "html/index.html";
        private const string CssDir = "html/css";
        private const int GWLP_WNDPROC = -4;
        private const int WA_INACTIVE = 0;
        // Base window dimensions at 96 DPI (100%). Will be scaled for high-DPI displays.
        private const int BaseWindowWidth = 418;
        private const int BaseWindowHeight = 580;
        private const int BaseTrayMenuWidth = 190;
        private const int BaseTrayMenuHeight = 260;
        private static int _windowWidth = BaseWindowWidth;
        private static int _windowHeight = BaseWindowHeight;
        private static int _trayCursorX = 0;
        private static int _trayCursorY = 0;

        private static volatile bool _devToolsOpen = false;

        private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumChildWindows(IntPtr hWndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type;
            public KEYBDINPUT ki;
            private uint _padding1; // union padding
            private uint _padding2;
            private uint _padding3;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll")]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        private const uint INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const ushort VK_F12 = 0x7B;
        private static int _showToken = 0;

        [STAThread]
        public static void Main(string[] args)
        {
            Environment.CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;

            // Configure WebView2 to avoid throttling background rendering and occlusion
            Environment.SetEnvironmentVariable(
                "WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS",
                "--disable-background-timer-throttling --disable-backgrounding-occluded-windows --disable-renderer-backgrounding --disable-features=CalculateNativeWinOcclusion"
            );

            // Global exception logging
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                File.WriteAllText("error.log", $"[AppDomain Error] {ex?.ToString()}");
            };
            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                File.WriteAllText("error.log", $"[Task Error] {e.Exception}");
                e.SetObserved();
            };

            // Single instance check
            _mutex = new Mutex(true, "ClipOne_Unique_Application_Mutex", out bool createdNew);
            if (!createdNew)
            {
                return;
            }

            _configService = new ConfigService();
            _config = _configService.GetConfig();
            _storageService = new StorageService(_configService);
            _storageService.OnHistoryChanged += () =>
            {
                SendHistoryToWeb();
            };
            _clipService = new ClipService(_config);

            ApplySkin();

            // Create dedicated native message listener window
            _msgWindow = new NativeMessageWindow(OnNativeMessage);
            WinAPIHelper.AddClipboardFormatListener(_msgWindow.Handle);

            _hotkeyAtom = HotKeyManager.GlobalAddAtom(HotkeyAtomStr);
            if (_config != null)
            {
                HotKeyManager.Register(_msgWindow.Handle, _hotkeyAtom, _config.HotkeyModifier, _config.HotkeyKey);
            }

            // Initialize Tray on the message window
            _trayManager = new TrayIconManager(
                _msgWindow.Handle,
                _configService,
                _storageService,
                onClear: () =>
                {
                    _storageService?.ClearHistory();
                    _window?.SendWebMessage("{\"type\": \"history\", \"data\": []}");
                },
                onReload: () =>
                {
                    string fullHtml = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DefaultHtml);
                    _window?.Load(fullHtml);
                },
                onApplySkin: ApplySkin,
                onOpenHotkeySettings: ShowHotkeySettingsModal,
                onToggleDevTools: () =>
                {
                    OpenDevTools();
                },
                onExit: () =>
                {
                    _window?.Close();
                },
                onShowContextMenu: (x, y) =>
                {
                    ShowWebTrayMenu(x, y);
                }
            );

            _window = new PhotinoWindow()
                .SetTitle("ClipOne")
                .SetUseOsDefaultLocation(false)
                .SetUseOsDefaultSize(false)
                .SetSize(BaseWindowWidth, BaseWindowHeight)
                .SetResizable(false)
                .SetTopMost(true)
                .SetChromeless(true)
                .SetContextMenuEnabled(true)
                .SetDevToolsEnabled(true)
                .SetLocation(new Point(-10000, -10000))
                .RegisterWebMessageReceivedHandler(OnWebMessageReceived);

            _window.WindowCreated += OnWindowCreated;
            _window.WindowClosing += OnWindowClosing;

            string fullHtmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DefaultHtml);
            _window.Load(fullHtmlPath);

            _window.WaitForClose();
        }

        private static void OnWindowCreated(object? sender, EventArgs e)
        {
            if (_window == null) return;
            _hWnd = _window.WindowHandle;

            // Subclass Photino window for focus loss detection
            _wndProcDelegate = PhotinoWndProc;
            _oldWndProc = WinAPIHelper.SetWindowLongPtr(_hWnd, GWLP_WNDPROC, Marshal.GetFunctionPointerForDelegate(_wndProcDelegate));

            // Set extended styles:
            // WS_EX_TOOLWINDOW  - hide from Alt+Tab
            // WS_EX_LAYERED     - enable alpha blending so we can fade to invisible
            //                     without calling SW_HIDE (which pauses WebView2 renderer)
            IntPtr exStyle = WinAPIHelper.GetWindowLongPtr(_hWnd, WinAPIHelper.GWL_EXSTYLE);
            WinAPIHelper.SetWindowLongPtr(_hWnd, WinAPIHelper.GWL_EXSTYLE,
                (IntPtr)(exStyle.ToInt64() | WinAPIHelper.WS_EX_TOOLWINDOW | WinAPIHelper.WS_EX_LAYERED));

            // Make fully transparent (alpha=0). Window stays "shown" so WebView2 keeps rendering.
            WinAPIHelper.SetLayeredWindowAttributes(_hWnd, 0, 0, WinAPIHelper.LWA_ALPHA);

            // Move off-screen so it is invisible even without alpha (belt-and-suspenders)
            WinAPIHelper.SetWindowPos(_hWnd, WinAPIHelper.HWND_TOPMOST,
                -10000, -10000, BaseWindowWidth, BaseWindowHeight,
                WinAPIHelper.SWP_NOACTIVATE | WinAPIHelper.SWP_SHOWWINDOW);

            // DWM Round Corners
            try
            {
                int cornerPreference = WinAPIHelper.DWMWCP_ROUND;
                WinAPIHelper.DwmSetWindowAttribute(_hWnd, WinAPIHelper.DWMWA_WINDOW_CORNER_PREFERENCE, ref cornerPreference, sizeof(int));
            }
            catch { }

            // Compute DPI-scaled window size (base size defined in logical 96dpi pixels)
            uint dpi = WinAPIHelper.GetDpiForWindow(_hWnd);
            if (dpi == 0) dpi = 96;
            _windowWidth = (int)(BaseWindowWidth * dpi / 96);
            _windowHeight = (int)(BaseWindowHeight * dpi / 96);
        }

        private static bool OnWindowClosing(object? sender, EventArgs e)
        {
            if (_msgWindow != null)
            {
                WinAPIHelper.RemoveClipboardFormatListener(_msgWindow.Handle);
                HotKeyManager.Unregister(_msgWindow.Handle, _hotkeyAtom);
                HotKeyManager.GlobalDeleteAtom(HotkeyAtomStr);
                _msgWindow.Dispose();
                _msgWindow = null;
            }

            _trayManager?.Dispose();
            _mutex?.Dispose();
            return false;
        }

        private static IntPtr OnNativeMessage(uint msg, IntPtr wParam, IntPtr lParam)
        {
            switch (msg)
            {
                case (uint)WinAPIHelper.WM_CLIPBOARDUPDATE:
                    if (_watchStatus && _clipService != null && _storageService != null && _window != null)
                    {
                        var clip = _clipService.HandClip();
                        if (!string.IsNullOrWhiteSpace(clip.ClipValue))
                        {
                            bool replaced = _storageService.AddClip(clip);
                            if (replaced)
                            {
                                SendHistoryToWeb();
                            }
                            else
                            {
                                string json = JsonSerializer.Serialize(clip, ClipJsonContext.Default.ClipModel);
                                _window.SendWebMessage("{\"type\": \"add\", \"data\": " + json + "}");
                            }

                            if (clip.NeedOverride)
                            {
                                Task.Run(() =>
                                {
                                    _watchStatus = false;
                                    _clipService.SetValueToClipboard(clip);
                                    _watchStatus = true;
                                });
                            }
                        }
                    }
                    return IntPtr.Zero;

                case (uint)WinAPIHelper.WM_HOTKEY:
                    if (wParam.ToInt32() == _hotkeyAtom)
                    {
                        ShowPopupWindow();
                    }
                    return IntPtr.Zero;

                case (uint)WinAPIHelper.WM_TRAYICON:
                    int lowParam = (int)(lParam.ToInt64() & 0xFFFF);
                    if (lowParam == WinAPIHelper.WM_RBUTTONUP)
                    {
                        if (WinAPIHelper.GetCursorPos(out var pt))
                        {
                            _trayManager?.ShowContextMenu(pt.X, pt.Y);
                        }
                    }
                    else if (lowParam == WinAPIHelper.WM_LBUTTONUP)
                    {
                        ShowPopupWindow();
                    }
                    return IntPtr.Zero;
            }

            return IntPtr.Zero;
        }

        private static IntPtr PhotinoWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            switch (msg)
            {
                case (uint)WinAPIHelper.WM_ACTIVATE:
                    int wa = (int)(wParam.ToInt64() & 0xFFFF);
                    if (wa == WA_INACTIVE)
                    {
                        IntPtr other = lParam;
                        if (other != IntPtr.Zero && (other == _hWnd || WinAPIHelper.IsChild(_hWnd, other)))
                        {
                            break;
                        }
                        CheckAndHideOnFocusLoss();
                    }
                    break;

                case (uint)WinAPIHelper.WM_KILLFOCUS:
                    IntPtr newFocus = wParam;
                    if (newFocus != IntPtr.Zero && (newFocus == _hWnd || WinAPIHelper.IsChild(_hWnd, newFocus)))
                    {
                        break;
                    }
                    CheckAndHideOnFocusLoss();
                    break;
            }

            return CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);
        }

        private static void CheckAndHideOnFocusLoss()
        {
            if (_devToolsOpen)
            {
                return;
            }

            IntPtr fg = WinAPIHelper.GetForegroundWindow();
            if (fg != IntPtr.Zero && (fg == _hWnd || WinAPIHelper.IsChild(_hWnd, fg)))
            {
                return;
            }

            IntPtr dtWin = FindDevToolsWindow();
            if (dtWin != IntPtr.Zero && WinAPIHelper.IsWindow(dtWin))
            {
                _devToolsOpen = true;
                Task.Run(MonitorDevToolsClose);
                return;
            }

            DiyHide();
        }

        private static void ShowPopupWindow()
        {
            if (_window == null || _hWnd == IntPtr.Zero) return;

            _activityWindow = WinAPIHelper.GetForegroundWindow();

            if (WinAPIHelper.GetCursorPos(out var point))
            {
                var workArea = WinAPIHelper.GetWorkArea();
                int width = _windowWidth;
                int height = _windowHeight;

                int x = point.X;
                int y = point.Y - 2;

                if (x + width > workArea.Right)
                    x = workArea.Right - width;
                if (y + height > workArea.Bottom)
                    y = workArea.Bottom - height - 2;
                if (x < workArea.Left) x = workArea.Left;
                if (y < workArea.Top) y = workArea.Top;

                // Move to position first while remaining transparent (alpha=0)
                WinAPIHelper.SetWindowPos(_hWnd, WinAPIHelper.HWND_TOPMOST, x, y, width, height,
                    WinAPIHelper.SWP_NOACTIVATE | WinAPIHelper.SWP_SHOWWINDOW);

                // Notify webview to reset state, render DOM and signal back
                _window.SendWebMessage("{\"type\": \"show\"}");

                // Fallback timer (35ms) to ensure window is displayed even if web message is delayed
                int currentToken = Interlocked.Increment(ref _showToken);
                Task.Delay(35).ContinueWith(_ =>
                {
                    if (Volatile.Read(ref _showToken) == currentToken)
                    {
                        MakeWindowVisible();
                    }
                });
            }
        }

        private static void MakeWindowVisible()
        {
            if (_hWnd == IntPtr.Zero) return;
            WinAPIHelper.SetLayeredWindowAttributes(_hWnd, 0, 255, WinAPIHelper.LWA_ALPHA);
            WinAPIHelper.SetForegroundWindow(_hWnd);
            WinAPIHelper.SetActiveWindow(_hWnd);
        }

        private static void ShowWebTrayMenu(int cursorX, int cursorY)
        {
            if (_window == null || _hWnd == IntPtr.Zero || _config == null) return;

            _trayCursorX = cursorX;
            _trayCursorY = cursorY;
            _activityWindow = WinAPIHelper.GetForegroundWindow();

            List<string> skins = new List<string>();
            if (Directory.Exists(CssDir))
            {
                string[] dirs = Directory.GetDirectories(CssDir);
                skins = dirs.Select(f => Path.GetFileName(f) ?? "")
                    .Where(n => !string.IsNullOrEmpty(n))
                    .Select(n => n.EndsWith("-light") ? n.Substring(0, n.Length - 6) : (n.EndsWith("-dark") ? n.Substring(0, n.Length - 5) : n))
                    .Distinct()
                    .OrderBy(n => n.Equals("fluent", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                    .ThenBy(n => n)
                    .ToList();
            }

            var menuData = new TrayMenuDto
            {
                Skins = skins,
                CurrentSkin = _config.SkinName,
                CurrentThemeMode = _config.ThemeMode,
                AutoStartup = _config.AutoStartup
            };

            string json = JsonSerializer.Serialize(menuData, ClipJsonContext.Default.TrayMenuDto);
            _window.SendWebMessage("{\"type\": \"showTrayMenu\", \"data\": " + json + "}");

            uint dpi = WinAPIHelper.GetDpiForWindow(_hWnd);
            if (dpi == 0) dpi = 96;

            int width = (int)(BaseTrayMenuWidth * dpi / 96);
            int height = (int)(BaseTrayMenuHeight * dpi / 96);

            var workArea = WinAPIHelper.GetWorkArea();

            int posX = cursorX - (width / 2);
            int posY = cursorY - height - 8;

            if (posX + width > workArea.Right) posX = workArea.Right - width - 6;
            if (posY + height > workArea.Bottom) posY = cursorY - height - 6;
            if (posY < workArea.Top) posY = cursorY + 6;
            if (posX < workArea.Left) posX = workArea.Left + 6;

            WinAPIHelper.SetWindowPos(_hWnd, WinAPIHelper.HWND_TOPMOST, posX, posY, width, height,
                WinAPIHelper.SWP_NOACTIVATE | WinAPIHelper.SWP_SHOWWINDOW);

            WinAPIHelper.SetLayeredWindowAttributes(_hWnd, 0, 255, WinAPIHelper.LWA_ALPHA);

            WinAPIHelper.SetForegroundWindow(_hWnd);
            WinAPIHelper.SetActiveWindow(_hWnd);
        }

        private static void DiyHide()
        {
            if (_devToolsOpen) return;

            if (_hWnd != IntPtr.Zero)
            {
                // Invalidate any pending show token
                Interlocked.Increment(ref _showToken);

                // Notify webview to reset state in background
                _window?.SendWebMessage("{\"type\": \"hide\"}");

                // Make transparent and move off-screen while keeping valid dimensions. Do NOT call SW_HIDE — it pauses WebView2.
                WinAPIHelper.SetLayeredWindowAttributes(_hWnd, 0, 0, WinAPIHelper.LWA_ALPHA);
                WinAPIHelper.SetWindowPos(_hWnd, WinAPIHelper.HWND_TOPMOST,
                    -10000, -10000, _windowWidth, _windowHeight,
                    WinAPIHelper.SWP_NOACTIVATE);

                if (_activityWindow != IntPtr.Zero)
                {
                    WinAPIHelper.SetForegroundWindow(_activityWindow);
                }
            }
        }

        private static void OpenDevTools()
        {
            if (_hWnd == IntPtr.Zero || _window == null) return;
            if (_devToolsOpen) return; // already open

            _devToolsOpen = true;

            // 1. Show the popup window and pin it (don't auto-hide on blur)
            ShowPopupWindow();

            // 2. Focus and send DevTools activation to all WebView2 child windows
            Task.Run(async () =>
            {
                await Task.Delay(150);

                var childWindows = new List<IntPtr>();
                EnumChildWindows(_hWnd, (h, _) =>
                {
                    childWindows.Add(h);
                    return true;
                }, IntPtr.Zero);

                foreach (var childHwnd in childWindows)
                {
                    WinAPIHelper.SetFocus(childHwnd);
                    // Post F12 directly
                    WinAPIHelper.PostMessage(childHwnd, (uint)WinAPIHelper.WM_KEYDOWN, (IntPtr)VK_F12, IntPtr.Zero);
                    WinAPIHelper.PostMessage(childHwnd, (uint)WinAPIHelper.WM_KEYUP, (IntPtr)VK_F12, IntPtr.Zero);
                }

                WinAPIHelper.SetForegroundWindow(_hWnd);
                await Task.Delay(60);
                SendDevToolsShortcut();

                // 3. Poll until the DevTools window disappears, then hide the popup
                await MonitorDevToolsClose();
            });
        }

        private static IntPtr FindWebViewChild(IntPtr parent)
        {
            IntPtr found = IntPtr.Zero;
            var sb = new System.Text.StringBuilder(256);
            EnumChildWindows(parent, (hWnd, lParam) =>
            {
                sb.Clear();
                GetClassName(hWnd, sb, sb.Capacity);
                string cls = sb.ToString();
                if (cls.StartsWith("Chrome_WidgetWin", StringComparison.OrdinalIgnoreCase))
                {
                    found = hWnd;
                    return false; // stop enum
                }
                return true;
            }, IntPtr.Zero);
            return found;
        }

        private static void SendDevToolsShortcut()
        {
            // Send Ctrl + Shift + I (Chromium standard DevTools shortcut)
            const ushort VK_CONTROL = 0x11;
            const ushort VK_SHIFT = 0x10;
            const ushort VK_I = 0x49;

            var inputs = new INPUT[6];
            inputs[0] = new INPUT { type = INPUT_KEYBOARD, ki = new KEYBDINPUT { wVk = VK_CONTROL } };
            inputs[1] = new INPUT { type = INPUT_KEYBOARD, ki = new KEYBDINPUT { wVk = VK_SHIFT } };
            inputs[2] = new INPUT { type = INPUT_KEYBOARD, ki = new KEYBDINPUT { wVk = VK_I } };
            inputs[3] = new INPUT { type = INPUT_KEYBOARD, ki = new KEYBDINPUT { wVk = VK_I, dwFlags = KEYEVENTF_KEYUP } };
            inputs[4] = new INPUT { type = INPUT_KEYBOARD, ki = new KEYBDINPUT { wVk = VK_SHIFT, dwFlags = KEYEVENTF_KEYUP } };
            inputs[5] = new INPUT { type = INPUT_KEYBOARD, ki = new KEYBDINPUT { wVk = VK_CONTROL, dwFlags = KEYEVENTF_KEYUP } };

            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());

            // Also send F12 as a fallback
            var f12Inputs = new INPUT[2];
            f12Inputs[0] = new INPUT { type = INPUT_KEYBOARD, ki = new KEYBDINPUT { wVk = VK_F12 } };
            f12Inputs[1] = new INPUT { type = INPUT_KEYBOARD, ki = new KEYBDINPUT { wVk = VK_F12, dwFlags = KEYEVENTF_KEYUP } };
            SendInput(2, f12Inputs, Marshal.SizeOf<INPUT>());
        }

        private static async Task MonitorDevToolsClose()
        {
            IntPtr devToolsWin = IntPtr.Zero;

            // Wait for DevTools window to appear (up to 10s)
            for (int i = 0; i < 40; i++)
            {
                await Task.Delay(250);
                devToolsWin = FindDevToolsWindow();
                if (devToolsWin != IntPtr.Zero && WinAPIHelper.IsWindow(devToolsWin))
                {
                    break;
                }
            }

            if (devToolsWin == IntPtr.Zero)
            {
                _devToolsOpen = false;
                DiyHide();
                return;
            }

            // Wait until DevTools window is closed or destroyed
            while (WinAPIHelper.IsWindow(devToolsWin))
            {
                await Task.Delay(350);
            }

            // DevTools closed — reset flag and hide the popup window
            _devToolsOpen = false;
            DiyHide();
        }

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

        private static IntPtr FindDevToolsWindow()
        {
            IntPtr found = IntPtr.Zero;
            var sb = new System.Text.StringBuilder(512);
            EnumWindows((hWnd, _) =>
            {
                sb.Clear();
                GetWindowText(hWnd, sb, sb.Capacity);
                string title = sb.ToString();
                if (!string.IsNullOrEmpty(title))
                {
                    if (title.Contains("DevTools", StringComparison.OrdinalIgnoreCase) ||
                        title.Contains("开发者工具", StringComparison.OrdinalIgnoreCase) ||
                        title.Contains("Developer Tools", StringComparison.OrdinalIgnoreCase))
                    {
                        found = hWnd;
                        return false;
                    }
                }
                return true;
            }, IntPtr.Zero);
            return found;
        }

        private static void ShowHotkeySettingsModal()
        {
            if (_config == null || _window == null) return;
            ShowPopupWindow();
            string dataJson = JsonSerializer.Serialize(new HotkeyDto { Modifier = _config.HotkeyModifier, Key = _config.HotkeyKey }, ClipJsonContext.Default.HotkeyDto);
            _window.SendWebMessage("{\"type\": \"hotkeySettings\", \"data\": " + dataJson + "}");
        }

        private static void SendHistoryToWeb()
        {
            if (_storageService == null || _window == null) return;
            var history = _storageService.GetHistory();
            string historyJson = JsonSerializer.Serialize(history, ClipJsonContext.Default.ListClipModel);
            _window.SendWebMessage("{\"type\": \"history\", \"data\": " + historyJson + "}");
        }

        private static void OnWebMessageReceived(object? sender, string message)
        {
            if (string.IsNullOrEmpty(message)) return;

            string[] parts = message.Split(new[] { '|' }, 2);
            string command = parts[0];
            string payload = parts.Length > 1 ? parts[1] : string.Empty;

            switch (command)
            {
                case "ready":
                    SendHistoryToWeb();
                    break;

                case "shown":
                    MakeWindowVisible();
                    break;

                case "PasteValue":
                    DiyHide();
                    if (!string.IsNullOrEmpty(payload))
                    {
                        string decoded = Uri.UnescapeDataString(payload);
                        var clip = JsonSerializer.Deserialize(decoded, ClipJsonContext.Default.ClipModel);
                        if (clip != null)
                        {
                            SinglePaste(clip);
                        }
                    }
                    break;

                case "PasteValueList":
                    DiyHide();
                    if (!string.IsNullOrEmpty(payload))
                    {
                        string decoded = Uri.UnescapeDataString(payload);
                        var list = JsonSerializer.Deserialize(decoded, ClipJsonContext.Default.ListClipModel);
                        if (list != null)
                        {
                            BatchPaste(list);
                        }
                    }
                    break;

                case "SetToClipBoard":
                    DiyHide();
                    if (!string.IsNullOrEmpty(payload))
                    {
                        string decoded = Uri.UnescapeDataString(payload);
                        var clip = JsonSerializer.Deserialize(decoded, ClipJsonContext.Default.ClipModel);
                        if (clip != null && _clipService != null)
                        {
                            _clipService.SetValueToClipboard(clip);
                        }
                    }
                    break;

                case "SaveHotkey":
                    if (!string.IsNullOrEmpty(payload) && _configService != null && _config != null && _msgWindow != null)
                    {
                        string decoded = Uri.UnescapeDataString(payload);
                        var dto = JsonSerializer.Deserialize(decoded, ClipJsonContext.Default.HotkeyDto);
                        if (dto != null && dto.Modifier > 0 && dto.Key > 0)
                        {
                            HotKeyManager.Unregister(_msgWindow.Handle, _hotkeyAtom);
                            bool registered = HotKeyManager.Register(_msgWindow.Handle, _hotkeyAtom, dto.Modifier, dto.Key);
                            if (registered)
                            {
                                _config.HotkeyModifier = dto.Modifier;
                                _config.HotkeyKey = dto.Key;
                                _configService.SaveSettings();
                            }
                            else
                            {
                                HotKeyManager.Register(_msgWindow.Handle, _hotkeyAtom, _config.HotkeyModifier, _config.HotkeyKey);
                            }
                        }
                    }
                    break;

                case "del":
                    if (!string.IsNullOrEmpty(payload) && _storageService != null)
                    {
                        _storageService.DeleteClipById(payload);
                    }
                    break;

                case "delIndex":
                    if (int.TryParse(payload, out int delIdx) && _storageService != null)
                    {
                        _storageService.DeleteClip(delIdx);
                    }
                    break;

                case "esc":
                    DiyHide();
                    break;

                case "TrayAction":
                    if (!string.IsNullOrEmpty(payload))
                    {
                        string[] actParts = payload.Split(new[] { '|' }, 2);
                        string act = actParts[0];
                        string actArg = actParts.Length > 1 ? actParts[1] : string.Empty;

                        switch (act)
                        {
                            case "clear":
                                DiyHide();
                                _storageService?.ClearHistory();
                                _window?.SendWebMessage("{\"type\": \"history\", \"data\": []}");
                                break;

                            case "reload":
                                DiyHide();
                                string fullHtml = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DefaultHtml);
                                _window?.Load(fullHtml);
                                break;

                            case "setSkin":
                                if (!string.IsNullOrEmpty(actArg) && _config != null && _configService != null)
                                {
                                    _config.SkinName = actArg;
                                    _configService.SaveSettings();
                                    ApplySkin();
                                }
                                DiyHide();
                                break;

                            case "setTheme":
                                if (!string.IsNullOrEmpty(actArg) && _config != null && _configService != null)
                                {
                                    _config.ThemeMode = actArg;
                                    _configService.SaveSettings();
                                    ApplySkin();
                                }
                                DiyHide();
                                break;

                            case "openHotkey":
                                DiyHide();
                                ShowHotkeySettingsModal();
                                break;

                            case "toggleStartup":
                                if (_config != null && _configService != null)
                                {
                                    _config.AutoStartup = !_config.AutoStartup;
                                    _configService.SetStartup(_config.AutoStartup);
                                    _configService.SaveSettings();
                                }
                                DiyHide();
                                break;

                            case "toggleDevTools":
                                DiyHide();
                                OpenDevTools();
                                break;

                            case "exit":
                                _window?.Close();
                                break;
                        }
                    }
                    break;

                case "ResizeTrayMenu":
                    if (int.TryParse(payload, out int logicalHeight) && logicalHeight > 80 && _hWnd != IntPtr.Zero)
                    {
                        uint dpi = WinAPIHelper.GetDpiForWindow(_hWnd);
                        if (dpi == 0) dpi = 96;

                        int width = (int)(BaseTrayMenuWidth * dpi / 96);
                        int height = (int)(logicalHeight * dpi / 96);

                        var workArea = WinAPIHelper.GetWorkArea();

                        int posX = _trayCursorX - (width / 2);
                        int posY = _trayCursorY - height - 8;

                        if (posX + width > workArea.Right) posX = workArea.Right - width - 6;
                        if (posY + height > workArea.Bottom) posY = _trayCursorY - height - 6;
                        if (posY < workArea.Top) posY = _trayCursorY + 6;
                        if (posX < workArea.Left) posX = workArea.Left + 6;

                        WinAPIHelper.SetWindowPos(_hWnd, WinAPIHelper.HWND_TOPMOST, posX, posY, width, height,
                            WinAPIHelper.SWP_NOACTIVATE | WinAPIHelper.SWP_SHOWWINDOW);
                    }
                    break;
            }
        }

        private static void SinglePaste(ClipModel clip)
        {
            if (_clipService == null) return;

            if (_msgWindow != null) WinAPIHelper.RemoveClipboardFormatListener(_msgWindow.Handle);
            _clipService.SetValueToClipboard(clip);
            Thread.Sleep(30);
            KeyboardKit.Keyboard.SendPaste();
            Thread.Sleep(30);
            if (_msgWindow != null) WinAPIHelper.AddClipboardFormatListener(_msgWindow.Handle);
        }

        private static void BatchPaste(List<ClipModel> clipList)
        {
            if (_clipService == null || clipList.Count == 0) return;

            if (_msgWindow != null) WinAPIHelper.RemoveClipboardFormatListener(_msgWindow.Handle);
            Thread.Sleep(60);
            for (int i = 0; i < clipList.Count; i++)
            {
                var clip = clipList[i];
                if (i != clipList.Count - 1 && !clip.ClipValue.EndsWith("\n") && !clip.ClipValue.EndsWith("\r\n"))
                {
                    clip.ClipValue += "\r\n";
                }
                _clipService.SetValueToClipboard(clip);
                Thread.Sleep(30);
                KeyboardKit.Keyboard.SendPaste();
                Thread.Sleep(80);
            }
            if (_msgWindow != null) WinAPIHelper.AddClipboardFormatListener(_msgWindow.Handle);
        }

        public static void ApplySkin()
        {
            if (_config == null) return;
            string actualSkinName = _config.SkinName;
            string modeSuffix = _config.ThemeMode switch
            {
                "Dark" => "-dark",
                "Light" => "-light",
                _ => DarkModeHelper.IsSystemDarkTheme() ? "-dark" : "-light"
            };

            if (_msgWindow != null)
            {
                DarkModeHelper.ApplyTheme(_msgWindow.Handle, _config.ThemeMode);
            }

            string baseHtmlDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "html");
            string cssDir = Path.Combine(baseHtmlDir, "css");
            string cssPath = Path.Combine(cssDir, actualSkinName + modeSuffix);
            if (!Directory.Exists(cssPath))
            {
                cssPath = Path.Combine(cssDir, actualSkinName);
                if (!Directory.Exists(cssPath))
                {
                    cssPath = Path.Combine(cssDir, actualSkinName + "-light");
                }
            }

            if (Directory.Exists(cssPath))
            {
                ChangeSkin(cssPath);
            }
        }

        private static void ChangeSkin(string cssPath)
        {
            string defaultHtmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DefaultHtml);
            if (!File.Exists(defaultHtmlPath)) return;

            string baseHtmlDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "html");
            string[] files = Directory.GetFiles(cssPath, "*.css");
            var cssHrefs = new List<string>();
            foreach (string file in files)
            {
                string rel = Path.GetRelativePath(baseHtmlDir, file).Replace("\\", "/");
                cssHrefs.Add(rel);
            }

            // 1. Update index.html on disk for next startup
            try
            {
                var fileLines = File.ReadAllLines(defaultHtmlPath).ToList();
                while (fileLines.Count > 0)
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

                foreach (string href in cssHrefs)
                {
                    fileLines.Add($"<link rel='stylesheet' type='text/css' href='{href}' />");
                }
                File.WriteAllLines(defaultHtmlPath, fileLines, Encoding.UTF8);
            }
            catch
            {
            }

            // 2. Real-time dynamic CSS hot-swap via WebMessage
            if (_window != null && cssHrefs.Count > 0)
            {
                string hrefsJson = JsonSerializer.Serialize(cssHrefs, ClipJsonContext.Default.ListString);
                _window.SendWebMessage("{\"type\": \"changeSkin\", \"css\": " + hrefsJson + "}");
            }
        }
    }
}
