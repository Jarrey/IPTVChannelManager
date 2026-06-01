using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using XmlTvSharp;
using IPTVChannelManager.Common;
using IPTVChannelManager.Models;
using IPTVChannelManager.Services;
using IPTVChannelManager.Views;

namespace IPTVChannelManager.ViewModels
{
    /// <summary>A tick on the time-header axis: pixel offset from midnight and display text.</summary>
    public record TimeMark(double Left, string Label);

    /// <summary>A single programme block rendered inside a row canvas.</summary>
    public class EpgProgrammeBlock : BindableBase
    {
        #region Fields

        private readonly DateTime _start;
        private readonly DateTime _stop;
        private bool _isCurrentlyAiring;

        #endregion

        #region Constructor

        public EpgProgrammeBlock(double left, double width, string title, string timeRange,
                                  bool isCurrent, Channel channel, DateTime start, DateTime stop)
        {
            Left = left;
            Width = width;
            Title = title;
            TimeRange = timeRange;
            Tooltip = $"{title}\n{timeRange}";
            IsCurrentlyAiring = isCurrent;
            Channel = channel;
            _start = start;
            _stop = stop;
        }

        #endregion

        #region Properties

        /// <summary>Horizontal offset in pixels from midnight.</summary>
        public double Left { get; }
        /// <summary>Width in pixels proportional to duration.</summary>
        public double Width { get; }
        public string Title { get; }
        public string TimeRange { get; }
        public string Tooltip { get; }
        /// <summary>The channel this block belongs to (for click-to-play).</summary>
        public Channel Channel { get; }

        public bool IsCurrentlyAiring
        {
            get => _isCurrentlyAiring;
            private set => SetProperty(ref _isCurrentlyAiring, value);
        }

        #endregion

        #region Methods

        /// <summary>Re-evaluate whether this block is currently on-air.</summary>
        public void RefreshAiring(DateTime now)
            => IsCurrentlyAiring = _start <= now && _stop > now;

        #endregion
    }

    /// <summary>One horizontal row in the EPG guide: one channel + its day's programmes.</summary>
    public class EpgGuideRow
    {
        #region Constructor

        public EpgGuideRow(Channel channel, IReadOnlyList<EpgProgrammeBlock> blocks)
        {
            Channel = channel;
            Blocks = blocks;
        }

        #endregion

        #region Properties

        public Channel Channel { get; }
        public IReadOnlyList<EpgProgrammeBlock> Blocks { get; }

        #endregion
    }

    public class EpgGuideViewModel : BindableBase
    {
        #region Fields

        public const double PixelsPerMinute = 2.0;       // 120 px/h, 2 880 px/24 h
        public const double RowHeight = 44.0;
        public const double TotalWidth = 24 * 60 * PixelsPerMinute; // 2 880

        private readonly IWindowService _windowService;
        private ObservableCollection<EpgGuideRow> _rows = new();
        private double _timeLineLeft;
        private string _nowText = DateTime.Now.ToString("HH:mm:ss");
        private double _nowLabelTop;
        private double _totalRowsHeight;
        private string _statusText = "Loading EPG data\u2026";
        private bool _isLoading;
        private ICommand _playChannelCommand;
        private IEnumerable<Channel> _channels = Array.Empty<Channel>();
        private readonly DispatcherTimer _timer;

        #endregion

        #region Constructor

        public EpgGuideViewModel(IWindowService windowService)
        {
            _windowService = windowService;
            var marks = new List<TimeMark>(24);
            var halfMarks = new List<TimeMark>(24);
            for (int h = 0; h < 24; h++)
            {
                marks.Add(new TimeMark(h * 60 * PixelsPerMinute, $"{h:D2}:00"));
                halfMarks.Add(new TimeMark((h * 60 + 30) * PixelsPerMinute, $"{h:D2}:30"));
            }
            TimeMarks = marks;
            HalfHourMarks = halfMarks;
            PlayChannelCommand = new DelegateCommand<Channel>(PlayRequested);
            ReloadCommand = new DelegateCommand(async () => await ReloadAsync(), () => !IsLoading);

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (s, e) => OnMinuteTick();
            _timer.Start();

            EpgService.Instance.CacheRefreshed += OnEpgCacheRefreshed;
        }

        #endregion

        #region Properties

        /// <summary>Hour labels for the fixed time-header row: (Left offset, "HH:00") pairs.</summary>
        public IReadOnlyList<TimeMark> TimeMarks { get; }

        /// <summary>Half-hour marks for the time header: (Left offset, "HH:30") pairs.</summary>
        public IReadOnlyList<TimeMark> HalfHourMarks { get; }

        public ObservableCollection<EpgGuideRow> Rows
        {
            get => _rows;
            private set => SetProperty(ref _rows, value);
        }

        public double TimeLineLeft
        {
            get => _timeLineLeft;
            private set => SetProperty(ref _timeLineLeft, value);
        }

        public string NowText
        {
            get => _nowText;
            private set => SetProperty(ref _nowText, value);
        }

        public double NowLabelTop
        {
            get => _nowLabelTop;
            set => SetProperty(ref _nowLabelTop, value);
        }

