using IPTVChannelManager.Common;
using Microsoft.Win32;
using System.Windows.Input;

namespace IPTVChannelManager.ViewModels
{
    public class SettingWindowViewModel : BindableBase
    {
        #region Fields

        private string _channelGroups;
        private string _logoUrlTemplate;
        private string _epgUrl;
        private string _unicastHost;
        private int _epgRefreshIntervalHours;
        private string _externalPlayerPath;

        #endregion

        #region Constructor

        public SettingWindowViewModel()
        {
            ChannelGroups = AppSettings.Instance.Get(AppSettings.ChannelGroups);
            LogoUrlTemplate = AppSettings.Instance.Get(AppSettings.LogoUrlTemplate);
            EpgUrl = AppSettings.Instance.Get(AppSettings.EpgUrl);
            UnicastHost = AppSettings.Instance.Get(AppSettings.UnicastHost);
            EpgRefreshIntervalHours = AppSettings.Instance.Get<int>(AppSettings.EpgRefreshIntervalHours);
            ExternalPlayerPath = AppSettings.Instance.Get(AppSettings.ExternalPlayerPath) ?? string.Empty;

            // command
            BrowseExternalPlayerCommand = new DelegateCommand(() =>
            {
                var dlg = new OpenFileDialog
                {
                    Title = "Select External Player Executable",
                    Filter = "Executables (*.exe)|*.exe|All files (*.*)|*.*"
                };
                if (dlg.ShowDialog() == true)
                {
                    ExternalPlayerPath = dlg.FileName;
                }
            });
        }

        #endregion

        #region Properties
        public string ChannelGroups
        {
            get => _channelGroups;
            set
            {
                SetProperty(ref _channelGroups, value);
                AppSettings.Instance.Set(AppSettings.ChannelGroups, value);
            }
        }

        public string LogoUrlTemplate
        {
            get => _logoUrlTemplate;
            set
            {
                SetProperty(ref _logoUrlTemplate, value);
                AppSettings.Instance.Set(AppSettings.LogoUrlTemplate, value);
            }
        }

        public string EpgUrl
        {
            get => _epgUrl;
            set
            {
                SetProperty(ref _epgUrl, value);
                AppSettings.Instance.Set(AppSettings.EpgUrl, value);
            }
        }

        public string UnicastHost
        {
            get => _unicastHost;
            set
            {
                SetProperty(ref _unicastHost, value);
                AppSettings.Instance.Set(AppSettings.UnicastHost, value);
            }
        }

        public int EpgRefreshIntervalHours
        {
            get => _epgRefreshIntervalHours;
            set
            {
                SetProperty(ref _epgRefreshIntervalHours, value);
                AppSettings.Instance.Set(AppSettings.EpgRefreshIntervalHours, value);
            }
        }

        public string ExternalPlayerPath
        {
            get => _externalPlayerPath;
            set
            {
                SetProperty(ref _externalPlayerPath, value);
                AppSettings.Instance.Set(AppSettings.ExternalPlayerPath, value);
            }
        }
        #endregion Properties

        #region Commands
        public ICommand BrowseExternalPlayerCommand { get; }
        #endregion Commands
    }
}
