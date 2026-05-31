using IPTVChannelManager.Common;
using System.Windows;

namespace IPTVChannelManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : BaseWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = new MainWindowViewModel();
        }

        private void IgnoreCheckBoxUpdated(object sender, RoutedEventArgs e)
        {
            var viewmodel = DataContext as MainWindowViewModel;
            viewmodel?.RaiseCountChange();
        }

        private void SettingButtonClick(object sender, RoutedEventArgs e)
        {
            var settingWindow = new SettingWindow
            {
                Owner = this
            };
            settingWindow.ShowDialog();
        }

        private void ScanButtonClick(object sender, RoutedEventArgs e)
        {
            var viewmodel = DataContext as MainWindowViewModel;
            var scannerWindow = new ScannerWindow(viewmodel?.Channels)
            {
                Owner = this
            };
            scannerWindow.ShowDialog();
        }
    }
}
