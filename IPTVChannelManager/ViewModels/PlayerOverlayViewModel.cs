using System;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using IPTVChannelManager.Common;

namespace IPTVChannelManager.ViewModels
{
    /// <summary>
    /// ViewModel for PlayerOverlayWindow. All UI state is exposed as bindable properties.
    /// </summary>
    public class PlayerOverlayViewModel : BindableBase
    {
        #region Fields

        private string _clockText = DateTime.Now.ToString(Constants.OverlayClockFormat);
        private string _channelName = string.Empty;
        private BitmapImage? _channelLogo;
        private Thickness _topInfoBarMargin = new Thickness(Constants.OverlayTopBarMarginHorizontal, Constants.OverlayTopBarMarginTopNormal, Constants.OverlayTopBarMarginHorizontal, 0);
        private Visibility _controlBarVisibility = Visibility.Collapsed;
        private string _fullscreenIcon = Constants.OverlayIconFullscreen;
        private string _fullscreenTooltip = Constants.OverlayTooltipFullscreen;
        private int _lastVolume = Constants.OverlayDefaultVolume;
        private int _volume = Constants.OverlayDefaultVolume;
        private string _volumeIcon = Constants.OverlayIconVolume;
        private bool _isMuted;
        private string _epgText = string.Empty;
        private readonly DispatcherTimer _clockTimer;
        private readonly DispatcherTimer _hideTimer;

        #endregion

        #region Constructor

        /// <summary>
        /// Commands require external actions injected at construction time so the
        /// ViewModel stays decoupled from the host window.
        /// </summary>
        public PlayerOverlayViewModel(Action toggleFullscreen)
        {
            ToggleFullscreenCommand  = new DelegateCommand(toggleFullscreen);
            ToggleMuteCommand        = new DelegateCommand(ToggleMute);
            ShowControlBarCommand    = new DelegateCommand(ShowControlBar);
            CleanupCommand           = new DelegateCommand(Cleanup);

            _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(Constants.OverlayClockIntervalSeconds) };
            _clockTimer.Tick += (s, e) => ClockText = DateTime.Now.ToString(Constants.OverlayClockFormat);
            _clockTimer.Start();

            _hideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(Constants.OverlayHideDelaySeconds) };
            _hideTimer.Tick += (s, e) =>
            {
                _hideTimer.Stop();
                ControlBarVisibility = Visibility.Collapsed;
            };
        }

        #endregion

        #region Properties

        public string ClockText
        {
            get => _clockText;
            private set => SetProperty(ref _clockText, value);
        }

        public string ChannelName
        {
            get => _channelName;
            private set => SetProperty(ref _channelName, value);
        }

        public BitmapImage? ChannelLogo
        {
            get => _channelLogo;
            private set => SetProperty(ref _channelLogo, value);
        }

        public Thickness TopInfoBarMargin
        {
            get => _topInfoBarMargin;
            private set => SetProperty(ref _topInfoBarMargin, value);
        }

        public Visibility ControlBarVisibility
        {
            get => _controlBarVisibility;
            private set => SetProperty(ref _controlBarVisibility, value);
        }

        public string FullscreenIcon
        {
            get => _fullscreenIcon;
            private set => SetProperty(ref _fullscreenIcon, value);
        }

        public string FullscreenTooltip
        {
            get => _fullscreenTooltip;
            private set => SetProperty(ref _fullscreenTooltip, value);
        }

        /// <summary>Volume level 0–100.</summary>
        public int Volume
        {
            get => _volume;
            set
            {
                if (SetProperty(ref _volume, value))
                {
                    RaisePropertyChanged(nameof(VolumePctText));
                    // Auto-unmute when the user raises the volume
                    if (value > 0 && _isMuted)
                        IsMuted = false;
                    if (value > 0)
                        _lastVolume = value;
                }
            }
        }

        public string VolumePctText => $"{_volume}%";

        public string VolumeIcon
        {
            get => _volumeIcon;
            private set => SetProperty(ref _volumeIcon, value);
        }

        /// <summary>Whether audio is muted. Drives <see cref="VolumeIcon"/> automatically.</summary>
        public bool IsMuted
        {
            get => _isMuted;
            set
            {
                if (SetProperty(ref _isMuted, value))
                    VolumeIcon = value ? Constants.OverlayIconMute : Constants.OverlayIconVolume;
            }
        }

        /// <summary>Currently-airing programme title and time, e.g. "新闻联播  12:00 – 13:00".</summary>
        public string EpgText
        {
            get => _epgText;
            set => SetProperty(ref _epgText, value);
        }

        #endregion

        #region Commands

        /// <summary>Command bound to the fullscreen/restore button.</summary>
        public DelegateCommand ToggleFullscreenCommand { get; }

        /// <summary>Command bound to the mute button.</summary>
        public DelegateCommand ToggleMuteCommand { get; }

        /// <summary>Command bound to MouseEnter on the overlay — shows the control bar.</summary>
        public DelegateCommand ShowControlBarCommand { get; }

        /// <summary>Command bound to the Window.Closed event — stops background timers.</summary>
        public DelegateCommand CleanupCommand { get; }

        #endregion

        #region Methods

        /// <summary>Show the control bar and reset the auto-hide timer.</summary>
        public void ShowControlBar()
        {
            ControlBarVisibility = Visibility.Visible;
            _hideTimer.Stop();
            _hideTimer.Start();
        }

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
            FullscreenIcon = isFullscreen ? Constants.OverlayIconRestore : Constants.OverlayIconFullscreen;
            FullscreenTooltip = isFullscreen ? Constants.OverlayTooltipRestore : Constants.OverlayTooltipFullscreen;
            // In fullscreen the title bar is hidden — no need to offset the top bar
            TopInfoBarMargin = isFullscreen
                ? new Thickness(Constants.OverlayTopBarMarginHorizontal, Constants.OverlayTopBarMarginTopFullscreen, Constants.OverlayTopBarMarginHorizontal, 0)
                : new Thickness(Constants.OverlayTopBarMarginHorizontal, Constants.OverlayTopBarMarginTopNormal, Constants.OverlayTopBarMarginHorizontal, 0);
        }

        private void ToggleMute()
        {
            if (IsMuted)
            {
                // Unmute: restore the last audible volume
                IsMuted = false;
                Volume = Math.Max(_lastVolume, 1);
            }
            else
            {
                // Mute: save current volume
                if (Volume > 0) _lastVolume = Volume;
                IsMuted = true;
            }
        }

        /// <summary>Immediately hide the control bar.</summary>
        private void HideControlBar()
        {
            ControlBarVisibility = Visibility.Collapsed;
            _hideTimer.Stop();
        }

        /// <summary>Stop all background timers. Called via <see cref="CleanupCommand"/> when the overlay window closes.</summary>
        private void Cleanup()
        {
            _clockTimer.Stop();
            _hideTimer.Stop();
        }

        #endregion
    }
}
