using System.Collections.ObjectModel;
using System.Windows;
using IPTVChannelManager.Models;
using IPTVChannelManager.Views;

namespace IPTVChannelManager.Services
{
    /// <summary>
    /// Abstracts window-navigation so ViewModels can request UI transitions
    /// without depending on concrete Window types.
    /// </summary>
    public interface IWindowService
    {
        void OpenSettingWindow();
        void OpenScannerWindow(ObservableCollection<Channel> channels);
        void OpenPlayerWindow(Channel channel);
        void OpenEpgGuideWindow(ObservableCollection<Channel> channels);
    }

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
