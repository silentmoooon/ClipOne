namespace ClipOne.model
{
    public class ClipModel
    {
        /// <summary>
        /// 数据类型
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// 值
        /// </summary>
        public string ClipValue { get; set; }

        /// <summary>
        ///  显示的值
        /// </summary>
        public string DisplayValue { get; set; }

        /// <summary>
        /// 源记录索引，用于在查询结果保留源记录的索引
        /// </summary>
        public int SourceId { get; set; }
        
    }
}
