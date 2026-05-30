using IPTVChannelManager.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using XmlTvSharp;

namespace IPTVChannelManager
{
    /// <summary>A tick on the time-header axis: pixel offset from midnight and display text.</summary>
    public record TimeMark(double Left, string Label);

    /// <summary>A single programme block rendered inside a row canvas.</summary>
    public class EpgProgrammeBlock : BindableBase
    {
        /// <summary>Horizontal offset in pixels from midnight.</summary>
        public double Left  { get; }
        /// <summary>Width in pixels proportional to duration.</summary>
        public double Width { get; }
        public string Title     { get; }
        public string TimeRange { get; }
        public string Tooltip   { get; }
        /// <summary>The channel this block belongs to (for click-to-play).</summary>
        public Channel Channel  { get; }

        private readonly DateTime _start;
        private readonly DateTime _stop;

        private bool _isCurrentlyAiring;
        public bool IsCurrentlyAiring
        {
            get => _isCurrentlyAiring;
            private set => SetProperty(ref _isCurrentlyAiring, value);
        }

        public EpgProgrammeBlock(double left, double width, string title, string timeRange,
                                  bool isCurrent, Channel channel, DateTime start, DateTime stop)
        {
            Left              = left;
            Width             = width;
            Title             = title;
            TimeRange         = timeRange;
            Tooltip           = $"{title}\n{timeRange}";
            IsCurrentlyAiring = isCurrent;
            Channel           = channel;
            _start            = start;
            _stop             = stop;
        }

        /// <summary>Re-evaluate whether this block is currently on-air.</summary>
        public void RefreshAiring(DateTime now)
            => IsCurrentlyAiring = _start <= now && _stop > now;
    }

    /// <summary>One horizontal row in the EPG guide: one channel + its day's programmes.</summary>
    public class EpgGuideRow
    {
        public Channel                          Channel { get; }
        public IReadOnlyList<EpgProgrammeBlock> Blocks  { get; }
        public ICommand                         PlayChannelCommand { get; }

        public EpgGuideRow(Channel channel, IReadOnlyList<EpgProgrammeBlock> blocks,
                            ICommand playChannelCommand)
        {
            Channel            = channel;
            Blocks             = blocks;
            PlayChannelCommand = playChannelCommand;
        }
    }

    public class EpgGuideViewModel : BindableBase
    {
        // ── Layout constants ─────────────────────────────────────────────────
        public const double PixelsPerMinute = 2.0;       // 120 px/h, 2 880 px/24 h
        public const double RowHeight       = 44.0;
        public const double TotalWidth      = 24 * 60 * PixelsPerMinute; // 2 880

        // ── Events ──────────────────────────────────────────────────────────
        /// <summary>Raised when the user clicks a channel or a currently-airing block.</summary>
        public event Action<Channel>? PlayRequested;

        // ── Bindable properties ──────────────────────────────────────────────
        private ObservableCollection<EpgGuideRow> _rows = new();
        public ObservableCollection<EpgGuideRow> Rows
        {
            get => _rows;
            private set => SetProperty(ref _rows, value);
        }

        private double _timeLineLeft;
        public double TimeLineLeft
        {
            get => _timeLineLeft;
            private set => SetProperty(ref _timeLineLeft, value);
        }

        private string _nowText = DateTime.Now.ToString("HH:mm:ss");
        public string NowText
        {
            get => _nowText;
            private set => SetProperty(ref _nowText, value);
        }

        private double _nowLabelTop;
        public double NowLabelTop
        {
            get => _nowLabelTop;
            internal set => SetProperty(ref _nowLabelTop, value);
        }

        private double _totalRowsHeight;
        public double TotalRowsHeight
        {
            get => _totalRowsHeight;
            private set => SetProperty(ref _totalRowsHeight, value);
        }

        private string _statusText = "Loading EPG data…";
        public string StatusText
        {
            get => _statusText;
            private set => SetProperty(ref _statusText, value);
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            internal set => SetProperty(ref _isLoading, value);
        }

        /// <summary>Hour labels for the fixed time-header row: (Left offset, "HH:00") pairs.</summary>
        public IReadOnlyList<TimeMark> TimeMarks { get; }

