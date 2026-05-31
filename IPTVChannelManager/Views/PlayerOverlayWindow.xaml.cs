using System.Windows;
using IPTVChannelManager.ViewModels;

namespace IPTVChannelManager.Views
{
    /// <summary>
    /// Transparent overlay window rendered on top of the VLC VideoView.
    /// All logic lives in <see cref="PlayerOverlayViewModel"/>; this class
    /// only handles window-geometry concerns.
    /// </summary>
    public partial class PlayerOverlayWindow : Window
    {
        #region Constructor

        public PlayerOverlayWindow(PlayerOverlayViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }

        #endregion

        #region Methods

        // ── Window geometry (cannot live in a ViewModel) ──────────────────────

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
                Top = owner.Top + 30;
                Width = owner.ActualWidth;
                Height = owner.ActualHeight - 30;
            }
        }

        #endregion
    }
}