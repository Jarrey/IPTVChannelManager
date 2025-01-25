namespace IPTVChannelManager
{
    public class Constants
    {
        public const string AppName = "IPTVChannelManager";
        public const string DateFormat = "yyyy-MM-dd";
        public const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss.fff";
        public const string DateFormatInQuery = "yyyyMMdd";
        public const double Epsilon = 1e-10;
        public const string Spliter = ",，| ";
        public const string DefaultHost = "rtp://";

        public const string ChannelDB = "channeldb.json";

        #region Default Setting Values
        public const string DefaultChannelGroups = "上海,央视,卫视,上海标清,央视标清,卫视标清,购物,其他";
        public const string DefaultLogoUrlTemplate = @"https://live.fanmingming.cn/tv/{0}.png";
        public const string DefaultEpgUrl = @"https://live.fanmingming.cn/e.xml";
        #endregion
    }
}
