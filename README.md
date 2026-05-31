# IPTVChannelManager

A Windows desktop application for managing, organizing, and playing IPTV channels. Built with WPF and .NET 10, it allows you to import channel lists from M3U/TXT files, maintain a persistent local channel database, play streams directly within the application using LibVLC, view an EPG programme guide, and discover new streams by scanning your network.

---

## Features

### Channel Management
- **Import** channel lists from `.m3u` / `.m3u8` playlists or plain-text (`.txt`) IPTV lists
- **Merge** imported channels with the existing local database — new, changed, and removed channels are clearly separated
- **Persist** your channel list as a local JSON database (`channeldb.json`) stored in `%AppData%\IPTVChannelManager`
- **Filter** channels by group using a sidebar group selector
- **Search / filter** channels by name within each group
- **Ignore** individual channels to exclude them from exports without deleting them
- **Export** your curated channel list back to `.txt` or `.m3u` format

### Unicast / Multicast Conversion
- Automatically strip or replace the multicast prefix (`rtp://`) with a custom unicast host during import and export
- Toggle the unicast/multicast conversion switch from the main window toolbar

### Built-in Player
- Play any channel directly inside the app via an integrated LibVLC-powered player window
- Hardware-accelerated decoding enabled by default
- **Double-click** the video area to toggle fullscreen
- Fullscreen mode hides the custom title bar / window chrome for a clean viewing experience
- Multi-monitor aware — fullscreen expands to the correct display

### Player Overlay (HUD)
A transparent topmost overlay window (works around the WPF/LibVLC Airspace limitation) provides:

| Element | Description |
|---|---|
| **Channel logo + name** | Displayed top-left |
| **Real-time clock** | Displayed top-right, refreshed every second |
| **Control bar** | Shown on mouse move, auto-hides after 10 seconds of inactivity |
| **Fullscreen button** | Toggle fullscreen / windowed mode |
| **Mute button** | Toggle audio mute with icon feedback |
| **Volume slider** | Adjust volume 0–100 % with percentage label |
| **Media info** | Shows video codec, audio codec, and channel layout (e.g. `Video: h264  \|  Audio: mp3  \|  Stereo`) |

### EPG Guide
A full-day Electronic Programme Guide rendered as a scrollable timeline:

| Feature | Description |
|---|---|
| **Timeline header** | Hour and half-hour tick marks spanning 00:00–24:00 |
| **Now indicator** | A vertical red line and real-time clock mark the current position |
| **Programme blocks** | Per-channel rows showing title, time range, and on-air highlight |
| **Click to play** | Click any programme block to start playing that channel |
| **Auto-refresh** | EPG data is downloaded, cached, and refreshed at a configurable interval (default: every 4 hours) |
| **XMLTV / gzip support** | Fetches plain `.xml` or gzip-compressed XMLTV feeds |
| **Manual reload** | Reload button forces an immediate EPG re-fetch |

### Channel Scanner
Discover live streams on your local network without importing any file:

| Feature | Description |
|---|---|
| **IP range scan** | Enter a start and end IP address and a port range to probe |
| **Parallel probing** | Configurable thread count (1–50) and per-stream timeout (1–60 s) |
| **LibVLC probing** | Uses a headless LibVLC instance (`--vout=dummy`) to verify each stream is playable |
| **Unicast / multicast** | Probes via the configured unicast relay or directly as `rtp://` multicast |
| **Color-coded log** | Real-time scan log with color coding: found, new, already-exists, error |
| **Preview & add** | Preview any found channel in the player, or add individual / all new channels to the database |
| **Scan settings persist** | IP range, port range, thread count, and timeout are saved to `AppSettings` |

### Settings
- Configure **channel group names** (used for filtering and import grouping)
- Set a custom **channel logo URL template** (default: `https://live.fanmingming.cn/tv/{0}.png`)
- Set a custom **EPG URL** for Electronic Programme Guide data (default: `https://live.fanmingming.cn/e.xml`)
- Set a custom **unicast host** for stream URL conversion
- Configure **EPG refresh interval** in hours (default: 4)

---

## Architecture

The project follows the **MVVM** pattern throughout:

```
IPTVChannelManager/
├── Common/
│   ├── BindableBase.cs          # INotifyPropertyChanged base class
│   ├── DelegateCommand.cs       # ICommand implementation (Prism-style)
│   ├── DelegateCommandBase.cs
│   ├── AbstractSettings.cs      # Generic settings persistence base
│   ├── BaseWindow.cs            # Custom window chrome (WindowStyle=None + WindowChrome)
│   ├── ImgConverter.cs          # IValueConverter for image binding
│   ├── ItemsControlFilter.cs    # CollectionView filter helper
│   ├── ParseHelper.cs
│   ├── PropertyObserver.cs
│   └── TypeHelper.cs
├── Models/
│   └── Channel.cs               # Channel model (BindableBase + JSON serialization)
├── Services/
│   ├── EpgService.cs            # Singleton XMLTV download, parse, and auto-refresh cache
│   ├── ImportExportHelper.cs    # M3U / TXT import and export logic
│   └── WindowService.cs         # IWindowService abstraction for opening child windows
├── ViewModels/
│   ├── MainWindowViewModel.cs   # Commands: Import, Export, Play, Add/Remove, EPG, Scanner
│   ├── PlayerViewModel.cs       # Stream URL construction and window title logic
│   ├── PlayerOverlayViewModel.cs# Overlay state: ToggleFullscreen, ToggleMute, volume
│   ├── EpgGuideViewModel.cs     # EPG timeline rows, now-indicator, click-to-play
│   ├── ScannerWindowViewModel.cs# IP range scan: thread pool, LibVLC probing, log
│   └── SettingWindowViewModel.cs
├── Views/
│   ├── MainWindow.xaml/.cs      # Main channel management UI
│   ├── PlayerWindow.xaml/.cs    # LibVLC player window + fullscreen logic + mouse hook
│   ├── PlayerOverlayWindow.xaml/.cs # Transparent overlay window (HUD)
│   ├── EpgGuideWindow.xaml/.cs  # EPG programme guide window
│   ├── ScannerWindow.xaml/.cs   # Network stream scanner window
│   └── SettingWindow.xaml/.cs   # Settings dialog
├── AppSettings.cs               # Application settings singleton (persisted)
├── Constants.cs                 # All application-wide constants
└── logos/                       # Bundled channel logo image assets
```

