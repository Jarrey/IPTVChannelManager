using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using XmlTvSharp;

namespace IPTVChannelManager
{
    /// <summary>
    /// Singleton service that downloads, parses and caches EPG (XMLTV) data from
    /// <see cref="AppSettings.EpgUrl"/>. Refreshes automatically every
    /// <see cref="RefreshIntervalHours"/> hours.
    /// </summary>
    public sealed class EpgService
    {
        #region Singleton
        public static EpgService Instance { get; } = new EpgService();
        private EpgService() { }
        #endregion

        // ── Internal snapshot (replaced atomically on each refresh) ──────────

        private sealed class EpgCache
        {
            /// <summary>Normalized display name / channel id → EPG channel id.</summary>
            public Dictionary<string, string> NameToId { get; }
            /// <summary>EPG channel id → sorted list of programmes.</summary>
            public Dictionary<string, List<XmlTvProgramme>> ProgrammesById { get; }

            public EpgCache(
                Dictionary<string, string> nameToId,
                Dictionary<string, List<XmlTvProgramme>> programmesById)
            {
                NameToId      = nameToId;
                ProgrammesById = programmesById;
            }
        }

        private static readonly HttpClient _http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(120)
        };

        private volatile EpgCache? _cache;
        private bool _started;

        /// <summary>True once the first successful load has finished.</summary>
        public bool IsLoaded => _cache != null;

        /// <summary>
        /// Raised on a thread-pool thread after the cache is replaced with fresh data.
        /// Subscribers that update UI must marshal to the dispatcher.
        /// </summary>
        public event EventHandler? CacheRefreshed;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Immediately clears the current cache and re-downloads / re-parses the EPG.
        /// Fires <see cref="CacheRefreshed"/> on success, just like the background loop.
        /// </summary>
        public async Task ForceReloadAsync()
        {
            _cache = null;
            await LoadOnceAsync();
        }

        /// <summary>
        /// Start the background load/refresh loop.  Safe to call multiple times;
        /// only the first call has any effect.
        /// </summary>
        public void StartLoad()
        {
            if (_started) return;
            _started = true;
            AppSettings.Instance.SettingChanged += OnSettingChanged;
            _ = RunLoopAsync();
            _ = ScheduleDailyAtMidnightAsync();
            _ = ScheduleDailyAt1AmAsync();
        }

        private void OnSettingChanged(object? sender, (string key, object value) e)
        {
            if (e.key == AppSettings.EpgUrl)
            {
                _cache = null;
                _ = LoadOnceAsync();
            }
            // EpgRefreshIntervalHours change is picked up automatically on the next loop iteration
        }

        /// <summary>
        /// Return the currently-airing programme for <paramref name="channelName"/>.
        /// The lookup tries the exact name first, then the channel logo name if
        /// different (the logo field often matches the EPG display name exactly).
        /// Returns <see langword="null"/> when EPG data is not yet loaded, the
        /// channel is not found, or no programme is currently scheduled.
        /// </summary>
        public XmlTvProgramme? GetCurrentProgramme(string? channelName)
            => GetCurrentProgramme(channelName, null);

        /// <summary>
        /// Overload that also accepts the channel's logo name as a secondary
        /// lookup key (useful because the logo filename usually matches the EPG
        /// channel display name: e.g. "CCTV1").
        /// </summary>
        public XmlTvProgramme? GetCurrentProgramme(string? channelName, string? logoName)
        {
            var cache = _cache;
            if (cache == null) return null;

            var now = DateTimeOffset.Now;

            // Try primary name first, then logo name as fallback
            foreach (var name in new[] { channelName, logoName })
            {
                if (string.IsNullOrWhiteSpace(name)) continue;
                if (!cache.NameToId.TryGetValue(name, out var channelId)) continue;
                if (!cache.ProgrammesById.TryGetValue(channelId, out var list)) continue;

                var prog = FindCurrentProgramme(list, now);
                if (prog != null) return prog;
            }

            return null;
        }

        /// <summary>
        /// Return all programmes scheduled for today for the given channel.
        /// Returns an empty list when EPG data is not loaded or the channel is unknown.
        /// </summary>
        public IReadOnlyList<XmlTvProgramme> GetTodayProgrammes(string? channelName, string? logoName = null)
        {
            var cache = _cache;
            if (cache == null) return Array.Empty<XmlTvProgramme>();

            var todayStart  = new DateTimeOffset(DateTime.Today, TimeZoneInfo.Local.GetUtcOffset(DateTime.Today));
            var todayEnd    = todayStart.AddDays(1);

            foreach (var name in new[] { channelName, logoName })
            {
                if (string.IsNullOrWhiteSpace(name)) continue;
                if (!cache.NameToId.TryGetValue(name, out var channelId)) continue;
                if (!cache.ProgrammesById.TryGetValue(channelId, out var list)) continue;

                return list
                    .Where(p => p.Stop > todayStart && p.Start < todayEnd)
                    .OrderBy(p => p.Start)
                    .ToList();
            }

            return Array.Empty<XmlTvProgramme>();
        }

        /// <summary>Format a programme as "Title  HH:mm – HH:mm".</summary>
        public static string FormatProgramme(XmlTvProgramme? prog)
        {
            if (prog == null) return string.Empty;

            // Prefer Chinese title, fall back to first available language
            string title = prog.Titles.TryGetValue("zh", out var t) ? t
                         : prog.Titles.Values.FirstOrDefault() ?? string.Empty;

            return $"{title}  {prog.Start.ToLocalTime():HH:mm} – {prog.Stop.ToLocalTime():HH:mm}";
        }

