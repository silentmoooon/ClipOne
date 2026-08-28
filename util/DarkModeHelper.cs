using System;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace ClipOne.util
{
    public static class DarkModeHelper
    {
        public enum PreferredAppMode
        {
            Default = 0,
            AllowDark = 1,
            ForceDark = 2,
            ForceLight = 3,
            Max = 4
        }

        private delegate int SetPreferredAppModeDelegate(PreferredAppMode appMode);
        private delegate bool AllowDarkModeForAppDelegate(bool allow);
        private delegate bool AllowDarkModeForWindowDelegate(IntPtr hWnd, bool allow);
        private delegate void FlushMenuThemesDelegate();

        private static bool _initialized = false;
        private static SetPreferredAppModeDelegate? _setPreferredAppMode;
        private static AllowDarkModeForAppDelegate? _allowDarkModeForApp;
        private static AllowDarkModeForWindowDelegate? _allowDarkModeForWindow;
        private static FlushMenuThemesDelegate? _flushMenuThemes;

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, IntPtr procOrdinal);

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode, EntryPoint = "SetWindowTheme")]
        private static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string? pszSubIdList);

        private static void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;

            try
            {
                if (NativeLibrary.TryLoad("uxtheme.dll", out IntPtr hUxTheme))
                {
                    // Ordinal 135: SetPreferredAppMode (Win10 1903+ / Win11) or AllowDarkModeForApp (Win10 1809)
                    IntPtr pSetPreferredAppMode = GetProcAddress(hUxTheme, (IntPtr)135);
                    if (pSetPreferredAppMode != IntPtr.Zero)
                    {
                        try
                        {
                            _setPreferredAppMode = Marshal.GetDelegateForFunctionPointer<SetPreferredAppModeDelegate>(pSetPreferredAppMode);
                        }
                        catch
                        {
                            _allowDarkModeForApp = Marshal.GetDelegateForFunctionPointer<AllowDarkModeForAppDelegate>(pSetPreferredAppMode);
                        }
                    }

                    // Ordinal 133: AllowDarkModeForWindow
                    IntPtr pAllowDarkModeForWindow = GetProcAddress(hUxTheme, (IntPtr)133);
                    if (pAllowDarkModeForWindow != IntPtr.Zero)
                    {
                        _allowDarkModeForWindow = Marshal.GetDelegateForFunctionPointer<AllowDarkModeForWindowDelegate>(pAllowDarkModeForWindow);
                    }

                    // Ordinal 136: FlushMenuThemes
                    IntPtr pFlushMenuThemes = GetProcAddress(hUxTheme, (IntPtr)136);
                    if (pFlushMenuThemes != IntPtr.Zero)
                    {
                        _flushMenuThemes = Marshal.GetDelegateForFunctionPointer<FlushMenuThemesDelegate>(pFlushMenuThemes);
                    }
                }
            }
            catch
            {
                // Graceful fallback on older or unsupported Windows platforms
            }
        }

        public static bool IsSystemDarkTheme()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                if (key?.GetValue("AppsUseLightTheme") is int appsUseLightTheme)
                {
                    return appsUseLightTheme == 0;
                }
            }
            catch { }
            return false;
        }

        public static bool ShouldUseDarkMode(string themeMode)
        {
            if (string.Equals(themeMode, "Dark", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            if (string.Equals(themeMode, "Light", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            return IsSystemDarkTheme();
        }

        public static void ApplyTheme(IntPtr hWnd, string themeMode)
        {
            EnsureInitialized();

            bool isDark = ShouldUseDarkMode(themeMode);

            // 1. Configure Preferred App Mode
            try
            {
                if (_setPreferredAppMode != null)
                {
                    PreferredAppMode mode = isDark ? PreferredAppMode.ForceDark : PreferredAppMode.ForceLight;
                    if (string.Equals(themeMode, "System", StringComparison.OrdinalIgnoreCase))
                    {
                        mode = PreferredAppMode.AllowDark;
                    }
                    _setPreferredAppMode(mode);
                }
                else if (_allowDarkModeForApp != null)
                {
                    _allowDarkModeForApp(isDark);
                }
            }
            catch { }

            // 2. Configure Window Dark Mode
            if (hWnd != IntPtr.Zero)
            {
                try
                {
                    _allowDarkModeForWindow?.Invoke(hWnd, isDark);
                }
                catch { }

                try
                {
                    int darkVal = isDark ? 1 : 0;
                    // Try attribute 20 (Win10 20H1+ and Win11), fallback to 19 (Win10 1809-1909)
                    if (WinAPIHelper.DwmSetWindowAttribute(hWnd, WinAPIHelper.DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkVal, sizeof(int)) != 0)
                    {
                        WinAPIHelper.DwmSetWindowAttribute(hWnd, WinAPIHelper.DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref darkVal, sizeof(int));
                    }
                }
                catch { }

                try
                {
                    SetWindowTheme(hWnd, isDark ? "DarkMode_Explorer" : "Explorer", null);
                }
                catch { }
            }

            // 3. Flush UxTheme Menu Theme cache
            try
            {
                _flushMenuThemes?.Invoke();
            }
            catch { }
        }
    }
}
