using IPTVChannelManager.Common;

namespace IPTVChannelManager
{
    public class SettingWindowViewModel : BindableBase
    {
        private string _channelGroups;
        private string _logoUrlTemplate;
        private string _epgUrl;
        private string _customHost;
        private string _playerPath;

        public SettingWindowViewModel()
        {
            ChannelGroups = AppSettings.Instance.Get(AppSettings.ChannelGroups);
            LogoUrlTemplate = AppSettings.Instance.Get(AppSettings.LogoUrlTemplate);
            EpgUrl = AppSettings.Instance.Get(AppSettings.EpgUrl);
            CustomHost = AppSettings.Instance.Get(AppSettings.CustomHost);
            PlayerPath = AppSettings.Instance.Get(AppSettings.PlayerPath);
        }

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

        public string CustomHost
        {
            get => _customHost;
            set
            {
                SetProperty(ref _customHost, value);
                AppSettings.Instance.Set(AppSettings.CustomHost, value);
            }
        }

        public string PlayerPath
        {
            get => _playerPath;
            set
            {
                SetProperty(ref _playerPath, value);
                AppSettings.Instance.Set(AppSettings.PlayerPath, value);
            }
        }
        #endregion Properties
    }
}
