using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace ClipOne.util
{
    public static class KeyboardKit
    {
        public const int VK_CONTROL = 0x11;
        public const int VK_V = 0x56;
        public const int VK_C = 0x43;
        public const int VK_MENU = 0x12; // Alt
        public const int VK_SHIFT = 0x10;
        public const int VK_LWIN = 0x5B;

        internal static class NativeMethods
        {
            internal const int KeyeventfKeyup = 0x0002;
            internal const int KeyeventfScancode = 0x0008;
            internal const int InputKeyboard = 1;

            [StructLayout(LayoutKind.Sequential)]
            internal struct INPUT
            {
                internal int type;
                internal INPUTUNION union;
            }

            [StructLayout(LayoutKind.Explicit)]
            internal struct INPUTUNION
            {
                [FieldOffset(0)]
                internal MOUSEINPUT mouseInput;
                [FieldOffset(0)]
                internal KEYBDINPUT keyboardInput;
            }

            [StructLayout(LayoutKind.Sequential)]
            internal struct MOUSEINPUT
            {
                internal int dx;
                internal int dy;
                internal int mouseData;
                internal int dwFlags;
                internal int time;
                internal IntPtr dwExtraInfo;
            }

            [StructLayout(LayoutKind.Sequential)]
            internal struct KEYBDINPUT
            {
                internal short wVk;
                internal short wScan;
                internal int dwFlags;
                internal int time;
                internal IntPtr dwExtraInfo;
            }

            [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
            internal static extern int GetSystemMetrics(int nIndex);

            [DllImport("user32.dll", CharSet = CharSet.Auto)]
            internal static extern int MapVirtualKey(int nVirtKey, int nMapType);

            [DllImport("user32.dll", SetLastError = true)]
            internal static extern int SendInput(int nInputs, ref INPUT mi, int cbSize);

            [DllImport("user32.dll", CharSet = CharSet.Auto)]
            internal static extern short GetAsyncKeyState(int vKey);
        }

        public static class Keyboard
        {
            public static void Press(int vk)
            {
                SendKeyboardInput(vk, true);
            }

            public static void Release(int vk)
            {
                SendKeyboardInput(vk, false);
            }

            public static void Type(int vk)
            {
                Press(vk);
                Release(vk);
            }

            public static void SendPaste()
            {
                Press(VK_CONTROL);
                Press(VK_V);
                Release(VK_V);
                Release(VK_CONTROL);
            }

            private static void SendKeyboardInput(int vk, bool press)
            {
                NativeMethods.INPUT ki = new NativeMethods.INPUT
                {
                    type = NativeMethods.InputKeyboard
                };
                ki.union.keyboardInput.wVk = (short)vk;
                ki.union.keyboardInput.wScan = (short)NativeMethods.MapVirtualKey(ki.union.keyboardInput.wVk, 0);

                int dwFlags = 0;
                if (ki.union.keyboardInput.wScan > 0)
                {
                    dwFlags |= NativeMethods.KeyeventfScancode;
                }

                if (!press)
                {
                    dwFlags |= NativeMethods.KeyeventfKeyup;
                }

                ki.union.keyboardInput.dwFlags = dwFlags;
                ki.union.keyboardInput.time = 0;
                ki.union.keyboardInput.dwExtraInfo = IntPtr.Zero;

                if (NativeMethods.SendInput(1, ref ki, Marshal.SizeOf(ki)) == 0)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
            }
        }
    }
}
