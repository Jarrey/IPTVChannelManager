using M3UParser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace IPTVChannelManager
{
    public class ImportExportHelper
    {
        public static IEnumerable<Channel> ImportFromTxt(string filepath, string unicastHost = null)
        {
            string content = File.ReadAllText(filepath);
            if (string.IsNullOrWhiteSpace(content)) yield break;
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
                        url = RemoveHost(url, string.IsNullOrWhiteSpace(unicastHost) ? Constants.DefaultMulticastHost : unicastHost);
                        yield return new Channel(name, url, group ?? string.Empty);
                    }
                }
                yield break;
            }
        }

        public static IEnumerable<Channel> ImportFromM3u(string filepath, string unicastHost = null)
        {
            using (Stream file = new FileStream(filepath, FileMode.Open))
            {
                var playlist = M3uParser.GetFromStream(file);
                return playlist?.PlaylistEntries?.Select(p =>
                {
                    var url = RemoveHost(p.Uri, string.IsNullOrWhiteSpace(unicastHost) ? Constants.DefaultMulticastHost : unicastHost);
                    return new Channel(p.Name, url, p.Group ?? string.Empty) { Id = p.Id };
                });
            }
        }

        public static string ExportToTxt(IEnumerable<Channel> channels, bool useUnicastHost = true)
        {
            string unicastHost = AppSettings.Instance.Get(AppSettings.UnicastHost);
            var groupedChannels = channels.Where(c => !c.Ignore).GroupBy(c => c.Group);
            StringBuilder sb = new StringBuilder();
            foreach (var channelGroup in groupedChannels)
            {
                string group = channelGroup.Key;
                if (string.IsNullOrWhiteSpace(group)) group = "其他";
                sb.AppendLine($"{group},#genre#");
                foreach (var channel in channelGroup)
                {
                    sb.AppendLine($"{channel.Name},{AddHost(channel.Url, useUnicastHost ? unicastHost : Constants.DefaultMulticastHost)}");
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }

        public static string ExportToM3u(IEnumerable<Channel> channels, bool useUnicastHost = true)
        {
            string unicastHost = AppSettings.Instance.Get(AppSettings.UnicastHost);
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
                    sb.AppendLine($"#EXTINF:-1 tvg-id=\"{channel.Id}\" tvg-name=\"{channel.Name}\" tvg-logo=\"{string.Format(logoTemplate, channel.Logo)}\" group-title=\"{group}\",{channel.Name}");
                    sb.AppendLine($"{AddHost(channel.Url, useUnicastHost ? unicastHost : Constants.DefaultMulticastHost)}");
                }
            }
            return sb.ToString();
        }

        public static string RemoveHost(string url, string unicastHost)
        {
            return url.Replace(unicastHost, string.Empty);
        }

        public static string AddHost(string url, string unicastHost)
        {
            return $"{unicastHost}{url}";
        }
    }
}
