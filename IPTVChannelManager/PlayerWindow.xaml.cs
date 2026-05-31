using IPTVChannelManager.Common;
using LibVLCSharp.Shared;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;

namespace IPTVChannelManager
{
    /// <summary>
    /// Code-behind for PlayerWindow.xaml
    /// </summary>
    public partial class PlayerWindow : BaseWindow, IDisposable
    {
        private static PlayerWindow? _instance;
        private LibVLC _libVlc;
        private MediaPlayer _mediaPlayer;
        private PlayerOverlayWindow _overlay;
        private PlayerOverlayViewModel _overlayVm;

        private bool _isFullscreen;
        private WindowState _prevWindowState;
        private ResizeMode _prevResizeMode;
        private double _prevLeft, _prevTop, _prevWidth, _prevHeight;
        private bool _isMuted;
        private int _lastVolume = 50;

        private Channel _currentChannel;
        private string? _currentChannelName;
        private string? _currentLogoName;
        private DispatcherTimer? _epgTimer;

        public static void ShowInstance(Channel channel)
        {
            if (_instance == null)
            {
                _instance = new PlayerWindow(channel);
                _instance.Closed += (s, e) => _instance = null;
                _instance.Show();
            }
            else
            {
                // Refresh channel list reference and rebuild
                _instance._currentChannel = channel;
                _instance.PlayNetworkStream();
                if (_instance.WindowState == WindowState.Minimized)
                    _instance.WindowState = WindowState.Normal;
                _instance.Activate();
            }
        }

        private PlayerWindow(Channel channel)
        {
            InitializeComponent();
            Core.Initialize();
            _libVlc = new LibVLC();
            _mediaPlayer = new MediaPlayer(_libVlc)
            {
                EnableHardwareDecoding = true,
                Volume = 50
            };
            VideoPlayer.MediaPlayer = _mediaPlayer;

            Loaded += PlayerWindow_Loaded;
            KeyDown += PlayerWindow_KeyDown;
            LocationChanged += (s, e) => _overlay?.SyncPosition(this);
            SizeChanged += (s, e) => _overlay?.SyncPosition(this);
            StateChanged += (s, e) => _overlay?.SyncPosition(this);
            Activated += (s, e) => { if (_overlay != null) _overlay.Topmost = true; };
            Deactivated += (s, e) => { if (_overlay != null) _overlay.Topmost = false; };

            _currentChannel = channel;
        }

        #region Properties
        public bool IsDisposed { get; private set; }
        #endregion Properties

        private void PlayerWindow_Loaded(object sender, RoutedEventArgs e)
        {
            InstallMouseHook();
            InitOverlay();
            PlayNetworkStream();
        }

        #region Overlay
        private void InitOverlay()
        {
            _overlayVm = new PlayerOverlayViewModel(
                toggleFullscreen: ToggleFullscreen,
                toggleMute: () =>
                {
                    _isMuted = !_isMuted;
                    if (_mediaPlayer != null)
                        _mediaPlayer.Volume = _isMuted ? 0 : _lastVolume;
                    _overlayVm.SetMuted(_isMuted);
                });

            // Volume slider is TwoWay-bound to VM.Volume — sync changes to VLC
            _overlayVm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName != nameof(PlayerOverlayViewModel.Volume)) return;
                int volume = _overlayVm.Volume;
                if (_mediaPlayer != null)
                    _mediaPlayer.Volume = volume;
                _lastVolume = volume;
                if (_isMuted && volume > 0)
                {
                    _isMuted = false;
                    _overlayVm.SetMuted(false);
                }
            };

            _overlay = new PlayerOverlayWindow(_overlayVm) { Owner = this };
            _overlay.SyncPosition(this);
            _overlay.Show();

