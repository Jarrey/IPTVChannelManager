using IPTVChannelManager.Common;

namespace IPTVChannelManager
{
    public sealed class AppSettings : AbstractSettings
    {
        #region Singleton
        private static AppSettings _instance;
        public static AppSettings Instance => _instance ??= new AppSettings();
        private AppSettings()
        {
        }
        #endregion Singleton

        public override string Name => nameof(AppSettings);

        #region Settings
        public const string ChannelGroups = nameof(ChannelGroups);
        public const string LogoUrlTemplate = nameof(LogoUrlTemplate);
        public const string EpgUrl = nameof(EpgUrl);
        public const string CustomHost = nameof(CustomHost);
        public const string ImportExportWithCustomHost = nameof(ImportExportWithCustomHost);

        protected override void InitSetting()
        {
            this[ChannelGroups] = Constants.DefaultChannelGroups;
            this[LogoUrlTemplate] = Constants.DefaultLogoUrlTemplate;
            this[EpgUrl] = Constants.DefaultEpgUrl;
            this[CustomHost] = string.Empty;
            this[ImportExportWithCustomHost] = true;
        }
        #endregion Settings
    }
}
