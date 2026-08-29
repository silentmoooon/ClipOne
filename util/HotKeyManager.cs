using System;
using System.Runtime.InteropServices;

namespace ClipOne.util
{
    /// <summary>
    /// 热键管理器：支持标准 Win32 RegisterHotKey 与系统保留热键 (Win+V) 的 WH_KEYBOARD_LL 底层钩子
    /// </summary>
    public class HotKeyManager
    {
        /// <summary>
        /// 热键消息
        /// </summary>
        public const int WM_HOTKEY = 0x312;
        public const int WmHotkey = 0x312;

        #region Win32 HotKey APIs

        /// <summary>
        /// 注册系统全局热键
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifuers, int vk);

        /// <summary>
        /// 注销系统全局热键
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        /// <summary>
        /// 向原子表中添加全局原子
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern short GlobalAddAtom(string lpString);

        /// <summary>
        /// 在表中搜索全局原子
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern short GlobalFindAtom(string lpString);

        /// <summary>
        /// 在表中删除全局原子
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern short GlobalDeleteAtom(string nAtom);

        #endregion

        #region Win32 Low-Level Keyboard Hook APIs

        public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr GetModuleHandle(string? lpModuleName);

        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public UIntPtr dwExtraInfo;
        }

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;

        private const int VK_SHIFT = 0x10;
        private const int VK_CONTROL = 0x11;
        private const int VK_MENU = 0x12; // Alt
        private const int VK_LWIN = 0x5B;
        private const int VK_RWIN = 0x5C;
        private const int VK_V = 0x56;

        private const uint KEYEVENTF_KEYUP = 0x0002;

        #endregion

        #region Fields & Lifecycle

        private static readonly object _syncLock = new object();
        private static IntPtr _hookHandle = IntPtr.Zero;
        // 静态字段保持对委托的引用，防止 Native AOT / GC 回收导致底层崩溃
        private static LowLevelKeyboardProc? _hookDelegate;
        private static IntPtr _targetHwnd = IntPtr.Zero;
        private static int _targetAtom = 0;

        static HotKeyManager()
        {
            AppDomain.CurrentDomain.ProcessExit += (s, e) =>
            {
                lock (_syncLock)
                {
                    if (_hookHandle != IntPtr.Zero)
                    {
                        UnhookWindowsHookEx(_hookHandle);
                        _hookHandle = IntPtr.Zero;
                    }
                }
            };
        }

        #endregion

        /// <summary>
        /// 定义辅助键枚举
        /// </summary>
        [Flags()]
        public enum KeyModifiers
        {
            None = 0,
            Alt = 1,
            Ctrl = 2,
            Shift = 4,
            WindowsKey = 8
        }

        /// <summary>
        /// 判断是否为 Win+V（仅 WindowsKey 修饰符，按键为 V）
        /// </summary>
        public static bool IsWinV(int modifier, int key)
        {
            return modifier == (int)KeyModifiers.WindowsKey && (key == 86 || key == 'V' || key == 'v');
        }

        /// <summary>
        /// 注册热键（自动根据是否为 Win+V 采用 Hook 或 RegisterHotKey）
        /// </summary>
        public static bool Register(IntPtr hWnd, int atom, int modifier, int key)
        {
            lock (_syncLock)
            {
                // 先注销可能存在的旧热键或钩子
                Unregister(hWnd, atom);

                if (IsWinV(modifier, key))
                {
                    _targetHwnd = hWnd;
                    _targetAtom = atom;

                    if (_hookHandle == IntPtr.Zero)
                    {
                        _hookDelegate = HookCallback;
                        IntPtr hMod = GetModuleHandle(null);
                        _hookHandle = SetWindowsHookEx(WH_KEYBOARD_LL, _hookDelegate, hMod, 0);
                        if (_hookHandle == IntPtr.Zero)
                        {
                            return false;
                        }
                    }
                    return true;
                }
                else
                {
                    return RegisterHotKey(hWnd, atom, modifier, key);
                }
            }
        }

        /// <summary>
        /// 注销热键
        /// </summary>
        public static void Unregister(IntPtr hWnd, int atom)
        {
            lock (_syncLock)
            {
                if (_hookHandle != IntPtr.Zero)
                {
                    UnhookWindowsHookEx(_hookHandle);
                    _hookHandle = IntPtr.Zero;
                }
                _targetHwnd = IntPtr.Zero;
                _targetAtom = 0;

                UnregisterHotKey(hWnd, atom);
            }
        }

        /// <summary>
        /// 全局键盘底层钩子回调函数
        /// </summary>
        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int msg = wParam.ToInt32();
                if (msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN)
                {
                    var kbd = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                    if (kbd.vkCode == (uint)VK_V)
                    {
                        bool winDown = (GetAsyncKeyState(VK_LWIN) & 0x8000) != 0 ||
                                       (GetAsyncKeyState(VK_RWIN) & 0x8000) != 0;
                        bool ctrlDown = (GetAsyncKeyState(VK_CONTROL) & 0x8000) != 0;
                        bool altDown = (GetAsyncKeyState(VK_MENU) & 0x8000) != 0;
                        bool shiftDown = (GetAsyncKeyState(VK_SHIFT) & 0x8000) != 0;

                        if (winDown && !ctrlDown && !altDown && !shiftDown)
                        {
                            // 模拟发送无副作用虚拟按键 (0x07)，使 Windows 判定 Win 键已被组合使用，防止松开 Win 键时误触发开始菜单
                            keybd_event(0x07, 0, 0, UIntPtr.Zero);
                            keybd_event(0x07, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

                            // 向目标消息窗口异步投递 WM_HOTKEY 消息
                            if (_targetHwnd != IntPtr.Zero && _targetAtom != 0)
                            {
                                WinAPIHelper.PostMessage(_targetHwnd, (uint)WM_HOTKEY, (IntPtr)_targetAtom, IntPtr.Zero);
                            }

                            // 消费该按键，阻断事件向操作系统传递，避免弹出 Windows 自带剪贴板
                            return (IntPtr)1;
                        }
                    }
                }
                else if (msg == WM_KEYUP || msg == WM_SYSKEYUP)
                {
                    var kbd = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                    if (kbd.vkCode == (uint)VK_V)
                    {
                        bool winDown = (GetAsyncKeyState(VK_LWIN) & 0x8000) != 0 ||
                                       (GetAsyncKeyState(VK_RWIN) & 0x8000) != 0;
                        if (winDown)
                        {
                            // 消费抬起事件
                            return (IntPtr)1;
                        }
                    }
                }
            }

            return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
        }
    }
}
