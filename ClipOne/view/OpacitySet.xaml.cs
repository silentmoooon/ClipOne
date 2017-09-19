using System;
using System.Collections.Generic;
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
    /// OpacitySet.xaml 的交互逻辑
    /// </summary>
    public partial class OpacitySet : Window
    {
        private MainWindow window;

        public OpacitySet()
        {
            InitializeComponent();
        }
        public OpacitySet(MainWindow window,double opacityValue)
        {
            InitializeComponent();
            this.window = window;
            sliderOpa.Value = opacityValue;
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            window.ChangeOpacity(sliderOpa.Value);
        }
    }
}
