using ClipOne.view;
using System;

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

            if (!MainWindow.isNotAllowHide)
            {
                window.PasteValueByIndex(id);
            }

        }
        /// <summary>
        /// 用于JS回调的方法，以粘贴条目到活动窗口
        /// </summary>
        /// <param name="msg"></param>
        public void PasteValueByRange(int firstId, int currentId)
        {

            if (!MainWindow.isNotAllowHide)
            {
                window.PasteValueByRange(firstId, currentId);
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

            window.DeleteByIndex(id);
        }

        

        /// <summary>
        /// 用于JS回调,提供搜索功能
        /// </summary>
        /// <param name="value"></param>
        public void Search(string value)
        {
            window.Search(value);
        }

        /// <summary>
        /// 用于JS回调,页面显示完成后,反馈页面高度,以设置窗体高度
        /// </summary>
        /// <param name="height"></param>
        public void ChangeWindowHeight(double height)
        {
            window.Dispatcher.Invoke(
     new Action(
   delegate
   {
       window.ChangeWindowHeight(height);

   }));



        }
    }
}
