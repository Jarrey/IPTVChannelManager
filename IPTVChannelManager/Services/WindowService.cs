using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
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
            string externalPlayer = AppSettings.Instance.Get(AppSettings.ExternalPlayerPath) ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(externalPlayer) && File.Exists(externalPlayer))
            {
                bool unicast = AppSettings.Instance.Get<bool>(AppSettings.ImportExportWithCustomHost);
                string host = AppSettings.Instance.Get(AppSettings.UnicastHost) ?? string.Empty;
                string streamUrl = unicast
                    ? $"{host}{channel.Url}"
                    : $"{Constants.DefaultMulticastHost}{channel.Url}";

                Process.Start(new ProcessStartInfo
                {
                    FileName = externalPlayer,
                    Arguments = $"\"{streamUrl}\"",
                    UseShellExecute = false
                });
            }
            else
            {
                PlayerWindow.ShowInstance(channel);
            }
        }

        public void OpenEpgGuideWindow(ObservableCollection<Channel> channels)
        {
            EpgGuideWindow.ShowInstance(channels);
        }

        #endregion
    }
}