### Key Design Decisions

- **Airspace workaround**: LibVLCSharp uses `HwndHost` internally, which prevents standard WPF overlays from rendering on top of the video. The player HUD is implemented as a separate transparent `Window` (`PlayerOverlayWindow`) that tracks the player window's position and size.
- **Global mouse hook**: A `WH_MOUSE_LL` low-level mouse hook is used to detect double-clicks on the VLC video surface (which does not receive standard WPF input events).
- **Commands over events**: All button interactions in the overlay are bound to `DelegateCommand` properties on `PlayerOverlayViewModel` — no logic lives in code-behind.
- **IWindowService abstraction**: Child windows (`PlayerWindow`, `EpgGuideWindow`, `ScannerWindow`, `SettingWindow`) are opened through `IWindowService` / `WindowService`, keeping view-model code testable and decoupled from concrete `Window` types.
- **EPG cache**: `EpgService` (singleton) downloads and parses the XMLTV feed on a background thread, stores an atomically replaced snapshot, and broadcasts `CacheRefreshed` to subscribers. Supports plain and gzip-compressed feeds.
- **Headless LibVLC probe**: The scanner creates a single `LibVLC("--vout=dummy", "--aout=dummy")` instance and probes each candidate URL with a configurable timeout using a `SemaphoreSlim`-capped thread pool.

---

## Dependencies

| Package | Version | Purpose |
|---|---|---|
| [LibVLCSharp.WPF](https://github.com/videolan/libvlcsharp) | 3.9.7.1 | WPF VideoView control |
| [VideoLAN.LibVLC.Windows](https://www.nuget.org/packages/VideoLAN.LibVLC.Windows) | 3.0.23.1 | Native libvlc binaries for Windows |
| [MaterialDesignThemes](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit) | 5.2.1 | UI theme and icons |
| [Newtonsoft.Json](https://www.newtonsoft.com/json) | 13.0.4 | Channel database JSON serialization |
| [Microsoft-WindowsAPICodePack-Shell](https://github.com/contre/Windows-API-Code-Pack-1.1) | 1.1.5 | Native file/folder picker dialogs |
| [Microsoft.Xaml.Behaviors.Wpf](https://github.com/microsoft/XamlBehaviorsWpf) | 1.1.142 | WPF XAML behaviors (e.g. `AutoScrollBehavior`) |
| [XmlTvSharp](https://www.nuget.org/packages/XmlTvSharp) | 1.1.2 | XMLTV EPG feed parsing |
| m3u-parser *(local)* | — | M3U/M3U8 playlist parsing (`lib/m3u-parser.dll`) |

---

## Requirements

- **OS**: Windows 10 / 11 (x64)
- **Runtime**: [.NET 7 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/7.0)
- **VLC**: Bundled via `VideoLAN.LibVLC.Windows` — no separate VLC installation required

---

## Getting Started

### Build from Source

1. Clone the repository:
   ```bash
   git clone https://github.com/Jarrey/IPTVChannelManager.git
   cd IPTVChannelManager
   ```

2. Open `IPTVChannelManager.sln` in **Visual Studio 2022** (or later).

3. Restore NuGet packages and build:
   ```bash
   dotnet restore
   dotnet build
   ```

4. Run:
   ```bash
   dotnet run --project IPTVChannelManager
   ```

### First Run

1. Launch the application.
2. Click **Import** in the toolbar to load a `.m3u` or `.txt` IPTV channel list.
3. New channels appear in the **New Channels** panel on the right. Use **Add** (or **Add All**) to move them into your database.
4. Click **Save** to persist your channel list.
5. Double-click any channel in the list to start playback.

---

## Channel List Format

### TXT format
```
Group Name#
Channel Name 1,rtsp://...
Channel Name 2,rtsp://...
```

### M3U format
Standard M3U8 playlist with `#EXTINF` metadata:
```m3u
#EXTM3U
#EXTINF:-1 tvg-id="..." tvg-logo="..." group-title="CCTV",CCTV-1
http://...
```

---

## Settings

Settings are stored in the Windows Registry under the application name. They can be changed via **Settings → Open Settings** in the menu bar.

| Setting | Default | Description |
|---|---|---|
| Channel Groups | `上海,央视,卫视,...` | Comma-separated group names used for filtering |
| Logo URL Template | `https://live.fanmingming.cn/tv/{0}.png` | URL pattern for fetching channel logos |
| EPG URL | `https://live.fanmingming.cn/e.xml` | Electronic Programme Guide data source |
| Unicast Host | *(empty)* | Host prefix to replace multicast URLs during import/export |
| Import/Export with Custom Host | `true` | Whether to apply the unicast host conversion |

---

## License

See [LICENSE.txt](LICENSE.txt) for details.
