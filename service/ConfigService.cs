using ClipOne.model;
using Microsoft.Win32;
using Newtonsoft.Json;
using System.Diagnostics;
using System.IO;

namespace ClipOne.service
{
   public class ConfigService
    {
     

        private readonly Config config=new Config();
        /// <summary>
        /// 配置文件路径
        /// </summary>
        private readonly string settingsPath = "config\\settings.json";

        

        /// <summary>
        /// /加载设置项
        /// </summary>
        public   Config GetConfig()
        {
            if (!File.Exists(settingsPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(settingsPath));
                return config;
            }
            string json = File.ReadAllText(settingsPath);
            Config tmpConfig = JsonConvert.DeserializeObject<Config>(json);
            config.HotkeyKey = tmpConfig.HotkeyKey;
            config.HotkeyModifier = tmpConfig.HotkeyModifier;
            config.MaxRecordCount = tmpConfig.MaxRecordCount;
            config.RecordCount = tmpConfig.RecordCount;
            config.SkinName = tmpConfig.SkinName;
            config.SupportFormat = tmpConfig.SupportFormat;
            
            if (config.AutoStartup)
            {
                SetStartup(true);
            }
            return config;
        }
 

        /// <summary>
        /// 设置开机启动
        /// </summary>
        public  void SetStartup(bool isAutoStartup)
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
