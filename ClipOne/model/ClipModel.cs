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
        /// 涉及到的图片资源,方便清除图片,针对QQ类型
        /// </summary>
        public string Images;

      
        
    }
}