        /// <summary>Half-hour marks for the time header: (Left offset, "HH:30") pairs.</summary>
        public IReadOnlyList<TimeMark> HalfHourMarks { get; }

        // ── Timer ────────────────────────────────────────────────────────────

        private readonly DispatcherTimer _timer;

        // ── Constructor ──────────────────────────────────────────────────────

        public EpgGuideViewModel()
        {
            var marks     = new List<TimeMark>(24);
            var halfMarks = new List<TimeMark>(24);
            for (int h = 0; h < 24; h++)
            {
                marks.Add(new TimeMark(h * 60 * PixelsPerMinute, $"{h:D2}:00"));
                halfMarks.Add(new TimeMark((h * 60 + 30) * PixelsPerMinute, $"{h:D2}:30"));
            }
            TimeMarks     = marks;
            HalfHourMarks = halfMarks;

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (s, e) => OnMinuteTick();
            _timer.Start();
        }

        // ── Public API ────────────────────────────────────────────────────────

        public async Task LoadAsync(IEnumerable<Channel> channels)
        {
            IsLoading  = true;
            StatusText = "Loading EPG data\u2026";

            // Snapshot to a list so the background thread can iterate safely
            var channelList = channels.ToList();
            var now         = DateTime.Now;

            var rows = await Task.Run(() =>
            {
                var result = new List<EpgGuideRow>(channelList.Count);
                foreach (var ch in channelList.Where(c => !c.Ignore))
                {
                    string logoName = System.IO.Path.GetFileNameWithoutExtension(ch.LogoUrl ?? "");
                    var progs  = EpgService.Instance.GetTodayProgrammes(ch.Name, logoName);
                    var blocks = BuildBlocks(progs, now, ch);
                    var playCmd = new DelegateCommand<Channel>(c => PlayRequested?.Invoke(c));
                    result.Add(new EpgGuideRow(ch, blocks, playCmd));
                }
                return result;
            });

            Rows            = new ObservableCollection<EpgGuideRow>(rows);
            TotalRowsHeight = rows.Count * RowHeight;
            StatusText      = rows.Count > 0
                ? $"{rows.Count} channels  \u00b7  {rows.Count(r => r.Blocks.Count > 0)} with EPG"
                : "No channels";

            UpdateTimeLine();
            IsLoading = false;
        }

        public void Cleanup() => _timer.Stop();

        // ── Helpers ───────────────────────────────────────────────────────────

        private void OnMinuteTick()
        {
            var now = DateTime.Now;
            NowText = now.ToString("HH:mm:ss");
            UpdateTimeLine();
            foreach (var row in Rows)
                foreach (var block in row.Blocks)
                    block.RefreshAiring(now);
        }

        private static IReadOnlyList<EpgProgrammeBlock> BuildBlocks(
            IReadOnlyList<XmlTvProgramme> progs, DateTime now, Channel channel)
        {
            if (progs.Count == 0) return Array.Empty<EpgProgrammeBlock>();

            var todayStart = DateTime.Today;
            var todayEnd   = todayStart.AddDays(1);
            var blocks     = new List<EpgProgrammeBlock>(progs.Count);

            foreach (var p in progs)
            {
                var start = p.Start.ToLocalTime().DateTime;
                var stop  = p.Stop.ToLocalTime().DateTime;

                // Clamp to the 24-hour window
                var ws = start < todayStart ? todayStart : start;
                var we = stop  > todayEnd   ? todayEnd   : stop;
                if (we <= ws) continue;

                double left  = (ws - todayStart).TotalMinutes * PixelsPerMinute;
                double width = (we - ws).TotalMinutes          * PixelsPerMinute;
                if (width < 2) continue;

                string title = p.Titles.TryGetValue("zh", out var t) ? t
                             : p.Titles.Values.FirstOrDefault() ?? string.Empty;
                string timeRange  = $"{start:HH:mm}–{stop:HH:mm}";
                bool   isCurrent  = start <= now && stop > now;

                blocks.Add(new EpgProgrammeBlock(left, width, title, timeRange, isCurrent,
                                                  channel, start, stop));
            }

            return blocks;
        }

        private void UpdateTimeLine()
            => TimeLineLeft = (DateTime.Now - DateTime.Today).TotalMinutes * PixelsPerMinute;
    }
}
