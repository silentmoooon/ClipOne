using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ClipOne.util
{
    class KeyboardKit
    {
        internal static class NativeMethods
        {
            #region User32
            // Various Win32 constants
            internal const int KeyeventfKeyup = 0x0002;
            internal const int KeyeventfScancode = 0x0008;
            internal const int InputKeyboard = 1;

            // Various Win32 data structures
            [StructLayout(LayoutKind.Sequential)]
            internal struct INPUT
            {
                internal int type;
                internal INPUTUNION union;
            };

            [StructLayout(LayoutKind.Explicit)]
            internal struct INPUTUNION
            {
                [FieldOffset(0)]
                internal MOUSEINPUT mouseInput;
                [FieldOffset(0)]
                internal KEYBDINPUT keyboardInput;
            };

            [StructLayout(LayoutKind.Sequential)]
            internal struct MOUSEINPUT
            {
                internal int dx;
                internal int dy;
                internal int mouseData;
                internal int dwFlags;
                internal int time;
                internal IntPtr dwExtraInfo;
            };

            [StructLayout(LayoutKind.Sequential)]
            internal struct KEYBDINPUT
            {
                internal short wVk;
                internal short wScan;
                internal int dwFlags;
                internal int time;
                internal IntPtr dwExtraInfo;
            };

            // Importing various Win32 APIs that we need for input
            [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
            internal static extern int GetSystemMetrics(int nIndex);

            [DllImport("user32.dll", CharSet = CharSet.Auto)]
            internal static extern int MapVirtualKey(int nVirtKey, int nMapType);

            [DllImport("user32.dll", SetLastError = true)]
            internal static extern int SendInput(int nInputs, ref INPUT mi, int cbSize);

            [DllImport("user32.dll", CharSet = CharSet.Auto)]
            internal static extern short VkKeyScan(char ch);

            #endregion User32
        }

        public static class Keyboard
        {
            /// <summary>
            /// Presses down a key.
            /// </summary>
            /// <param name="key">The key to press.</param>
            public static void Press(Key key)
            {
                SendKeyboardInput(key, true);
            }

            /// <summary>
            /// Releases a key.
            /// </summary>
            /// <param name="key">The key to release.</param>
            public static void Release(Key key)
            {
                SendKeyboardInput(key, false);
            }

            /// <summary>
            /// Resets the system keyboard to a clean state.
            /// </summary>
            public static void Reset()
            {
                foreach (Key key in Enum.GetValues(typeof(Key)))
                {
                    if (key != Key.None && (System.Windows.Input.Keyboard.GetKeyStates(key) & KeyStates.Down) > 0)
                    {
                        Release(key);
                    }
                }
            }

            /// <summary>
            /// Performs a press-and-release operation for the specified key, which is effectively equivallent to typing.
            /// </summary>
            /// <param name="key">The key to press.</param>
            public static void Type(Key key)
            {
                
                Press(key);
                //System.Threading.Thread.Sleep(100);
                Release(key);
            }



            
            private static void SendKeyboardInput(Key key, bool press)
            {

                NativeMethods.INPUT ki = new NativeMethods.INPUT
                {
                    type = NativeMethods.InputKeyboard
                };
                ki.union.keyboardInput.wVk = (short)KeyInterop.VirtualKeyFromKey(key);
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
                ki.union.keyboardInput.dwExtraInfo = new IntPtr(0);

                if (NativeMethods.SendInput(1, ref ki, Marshal.SizeOf(ki)) == 0)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
            }

        }
    }
}