        // ── Background loading ────────────────────────────────────────────────

        private async Task RunLoopAsync()
        {
            while (true)
            {
                await LoadOnceAsync();
                int hours = Math.Clamp(AppSettings.Instance.Get<int>(AppSettings.EpgRefreshIntervalHours), 1, 168);
                await Task.Delay(TimeSpan.FromHours(hours));
            }
        }

        /// <summary>Forces a full EPG reload every day at 00:00 local time so the guide always shows the new day's schedule.</summary>
        private async Task ScheduleDailyAtMidnightAsync()
        {
            while (true)
            {
                var nextMidnight = DateTime.Today.AddDays(1);   // tomorrow at 00:00:00
                await Task.Delay(nextMidnight - DateTime.Now);
                _cache = null;
                await LoadOnceAsync();
            }
        }

        /// <summary>Fires a reload every day at 01:00 local time, independent of the interval loop.</summary>
        private async Task ScheduleDailyAt1AmAsync()
        {
            while (true)
            {
                var now   = DateTime.Now;
                var next1am = DateTime.Today.AddDays(now.Hour >= 1 ? 1 : 0).AddHours(1);
                await Task.Delay(next1am - now);
                _cache = null;
                await LoadOnceAsync();
            }
        }

        private async Task LoadOnceAsync()
        {
            string url = AppSettings.Instance.Get(AppSettings.EpgUrl);
            if (string.IsNullOrWhiteSpace(url)) return;

            string downloadFile = Path.Combine(Path.GetTempPath(), "iptvcm_epg_dl");
            string xmlFile      = string.Empty;
            try
            {
                using var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();
                await using var fs = new FileStream(
                    downloadFile, FileMode.Create, FileAccess.Write, FileShare.None, 65536, useAsync: true);
                await response.Content.CopyToAsync(fs);
            }
            catch
            {
                // Network error — keep previous cache and retry on next interval
                TryDelete(downloadFile);
                return;
            }

            try
            {
                xmlFile = await DecompressIfNeededAsync(downloadFile);

                var settings = new XmlTvReaderSettings
                {
                    TimeZone        = TimeZoneInfo.Local,
                    DefaultLanguage = "zh"
                };

                var result = await XmlTvReader.ReadAllAsync(xmlFile, settings, CancellationToken.None);

                // Build name → channel-id lookup (case-insensitive)
                var nameToId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var ch in result.Channels)
                {
                    if (!nameToId.ContainsKey(ch.Id))
                        nameToId[ch.Id] = ch.Id;

                    foreach (var name in ch.DisplayNames.Values)
                    {
                        if (!string.IsNullOrWhiteSpace(name) && !nameToId.ContainsKey(name!))
                            nameToId[name!] = ch.Id;
                    }
                }

                // Build channel-id → programmes lookup
                var programmesById = new Dictionary<string, List<XmlTvProgramme>>(StringComparer.OrdinalIgnoreCase);
                foreach (var prog in result.Programmes)
                {
                    if (!programmesById.TryGetValue(prog.ChannelId, out var list))
                        programmesById[prog.ChannelId] = list = new List<XmlTvProgramme>();
                    list.Add(prog);
                }

                _cache = new EpgCache(nameToId, programmesById);
                CacheRefreshed?.Invoke(this, EventArgs.Empty);
            }
            catch
            {
                // Parse error — keep previous cache
            }
            finally
            {
                TryDelete(downloadFile);
                if (!string.IsNullOrEmpty(xmlFile) && xmlFile != downloadFile)
                    TryDelete(xmlFile);
            }
        }

        /// <summary>
        /// Checks the magic bytes of <paramref name="file"/> and decompresses it if it
        /// is GZip (1F 8B) or ZIP (50 4B).  Returns the path of the XML file to parse;
        /// if decompression was performed the returned path is a new temp file that the
        /// caller is responsible for deleting.
        /// </summary>
        private static async Task<string> DecompressIfNeededAsync(string file)
        {
            byte[] magic = new byte[4];
            await using (var probe = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                _ = await probe.ReadAsync(magic, 0, 4);

            // GZip: magic = 1F 8B
            if (magic[0] == 0x1F && magic[1] == 0x8B)
            {
                string outFile = file + ".xml";
                await using var gz  = new GZipStream(
                    new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read),
                    CompressionMode.Decompress);
                await using var out_ = new FileStream(outFile, FileMode.Create, FileAccess.Write);
                await gz.CopyToAsync(out_);
                return outFile;
            }

            // ZIP: magic = 50 4B 03 04
            if (magic[0] == 0x50 && magic[1] == 0x4B)
            {
                string outFile = file + ".xml";
                using var zip   = System.IO.Compression.ZipFile.OpenRead(file);
                var entry = zip.Entries.FirstOrDefault(e =>
                                e.Name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                            ?? zip.Entries.FirstOrDefault();
                if (entry != null)
                {
                    using var entryStream = entry.Open();
                    await using var out_  = new FileStream(outFile, FileMode.Create, FileAccess.Write);
                    await entryStream.CopyToAsync(out_);
                    return outFile;
                }
            }

            // Plain XML — no decompression needed
            return file;
        }

        private static void TryDelete(string path)
        {
            try { if (!string.IsNullOrEmpty(path)) File.Delete(path); } catch { }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static XmlTvProgramme? FindCurrentProgramme(
            List<XmlTvProgramme> programmes, DateTimeOffset now)
        {
            // Linear scan is fast enough; programmes are typically ordered by time
            foreach (var p in programmes)
            {
                if (p.Start <= now && p.Stop > now)
                    return p;
            }
            return null;
        }
    }
}
