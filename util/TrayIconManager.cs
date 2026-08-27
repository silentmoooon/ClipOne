using ClipOne.model;
using ClipOne.service;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace ClipOne.util
{
    public class TrayIconManager : IDisposable
    {
        private const uint NIM_ADD = 0x00000000;
        private const uint NIM_MODIFY = 0x00000001;
        private const uint NIM_DELETE = 0x00000002;

        private const uint NIF_MESSAGE = 0x00000001;
        private const uint NIF_ICON = 0x00000002;
        private const uint NIF_TIP = 0x00000004;

        private const uint MF_STRING = 0x00000000;
        private const uint MF_POPUP = 0x00000010;
        private const uint MF_SEPARATOR = 0x00000800;
        private const uint MF_CHECKED = 0x00000008;
        private const uint MF_UNCHECKED = 0x00000000;
        private const uint MF_GRAYED = 0x00000001;
        private const uint MF_DISABLED = 0x00000002;

        private const uint TPM_RETURNCMD = 0x0100;
        private const uint TPM_RIGHTBUTTON = 0x0002;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct NOTIFYICONDATA
        {
            public uint cbSize;
            public IntPtr hWnd;
            public uint uID;
            public uint uFlags;
            public uint uCallbackMessage;
            public IntPtr hIcon;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szTip;
            public uint dwState;
            public uint dwStateMask;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string szInfo;
            public uint uTimeoutOrVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string szInfoTitle;
            public uint dwInfoFlags;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern bool Shell_NotifyIcon(uint dwMessage, ref NOTIFYICONDATA lpData);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr CreatePopupMenu();

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool DestroyMenu(IntPtr hMenu);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool AppendMenuW(IntPtr hMenu, uint uFlags, UIntPtr uIDNewItem, string lpNewItem);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint TrackPopupMenuEx(IntPtr hMenu, uint uFlags, int x, int y, IntPtr hWnd, IntPtr lpTPMParams);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadImage(IntPtr hinst, string lpszName, uint uType, int cxDesired, int cyDesired, uint fuLoad);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern uint ExtractIconEx(string lpszFile, int nIconIndex, IntPtr[]? phiconLarge, IntPtr[]? phiconSmall, uint nIcons);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr GetModuleHandle(string? lpModuleName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

        private readonly IntPtr _hWnd;
        private readonly ConfigService _configService;
        private readonly Config _config;
        private readonly StorageService _storageService;
        private readonly Action _onClear;
        private readonly Action _onReload;
        private readonly Action _onApplySkin;
        private readonly Action _onOpenHotkeySettings;
        private readonly Action _onToggleDevTools;
        private readonly Action _onExit;

        private NOTIFYICONDATA _nid;
        private IntPtr _hIcon = IntPtr.Zero;
        private bool _isAdded = false;

        private const string CSS_DIR = "html/css";

        public TrayIconManager(
            IntPtr hWnd,
            ConfigService configService,
            StorageService storageService,
            Action onClear,
            Action onReload,
            Action onApplySkin,
            Action onOpenHotkeySettings,
            Action onToggleDevTools,
            Action onExit)
        {
            _hWnd = hWnd;
            _configService = configService;
            _config = configService.GetConfig();
            _storageService = storageService;
            _onClear = onClear;
            _onReload = onReload;
            _onApplySkin = onApplySkin;
            _onOpenHotkeySettings = onOpenHotkeySettings;
            _onToggleDevTools = onToggleDevTools;
            _onExit = onExit;

            InitializeTray();
        }

        private void InitializeTray()
        {
            // 1. Extract embedded icon directly from current running .exe
            try
            {
                string? exePath = Environment.ProcessPath;
                if (!string.IsNullOrEmpty(exePath))
                {
                    IntPtr[] smallIcons = new IntPtr[1];
                    uint count = ExtractIconEx(exePath, 0, null, smallIcons, 1);
                    if (count > 0 && smallIcons[0] != IntPtr.Zero)
                    {
                        _hIcon = smallIcons[0];
                    }
                }
            }
            catch { }

            // 2. Fallback: Load from module resources
            if (_hIcon == IntPtr.Zero)
            {
                IntPtr hModule = GetModuleHandle(null);
                _hIcon = LoadIcon(hModule, (IntPtr)32512);
                if (_hIcon == IntPtr.Zero)
                {
                    _hIcon = LoadIcon(hModule, (IntPtr)1);
                }
            }

            // 3. Fallback: Load from standalone file if present
            if (_hIcon == IntPtr.Zero)
            {
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ClipOne.ico");
                if (File.Exists(iconPath))
                {
                    _hIcon = LoadImage(IntPtr.Zero, iconPath, 1, 16, 16, 0x0010);
                }
            }

            _nid = new NOTIFYICONDATA
            {
                cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATA>(),
                hWnd = _hWnd,
                uID = 1001,
                uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP,
                uCallbackMessage = (uint)WinAPIHelper.WM_TRAYICON,
                hIcon = _hIcon,
                szTip = "ClipOne"
            };

            _isAdded = Shell_NotifyIcon(NIM_ADD, ref _nid);
        }

        public void ShowContextMenu(int x, int y)
        {
            IntPtr hMenu = CreatePopupMenu();
            if (hMenu == IntPtr.Zero) return;

            Dictionary<uint, Action> actionMap = new Dictionary<uint, Action>();
            uint cmdId = 1000;

            // 1. 清空
            uint idClear = cmdId++;
            AppendMenuW(hMenu, MF_STRING, (UIntPtr)idClear, "清空");
            actionMap[idClear] = _onClear;

            // 2. 刷新
            uint idReload = cmdId++;
            AppendMenuW(hMenu, MF_STRING, (UIntPtr)idReload, "刷新");
            actionMap[idReload] = _onReload;

            AppendMenuW(hMenu, MF_SEPARATOR, UIntPtr.Zero, string.Empty);

            // 3. 格式子菜单
            IntPtr hFormatMenu = CreatePopupMenu();
            foreach (ClipType type in Enum.GetValues<ClipType>())
            {
                uint idFmt = cmdId++;
                string name = Enum.GetName(type) ?? type.ToString();
                uint flags = MF_STRING;
                if ((_config.SupportFormat & type) != 0)
                {
                    flags |= MF_CHECKED;
                }
                if (type == ClipType.text)
                {
                    flags |= MF_DISABLED | MF_GRAYED;
                }
                AppendMenuW(hFormatMenu, flags, (UIntPtr)idFmt, name);

                if (type != ClipType.text)
                {
                    actionMap[idFmt] = () =>
                    {
                        if ((_config.SupportFormat & type) != 0)
                        {
                            _config.SupportFormat &= ~type;
                        }
                        else
                        {
                            _config.SupportFormat |= type;
                        }
                        _configService.SaveSettings();
                    };
                }
            }
            AppendMenuW(hMenu, MF_POPUP, (UIntPtr)hFormatMenu.ToInt64(), "格式");

            // 4. 皮肤子菜单
            IntPtr hSkinMenu = CreatePopupMenu();
            if (Directory.Exists(CSS_DIR))
            {
                string[] fileList = Directory.GetDirectories(CSS_DIR);
                var baseSkins = fileList.Select(f => Path.GetFileName(f))
                    .Select(n => n.EndsWith("-light") ? n.Substring(0, n.Length - 6) : (n.EndsWith("-dark") ? n.Substring(0, n.Length - 5) : n))
                    .Distinct().ToList();

                foreach (string skinName in baseSkins)
                {
                    uint idSkin = cmdId++;
                    uint flags = MF_STRING;
                    if (_config.SkinName.Equals(skinName, StringComparison.OrdinalIgnoreCase))
                    {
                        flags |= MF_CHECKED;
                    }
                    AppendMenuW(hSkinMenu, flags, (UIntPtr)idSkin, skinName);
                    actionMap[idSkin] = () =>
                    {
                        _config.SkinName = skinName;
                        _configService.SaveSettings();
                        _onApplySkin();
                    };
                }
            }
            AppendMenuW(hMenu, MF_POPUP, (UIntPtr)hSkinMenu.ToInt64(), "皮肤");

            // 5. 主题模式子菜单
            IntPtr hThemeMenu = CreatePopupMenu();
            string[] modes = new[] { "System", "Light", "Dark" };
            string[] modeHeaders = new[] { "跟随系统", "浅色", "深色" };
            for (int i = 0; i < modes.Length; i++)
            {
                uint idTheme = cmdId++;
                string mode = modes[i];
                uint flags = MF_STRING;
                if (_config.ThemeMode == mode) flags |= MF_CHECKED;
                AppendMenuW(hThemeMenu, flags, (UIntPtr)idTheme, modeHeaders[i]);
                actionMap[idTheme] = () =>
                {
                    _config.ThemeMode = mode;
                    _configService.SaveSettings();
                    _onApplySkin();
                };
            }
            AppendMenuW(hMenu, MF_POPUP, (UIntPtr)hThemeMenu.ToInt64(), "主题模式");

            // 6. 热键
            uint idHotkey = cmdId++;
            AppendMenuW(hMenu, MF_STRING, (UIntPtr)idHotkey, "热键设置");
            actionMap[idHotkey] = _onOpenHotkeySettings;

            // 7. 开机自启
            uint idStartup = cmdId++;
            uint startupFlags = MF_STRING | (_config.AutoStartup ? MF_CHECKED : MF_UNCHECKED);
            AppendMenuW(hMenu, startupFlags, (UIntPtr)idStartup, "开机自启");
            actionMap[idStartup] = () =>
            {
                _config.AutoStartup = !_config.AutoStartup;
                _configService.SetStartup(_config.AutoStartup);
                _configService.SaveSettings();
            };

            AppendMenuW(hMenu, MF_SEPARATOR, UIntPtr.Zero, string.Empty);

            // 8. 开发者工具
            uint idDevTools = cmdId++;
            AppendMenuW(hMenu, MF_STRING, (UIntPtr)idDevTools, "开发者工具");
            actionMap[idDevTools] = _onToggleDevTools;

            // 9. 退出
            uint idExit = cmdId++;
            AppendMenuW(hMenu, MF_STRING, (UIntPtr)idExit, "退出");
            actionMap[idExit] = _onExit;

            WinAPIHelper.SetForegroundWindow(_hWnd);
            uint selected = TrackPopupMenuEx(hMenu, TPM_RETURNCMD | TPM_RIGHTBUTTON, x, y, _hWnd, IntPtr.Zero);
            DestroyMenu(hMenu);

            if (selected != 0 && actionMap.TryGetValue(selected, out var action))
            {
                action?.Invoke();
            }
        }

        public void Dispose()
        {
            if (_isAdded)
            {
                Shell_NotifyIcon(NIM_DELETE, ref _nid);
                _isAdded = false;
            }

            if (_hIcon != IntPtr.Zero)
            {
                DestroyIcon(_hIcon);
                _hIcon = IntPtr.Zero;
            }
        }
    }
}
