using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        ///  对于QQ和FILE类型，用该字段做显示，ClipValue做赋值，其他类型的既显示又赋值
        /// </summary>
        public string DisplayValue { get; set; }


        /// <summary>
        /// 大概需要的显示高度
        /// </summary>
        public int Height { get; set; }
    }
}
