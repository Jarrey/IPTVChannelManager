using IPTVChannelManager.Common;
using System;
using System.Collections.ObjectModel;
using System.Windows;

namespace IPTVChannelManager
{
    /// <summary>
    /// Interaction logic for ScannerWindow.xaml
    /// </summary>
    public partial class ScannerWindow : BaseWindow
    {
        private PlayerWindow _playerWindow;

        public ScannerWindow(ObservableCollection<Channel> existingChannels)
        {
            InitializeComponent();
            var vm = new ScannerWindowViewModel(existingChannels);
            DataContext = vm;
            vm.PlayChannelRequested += OnPlayChannelRequested;

            // Auto-scroll log to latest entry
            vm.Logs.CollectionChanged += (s, e) =>
            {
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (LogListBox.Items.Count > 0)
                    {
                        LogListBox.ScrollIntoView(LogListBox.Items[LogListBox.Items.Count - 1]);
                    }
                });
            };
        }

        protected override void OnClosed(EventArgs e)
        {
            if (DataContext is ScannerWindowViewModel vm)
            {
                vm.Cleanup();
            }
            base.OnClosed(e);
        }

        private void OnPlayChannelRequested(Channel channel)
        {
            if (_playerWindow == null || _playerWindow.IsDisposed)
            {
                _playerWindow = new PlayerWindow();
                _playerWindow.Show();
            }
            else
            {
                _playerWindow.Activate();
            }
            _playerWindow.Title = $"{channel.Name} - {channel.Url}";
            _playerWindow.PlayNetworkStream(channel.Url, channel.Name);
        }
    }
}
