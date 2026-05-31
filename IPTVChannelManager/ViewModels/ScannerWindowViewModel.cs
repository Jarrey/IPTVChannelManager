using IPTVChannelManager.Common;
using IPTVChannelManager.Models;
using IPTVChannelManager.Services;
using LibVLCSharp.Shared;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace IPTVChannelManager.ViewModels
{
    public enum ScanLogLevel { Info, Found, New, Exists, Error, Complete }

    public class ScanLogEntry
    {
        public string Message { get; }
        public string Foreground { get; }

        public ScanLogEntry(string message, ScanLogLevel level = ScanLogLevel.Info)
        {
            Message = message;
            Foreground = level switch
            {
                ScanLogLevel.Found    => "#4CAF50",
                ScanLogLevel.New      => "#FFC107",
                ScanLogLevel.Exists   => "#4DD0E1",
                ScanLogLevel.Error    => "#EF5350",
                ScanLogLevel.Complete => "#AB47BC",
                _                     => "#CCCCCC"
            };
        }
    }

    public class ScannerWindowViewModel : BindableBase
    {
        #region Fields

        private const int MaxLogEntries = 10000;

        private readonly ObservableCollection<Channel> _existingChannels;
        private readonly IWindowService _windowService;
        private CancellationTokenSource _cts;
        private SemaphoreSlim _semaphore;
        private LibVLC _probeLibVlc;

        // Thread-safe counters (Interlocked)
        private int _scannedCounter;
        private int _foundCounter;

        // Backing fields for settings
        private bool _useUnicast;
        private string _scanIpStart;
        private string _scanIpEnd;
        private int _scanPortStart;
        private int _scanPortEnd;
        private int _maxThreads;
        private int _timeoutSeconds;

        // Backing fields for state
        private bool _isScanning;
        private int _totalCount;
        private int _scannedCount;
        private int _foundCount;

        // Command backing fields (needed for RaiseCanExecuteChanged)
        private readonly DelegateCommand _startScanCommand;
        private readonly DelegateCommand _stopScanCommand;
        private readonly DelegateCommand _clearResultsCommand;
        private readonly DelegateCommand _addAllChannelsCommand;
        private readonly DelegateCommand _closeCommand;

        #endregion

        #region Constructor

        public ScannerWindowViewModel(ObservableCollection<Channel> existingChannels, IWindowService windowService)
        {
            _existingChannels = existingChannels;
            _windowService = windowService;

            // Load persisted settings (use property setters so clamping is applied)
            _useUnicast     = AppSettings.Instance.Get<bool>(AppSettings.ImportExportWithCustomHost);
            _scanIpStart    = AppSettings.Instance.Get(AppSettings.ScanIpStart);
            _scanIpEnd      = AppSettings.Instance.Get(AppSettings.ScanIpEnd);
            _scanPortStart  = AppSettings.Instance.Get<int>(AppSettings.ScanPortStart);
            _scanPortEnd    = AppSettings.Instance.Get<int>(AppSettings.ScanPortEnd);
            _maxThreads     = Math.Clamp(AppSettings.Instance.Get<int>(AppSettings.ScanMaxThreads), 1, 50);
            _timeoutSeconds = Math.Clamp(AppSettings.Instance.Get<int>(AppSettings.ScanTimeoutSeconds), 1, 60);
            AppSettings.Instance.SettingChanged += OnSettingChanged;

            Logs             = new ObservableCollection<ScanLogEntry>();
            NewFoundChannels = new ObservableCollection<Channel>();

            _startScanCommand    = new DelegateCommand(async () => await StartScanAsync(), () => !IsScanning);
            _stopScanCommand     = new DelegateCommand(StopScan, () => IsScanning);
            _clearResultsCommand = new DelegateCommand(ClearResults, () => !IsScanning);
            _addAllChannelsCommand = new DelegateCommand(AddAllChannels, () => !IsScanning && NewFoundChannels.Count > 0);
            _closeCommand = new DelegateCommand(Cleanup);

            StartScanCommand    = _startScanCommand;
            StopScanCommand     = _stopScanCommand;
            ClearResultsCommand = _clearResultsCommand;
            AddChannelCommand   = new DelegateCommand<Channel>(AddChannel);
            AddAllChannelsCommand = _addAllChannelsCommand;
            PreviewChannelCommand = new DelegateCommand<Channel>(ch => _windowService.OpenPlayerWindow(ch));
            CloseCommand = _closeCommand;

            Core.Initialize();
            _probeLibVlc = new LibVLC("--vout=dummy", "--aout=dummy", "--no-stats");
        }

        #endregion

        #region Properties

        /// <summary>Mirrors AppSettings.ImportExportWithCustomHost: true = unicast, false = multicast.</summary>
        public bool UseUnicast
        {
            get => _useUnicast;
            set
            {
                SetProperty(ref _useUnicast, value);
                AppSettings.Instance.Set(AppSettings.ImportExportWithCustomHost, value);
                RaisePropertyChanged(nameof(UrlPatternDisplay));
            }
        }

        /// <summary>
        /// Shows the effective scan URL pattern.
        /// Unicast: {UnicastHost}/{[IP]:[Port]}  e.g. http://your-relay-server/udp/[IP]:[Port]
        /// Multicast: rtp://[IP]:[Port]  (UDP probe, no HTTP)
        /// </summary>
        public string UrlPatternDisplay
        {
            get
            {
                if (_useUnicast)
                {
                    string unicastHost = AppSettings.Instance.Get(AppSettings.UnicastHost);
                    if (string.IsNullOrWhiteSpace(unicastHost))
                        return "(UnicastHost not set — configure in Settings)";
                    return $"{unicastHost.TrimEnd('/')}/[IP]:[Port]";
                }
                return $"{Constants.DefaultMulticastHost}[IP]:[Port]";
            }
        }

        public string ScanIpStart
        {
            get => _scanIpStart;
            set { SetProperty(ref _scanIpStart, value); AppSettings.Instance.Set(AppSettings.ScanIpStart, value); }
        }

        public string ScanIpEnd
        {
            get => _scanIpEnd;
            set { SetProperty(ref _scanIpEnd, value); AppSettings.Instance.Set(AppSettings.ScanIpEnd, value); }
        }

        public int ScanPortStart
        {
            get => _scanPortStart;
            set { SetProperty(ref _scanPortStart, value); AppSettings.Instance.Set(AppSettings.ScanPortStart, value); }
        }

        public int ScanPortEnd
        {
            get => _scanPortEnd;
            set { SetProperty(ref _scanPortEnd, value); AppSettings.Instance.Set(AppSettings.ScanPortEnd, value); }
        }

        public int MaxThreads
        {
            get => _maxThreads;
            set
            {
                int clamped = Math.Max(1, Math.Min(value, 50));
                SetProperty(ref _maxThreads, clamped);
                AppSettings.Instance.Set(AppSettings.ScanMaxThreads, clamped);
            }
        }

        public int TimeoutSeconds
        {
            get => _timeoutSeconds;
            set
            {
                int clamped = Math.Max(1, Math.Min(value, 60));
                SetProperty(ref _timeoutSeconds, clamped);
                AppSettings.Instance.Set(AppSettings.ScanTimeoutSeconds, clamped);
            }
        }

        public bool IsScanning
        {
            get => _isScanning;
            private set
            {
                SetProperty(ref _isScanning, value);
                RaisePropertyChanged(nameof(IsNotScanning));
                _startScanCommand.RaiseCanExecuteChanged();
                _stopScanCommand.RaiseCanExecuteChanged();
                _clearResultsCommand.RaiseCanExecuteChanged();
                _addAllChannelsCommand.RaiseCanExecuteChanged();
            }
        }

        public bool IsNotScanning => !IsScanning;

        public int TotalCount
        {
            get => _totalCount;
            private set => SetProperty(ref _totalCount, value);
        }

        public int ScannedCount
        {
            get => _scannedCount;
            private set
            {
                SetProperty(ref _scannedCount, value);
                RaisePropertyChanged(nameof(ProgressPercent));
                RaisePropertyChanged(nameof(ProgressText));
            }
        }

        public int FoundCount
        {
            get => _foundCount;
            private set
            {
                SetProperty(ref _foundCount, value);
                RaisePropertyChanged(nameof(ProgressText));
            }
        }

        public double ProgressPercent => TotalCount > 0 ? (double)ScannedCount / TotalCount * 100.0 : 0;

        public string ProgressText => $"{ScannedCount} / {TotalCount}   |   New: {FoundCount}";

        public ObservableCollection<ScanLogEntry> Logs { get; }
        public ObservableCollection<Channel> NewFoundChannels { get; }

        #endregion Properties

        #region Commands

        public ICommand StartScanCommand { get; }
        public ICommand StopScanCommand { get; }
        public ICommand ClearResultsCommand { get; }
        public ICommand AddChannelCommand { get; }
        public ICommand AddAllChannelsCommand { get; }
        public ICommand PreviewChannelCommand { get; }
        public ICommand CloseCommand { get; }
        #endregion

        #region Methods

        private async Task StartScanAsync()
        {
            // Validate inputs
            var addresses = GenerateAddresses().ToList();
            if (addresses.Count == 0)
            {
                AddLog("[ERROR] No addresses could be generated. Check IP range and port range settings.", ScanLogLevel.Error);
                return;
            }

            // Clear previous results before starting
            Logs.Clear();
            NewFoundChannels.Clear();

            IsScanning = true;
            _scannedCounter = 0;
            _foundCounter   = 0;
            ScannedCount    = 0;
            FoundCount      = 0;
            TotalCount      = addresses.Count;

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            _semaphore?.Dispose();
            _semaphore = new SemaphoreSlim(MaxThreads, MaxThreads);

            AddLog($"[{DateTime.Now:HH:mm:ss}] Starting scan: {addresses.Count} addresses, {MaxThreads} threads, timeout {TimeoutSeconds}s", ScanLogLevel.Info);
            AddLog($"[{DateTime.Now:HH:mm:ss}] Mode: {(_useUnicast ? "Unicast" : "Multicast")}   URL pattern: {UrlPatternDisplay}", ScanLogLevel.Info);
            AddLog($"[{DateTime.Now:HH:mm:ss}] Channels in database: {_existingChannels.Count}", ScanLogLevel.Info);

            try
            {
                var tasks = addresses.Select(a => ScanAddressAsync(a.ip, a.port, _cts.Token));
                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException)
            {
                AddLog($"[{DateTime.Now:HH:mm:ss}] Scan stopped by user.", ScanLogLevel.Error);
            }
            catch (Exception ex)
            {
                AddLog($"[{DateTime.Now:HH:mm:ss}] Scan exception: {ex.Message}", ScanLogLevel.Error);
            }
            finally
            {
                IsScanning = false;
                _addAllChannelsCommand.RaiseCanExecuteChanged();
                AddLog($"[{DateTime.Now:HH:mm:ss}] Scan complete. Scanned {ScannedCount}/{TotalCount}, new channels found: {FoundCount}", ScanLogLevel.Complete);
            }
        }

        private void StopScan()
        {
            _cts?.Cancel();
            AddLog($"[{DateTime.Now:HH:mm:ss}] Stopping scan...", ScanLogLevel.Error);
        }

        private void ClearResults()
        {
            Logs.Clear();
            NewFoundChannels.Clear();
            TotalCount   = 0;
            ScannedCount = 0;
            FoundCount   = 0;
            _addAllChannelsCommand.RaiseCanExecuteChanged();
        }

        private IEnumerable<(string ip, int port)> GenerateAddresses()
        {
            string startStr = ScanIpStart?.Trim() ?? string.Empty;
            string endStr   = ScanIpEnd?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(startStr)) yield break;

            // If end is empty or same as start, treat as single IP
            if (string.IsNullOrWhiteSpace(endStr))
                endStr = startStr;

            if (!System.Net.IPAddress.TryParse(startStr, out var startAddr))
                yield break;
            if (!System.Net.IPAddress.TryParse(endStr, out var endAddr))
                endAddr = startAddr;

            byte[] sb = startAddr.GetAddressBytes();
            byte[] eb = endAddr.GetAddressBytes();
            uint startIp = ((uint)sb[0] << 24) | ((uint)sb[1] << 16) | ((uint)sb[2] << 8) | sb[3];
            uint endIp   = ((uint)eb[0] << 24) | ((uint)eb[1] << 16) | ((uint)eb[2] << 8) | eb[3];

            if (startIp > endIp) yield break;

            int portStart = Math.Min(ScanPortStart, ScanPortEnd);
            int portEnd   = Math.Max(ScanPortStart, ScanPortEnd);
            if (portStart <= 0) portStart = 1;
            if (portEnd > 65535) portEnd = 65535;

            for (uint ip = startIp; ip <= endIp; ip++)
            {
                string ipStr = $"{(ip >> 24) & 0xFF}.{(ip >> 16) & 0xFF}.{(ip >> 8) & 0xFF}.{ip & 0xFF}";
                for (int port = portStart; port <= portEnd; port++)
                {
                    yield return (ipStr, port);
                }
            }
        }

        private async Task ScanAddressAsync(string ip, int port, CancellationToken token)
        {
            await _semaphore.WaitAsync(token);
            try
            {
                if (token.IsCancellationRequested) return;

                string url = BuildScanUrl(ip, port);
                AddLog($"[{DateTime.Now:HH:mm:ss}] Probing  {url}");

                bool hasMedia = await ProbeWithVlcAsync(url, TimeoutSeconds * 1000, token);
                if (hasMedia)
                {
                    AddLog($"[{DateTime.Now:HH:mm:ss}] \u2713 Media decoded  {url}", ScanLogLevel.Found);
                    ReportChannel(url, url, string.Empty);
                }
                else
                {
                    AddLog($"[{DateTime.Now:HH:mm:ss}]   No content  {url}  — skipped");
                }
            }
            finally
            {
                _semaphore.Release();
                int scanned = Interlocked.Increment(ref _scannedCounter);
                int found   = Volatile.Read(ref _foundCounter);
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    ScannedCount = scanned;
                    FoundCount   = found;
                });
            }
        }

        /// <summary>
        /// Probes <paramref name="url"/> by attempting to decode the media stream via LibVLC.
        /// Returns true only when LibVLC transitions to Playing state — meaning real media
        /// content was successfully decoded. Returns false on error or timeout.
        /// Works for HTTP/HTTPS (unicast, HLS, M3U playlist) and RTP/UDP (multicast).
        /// </summary>
        private async Task<bool> ProbeWithVlcAsync(string url, int timeoutMs, CancellationToken token)
        {
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            Media media   = null;
            MediaPlayer player = null;
            try
            {
                media  = new Media(_probeLibVlc, new Uri(url));
                player = new MediaPlayer(_probeLibVlc);

                player.Playing         += (s, e) => tcs.TrySetResult(true);
                player.EncounteredError += (s, e) => tcs.TrySetResult(false);
                player.EndReached      += (s, e) => tcs.TrySetResult(false);

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
                cts.CancelAfter(timeoutMs);
                using (cts.Token.Register(() => tcs.TrySetResult(false)))
                {
                    player.Play(media);
                    return await tcs.Task;
                }
            }
            catch { return false; }
            finally
            {
                try { player?.Stop(); } catch { }
                player?.Dispose();
                media?.Dispose();
            }
        }

        /// <summary>
        /// Checks <paramref name="channelUrl"/> against the existing DB and logs/adds accordingly.
        /// Strips the UnicastHost prefix (if present) to produce a relative URL for comparison,
        /// since the DB may store channels as relative paths (e.g. "233.18.204.73:5140" without host).
        /// </summary>
        private void ReportChannel(string channelName, string channelUrl, string group)
        {
            // normalizedUrl: strip UnicastHost prefix for DB comparison
            // (DB stores relative paths, e.g. "233.18.204.73:5140", not the full relay URL)
            string normalizedUrl = channelUrl;
            if (_useUnicast)
            {
                string unicastHost = AppSettings.Instance.Get(AppSettings.UnicastHost);
                string prefix = unicastHost.TrimEnd('/') + '/';
                if (channelUrl.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    normalizedUrl = channelUrl.Substring(prefix.Length);
            }

            bool existsInDb = _existingChannels.Any(c =>
                string.Equals(c.Url, channelUrl,    StringComparison.OrdinalIgnoreCase) ||
                (!ReferenceEquals(normalizedUrl, channelUrl) &&
                  string.Equals(c.Url, normalizedUrl, StringComparison.OrdinalIgnoreCase)));

            if (existsInDb)
            {
                AddLog($"[{DateTime.Now:HH:mm:ss}]   [EXISTS]  {channelName}   {channelUrl}", ScanLogLevel.Exists);
            }
            else
            {
                AddLog($"[{DateTime.Now:HH:mm:ss}]   [NEW]  {channelName}   {channelUrl}", ScanLogLevel.New);
                Interlocked.Increment(ref _foundCounter);
                var channel = new Channel(channelName, channelUrl, group);
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    NewFoundChannels.Add(channel);
                    _addAllChannelsCommand.RaiseCanExecuteChanged();
                });
            }
        }

        private void AddChannel(Channel channel)
        {
            if (channel == null) return;
            _existingChannels.Add(channel);
            NewFoundChannels.Remove(channel);
            AddLog($"[{DateTime.Now:HH:mm:ss}] Added to database: {channel.Name}  {channel.Url}", ScanLogLevel.Complete);
            _addAllChannelsCommand.RaiseCanExecuteChanged();
        }

        private void AddAllChannels()
        {
            var toAdd = NewFoundChannels.ToList();
            foreach (var ch in toAdd)
                _existingChannels.Add(ch);
            NewFoundChannels.Clear();
            AddLog($"[{DateTime.Now:HH:mm:ss}] Added all {toAdd.Count} new channel(s) to the database.", ScanLogLevel.Complete);
            _addAllChannelsCommand.RaiseCanExecuteChanged();
        }

        /// <summary>
        /// Builds the URL to probe for a given IP:port.
        /// Unicast: appends ip:port directly to the UnicastHost relay prefix.
        ///   UnicastHost = "http://your-relay-server/udp/"  ip = "233.18.204.73"  port = 5140
        ///   → "http://your-relay-server/udp/233.18.204.73:5140"
        /// Multicast: rtp://ip:port  (probed via UDP, never via HttpClient)
        /// </summary>
        private string BuildScanUrl(string ip, int port)
        {
            if (_useUnicast)
            {
                string unicastHost = AppSettings.Instance.Get(AppSettings.UnicastHost);
                return $"{unicastHost.TrimEnd('/')}/{ip}:{port}";
            }
            return $"{Constants.DefaultMulticastHost}{ip}:{port}";
        }

        private void OnSettingChanged(object sender, (string key, object value) e)
        {
            if (e.key == AppSettings.UnicastHost)
                RaisePropertyChanged(nameof(UrlPatternDisplay));

            if (e.key == AppSettings.ImportExportWithCustomHost && e.value is bool newVal && newVal != _useUnicast)
            {
                _useUnicast = newVal;
                RaisePropertyChanged(nameof(UseUnicast));
                RaisePropertyChanged(nameof(UrlPatternDisplay));
            }
        }

        public void Cleanup()
        {
            AppSettings.Instance.SettingChanged -= OnSettingChanged;
            _cts?.Cancel();
            _cts?.Dispose();
            _semaphore?.Dispose();
            _probeLibVlc?.Dispose();
        }

        private void AddLog(string message, ScanLogLevel level = ScanLogLevel.Info)
        {
            var entry = new ScanLogEntry(message, level);
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (Logs.Count >= MaxLogEntries)
                    Logs.RemoveAt(0);
                Logs.Add(entry);
            });
        }

        #endregion
    }
}
