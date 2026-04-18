namespace IPTVChannelManager
{
    public class Constants
    {
        /// <summary>Application name.</summary>
        public const string AppName = "IPTVChannelManager";

        /// <summary>Date format used for display (e.g. "2024-01-01").</summary>
        public const string DateFormat = "yyyy-MM-dd";

        /// <summary>Date-time format used for display with milliseconds (e.g. "2024-01-01 12:00:00.000").</summary>
        public const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss.fff";

        /// <summary>Compact date format used in database queries (e.g. "20240101").</summary>
        public const string DateFormatInQuery = "yyyyMMdd";

        /// <summary>Tolerance value for floating-point comparisons.</summary>
        public const double Epsilon = 1e-10;

        /// <summary>Delimiter characters used when splitting channel-related strings.</summary>
        public const string Spliter = ",，| ";

        /// <summary>Default URL prefix for multicast streams.</summary>
        public const string DefaultMulticastHost = "rtp://";

        /// <summary>File name of the local channel database.</summary>
        public const string ChannelDB = "channeldb.json";

        #region Default Setting Values
        /// <summary>Comma-separated list of default channel group names.</summary>
        public const string DefaultChannelGroups = "上海,央视,卫视,上海标清,央视标清,卫视标清,购物,其他";

        /// <summary>URL template for fetching channel logos; {0} is replaced by the channel name.</summary>
        public const string DefaultLogoUrlTemplate = @"https://live.fanmingming.cn/tv/{0}.png";

        /// <summary>Default EPG (Electronic Programme Guide) data source URL.</summary>
        public const string DefaultEpgUrl = @"https://live.fanmingming.cn/e.xml";
        #endregion

        #region PlayerOverlay
        /// <summary>Interval in seconds at which the overlay clock is refreshed.</summary>
        public const int OverlayClockIntervalSeconds = 1;

        /// <summary>Delay in seconds before the control bar is automatically hidden after the last mouse move.</summary>
        public const int OverlayHideDelaySeconds = 10;

        /// <summary>DateTime format string used by the overlay clock display.</summary>
        public const string OverlayClockFormat = "HH:mm:ss";

        /// <summary>Default volume level (0–100) applied when the overlay is first created.</summary>
        public const int OverlayDefaultVolume = 50;

        /// <summary>Top margin of the info bar in normal (windowed) mode.</summary>
        public const double OverlayTopBarMarginTopNormal = 40;

        /// <summary>Top margin of the info bar in fullscreen mode (title bar is hidden, so smaller offset).</summary>
        public const double OverlayTopBarMarginTopFullscreen = 10;

        /// <summary>Left and right margin of the top info bar.</summary>
        public const double OverlayTopBarMarginHorizontal = 15;

        /// <summary>Unicode icon shown on the fullscreen button when in windowed mode (enter fullscreen).</summary>
        public const string OverlayIconFullscreen = "\u26F6";

        /// <summary>Unicode icon shown on the fullscreen button when in fullscreen mode (restore window).</summary>
        public const string OverlayIconRestore = "\u29C9";

        /// <summary>Tooltip text for the fullscreen button when in windowed mode.</summary>
        public const string OverlayTooltipFullscreen = "Fullscreen";

        /// <summary>Tooltip text for the fullscreen button when in fullscreen mode.</summary>
        public const string OverlayTooltipRestore = "Restore";

        /// <summary>Unicode icon shown on the volume button when audio is unmuted.</summary>
        public const string OverlayIconVolume = "\U0001F50A";

        /// <summary>Unicode icon shown on the volume button when audio is muted.</summary>
        public const string OverlayIconMute = "\U0001F507";
        #endregion
    }
}
