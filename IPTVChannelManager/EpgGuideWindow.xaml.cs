using IPTVChannelManager.Common;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace IPTVChannelManager
{
    /// <summary>
    /// Non-modal, Topmost EPG guide window.  Always a singleton: call
    /// <see cref="ShowInstance"/> to open or bring it to the front.
    /// </summary>
    public partial class EpgGuideWindow : BaseWindow
    {
        // ── Singleton ─────────────────────────────────────────────────────────
        private static EpgGuideWindow? _instance;

        public static void ShowInstance(IEnumerable<Channel> channels)
        {
            if (_instance == null)
            {
                _instance = new EpgGuideWindow(channels);
                _instance.Closed += (s, e) => _instance = null;
                _instance.Show();
            }
            else
            {
                // Refresh channel list reference and rebuild
                _instance._channels = channels;
                _ = (_instance.DataContext as EpgGuideViewModel)?.LoadAsync(channels);
                if (_instance.WindowState == WindowState.Minimized)
                    _instance.WindowState = WindowState.Normal;
                _instance.Activate();
            }
        }

        // ── Instance ──────────────────────────────────────────────────────────
        private readonly EpgGuideViewModel _vm;
        private IEnumerable<Channel> _channels;

        private EpgGuideWindow(IEnumerable<Channel> channels)
        {
            _channels = channels;
            _vm = new EpgGuideViewModel();
            DataContext = _vm;
            InitializeComponent();

            EpgService.Instance.CacheRefreshed += OnEpgCacheRefreshed;

            Loaded += async (s, e) =>
            {
                await _vm.LoadAsync(_channels);
                // Scroll programme area so current time is centred
                ScrollToNow();
            };
        }

        protected override void OnClosed(EventArgs e)
        {
            EpgService.Instance.CacheRefreshed -= OnEpgCacheRefreshed;
            _vm.Cleanup();
            base.OnClosed(e);
        }

        private void OnEpgCacheRefreshed(object? sender, EventArgs e)
        {
            // Fired on a background thread — marshal to UI thread.
            // Skip when a manual reload is already in progress (it will call LoadAsync itself).
            if (_reloadInProgress) return;
            Dispatcher.BeginInvoke(() => _ = _vm.LoadAsync(_channels));
        }

        // ── Reload button ─────────────────────────────────────────────────────

        private bool _reloadInProgress;

        private async void ReloadButton_Click(object sender, RoutedEventArgs e)
        {
            if (_reloadInProgress) return;
            _reloadInProgress = true;
            _vm.IsLoading = true;
            try
            {
                await EpgService.Instance.ForceReloadAsync();
                await _vm.LoadAsync(_channels);
            }
            finally
            {
                _vm.IsLoading = false;
                _reloadInProgress = false;
            }
        }

        // ── Scroll synchronisation ────────────────────────────────────────────

        private bool _suppressScrollSync;

        private void ProgrammeScroll_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (_suppressScrollSync) return;
            _suppressScrollSync = true;
            if (e.HorizontalChange != 0)
                TimeHeaderScroll.ScrollToHorizontalOffset(e.HorizontalOffset);
            if (e.VerticalChange != 0)
            {
                ChannelNamesScroll.ScrollToVerticalOffset(e.VerticalOffset);
                _vm.NowLabelTop = e.VerticalOffset;
            }
            _suppressScrollSync = false;
        }

        private void ChannelNamesScroll_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (_suppressScrollSync || e.VerticalChange == 0) return;
            _suppressScrollSync = true;
            ProgrammeScroll.ScrollToVerticalOffset(e.VerticalOffset);
            _vm.NowLabelTop = e.VerticalOffset;
            _suppressScrollSync = false;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// Scroll the programme area horizontally so the current-time line
        /// is roughly centred in the visible viewport.
        /// </summary>
        private void ScrollToNow()
        {
            double offset = _vm.TimeLineLeft - ProgrammeScroll.ActualWidth / 2;
            if (offset < 0) offset = 0;
            ProgrammeScroll.ScrollToHorizontalOffset(offset);
        }
    }
}
