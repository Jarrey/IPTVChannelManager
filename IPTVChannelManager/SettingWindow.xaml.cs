using IPTVChannelManager.Common;
using System.Windows;

namespace IPTVChannelManager
{
    /// <summary>
    /// Interaction logic for SettingWindow.xaml
    /// </summary>
    public partial class SettingWindow : BaseWindow
    {
        public SettingWindow()
        {
            InitializeComponent();
            this.DataContext = new SettingWindowViewModel();
        }

        private void CloseButtonClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
