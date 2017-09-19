using System.Windows;

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
