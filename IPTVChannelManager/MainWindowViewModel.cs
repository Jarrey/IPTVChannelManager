using IPTVChannelManager.Common;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;

namespace IPTVChannelManager
{
    public class MainWindowViewModel : BindableBase
    {
        private static string DBPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Constants.AppName, Constants.ChannelDB);
        private ObservableCollection<Channel> _channels;
        private ObservableCollection<Channel> _newChannels;
        private ObservableCollection<Channel> _oldChannels;
        private string[] _channelGroups;
        private bool _processCustomHost;
        private string _customHost;
        private int _activeCount;
        private PlayerWindow _playerWindow;

        public MainWindowViewModel()
        {
            NewChannels = new ObservableCollection<Channel>();
            OldChannels = new ObservableCollection<Channel>();
            ChannelGroups = AppSettings.Instance.Get(AppSettings.ChannelGroups)?.Split(Constants.Spliter.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            ProcessCustomHost = AppSettings.Instance.Get<bool>(AppSettings.ImportExportWithCustomHost);
            CustomHost = AppSettings.Instance.Get(AppSettings.CustomHost);
            AppSettings.Instance.SettingChanged += SettingChanged;
            Channels = LoadChannelDB() ?? new ObservableCollection<Channel>();

            ImportChannelsCommand = new DelegateCommand(ImportChannels);
            ReloadChannelsDBCommand = new DelegateCommand(() => Channels = LoadChannelDB() ?? new ObservableCollection<Channel>());
            SaveChannelsDBCommand = new DelegateCommand(SaveChannelsDB);
            RemoveDBChannelCommand = new DelegateCommand<Channel>(RemoveDBChannel);
            AddCommand = new DelegateCommand<Channel>(AddChannel);
            RemoveCommand = new DelegateCommand<Channel>(RemoveChannel);
            ExportToTxtCommand = new DelegateCommand(ExportToTxt);
            ExportToM3uCommand = new DelegateCommand(ExportToM3u);
            PlayCommand = new DelegateCommand<Channel>(Play);
            AddAllCommand = new DelegateCommand(AddAllChannels);
            RemoveAllCommand = new DelegateCommand(RemoveAllChannels);
        }

        #region Properties
        public ObservableCollection<Channel> Channels
        {
            get => _channels;
            set => SetProperty(ref _channels, value);
        }

        public string Count => $"({Channels?.Where(c => !c.Ignore)?.Count()}/{Channels?.Count})";

        public void RaiseCountChange() => RaisePropertyChanged(nameof(Count));

        public ObservableCollection<Channel> NewChannels
        {
            get => _newChannels;
            set => SetProperty(ref _newChannels, value);
        }

        public ObservableCollection<Channel> OldChannels
        {
            get => _oldChannels;
            set => SetProperty(ref _oldChannels, value);
        }

        public string[] ChannelGroups
        {
            get => _channelGroups;
            set => SetProperty(ref _channelGroups, value);
        }

        public bool ProcessCustomHost
        {
            get => _processCustomHost;
            set
            {
                SetProperty(ref _processCustomHost, value);
                AppSettings.Instance.Set(AppSettings.ImportExportWithCustomHost, value);
            }
        }

        public string CustomHost
        {
            get => _customHost;
            set => SetProperty(ref _customHost, value);
        }

        #endregion Properties

        #region Filter Properties
        private string _filterText;
        public string FilterText
        {
            get => _filterText;
            set
            {
                _filterText = value;
                RaisePropertyChanged(nameof(Filter));
                RaisePropertyChanged(nameof(HasFilterText));
                RaisePropertyChanged(nameof(FilterText));
            }
        }

        public bool HasFilterText
        {
            get => !string.IsNullOrWhiteSpace(FilterText);
        }

        public Predicate<object> Filter
        {
            get => new((obj) =>
            {
                if (obj is Channel channel)
                {
                    return string.IsNullOrWhiteSpace(FilterText) ||
                           channel.Name?.Contains(FilterText, StringComparison.InvariantCultureIgnoreCase) == true ||
                           channel.Logo?.StartsWith(FilterText, StringComparison.InvariantCultureIgnoreCase) == true ||
                           channel.Url?.StartsWith(FilterText, StringComparison.InvariantCultureIgnoreCase) == true ||
                           channel.Group?.StartsWith(FilterText, StringComparison.InvariantCultureIgnoreCase) == true;
                }
                return false;
            });
        }
        #endregion Filter Properties

        #region Commands
        public ICommand ImportChannelsCommand { get; }
        public ICommand ReloadChannelsDBCommand { get; }
        public ICommand SaveChannelsDBCommand { get; }
        public ICommand RemoveDBChannelCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand AddAllCommand { get; }
        public ICommand RemoveCommand { get; }
        public ICommand RemoveAllCommand { get; }
        public ICommand ExportToTxtCommand { get; }
        public ICommand ExportToM3uCommand { get; }
        public ICommand PlayCommand { get; }
        #endregion Commands

        #region Methods
        private void SettingChanged(object? sender, (string, object) e)
        {
            ChannelGroups = AppSettings.Instance.Get(AppSettings.ChannelGroups)?.Split(Constants.Spliter.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            CustomHost = AppSettings.Instance.Get(AppSettings.CustomHost);
        }

        private ObservableCollection<Channel> LoadChannelDB()
        {
            if (File.Exists(DBPath))
            {
                try
                {
                    List<Channel> channelDB = JsonConvert.DeserializeObject<List<Channel>>(File.ReadAllText(DBPath));
                    List<string> groupOrder = ChannelGroups.ToList();
                    return new ObservableCollection<Channel>(channelDB?.OrderBy(c => string.IsNullOrWhiteSpace(c.Group) ? int.MaxValue : groupOrder.IndexOf(c.Group))?.ToList());
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{ex.Message}, {ex}");
                }
            }
            return null;
        }

        private void ImportChannels()
        {
            try
            {
                using (CommonFileDialog openFileDialog = new CommonOpenFileDialog()
                {
                    Title = nameof(ImportChannels),
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    EnsureFileExists = true,
                    EnsurePathExists = true,
                    EnsureReadOnly = false,
                    EnsureValidNames = true,
                    Multiselect = false,
                    ShowPlacesList = true
                })
                {
                    if (openFileDialog.ShowDialog() == CommonFileDialogResult.Ok && File.Exists(openFileDialog.FileName))
                    {
                        var channels = ImportExportHelper.ImportFromTxt(File.ReadAllText(openFileDialog.FileName), ProcessCustomHost ? CustomHost : null);
                        NewChannels.Clear();
                        foreach (var channel in channels)
                        {
                            if (Channels.All(c => c.Url != channel.Url))
                            {
                                NewChannels.Add(channel);
                            }
                        }
                        OldChannels.Clear();
                        foreach (var channel in Channels)
                        {
                            if (channels.All(c => c.Url != channel.Url))
                            {
                                OldChannels.Add(channel);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}, {ex}");
            }
        }

        private void ExportToM3u()
        {
            try
            {
                using (CommonFileDialog saveFileDialog = new CommonSaveFileDialog()
                {
                    Title = nameof(ExportToM3u),
                    DefaultFileName = "IPTV.m3u",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    EnsurePathExists = true,
                    EnsureReadOnly = false,
                    EnsureValidNames = true,
                    ShowPlacesList = true
                })
                {
                    saveFileDialog.Filters.Add(new CommonFileDialogFilter("m3u Files", "*.m3u"));
                    if (saveFileDialog.ShowDialog() == CommonFileDialogResult.Ok)
                    {
                        string channels = ImportExportHelper.ExportToM3u(Channels, ProcessCustomHost);
                        File.WriteAllText(saveFileDialog.FileName, channels);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}, {ex}");
            }
        }

        private void ExportToTxt()
        {
            try
            {
                using (CommonFileDialog saveFileDialog = new CommonSaveFileDialog()
                {
                    Title = nameof(ExportToTxt),
                    DefaultFileName = "IPTV.txt",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    EnsurePathExists = true,
                    EnsureReadOnly = false,
                    EnsureValidNames = true,
                    ShowPlacesList = true
                })
                {
                    saveFileDialog.Filters.Add(new CommonFileDialogFilter("txt Files", "*.txt"));
                    if (saveFileDialog.ShowDialog() == CommonFileDialogResult.Ok)
                    {
                        string channels = ImportExportHelper.ExportToTxt(Channels, ProcessCustomHost);
                        File.WriteAllText(saveFileDialog.FileName, channels);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}, {ex}");
            }
        }

        private void SaveChannelsDB()
        {
            try
            {
                string channelDB = JsonConvert.SerializeObject(Channels);
                File.WriteAllText(DBPath, channelDB);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}, {ex}");
            }
        }

        private void RemoveDBChannel(Channel channel)
        {
            if (channel == null) return;
            if (Channels.Any(c => c.Url == channel.Url) || Channels.Contains(channel))
            {
                Channels.Remove(channel);
            }
            if (NewChannels.Any(c => c.Url == channel.Url) || NewChannels.Contains(channel))
            {
                NewChannels.Remove(channel);
            }
            if (OldChannels.Any(c => c.Url == channel.Url) || OldChannels.Contains(channel))
            {
                OldChannels.Remove(channel);
            }
        }

        private void AddChannel(Channel channel)
        {
            if (channel == null) return;
            if (Channels.Any(c => c.Url == channel.Url) || Channels.Contains(channel)) return;
            Channels.Add(channel);
        }

        private void AddAllChannels()
        {
            foreach (var channel in NewChannels)
            {
                AddChannel(channel);
            }
        }

        private void RemoveChannel(Channel channel)
        {
            if (channel == null) return;
            if (Channels.All(c => c.Url != channel.Url) || !Channels.Contains(channel)) return;
            Channels.Remove(channel);
        }

        private void RemoveAllChannels()
        {
            foreach (var channel in OldChannels)
            {
                RemoveChannel(channel);
            }
        }

        private void Play(Channel channel)
        {
            if (channel == null || string.IsNullOrWhiteSpace(channel.Url)) return;
            try
            {
                string streamUrl = ProcessCustomHost ? $"{CustomHost}{channel.Url}" : $"{Constants.DefaultHost}{channel.Url}";
                if (_playerWindow == null || _playerWindow.IsDisposed)
                {
                    _playerWindow = new PlayerWindow();
                    _playerWindow.Show();
                }
                else
                {
                    _playerWindow.Activate();
                }
                _playerWindow.Title = channel.Name;
                _playerWindow.PlayNetworkStream(streamUrl);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}, {ex}");
            }
        }
        #endregion Methods
    }
}
