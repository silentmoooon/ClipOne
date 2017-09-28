using ClipOne.view;
using System;
using System.IO;
using System.Threading;

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
        public void PasteValue(string clipStr)
        {

            if (!MainWindow.isNotAllowHide)
            {
                window.PasteValue(clipStr);
            }

        }
        /// <summary>
        /// 用于JS回调的方法，以粘贴条目到活动窗口
        /// </summary>
        /// <param name="msg"></param>
        public void PasteValueList(string clipListStr)
        {

            if (!MainWindow.isNotAllowHide)
            {
                window.PasteValueList(clipListStr);
            }

        }

        /// <summary>
        /// 用于JS回调的方法，以预览图片
        /// </summary>
        /// <param name="msg"></param>
        public void Preview(string path)
        {

            window.Dispatcher.Invoke(
       new Action(
     delegate
     {
         window.ShowPreviewForm(path);

     }));



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
        public void DeleteImage(string path)
        {
            new Thread(new ParameterizedThreadStart(DeleteFile)).Start(path);
        }

        private void DeleteFile(object path)
        {
            for (int i = 0; i < 3; i++)
            {
                Thread.Sleep(i*500);
                try
                {
                    File.Delete(path.ToString());
                    return;
                }
                catch
                {

                }
            }
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
