using System;
using System.Runtime.InteropServices;

namespace ClipOne.util
{
    class WinAPIHelper
    {
        public struct POINT
        {
            public int X;
            public int Y;
            public POINT(int x, int y)
            {
                this.X = x;
                this.Y = y;
            }
        }

        /// <summary>
        /// 监听剪切板
        /// </summary>
        /// <param name="hwnd"></param>
        /// <returns></returns>
        [DllImport("user32.dll")]
        public static extern bool AddClipboardFormatListener(IntPtr hwnd);

        /// <summary>
        /// 取消监听剪切板
        /// </summary>
        /// <param name="hwnd"></param>
        /// <returns></returns>
        [DllImport("user32.dll")]
        public static extern bool RemoveClipboardFormatListener(IntPtr hwnd);
        [DllImport("user32.dll")]
        public static extern IntPtr GetOpenClipboardWindow();
        /// <summary>   
        /// 获取鼠标的坐标   
        /// </summary>   
        /// <param name="lpPoint">传址参数，坐标point类型</param>   
        /// <returns>获取成功返回真</returns>   
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool GetCursorPos(out POINT pt);

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern IntPtr GetForegroundWindow();
        [DllImport("User32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("kernel32")]
        public static extern IntPtr GetCurrentThreadId();
        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern int GetWindowThreadProcessId(IntPtr hwnd, IntPtr ProcessId);
        [DllImport("user32.dll")]
        public static extern IntPtr AttachThreadInput(IntPtr idAttach, IntPtr idAttachTo, int fAttach);
        [DllImport("user32.dll")]
        public static extern IntPtr GetFocus();
        //发送按键消息必用PostMessage,SendMessage有时会不起作用
        [DllImport("user32.dll", EntryPoint = "PostMessage", SetLastError = true)]
        public extern static bool PostMessage(IntPtr hWnd, uint Msg, int wParam, uint lParam);
        [DllImport("user32.dll")]
        public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, uint dwExtraInfo);
        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, uint Msg, int wParam, uint lParam);

        // 查找窗体  

        // @para1: 窗体的类名 例如对话框类是"#32770"  

        // @para2: 窗体的标题 例如打开记事本 标题是"无标题 - 记事本" 注意 - 号两侧的空格  

        // return: 窗体的句柄  

        [DllImport("User32.dll", EntryPoint = "FindWindow")]

        public static extern IntPtr FindWindow(string className, string windowName);
 

        [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
        public static extern Int32 SetWindowLong(IntPtr hWnd, int nIndex, Int32 dwNewLong);
        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowLong(IntPtr hWnd, int nIndex);
    }
}
