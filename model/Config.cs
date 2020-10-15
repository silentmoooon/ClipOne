using ClipOne.util;
using System.Windows.Input;

namespace ClipOne.model
{
   public class Config
    {
        /// <summary>
        /// 快捷键修饰键
        /// </summary>
        public int HotkeyModifier { get; set; } = (int)HotKeyManager.KeyModifiers.Alt;
        /// <summary>
        /// 快捷键按键,默认为v
        /// </summary>
        public int HotkeyKey { get; set; } = KeyInterop.VirtualKeyFromKey(Key.V);


        /// <summary>
        /// 是否开机启动
        /// </summary>
        public bool AutoStartup { get; set; } = false;

        /// <summary>
        /// 默认保存记录数
        /// </summary>
        public int RecordCount { get; set; } = 100;

        /// <summary>
        /// 允许保存的最大记录数
        /// </summary>
        public int MaxRecordCount { get; set; } = 500;



        /// <summary>
        /// 默认皮肤
        /// </summary>
        public string SkinName { get; set; } = "stand";


        /// <summary>
        /// 默认支持格式
        /// </summary>
        public  ClipType SupportFormat { get; set; } = ClipType.qq | ClipType.html | ClipType.image | ClipType.file | ClipType.text;
    }
}
