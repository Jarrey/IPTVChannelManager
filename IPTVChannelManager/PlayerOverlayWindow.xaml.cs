using System;
using System.Windows;
using System.Windows.Threading;

namespace IPTVChannelManager
{
    /// <summary>
    /// PlayerOverlayWindow.xaml 的交互逻辑
    /// 透明覆盖窗口，解决 VLC HwndHost 的 Airspace 问题
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

            // 实时时钟，每秒刷新
            _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _clockTimer.Tick += (s, e) => ClockText.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            _clockTimer.Start();
            ClockText.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // 控制栏自动隐藏计时器
            _hideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            _hideTimer.Tick += (s, e) =>
            {
                _hideTimer.Stop();
                ControlBar.Visibility = Visibility.Collapsed;
            };

            // 初始隐藏控制栏
            ControlBar.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// 显示控制栏并重置自动隐藏计时器（由宿主窗口的鼠标移动调用）
        /// </summary>
        public void ShowControlBar()
        {
            ControlBar.Visibility = Visibility.Visible;
            _hideTimer.Stop();
            _hideTimer.Start();
        }

        /// <summary>
        /// 隐藏控制栏
        /// </summary>
        public void HideControlBar()
        {
            ControlBar.Visibility = Visibility.Collapsed;
            _hideTimer.Stop();
        }

        /// <summary>
        /// 设置频道信息
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
                // logo 加载失败不影响播放
            }
        }

        /// <summary>
        /// 更新全屏图标显示
        /// </summary>
        public void UpdateFullscreenIcon(bool isFullscreen)
        {
            FullscreenIcon.Text = isFullscreen ? "\u29C9" : "\u26F6";
            FullscreenButton.ToolTip = isFullscreen ? "还原" : "全屏";
            // 全屏时顶部不需要避让标题栏，窗口模式下需要
            TopInfoBar.Margin = isFullscreen
                ? new Thickness(15, 10, 15, 0)
                : new Thickness(15, 40, 15, 0);
        }

        /// <summary>
        /// 更新静音图标
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
