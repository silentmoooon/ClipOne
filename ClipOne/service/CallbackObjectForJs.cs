using ClipOne.view;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace ClipOne.service
{
    public class CallbackObjectForJs
    {
        MainWindow window;
        public CallbackObjectForJs(MainWindow w)
        {
            this.window = w;
        }

        /// <summary>
        /// 用于JS回调的方法，以粘贴条目到活动窗口
        /// </summary>
        /// <param name="msg"></param>
        public void PasteValue(string id)
        {

            Console.WriteLine(id);

            window.PasteValueByIndex(int.Parse(id));


        }

        /// <summary>
        /// 用于JS回调的方法，以预览图片
        /// </summary>
        /// <param name="msg"></param>
        public void Preview(int id)
        {
            
            window.PreviewByIndex(id);


        }

        /// <summary>
        /// 用于JS回调的方法，以隐藏预览窗口
        /// </summary>
        /// <param name="msg"></param>
        public void HidePreview()
        {
            window.HidePreview();


        }

        /// <summary>
        /// JS回调方法，用于删除某一项数据
        /// </summary>
        /// <param name="msg"></param>
        public void DeleteClip(int id)
        {
            Console.WriteLine(id);
            window.DeleteClip(id);
        }

        public void ChangeWindowHeight(double height, string colorStr)
        {

            string[] str = colorStr.Replace("rgb(", "").Replace(")", "").Split(',');
            int r = int.Parse(str[0].Trim());
            int g = int.Parse(str[1].Trim());
            int b = int.Parse(str[2].Trim());
            SolidColorBrush brush = new SolidColorBrush(Color.FromRgb((byte)r, (byte)g, (byte)b));


            window.Dispatcher.Invoke(
        new Action(
      delegate
      {
          window.Height = height + 10;

      }));


        }
    }
}
