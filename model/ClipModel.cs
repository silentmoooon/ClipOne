using System;
using System.Text.Json.Serialization;

namespace ClipOne.model
{
    public class ClipModel
    {
        /// <summary>
        /// 全局唯一标识
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        /// <summary>
        /// 创建该记录的设备唯一标识（不写入存储，由分片目录名推断）
        /// </summary>
        [JsonIgnore]
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
        /// 值, 当为图片类型时, 保存资产相对路径（如 assets/xxx.bmp）或旧 Base64
        /// </summary>
        public string ClipValue { get; set; } = string.Empty;

        /// <summary>
        /// 显示的值, 当为图片类型时, 保存预览文本或文件名
        /// </summary>
        public string DisplayValue { get; set; } = string.Empty;

        private string? _plainText = null;

        /// <summary>
        /// 原始文字, 供 html、QQ、WECHAT 类型使用
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string? PlainText
        {
            get => _plainText;
            set => _plainText = string.IsNullOrEmpty(value) ? null : value;
        }

        /// <summary>
        /// 如果是复制的网页上的 gif, 则覆盖回剪切板, 方便直接粘贴
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool NeedOverride { get; set; }

        /// <summary>
        /// 墓碑标记 (是否已软删除)
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// 删除时间戳 (UTC 毫秒)
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
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
