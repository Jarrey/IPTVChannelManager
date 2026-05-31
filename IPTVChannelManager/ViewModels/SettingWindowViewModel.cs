using IPTVChannelManager.Common;

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

        #endregion

        #region Constructor

        public SettingWindowViewModel()
        {
            ChannelGroups            = AppSettings.Instance.Get(AppSettings.ChannelGroups);
            LogoUrlTemplate          = AppSettings.Instance.Get(AppSettings.LogoUrlTemplate);
            EpgUrl                   = AppSettings.Instance.Get(AppSettings.EpgUrl);
            UnicastHost              = AppSettings.Instance.Get(AppSettings.UnicastHost);
            EpgRefreshIntervalHours  = AppSettings.Instance.Get<int>(AppSettings.EpgRefreshIntervalHours);
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
        #endregion Properties
    }
}
