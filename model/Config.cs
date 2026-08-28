using ClipOne.util;

namespace ClipOne.model
{
    public class Config
    {
        /// <summary>
        /// 快捷键修饰键 (1=Alt, 2=Ctrl, 4=Shift, 8=Win)
        /// </summary>
        public int HotkeyModifier { get; set; } = (int)HotKeyManager.KeyModifiers.WindowsKey;

        /// <summary>
        /// 快捷键按键, 默认为 'V' (0x56 / 86)
        /// </summary>
        public int HotkeyKey { get; set; } = 86; // VK_V

        /// <summary>
        /// 是否开机启动
        /// </summary>
        public bool AutoStartup { get; set; } = false;

        /// <summary>
        /// 默认皮肤
        /// </summary>
        public string SkinName { get; set; } = "fluent";

        /// <summary>
        /// 主题模式 (Light, Dark, System)
        /// </summary>
        public string ThemeMode { get; set; } = "System";

        /// <summary>
        /// 自定义同步目录路径 (为空时默认使用应用所在目录下的 data/ 文件夹)
        /// </summary>
        public string SyncFolder { get; set; } = string.Empty;

        /// <summary>
        /// 默认支持格式
        /// </summary>
        public ClipType SupportFormat { get; set; } = ClipType.qq | ClipType.html | ClipType.image | ClipType.file | ClipType.text;
    }
}