            // EPG: refresh the current-programme display every 30 seconds
            _epgTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
            _epgTimer.Tick += (s, e) => RefreshEpg();
            _epgTimer.Start();
        }

        #endregion Overlay

        #region Low-level Mouse Hook - Double-click Detection
        private const int WH_MOUSE_LL = 14;
        private const int WM_MOUSEMOVE = 0x0200;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_MOUSEWHEEL = 0x020A;

        private POINT _lastMovePoint;
        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
        private LowLevelMouseProc _mouseProc;  // Keep reference to prevent GC collection
        private IntPtr _mouseHookHandle;
        private DateTime _lastClickTime = DateTime.MinValue;

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int x, y; }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern uint GetDoubleClickTime();

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(POINT point);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsChild(IntPtr hWndParent, IntPtr hWnd);

        private void InstallMouseHook()
        {
            _mouseProc = MouseHookCallback;
            using var process = Process.GetCurrentProcess();
            using var module = process.MainModule;
            _mouseHookHandle = SetWindowsHookEx(WH_MOUSE_LL, _mouseProc,
                GetModuleHandle(module.ModuleName), 0);
        }

        private void UninstallMouseHook()
        {
            if (_mouseHookHandle != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_mouseHookHandle);
                _mouseHookHandle = IntPtr.Zero;
            }
        }

        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                var hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);

                if ((int)wParam == WM_MOUSEMOVE && IsClickInsideThisWindow(hookStruct.pt))
                {
                    var pt = hookStruct.pt;
                    if (pt.x != _lastMovePoint.x || pt.y != _lastMovePoint.y)
                    {
                        _lastMovePoint = pt;
                        Dispatcher.BeginInvoke(() => _overlayVm?.ShowControlBar());
                    }
                }
                else if ((int)wParam == WM_LBUTTONDOWN && IsClickInsideThisWindow(hookStruct.pt))
                {
                    var now = DateTime.UtcNow;
                    var elapsed = (now - _lastClickTime).TotalMilliseconds;

                    if (elapsed <= GetDoubleClickTime())
                    {
                        _lastClickTime = DateTime.MinValue;
                        Dispatcher.Invoke(ToggleFullscreen);
                    }
                    else
                    {
                        _lastClickTime = now;
                    }
                }
                else if ((int)wParam == WM_MOUSEWHEEL && IsClickInsideThisWindow(hookStruct.pt))
                {
                    // High word of mouseData holds the wheel delta (±120 per notch)
                    int delta = (short)(hookStruct.mouseData >> 16);
                    Dispatcher.Invoke(() =>
                    {
                        if (_overlay == null) return;
                        const int step = 5;
                        _overlayVm.Volume = Math.Clamp(
                            _overlayVm.Volume + (delta > 0 ? step : -step), 0, 100);
                        _overlayVm.ShowControlBar();
                    });
                }
            }
            return CallNextHookEx(_mouseHookHandle, nCode, wParam, lParam);
        }

        private bool IsClickInsideThisWindow(POINT pt)
        {
            var wndHelper = new WindowInteropHelper(this);
            if (wndHelper.Handle == IntPtr.Zero) return false;

            IntPtr clickedWnd = WindowFromPoint(pt);
            // The clicked window is this window itself or a child window (including VLC render window)
            return clickedWnd == wndHelper.Handle || IsChild(wndHelper.Handle, clickedWnd);
        }
        #endregion Low-level Mouse Hook - Double-click Detection

        private void PlayNetworkStream()
        {
            try
            {
                if (_currentChannel == null || string.IsNullOrWhiteSpace(_currentChannel.Url)) return;
                bool unicastMulticast = AppSettings.Instance.Get<bool>(AppSettings.ImportExportWithCustomHost);
                string unicastHost = AppSettings.Instance.Get(AppSettings.UnicastHost);
                string streamUrl = unicastMulticast ? $"{unicastHost}{_currentChannel.Url}" : $"{Constants.DefaultMulticastHost}{_currentChannel.Url}";
                Title = $"{_currentChannel.Name} - {streamUrl}";
                _overlayVm?.SetChannelInfo(_currentChannel.Name, _currentChannel.LogoUrl);
                _currentChannelName = _currentChannel.Name;
                // Logo filename (without extension) often matches EPG display name
                _currentLogoName = !string.IsNullOrEmpty(_currentChannel.LogoUrl)
                    ? System.IO.Path.GetFileNameWithoutExtension(_currentChannel.LogoUrl)
                    : null;
                RefreshEpg();
                using (var media = new Media(_libVlc, new Uri(streamUrl)))
                {
                    _mediaPlayer.Play(media);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshEpg()
        {
            if (_overlay == null) return;
            var prog = EpgService.Instance.GetCurrentProgramme(_currentChannelName, _currentLogoName);
            _overlayVm.EpgText = EpgService.FormatProgramme(prog);
        }

        #region Fullscreen Toggle

        private void PlayerWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (_isFullscreen && e.Key == Key.Escape)
                ToggleFullscreen();
        }

        private void ToggleFullscreen()
        {
            if (!_isFullscreen)
            {
                // Save current window state
                _prevWindowState = WindowState;
                _prevResizeMode = ResizeMode;
                _prevLeft = Left;
                _prevTop = Top;
                _prevWidth = Width;
                _prevHeight = Height;

                // Hide custom title bar and border
                SetTitleBarVisibility(false);

                // Enter fullscreen
                ResizeMode = ResizeMode.NoResize;
                WindowState = WindowState.Normal;   // Reset first to ensure Maximized transition works
                WindowState = WindowState.Maximized;
                _isFullscreen = true;
            }
            else
            {
                // Exit fullscreen, restore previous state
                ResizeMode = _prevResizeMode;
                WindowState = _prevWindowState;
                Left = _prevLeft;
                Top = _prevTop;
                Width = _prevWidth;
                Height = _prevHeight;

                // Restore title bar and border
                SetTitleBarVisibility(true);
                _isFullscreen = false;
            }

            _overlayVm?.SetFullscreen(_isFullscreen);
            _overlay?.SyncPosition(this);
        }

        /// <summary>
        /// Show or hide the title bar and border defined in BaseWindow's ControlTemplate.
        /// </summary>
        private void SetTitleBarVisibility(bool visible)
        {
            if (Template.FindName("PART_WindowTitle", this) is UIElement titleBar)
                titleBar.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;

            if (Template.FindName("PART_WindowTitleGrid", this) is FrameworkElement grid)
            {
                var g = (System.Windows.Controls.Grid)grid;
                if (g.RowDefinitions.Count > 0)
                    g.RowDefinitions[0].Height = visible ? new GridLength(30) : new GridLength(0);
                // The template sets Margin="5" when Maximized; clear it in fullscreen
                g.Margin = visible ? new Thickness(5) : new Thickness(0);
            }

            if (Template.FindName("PART_WindowTitleBorder", this) is System.Windows.Controls.Border border)
            {
                border.BorderThickness = visible ? new Thickness(1) : new Thickness(0);
                border.Padding = visible ? new Thickness(0) : new Thickness(0);
            }

            // Remove WindowChrome caption area in fullscreen; restore on exit
            var chrome = System.Windows.Shell.WindowChrome.GetWindowChrome(this);
            if (chrome != null)
            {
                chrome.CaptionHeight = visible ? 26 : 0;
                chrome.ResizeBorderThickness = visible ? new Thickness(5) : new Thickness(0);
            }
        }
        #endregion Fullscreen Toggle

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            Dispose();
        }

        public void Dispose()
        {
            _epgTimer?.Stop();
            UninstallMouseHook();
            _overlay?.Close();
            _overlay = null;
            _overlayVm = null;
            _mediaPlayer?.Stop();
            _mediaPlayer?.Dispose();
            _libVlc?.Dispose();
            Loaded -= PlayerWindow_Loaded;
            KeyDown -= PlayerWindow_KeyDown;
            IsDisposed = true;
        }
    }
}
