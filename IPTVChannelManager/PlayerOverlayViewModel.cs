using IPTVChannelManager.Common;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media.Imaging;

namespace IPTVChannelManager
{
    /// <summary>
    /// ViewModel for PlayerOverlayWindow. All UI state is exposed as bindable properties.
    /// </summary>
    public class PlayerOverlayViewModel : BindableBase
    {
        // ── Clock ────────────────────────────────────────────────────────────

        private string _clockText = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        public string ClockText
        {
            get => _clockText;
            set => SetProperty(ref _clockText, value);
        }

        // ── Channel info ─────────────────────────────────────────────────────

        private string _channelName = string.Empty;
        public string ChannelName
        {
            get => _channelName;
            set => SetProperty(ref _channelName, value);
        }

        private BitmapImage? _channelLogo;
        public BitmapImage? ChannelLogo
        {
            get => _channelLogo;
            set => SetProperty(ref _channelLogo, value);
        }

        private Thickness _topInfoBarMargin = new Thickness(15, 40, 15, 0);
        public Thickness TopInfoBarMargin
        {
            get => _topInfoBarMargin;
            set => SetProperty(ref _topInfoBarMargin, value);
        }

        // ── Control bar visibility ────────────────────────────────────────────

        private Visibility _controlBarVisibility = Visibility.Collapsed;
        public Visibility ControlBarVisibility
        {
            get => _controlBarVisibility;
            set => SetProperty(ref _controlBarVisibility, value);
        }

        // ── Fullscreen ────────────────────────────────────────────────────────

        private string _fullscreenIcon = "\u26F6";
        public string FullscreenIcon
        {
            get => _fullscreenIcon;
            set => SetProperty(ref _fullscreenIcon, value);
        }

        private string _fullscreenTooltip = "Fullscreen";
        public string FullscreenTooltip
        {
            get => _fullscreenTooltip;
            set => SetProperty(ref _fullscreenTooltip, value);
        }

        // ── Volume ────────────────────────────────────────────────────────────

        private int _volume = 50;
        /// <summary>Volume level 0–100.</summary>
        public int Volume
        {
            get => _volume;
            set
            {
                if (SetProperty(ref _volume, value))
                    RaisePropertyChanged(nameof(VolumePctText));
            }
        }

        public string VolumePctText => $"{_volume}%";

        private string _volumeIcon = "\U0001F50A";
        public string VolumeIcon
        {
            get => _volumeIcon;
            set => SetProperty(ref _volumeIcon, value);
        }

        // ── Media info ────────────────────────────────────────────────────────

        private string _mediaInfoText = string.Empty;
        public string MediaInfoText
        {
            get => _mediaInfoText;
            set => SetProperty(ref _mediaInfoText, value);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// Update all channel display properties from a channel object.
        /// </summary>
        public void SetChannelInfo(string channelName, string logoUrl)
        {
            ChannelName = channelName ?? string.Empty;
            ChannelLogo = null;
            try
            {
                if (!string.IsNullOrEmpty(logoUrl))
                {
                    var uri = new Uri(logoUrl, UriKind.RelativeOrAbsolute);
                    ChannelLogo = new BitmapImage(uri);
                }
            }
            catch
            {
                // Logo load failure should not affect playback
            }
        }

        /// <summary>
        /// Toggle fullscreen icon and top bar margin.
        /// </summary>
        public void SetFullscreen(bool isFullscreen)
        {
            FullscreenIcon = isFullscreen ? "\u29C9" : "\u26F6";
            FullscreenTooltip = isFullscreen ? "Restore" : "Fullscreen";
            // In fullscreen the title bar is hidden — no need to offset the top bar
            TopInfoBarMargin = isFullscreen
                ? new Thickness(15, 10, 15, 0)
                : new Thickness(15, 40, 15, 0);
        }

        /// <summary>
        /// Toggle mute icon.
        /// </summary>
        public void SetMuted(bool isMuted)
        {
            VolumeIcon = isMuted ? "\U0001F507" : "\U0001F50A";
        }

        /// <summary>
        /// Build and set the media stream info string.
        /// </summary>
        public void SetMediaInfo(string videoCodec, string audioCodec, int audioChannels)
        {
            string channelLabel = audioChannels switch
            {
                1 => "Mono",
                2 => "Stereo",
                6 => "5.1",
                8 => "7.1",
                _ => audioChannels > 0 ? $"{audioChannels}ch" : string.Empty
            };

            var parts = new List<string>();
            if (!string.IsNullOrEmpty(videoCodec))   parts.Add($"Video: {videoCodec}");
            if (!string.IsNullOrEmpty(audioCodec))   parts.Add($"Audio: {audioCodec}");
            if (!string.IsNullOrEmpty(channelLabel)) parts.Add(channelLabel);

            MediaInfoText = string.Join("  |  ", parts);
        }
    }
}
