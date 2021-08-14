using System;
using System.Diagnostics;

namespace ClipOne.model
{
    public class ClipModel
    {
        /// <summary>
        /// 数据类型
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// 值,当为图片类型时,保存文件格式
        /// </summary>
        public string ClipValue { get; set; }

        /// <summary>
        ///  显示的值,当为图片类型时,保存base64
        /// </summary>
        public string DisplayValue { get; set; }


        /// <summary>
        /// 原始文字,供html、QQ、WECHAT类型使用
        /// </summary>
        public string PlainText { get; set; }

        /// <summary>
        /// 如果是复制的网页上的gif,则覆盖回剪切板,方便直接粘贴
        /// </summary>
        public bool NeedOverride { get; set; }


        public override bool Equals(object obj)
        {
       
            return ToString() == obj.ToString();
        }

        public override string ToString()
        {
            return   Type + ClipValue + DisplayValue + PlainText;
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }




    }
}
