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
        public void PasteValue(int id)
        {

            if (!MainWindow.isDevTools) { 
            window.PasteValueByIndex(id);
            }

        }
        /// <summary>
        /// 用于JS回调的方法，以粘贴条目到活动窗口
        /// </summary>
        /// <param name="msg"></param>
        public void PasteValueByRange(int firstId,int currentId)
        {

            if (!MainWindow.isDevTools)
            {
                window.PasteValueByRange(firstId,currentId);
            }

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
            
            window.DeleteClip(id);
        }

        public void SelectIndex(int id)
        {
           
            window.selectedIndex = id;
        }

        
        public void Search(string value)
        {
            window.Search(value);
        }
        public void ChangeWindowHeight(double height)
        {
 
            window.Dispatcher.Invoke(
        new Action(
      delegate
      {
          window.Height = height+25 ;

      }));


        }
    }
}
