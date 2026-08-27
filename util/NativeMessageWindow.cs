using System;
using System.Runtime.InteropServices;

namespace ClipOne.util
{
    public class NativeMessageWindow : IDisposable
    {
        public delegate IntPtr MessageHandler(uint msg, IntPtr wParam, IntPtr lParam);

        private delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WNDCLASSEX
        {
            public int cbSize;
            public int style;
            public IntPtr lpfnWndProc;
            public int cbClsExtra;
            public int cbWndExtra;
            public IntPtr hInstance;
            public IntPtr hIcon;
            public IntPtr hCursor;
            public IntPtr hbrBackground;
            public string? lpszMenuName;
            public string lpszClassName;
            public IntPtr hIconSm;
        }

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern ushort RegisterClassEx([In] ref WNDCLASSEX lpwcx);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateWindowEx(
            uint dwExStyle,
            string lpClassName,
            string lpWindowName,
            uint dwStyle,
            int x, int y, int nWidth, int nHeight,
            IntPtr hWndParent,
            IntPtr hMenu,
            IntPtr hInstance,
            IntPtr lpParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool UnregisterClass(string lpClassName, IntPtr hInstance);

        [DllImport("user32.dll")]
        private static extern IntPtr DefWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr GetModuleHandle(string? lpModuleName);

        private static readonly IntPtr HWND_MESSAGE = new IntPtr(-3);

        private readonly WndProc _wndProcDelegate;
        private readonly MessageHandler _handler;
        private readonly string _className;
        private readonly IntPtr _hInstance;
        private IntPtr _hWnd;
        private bool _disposed;

        public IntPtr Handle => _hWnd;

        public NativeMessageWindow(MessageHandler handler)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
            _className = "ClipOne_MsgWin_" + Guid.NewGuid().ToString("N");
            _hInstance = GetModuleHandle(null);

            _wndProcDelegate = CustomWndProc;

            WNDCLASSEX wc = new WNDCLASSEX
            {
                cbSize = Marshal.SizeOf<WNDCLASSEX>(),
                lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProcDelegate),
                hInstance = _hInstance,
                lpszClassName = _className
            };

            ushort regResult = RegisterClassEx(ref wc);
            if (regResult == 0)
            {
                int error = Marshal.GetLastWin32Error();
                throw new InvalidOperationException($"Failed to register message window class: {error}");
            }

            const uint WS_POPUP = 0x80000000;
            _hWnd = CreateWindowEx(
                0,
                _className,
                "ClipOneMessageWindow",
                WS_POPUP,
                0, 0, 0, 0,
                IntPtr.Zero,
                IntPtr.Zero,
                _hInstance,
                IntPtr.Zero);

            if (_hWnd == IntPtr.Zero)
            {
                int error = Marshal.GetLastWin32Error();
                UnregisterClass(_className, _hInstance);
                throw new InvalidOperationException($"Failed to create message window: {error}");
            }
        }

        private IntPtr CustomWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            IntPtr result = _handler(msg, wParam, lParam);
            if (result != IntPtr.Zero)
            {
                return result;
            }
            return DefWindowProc(hWnd, msg, wParam, lParam);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                if (_hWnd != IntPtr.Zero)
                {
                    DestroyWindow(_hWnd);
                    _hWnd = IntPtr.Zero;
                }
                UnregisterClass(_className, _hInstance);
            }
            GC.SuppressFinalize(this);
        }

        ~NativeMessageWindow()
        {
            Dispose();
        }
    }
}
