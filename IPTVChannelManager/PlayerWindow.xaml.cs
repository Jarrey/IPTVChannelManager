using IPTVChannelManager.Common;
using LibVLCSharp.Shared;
using System.Windows;
using System;

namespace IPTVChannelManager
{
    /// <summary>
    /// PlayerWindow.xaml 的交互逻辑
    /// </summary>
    public partial class PlayerWindow : BaseWindow, IDisposable
    {
        private LibVLC _libVlc;
        private MediaPlayer _mediaPlayer;

        public PlayerWindow()
        {
            InitializeComponent();
            Core.Initialize();
            _libVlc = new LibVLC();
            _mediaPlayer = new MediaPlayer(_libVlc);
            _mediaPlayer.EnableHardwareDecoding = true;
            VideoPlayer.MediaPlayer = _mediaPlayer;
        }

        #region Properties
        public bool IsDisposed { get; private set; }
        #endregion Properties

        public void PlayNetworkStream(string streamUrl)
        {
            try
            {
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

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            Dispose();
        }

        public void Dispose()
        {
            _mediaPlayer?.Stop();
            _mediaPlayer?.Dispose();
            _libVlc?.Dispose();
            IsDisposed = true;
        }
    }
}
