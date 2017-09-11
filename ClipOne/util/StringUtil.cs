using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClipOne.util
{
  public  static class  StringUtil
    {
        /// <summary>
        /// 只替换第一个匹配的字符串
        /// </summary>
        /// <param name="s"></param>
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        /// <returns></returns>
        public static string ReplaceFirst(this string s,string oldValue,string newValue)
        {
            int index = s.IndexOf(oldValue);
            return s.Remove(index, oldValue.Length).Insert(index,newValue);

        }

        /// <summary>
        /// 得到字符串B在当前字符串内出现的次数
        /// </summary>
        /// <param name="s"></param>
        /// <param name="str"></param>
        /// <returns></returns>
        public static int GetOccurTimes(this string s,string str)
        {
            return (s.Length - s.Replace(str, "").Length) / str.Length;
        }
    }
}
