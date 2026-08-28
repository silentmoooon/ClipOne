using System;

namespace ClipOne.model
{
    public class ClipModel
    {
        /// <summary>
        /// 全局唯一标识
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        /// <summary>
        /// 创建该记录的设备唯一标识
        /// </summary>
        public string DeviceId { get; set; } = string.Empty;

        /// <summary>
        /// 创建时间戳 (UTC 毫秒)
        /// </summary>
        public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        /// <summary>
        /// 数据类型 (text, html, qq, wechat, image, file)
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// 值, 当为图片类型时, 保存文件路径或 Base64
        /// </summary>
        public string ClipValue { get; set; } = string.Empty;

        /// <summary>
        /// 显示的值, 当为图片类型时, 保存 base64 或预览文本
        /// </summary>
        public string DisplayValue { get; set; } = string.Empty;

        /// <summary>
        /// 原始文字, 供 html、QQ、WECHAT 类型使用
        /// </summary>
        public string PlainText { get; set; } = string.Empty;

        /// <summary>
        /// 如果是复制的网页上的 gif, 则覆盖回剪切板, 方便直接粘贴
        /// </summary>
        public bool NeedOverride { get; set; }

        /// <summary>
        /// 墓碑标记 (是否已软删除)
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// 删除时间戳 (UTC 毫秒)
        /// </summary>
        public long DeleteTimestamp { get; set; } = 0;

        public override bool Equals(object? obj)
        {
            if (obj is not ClipModel other) return false;
            if (!string.IsNullOrEmpty(Id) && !string.IsNullOrEmpty(other.Id) && Id == other.Id) return true;
            return ToString() == other.ToString();
        }

        public override string ToString()
        {
            return Type + ClipValue + DisplayValue + PlainText;
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }
    }
}
