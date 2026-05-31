using System.Collections.ObjectModel;
using System.Windows;
using IPTVChannelManager.Common;
using IPTVChannelManager.Models;
using IPTVChannelManager.Views;

namespace IPTVChannelManager.Services
{
    /// <summary>
    /// Concrete implementation of <see cref="IWindowService"/> that opens real WPF windows.
    /// </summary>
    public class WindowService : IWindowService
    {
        #region Fields

        private readonly Window _owner;

        #endregion

        #region Constructor

        public WindowService(Window owner)
        {
            _owner = owner;
        }

        #endregion

        #region Methods

        public void OpenSettingWindow()
        {
            var win = new SettingWindow { Owner = _owner };
            win.ShowDialog();
        }

        public void OpenScannerWindow(ObservableCollection<Channel> channels)
        {
            var win = new ScannerWindow(channels, this) { Owner = _owner };
            win.ShowDialog();
        }

        public void OpenPlayerWindow(Channel channel)
        {
            PlayerWindow.ShowInstance(channel);
        }

        public void OpenEpgGuideWindow(ObservableCollection<Channel> channels)
        {
            EpgGuideWindow.ShowInstance(channels);
        }

        #endregion
    }
}
