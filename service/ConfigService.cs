using ClipOne.model;
using Microsoft.Win32;
using Newtonsoft.Json;
using System.Diagnostics;
using System.IO;

namespace ClipOne.service
{
    public class ConfigService
    {


        private readonly Config config;
        /// <summary>
        /// 配置文件路径
        /// </summary>
        private readonly string settingsPath = "config\\settings.json";

        public ConfigService()
        {
            if (!File.Exists(settingsPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(settingsPath));
                config = new Config();
            }
            else
            {
                string json = File.ReadAllText(settingsPath);
                config = JsonConvert.DeserializeObject<Config>(json);
                if (config.AutoStartup)
                {
                    SetStartup(true);
                }
            }
        }


        /// <summary>
        /// /加载设置项
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

            RegistryKey reg = Registry.CurrentUser.CreateSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run");

            string exePath = Process.GetCurrentProcess().MainModule.FileName;
            string exeName = Process.GetCurrentProcess().MainModule.ModuleName;
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

        /// <summary>
        /// 保存设置
        /// </summary>
        public void SaveSettings()
        {
            string json = JsonConvert.SerializeObject(config);
            File.WriteAllText(settingsPath, json);
        }
    }
}
