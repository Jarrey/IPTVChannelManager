using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using IPTVChannelManager.Common;
using IPTVChannelManager.Models;
using IPTVChannelManager.ViewModels;

namespace IPTVChannelManager.Views
{
    /// <summary>
    /// Non-modal, Topmost EPG guide window.  Always a singleton: call
    /// <see cref="ShowInstance"/> to open or bring it to the front.
    /// </summary>
    public partial class EpgGuideWindow : BaseWindow
    {
        #region Fields

        private static EpgGuideWindow? _instance;

        private readonly EpgGuideViewModel _vm;
        private bool _suppressScrollSync;

        #endregion

        #region Constructor

        private EpgGuideWindow(IEnumerable<Channel> channels)
        {
            _vm = new EpgGuideViewModel();
            DataContext = _vm;
            InitializeComponent();

            Loaded += async (s, e) =>
            {
                await _vm.LoadAsync(channels);
                // Scroll programme area so current time is centred
                ScrollToNow();
            };
        }

        #endregion

        #region Methods

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
                // Refresh channel list in ViewModel and reload
                (_instance.DataContext as EpgGuideViewModel)?.LoadAsync(channels);
                if (_instance.WindowState == WindowState.Minimized)
                    _instance.WindowState = WindowState.Normal;
                _instance.Activate();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _vm.Cleanup();
            base.OnClosed(e);
        }

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

        #endregion
    }
}
