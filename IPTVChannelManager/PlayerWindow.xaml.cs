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
    /// PlayerWindow.xaml 的交互逻辑
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

        #region 覆盖层
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

            // 鼠标移动检测计时器（通过全局钩子触发）
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
        #endregion 覆盖层

        #region 鼠标钩子双击检测
        private const int WH_MOUSE_LL = 14;
        private const int WM_LBUTTONDOWN = 0x0201;

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
        private LowLevelMouseProc _mouseProc;  // 防止 GC 回收
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

                // 检查点击是否在本窗口范围内
                if (IsClickInsideThisWindow(hookStruct.pt))
                {
                    var now = DateTime.UtcNow;
                    var elapsed = (now - _lastClickTime).TotalMilliseconds;

                    if (elapsed <= GetDoubleClickTime())
                    {
                        _lastClickTime = DateTime.MinValue; // 重置，防止三击触发
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
            // 点击的窗口是本窗口本身或其子窗口（包括 VLC 渲染窗口）
            return clickedWnd == wndHelper.Handle || IsChild(wndHelper.Handle, clickedWnd);
        }
        #endregion 鼠标钩子双击检测

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

        #region 全屏切换
        private void PlayerWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (_isFullscreen && e.Key == Key.Escape)
                ToggleFullscreen();
        }

        private void ToggleFullscreen()
        {
            if (!_isFullscreen)
            {
                // 保存当前窗口状态
                _prevWindowState = WindowState;
                _prevResizeMode = ResizeMode;
                _prevLeft = Left;
                _prevTop = Top;
                _prevWidth = Width;
                _prevHeight = Height;

                // 隐藏自定义标题栏和边框
                SetTitleBarVisibility(false);

                // 进入全屏
                ResizeMode = ResizeMode.NoResize;
                WindowState = WindowState.Normal;   // 先还原，避免最大化切换不生效
                WindowState = WindowState.Maximized;
                _isFullscreen = true;
            }
            else
            {
                // 退出全屏，恢复原始状态
                ResizeMode = _prevResizeMode;
                WindowState = _prevWindowState;
                Left = _prevLeft;
                Top = _prevTop;
                Width = _prevWidth;
                Height = _prevHeight;

                // 恢复标题栏和边框
                SetTitleBarVisibility(true);
                _isFullscreen = false;
            }

            _overlay?.UpdateFullscreenIcon(_isFullscreen);
            _overlay?.SyncPosition(this);
        }

        /// <summary>
        /// 隐藏/显示 BaseWindow 模板中的标题栏和边框
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
                // 模板在 Maximized 时会设置 Margin="5"，全屏时清零
                g.Margin = visible ? new Thickness(5) : new Thickness(0);
            }

            if (Template.FindName("PART_WindowTitleBorder", this) is System.Windows.Controls.Border border)
            {
                border.BorderThickness = visible ? new Thickness(1) : new Thickness(0);
                border.Padding = visible ? new Thickness(0) : new Thickness(0);
            }

            // 全屏时移除 WindowChrome 的标题栏区域，退出时恢复
            var chrome = System.Windows.Shell.WindowChrome.GetWindowChrome(this);
            if (chrome != null)
            {
                chrome.CaptionHeight = visible ? 26 : 0;
                chrome.ResizeBorderThickness = visible ? new Thickness(5) : new Thickness(0);
            }
        }
        #endregion 全屏切换

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