        public double TotalRowsHeight
        {
            get => _totalRowsHeight;
            private set => SetProperty(ref _totalRowsHeight, value);
        }

        public string StatusText
        {
            get => _statusText;
            private set => SetProperty(ref _statusText, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        #endregion

        #region Commands

        public ICommand PlayChannelCommand
        {
            get => _playChannelCommand;
            private set => SetProperty(ref _playChannelCommand, value);
        }

        public ICommand ReloadCommand { get; private set; }

        #endregion

        #region Methods

        private static IReadOnlyList<EpgProgrammeBlock> BuildBlocks(
            IReadOnlyList<XmlTvProgramme> progs, DateTime now, Channel channel)
        {
            if (progs.Count == 0) return Array.Empty<EpgProgrammeBlock>();

            var todayStart = DateTime.Today;
            var todayEnd = todayStart.AddDays(1);
            var blocks = new List<EpgProgrammeBlock>(progs.Count);

            foreach (var p in progs)
            {
                var start = p.Start.ToLocalTime().DateTime;
                var stop = p.Stop.ToLocalTime().DateTime;

                // Clamp to the 24-hour window
                var ws = start < todayStart ? todayStart : start;
                var we = stop > todayEnd ? todayEnd : stop;
                if (we <= ws) continue;

                double left = (ws - todayStart).TotalMinutes * PixelsPerMinute;
                double width = (we - ws).TotalMinutes * PixelsPerMinute;
                if (width < 2) continue;

                string title = p.Titles.TryGetValue("zh", out var t) ? t
                             : p.Titles.Values.FirstOrDefault() ?? string.Empty;
                string timeRange = $"{start:HH:mm}\u2013{stop:HH:mm}";
                bool isCurrent = start <= now && stop > now;

                blocks.Add(new EpgProgrammeBlock(left, width, title, timeRange, isCurrent, channel, start, stop));
            }

            return blocks;
        }

        /// <summary>
        /// Update the channel list that <see cref="ReloadCommand"/> and the
        /// automatic EPG-cache-refresh path will use.
        /// </summary>
        public void SetChannels(IEnumerable<Channel> channels)
        {
            _channels = channels ?? Array.Empty<Channel>();
        }

        /// <summary>Load (or reload) the EPG rows using the given channel list, and store it for future auto-refreshes.</summary>
        public async Task LoadAsync(IEnumerable<Channel> channels)
        {
            SetChannels(channels);
            await ReloadAsync();
        }

        public void Cleanup()
        {
            _timer.Stop();
            EpgService.Instance.CacheRefreshed -= OnEpgCacheRefreshed;
        }

        /// <summary>Reload EPG rows from the last-known channel list (used by <see cref="ReloadCommand"/> and auto-refresh).</summary>
        private async Task ReloadAsync()
        {
            if (IsLoading) return;
            IsLoading = true;
            ((DelegateCommand)ReloadCommand).RaiseCanExecuteChanged();
            StatusText = "Loading EPG data\u2026";

            var channelList = _channels.ToList();
            var now = DateTime.Now;

            var rows = await Task.Run(() =>
            {
                var result = new List<EpgGuideRow>(channelList.Count);
                foreach (var ch in channelList.Where(c => !c.Ignore))
                {
                    string logoName = System.IO.Path.GetFileNameWithoutExtension(ch.LogoUrl ?? "");
                    var progs = EpgService.Instance.GetTodayProgrammes(ch.Name, logoName);
                    var blocks = BuildBlocks(progs, now, ch);
                    result.Add(new EpgGuideRow(ch, blocks));
                }
                return result;
            });

            Rows = new ObservableCollection<EpgGuideRow>(rows);
            TotalRowsHeight = rows.Count * RowHeight;
            StatusText = rows.Count > 0
                ? $"{rows.Count} channels  \u00b7  {rows.Count(r => r.Blocks.Count > 0)} with EPG"
                : "No channels";

            UpdateTimeLine();
            IsLoading = false;
            ((DelegateCommand)ReloadCommand).RaiseCanExecuteChanged();
        }

        private async void OnEpgCacheRefreshed(object? sender, EventArgs e)
        {
            // Fired on a background thread — marshal to UI thread.
            // Skip when a reload is already in progress.
            if (IsLoading) return;
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                await EpgService.Instance.ForceReloadAsync();
                await ReloadAsync();
            });
        }

        private void PlayRequested(Channel channel)
        {
            if (channel == null || string.IsNullOrWhiteSpace(channel.Url)) return;
            try
            {
                _windowService.OpenPlayerWindow(channel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}, {ex}");
            }
        }

        private void OnMinuteTick()
        {
            var now = DateTime.Now;
            NowText = now.ToString("HH:mm:ss");
            UpdateTimeLine();
            foreach (var row in Rows)
                foreach (var block in row.Blocks)
                    block.RefreshAiring(now);
        }

        private void UpdateTimeLine()
            => TimeLineLeft = (DateTime.Now - DateTime.Today).TotalMinutes * PixelsPerMinute;

        #endregion
    }
}
