using IPTVChannelManager.Common;
using IPTVChannelManager.Models;
using System.IO;

namespace IPTVChannelManager.ViewModels
{
    /// <summary>
    /// Holds the channel-level state for the player window (current channel,
    /// stream URL construction, window title) so that <see cref="PlayerWindow"/>
    /// code-behind is free of business logic.
    /// </summary>
    public class PlayerViewModel : BindableBase
    {
        #region Fields

        private string _windowTitle = string.Empty;
        private Channel? _currentChannel;

        #endregion

        #region Properties

        public string WindowTitle
        {
            get => _windowTitle;
            set => SetProperty(ref _windowTitle, value);
        }

        /// <summary>Display name of the current channel (for EPG look-up).</summary>
        public string? CurrentChannelName => _currentChannel?.Name;

        /// <summary>
        /// Logo file stem (without extension) — often matches the EPG display name.
        /// </summary>
        public string? CurrentLogoName =>
            !string.IsNullOrEmpty(_currentChannel?.LogoUrl)
                ? Path.GetFileNameWithoutExtension(_currentChannel.LogoUrl)
                : null;

        /// <summary>Logo URL of the current channel (for overlay artwork).</summary>
        public string? CurrentLogoUrl => _currentChannel?.LogoUrl;

        #endregion

        #region Methods

        /// <summary>Update the active channel. Call before <see cref="BuildStreamUrl"/>.</summary>
        public void SetChannel(Channel channel)
        {
            _currentChannel = channel;
        }

        /// <summary>
        /// Reads current AppSettings, builds the playback URL, and updates
        /// <see cref="WindowTitle"/>. Returns the ready-to-play URL string.
        /// </summary>
        public string BuildStreamUrl()
        {
            if (_currentChannel == null) return string.Empty;

            bool unicast = AppSettings.Instance.Get<bool>(AppSettings.ImportExportWithCustomHost);
            string host = AppSettings.Instance.Get(AppSettings.UnicastHost) ?? string.Empty;
            string url = unicast
                ? $"{host}{_currentChannel.Url}"
                : $"{Constants.DefaultMulticastHost}{_currentChannel.Url}";

            WindowTitle = $"{_currentChannel.Name} - {url}";
            RaisePropertyChanged(nameof(CurrentChannelName));
            RaisePropertyChanged(nameof(CurrentLogoName));
            RaisePropertyChanged(nameof(CurrentLogoUrl));
            return url;
        }

        #endregion
    }
}
