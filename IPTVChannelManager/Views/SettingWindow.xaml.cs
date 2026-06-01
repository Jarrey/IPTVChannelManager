using IPTVChannelManager.Common;
using IPTVChannelManager.ViewModels;
using Microsoft.Win32;
using System.Windows;

namespace IPTVChannelManager.Views
{
    /// <summary>
    /// Interaction logic for SettingWindow.xaml
    /// </summary>
    public partial class SettingWindow : BaseWindow
    {
        #region Constructor

        public SettingWindow()
        {
            InitializeComponent();
            this.DataContext = new SettingWindowViewModel();
        }

        #endregion
    }
}
