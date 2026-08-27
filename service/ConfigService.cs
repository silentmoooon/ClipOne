using ClipOne.model;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace ClipOne.service
{
    public class ConfigService
    {
        private readonly Config config;
        /// <summary>
        /// 配置文件路径
        /// </summary>
        private readonly string settingsPath = Path.Combine("config", "settings.json");

        public ConfigService()
        {
            if (!File.Exists(settingsPath))
            {
                string? dir = Path.GetDirectoryName(settingsPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                config = new Config();
                SaveSettings();
            }
            else
            {
                try
                {
                    string json = File.ReadAllText(settingsPath);
                    config = JsonSerializer.Deserialize(json, ClipJsonContext.Default.Config) ?? new Config();
                }
                catch
                {
                    config = new Config();
                }

                if (config.AutoStartup)
                {
                    SetStartup(true);
                }
            }
        }

        /// <summary>
        /// 加载设置项
        /// </summary>
        public Config GetConfig()
        {
            return config;
        }

        /// <summary>
        /// 设置开机启动
        /// </summary>
        public void SetStartup(bool isAutoStartup)
        {
            try
            {
                using RegistryKey? reg = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run");
                if (reg == null) return;

                using Process currentProcess = Process.GetCurrentProcess();
                string? exePath = currentProcess.MainModule?.FileName;
                string exeName = currentProcess.MainModule?.ModuleName ?? "ClipOne";

                if (string.IsNullOrEmpty(exePath)) return;

                if (!isAutoStartup)
                {
                    if (reg.GetValue(exeName) != null)
                    {
                        reg.DeleteValue(exeName);
                    }
                }
                else
                {
                    reg.SetValue(exeName, exePath);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Failed to set startup registry: {ex.Message}");
            }
        }

        /// <summary>
        /// 保存设置
        /// </summary>
        public void SaveSettings()
        {
            try
            {
                string? dir = Path.GetDirectoryName(settingsPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                string json = JsonSerializer.Serialize(config, ClipJsonContext.Default.Config);
                File.WriteAllText(settingsPath, json);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Failed to save settings: {ex.Message}");
            }
        }
    }
}
