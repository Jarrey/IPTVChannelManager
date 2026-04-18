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
        private LibVLC _libVlc;
        private MediaPlayer _mediaPlayer;
        private PlayerOverlayWindow _overlay;
        private DispatcherTimer _mouseMoveTimer;

        private bool _isFullscreen;
        private WindowState _prevWindowState;
        private ResizeMode _prevResizeMode;
        private double _prevLeft, _prevTop, _prevWidth, _prevHeight;
        private bool _isMuted;
        private int _lastVolume = 100;

        public PlayerWindow()
        {
            InitializeComponent();
            Core.Initialize();
            _libVlc = new LibVLC();
            _mediaPlayer = new MediaPlayer(_libVlc);
            _mediaPlayer.EnableHardwareDecoding = true;
            VideoPlayer.MediaPlayer = _mediaPlayer;

            Loaded += PlayerWindow_Loaded;
            KeyDown += PlayerWindow_KeyDown;
            LocationChanged += (s, e) => _overlay?.SyncPosition(this);
            SizeChanged += (s, e) => _overlay?.SyncPosition(this);
            StateChanged += (s, e) => _overlay?.SyncPosition(this);
        }

        #region Properties
        public bool IsDisposed { get; private set; }
        #endregion Properties

        private void PlayerWindow_Loaded(object sender, RoutedEventArgs e)
        {
            InstallMouseHook();
            InitOverlay();
        }

        #region Overlay
        private void InitOverlay()
        {
            _overlay = new PlayerOverlayWindow { Owner = this };
            _overlay.FullscreenToggleRequested += ToggleFullscreen;
            _overlay.VolumeChanged += volume =>
            {
                if (_mediaPlayer != null)
                    _mediaPlayer.Volume = volume;
                _lastVolume = volume;
                if (_isMuted && volume > 0)
                {
                    _isMuted = false;
                    _overlay.UpdateMuteIcon(false);
                }
            };
            _overlay.MuteToggleRequested += () =>
            {
                _isMuted = !_isMuted;
                if (_mediaPlayer != null)
                    _mediaPlayer.Volume = _isMuted ? 0 : _lastVolume;
                _overlay.UpdateMuteIcon(_isMuted);
            };
            _overlay.SyncPosition(this);
            _overlay.Show();

            // Mouse movement detection timer (polled via global hook)
            _mouseMoveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _lastMousePos = GetCursorPos();
            _mouseMoveTimer.Tick += (s, e) =>
            {
                var pos = GetCursorPos();
                if (pos != _lastMousePos)
                {
                    _lastMousePos = pos;
                    if (IsClickInsideThisWindow(new POINT { x = (int)pos.X, y = (int)pos.Y }))
                    {
                        _overlay?.ShowControlBar();
                    }
                }
            };
            _mouseMoveTimer.Start();
        }

        private System.Windows.Point _lastMousePos;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(out POINT lpPoint);

        private static System.Windows.Point GetCursorPos()
        {
            GetCursorPos(out POINT pt);
            return new System.Windows.Point(pt.x, pt.y);
        }
        #endregion Overlay

        #region Low-level Mouse Hook - Double-click Detection
        private const int WH_MOUSE_LL = 14;
        private const int WM_LBUTTONDOWN = 0x0201;

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
            if (nCode >= 0 && (int)wParam == WM_LBUTTONDOWN)
            {
                var hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);

                // Check if click is within this window's bounds
                if (IsClickInsideThisWindow(hookStruct.pt))
                {
                    var now = DateTime.UtcNow;
                    var elapsed = (now - _lastClickTime).TotalMilliseconds;

                    if (elapsed <= GetDoubleClickTime())
                    {
                        _lastClickTime = DateTime.MinValue; // Reset to prevent triple-click trigger
                        Dispatcher.Invoke(ToggleFullscreen);
                    }
                    else
                    {
                        _lastClickTime = now;
                    }
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

        public void PlayNetworkStream(string streamUrl, string channelName = null, string logoUrl = null)
        {
            try
            {
                _overlay?.SetChannelInfo(channelName, logoUrl);
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

            _overlay?.UpdateFullscreenIcon(_isFullscreen);
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
            _mouseMoveTimer?.Stop();
            UninstallMouseHook();
            _overlay?.Close();
            _overlay = null;
            _mediaPlayer?.Stop();
            _mediaPlayer?.Dispose();
            _libVlc?.Dispose();
            IsDisposed = true;
        }
    }
}
