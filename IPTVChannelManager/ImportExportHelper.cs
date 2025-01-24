using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace IPTVChannelManager
{
    public class ImportExportHelper
    {
        private const string DefaultHost = "rtp://";
        public static IEnumerable<Channel> ImportChannelsFromTxt(string content, string customHost = null)
        {
            using (var reader = new StringReader(content))
            {
                string line = null;
                string group = null;
                while ((line = reader.ReadLine()) != null)
                {
                    line = line?.Trim();
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    if (line?.Contains("#") == true)
                    {
                        group = line.Split("#".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)?.FirstOrDefault();
                        continue;
                    }
                    var info = line.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                    if (info?.Length > 1)
                    {
                        var name = info[0]?.Trim() ?? string.Empty;
                        var url = info[1]?.Trim() ?? string.Empty;
                        url = RemoveHost(url, string.IsNullOrWhiteSpace(customHost) ? DefaultHost : customHost);
                        yield return new Channel(name, url, group ?? string.Empty);
                    }
                }
                yield break;
            }
        }

        public static string ExportToTxt(IEnumerable<Channel> channels, bool useCustomHost = true)
        {
            string customHost = AppSettings.Instance.Get(AppSettings.CustomHost);

            var groupedChannels = channels.Where(c => !c.Ignore).GroupBy(c => c.Group);
            StringBuilder sb = new StringBuilder();
            foreach (var channelGroup in groupedChannels)
            {
                string group = channelGroup.Key;
                if (string.IsNullOrWhiteSpace(group)) group = "其他";
                sb.AppendLine($"{group},#genre#");
                foreach (var channel in channelGroup)
                {
                    sb.AppendLine($"{channel.Name},{AddHost(channel.Url, useCustomHost ? customHost : DefaultHost)}");
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }

        public static string ExportToM3u(IEnumerable<Channel> channels, bool useCustomHost = true)
        {
            string customHost = AppSettings.Instance.Get(AppSettings.CustomHost);
            string epg = AppSettings.Instance.Get(AppSettings.EpgUrl);
            string logoTemplate = AppSettings.Instance.Get(AppSettings.LogoUrlTemplate);

            var groupedChannels = channels.Where(c => !c.Ignore).GroupBy(c => c.Group);
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"#EXTM3U x-tvg-url=\"{epg}\"");
            foreach (var channelGroup in groupedChannels)
            {
                string group = channelGroup.Key;
                if (string.IsNullOrWhiteSpace(group)) group = "其他";
                sb.AppendLine($"#{group}");
                foreach (var channel in channelGroup)
                {
                    sb.AppendLine($"#EXTINF:-1 tvg-id=\"{channel.Logo}\" tvg-name=\"{channel.Name}\" tvg-logo=\"{string.Format(logoTemplate, channel.Logo)}\" group-title=\"{group}\",{channel.Name}");
                    sb.AppendLine($"{AddHost(channel.Url, useCustomHost ? customHost : DefaultHost)}");
                }
            }
            return sb.ToString();
        }

        private static string RemoveHost(string url, string customHost)
        {
            return url.Replace(customHost, string.Empty);
        }

        private static string AddHost(string url, string customHost)
        {
            return $"{customHost}{url}";
        }
    }
}
