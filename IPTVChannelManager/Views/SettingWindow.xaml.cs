using IPTVChannelManager.Common;
using IPTVChannelManager.ViewModels;

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
