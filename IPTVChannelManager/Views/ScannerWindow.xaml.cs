using System;
using System.Collections.ObjectModel;
using IPTVChannelManager.Common;
using IPTVChannelManager.Models;
using IPTVChannelManager.ViewModels;

namespace IPTVChannelManager.Views
{
    /// <summary>
    /// Interaction logic for ScannerWindow.xaml
    /// </summary>
    public partial class ScannerWindow : BaseWindow
    {
        #region Constructor

        public ScannerWindow(ObservableCollection<Channel> existingChannels, IWindowService windowService)
        {
            InitializeComponent();
            DataContext = new ScannerWindowViewModel(existingChannels, windowService);
        }

        #endregion
    }
}
