using System.Windows;
using IPTVChannelManager.Common;
using IPTVChannelManager.Services;
using IPTVChannelManager.ViewModels;

namespace IPTVChannelManager.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : BaseWindow
    {
        #region Constructor

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = new MainWindowViewModel(new WindowService(this));
        }

        #endregion
    }
}
