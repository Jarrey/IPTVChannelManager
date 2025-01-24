using IPTVChannelManager.Common;

namespace IPTVChannelManager
{
    public sealed class AppSettings : AbstractSettings
    {
        private static AppSettings _instance;
        public static AppSettings Instance => _instance ??= new AppSettings();
        private AppSettings()
        {
        }

        public override string Name => nameof(AppSettings);
        #region Non-persistenced Settings
        #endregion Non-persistenced Settings

        public override void LoadSetting(string scope = null)
        {
            base.LoadSetting(scope);
        }

        #region Settings
        public const string ChannelGroups = nameof(ChannelGroups);
        public const string LogoUrlTemplate = nameof(LogoUrlTemplate);
        public const string EpgUrl = nameof(EpgUrl);
        public const string CustomHost = nameof(CustomHost);
        public const string ImportExportWithCustomHost = nameof(ImportExportWithCustomHost);
        public const string PlayerPath = nameof(PlayerPath);

        protected override void InitSetting()
        {
            this[ChannelGroups] = Constants.DefaultChannelGroups;
            this[LogoUrlTemplate] = Constants.DefaultLogoUrlTemplate;
            this[EpgUrl] = Constants.DefaultEpgUrl;
            this[CustomHost] = Constants.DefaultCustomHost;
            this[ImportExportWithCustomHost] = true;
            this[PlayerPath] = string.Empty;
        }
        #endregion Settings
    }
}
