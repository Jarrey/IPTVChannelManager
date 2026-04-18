using System;
using System.Windows;
using System.Windows.Threading;

namespace IPTVChannelManager
{
    /// <summary>
    /// Transparent overlay window rendered on top of the VLC VideoView to work around the WPF Airspace limitation.
    /// All UI state is managed by <see cref="PlayerOverlayViewModel"/> via data binding.
    /// </summary>
    public partial class PlayerOverlayWindow : Window
    {
        private readonly DispatcherTimer _clockTimer;
        private readonly DispatcherTimer _hideTimer;

        /// <summary>The ViewModel that drives all bindings in this window.</summary>
        public PlayerOverlayViewModel VM { get; } = new PlayerOverlayViewModel();

        /// <summary>Fired when the fullscreen/restore button is clicked.</summary>
        public event Action? FullscreenToggleRequested;

        /// <summary>Fired when the mute button is clicked.</summary>
        public event Action? MuteToggleRequested;

        public PlayerOverlayWindow()
        {
            InitializeComponent();
            DataContext = VM;

            // Real-time clock, refreshed every second
            _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _clockTimer.Tick += (s, e) => VM.ClockText = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            _clockTimer.Start();

            // Auto-hide timer for the control bar
            _hideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            _hideTimer.Tick += (s, e) =>
            {
                _hideTimer.Stop();
                VM.ControlBarVisibility = Visibility.Collapsed;
            };
        }

        /// <summary>Show the control bar and reset the auto-hide timer.</summary>
        public void ShowControlBar()
        {
            VM.ControlBarVisibility = Visibility.Visible;
            _hideTimer.Stop();
            _hideTimer.Start();
        }

        /// <summary>Immediately hide the control bar.</summary>
        public void HideControlBar()
        {
            VM.ControlBarVisibility = Visibility.Collapsed;
            _hideTimer.Stop();
        }

        /// <summary>Set the channel name and logo displayed in the top-left info bar.</summary>
        public void SetChannelInfo(string channelName, string logoUrl)
            => VM.SetChannelInfo(channelName, logoUrl);

        /// <summary>Update the fullscreen button icon and top bar margin.</summary>
        public void UpdateFullscreenIcon(bool isFullscreen)
            => VM.SetFullscreen(isFullscreen);

        /// <summary>Update the mute button icon.</summary>
        public void UpdateMuteIcon(bool isMuted)
            => VM.SetMuted(isMuted);

        /// <summary>Update the media stream info label.</summary>
        public void UpdateMediaInfo(string videoCodec, string audioCodec, int audioChannels)
            => VM.SetMediaInfo(videoCodec, audioCodec, audioChannels);

        /// <summary>Sync overlay position and size to the owner window (supports multi-monitor).</summary>
        public void SyncPosition(Window owner)
        {
            if (owner.WindowState == WindowState.Maximized)
            {
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
            => FullscreenToggleRequested?.Invoke();

        private void MuteButton_Click(object sender, RoutedEventArgs e)
            => MuteToggleRequested?.Invoke();

        protected override void OnClosed(EventArgs e)
        {
            _clockTimer.Stop();
            _hideTimer.Stop();
            base.OnClosed(e);
        }
    }
}


