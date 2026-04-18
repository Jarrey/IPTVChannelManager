using System;
using System.Windows;
using System.Windows.Threading;

namespace IPTVChannelManager
{
    /// <summary>
    /// Transparent overlay window rendered on top of the VLC VideoView to work around the WPF Airspace limitation.
    /// </summary>
    public partial class PlayerOverlayWindow : Window
    {
        private readonly DispatcherTimer _clockTimer;
        private readonly DispatcherTimer _hideTimer;

        /// <summary>全屏/还原按钮点击事件</summary>
        public event Action FullscreenToggleRequested;

        /// <summary>音量变化事件 (0-100)</summary>
        public event Action<int> VolumeChanged;

        /// <summary>静音切换事件</summary>
        public event Action MuteToggleRequested;

        public PlayerOverlayWindow()
        {
            InitializeComponent();

            // Real-time clock, refreshed every second
            _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _clockTimer.Tick += (s, e) => ClockText.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            _clockTimer.Start();
            ClockText.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // Auto-hide timer for the control bar
            _hideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            _hideTimer.Tick += (s, e) =>
            {
                _hideTimer.Stop();
                ControlBar.Visibility = Visibility.Collapsed;
            };

            // Hide control bar initially
            ControlBar.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Show the control bar and reset the auto-hide timer (called by the host window on mouse move).
        /// </summary>
        public void ShowControlBar()
        {
            ControlBar.Visibility = Visibility.Visible;
            _hideTimer.Stop();
            _hideTimer.Start();
        }

        /// <summary>
        /// Immediately hide the control bar.
        /// </summary>
        public void HideControlBar()
        {
            ControlBar.Visibility = Visibility.Collapsed;
            _hideTimer.Stop();
        }

        /// <summary>
        /// Set the channel name and logo displayed in the top-left info bar.
        /// </summary>
        public void SetChannelInfo(string channelName, string logoUrl)
        {
            ChannelNameText.Text = channelName ?? string.Empty;
            try
            {
                if (!string.IsNullOrEmpty(logoUrl))
                {
                    var uri = new Uri(logoUrl, UriKind.RelativeOrAbsolute);
                    ChannelLogoImage.Source = new System.Windows.Media.Imaging.BitmapImage(uri);
                }
            }
            catch
            {
                // Logo load failure should not affect playback
            }
        }

        /// <summary>
        /// Update the fullscreen button icon and adjust top bar margin for windowed vs fullscreen.
        /// </summary>
        public void UpdateFullscreenIcon(bool isFullscreen)
        {
            FullscreenIcon.Text = isFullscreen ? "\u29C9" : "\u26F6";
            FullscreenButton.ToolTip = isFullscreen ? "还原" : "全屏";
            // In fullscreen the title bar is hidden, so no need to offset the top bar
            TopInfoBar.Margin = isFullscreen
                ? new Thickness(15, 10, 15, 0)
                : new Thickness(15, 40, 15, 0);
        }

        /// <summary>
        /// Update the mute button icon.
        /// </summary>
        public void UpdateMuteIcon(bool isMuted)
        {
            VolumeIcon.Text = isMuted ? "\U0001F507" : "\U0001F50A";
        }

        /// <summary>
        /// Sync overlay position and size to the owner window (supports multi-monitor).
        /// </summary>
        public void SyncPosition(Window owner)
        {
            if (owner.WindowState == WindowState.Maximized)
            {
                // Get the screen that contains the owner window
                var hwnd = new System.Windows.Interop.WindowInteropHelper(owner).Handle;
                var screen = System.Windows.Forms.Screen.FromHandle(hwnd);
                var bounds = screen.Bounds;

                Left = bounds.Left;
                Top = bounds.Top;
                Width = bounds.Width;
                Height = bounds.Height;
            }
            else
            {
                Left = owner.Left;
                Top = owner.Top;
                Width = owner.ActualWidth;
                Height = owner.ActualHeight;
            }
        }

        private void FullscreenButton_Click(object sender, RoutedEventArgs e)
        {
            FullscreenToggleRequested?.Invoke();
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            VolumeChanged?.Invoke((int)e.NewValue);
        }

        private void MuteButton_Click(object sender, RoutedEventArgs e)
        {
            MuteToggleRequested?.Invoke();
        }

        protected override void OnClosed(EventArgs e)
        {
            _clockTimer.Stop();
            _hideTimer.Stop();
            base.OnClosed(e);
        }
    }
}
