using ClipOne.util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ClipOne.view
{


    /// <summary>
    /// PreviewForm.xaml 的交互逻辑
    /// </summary>
    public partial class PreviewForm : Window
    {
        /// <summary>
        /// 图片路径 
        /// </summary>
        public string ImgPath { get; set; }
        MainWindow window;
        
       
        private PreviewForm()
        {
            InitializeComponent();
        }
        public PreviewForm(MainWindow window)
        {
            InitializeComponent();
            this.window = window;

        }



        private void Window_LostFocus(object sender, RoutedEventArgs e)
        {

            this.Hide();
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {

            this.Hide();
        }

        private void Window_MouseEnter(object sender, MouseEventArgs e)
        {
            this.Hide();
        }


        private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue == true)
            {
 
                if (File.Exists(ImgPath))
                {

                    BitmapImage bi = new BitmapImage();
                    
                    bi.BeginInit();
                   
                    bi.UriSource = new Uri("pack://SiteOfOrigin:,,,/" + ImgPath + "", UriKind.RelativeOrAbsolute);
                    bi.EndInit();


                    imageShow.Source = bi;
                    
                    WinAPIHelper.POINT p = new WinAPIHelper.POINT();

                

                    if (WinAPIHelper.GetCursorPos(out p))
                    {
                        double x = SystemParameters.WorkArea.Width;//得到屏幕工作区域宽度
                        double y = SystemParameters.WorkArea.Height;//得到屏幕工作区域高度
                        double mx = CursorHelp.ConvertPixelsToDIPixels(p.X);
                        double my = CursorHelp.ConvertPixelsToDIPixels(p.Y);

                        if (bi.Height > this.MaxHeight)
                        {
                            this.Top = my - (this.MaxHeight / 2);
                        }
                        else
                        {
                            this.Top = my - bi.Height / 2;
                        }
                        if (this.Top + bi.Height > x)
                        {
                            this.Top = x - bi.Height - 10;
                        }



                        double caclWitch = bi.Width;

                        if (caclWitch > MaxWidth)
                        {
                            caclWitch = MaxWidth;
                        }
                        if (window.Left > x - (window.Left + window.ActualWidth))
                        {
                            this.Left = window.Left - caclWitch;

                        }
                        else
                        {
                            this.Left = window.Left + window.ActualWidth;
                        }


                    }
                }

            }

        }

        
    }
}
