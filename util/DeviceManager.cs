using Microsoft.Win32;
using System;
using System.IO;

namespace ClipOne.util
{
    public static class DeviceManager
    {
        private const string RegSubKey = @"Software\ClipOne";
        private const string DeviceIdValue = "DeviceId";
        private const string DeviceNameValue = "DeviceName";

        private static string? _deviceId;
        private static string? _deviceName;

        public static string DeviceId
        {
            get
            {
                if (string.IsNullOrEmpty(_deviceId))
                {
                    Initialize();
                }
                return _deviceId!;
            }
        }

        public static string DeviceName
        {
            get
            {
                if (string.IsNullOrEmpty(_deviceName))
                {
                    Initialize();
                }
                return _deviceName!;
            }
        }

        public static string DeviceFolderTag
        {
            get
            {
                // Sanitize device name for safe folder path
                string safeName = DeviceName;
                foreach (char c in Path.GetInvalidFileNameChars())
                {
                    safeName = safeName.Replace(c, '_');
                }
                return $"device_{safeName}_{DeviceId}";
            }
        }

        private static void Initialize()
        {
            // 1. Try reading from Registry HKCU\Software\ClipOne
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegSubKey, writable: true) ??
                                Registry.CurrentUser.CreateSubKey(RegSubKey, writable: true);

                if (key != null)
                {
                    _deviceId = key.GetValue(DeviceIdValue) as string;
                    _deviceName = key.GetValue(DeviceNameValue) as string;

                    if (string.IsNullOrWhiteSpace(_deviceId))
                    {
                        _deviceId = Guid.NewGuid().ToString("N")[..8];
                        key.SetValue(DeviceIdValue, _deviceId);
                    }

                    if (string.IsNullOrWhiteSpace(_deviceName))
                    {
                        _deviceName = Environment.MachineName;
                        key.SetValue(DeviceNameValue, _deviceName);
                    }

                    return;
                }
            }
            catch
            {
                // Fallback below
            }

            // 2. Fallback to %LOCALAPPDATA%\ClipOne\device.txt
            try
            {
                string localAppDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ClipOne");
                if (!Directory.Exists(localAppDir))
                {
                    Directory.CreateDirectory(localAppDir);
                }

                string idFile = Path.Combine(localAppDir, "device_id.txt");
                if (File.Exists(idFile))
                {
                    _deviceId = File.ReadAllText(idFile).Trim();
                }

                if (string.IsNullOrWhiteSpace(_deviceId))
                {
                    _deviceId = Guid.NewGuid().ToString("N")[..8];
                    File.WriteAllText(idFile, _deviceId);
                }

                _deviceName = Environment.MachineName;
            }
            catch
            {
                _deviceId = "dev_" + Environment.MachineName.ToLowerInvariant();
                _deviceName = Environment.MachineName;
            }
        }
    }
}
