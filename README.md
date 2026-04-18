# IPTVChannelManager

A Windows desktop application for managing, organizing, and playing IPTV channels. Built with WPF and .NET 7, it allows you to import channel lists from M3U/TXT files, maintain a persistent local channel database, and play streams directly within the application using LibVLC.

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

### Settings
- Configure **channel group names** (used for filtering and import grouping)
- Set a custom **channel logo URL template** (default: `https://live.fanmingming.cn/tv/{0}.png`)
- Set a custom **EPG URL** for Electronic Programme Guide data
- Set a custom **unicast host** for stream URL conversion

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
├── MainWindow.xaml/.cs          # Main channel management UI
├── MainWindowViewModel.cs       # Commands: Import, Export, Play, Add/Remove channels
├── PlayerWindow.xaml/.cs        # LibVLC player window + fullscreen logic + mouse hook
├── PlayerOverlayWindow.xaml/.cs # Transparent overlay window (HUD)
├── PlayerOverlayViewModel.cs    # Overlay state + ToggleFullscreenCommand + ToggleMuteCommand
├── SettingWindow.xaml/.cs       # Settings dialog
├── SettingWindowViewModel.cs
├── Channel.cs                   # Channel model (BindableBase + JSON serialization)
├── AppSettings.cs               # Application settings singleton (persisted)
├── ImportExportHelper.cs        # M3U / TXT import and export logic
├── Constants.cs                 # All application-wide constants
└── logos/                       # Bundled channel logo image assets
```

### Key Design Decisions

- **Airspace workaround**: LibVLCSharp uses `HwndHost` internally, which prevents standard WPF overlays from rendering on top of the video. The player HUD is implemented as a separate transparent `Window` (`PlayerOverlayWindow`) that tracks the player window's position and size.
- **Global mouse hook**: A `WH_MOUSE_LL` low-level mouse hook is used to detect double-clicks on the VLC video surface (which does not receive standard WPF input events).
- **Commands over events**: All button interactions in the overlay are bound to `DelegateCommand` properties on `PlayerOverlayViewModel` — no logic lives in code-behind.

---

## Dependencies

| Package | Version | Purpose |
|---|---|---|
| [LibVLCSharp.WPF](https://github.com/videolan/libvlcsharp) | 3.9.2 | WPF VideoView control |
| [VideoLAN.LibVLC.Windows](https://www.nuget.org/packages/VideoLAN.LibVLC.Windows) | 3.0.21 | Native libvlc binaries for Windows |
| [MaterialDesignThemes](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit) | 5.2.1 | UI theme and icons |
| [Newtonsoft.Json](https://www.newtonsoft.com/json) | 13.0.3 | Channel database JSON serialization |
| [Microsoft-WindowsAPICodePack-Shell](https://github.com/contre/Windows-API-Code-Pack-1.1) | 1.1.5 | Native file/folder picker dialogs |
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
