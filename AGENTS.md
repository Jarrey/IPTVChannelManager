# IPTVChannelManager Agent Notes

- Repo shape: `IPTVChannelManager.sln` contains one WPF desktop app project: `IPTVChannelManager/IPTVChannelManager.csproj` (`net7.0-windows`, `UseWPF`, `WinExe`).
- Build from repo root with `dotnet build IPTVChannelManager.sln`.
- Run from repo root with `dotnet run --project .\IPTVChannelManager\IPTVChannelManager.csproj`.
- Publish profile exists at `IPTVChannelManager/Properties/PublishProfiles/FolderProfile.pubxml`: `Release`, `win-x64`, output to `IPTVChannelManager/bin/publish/`.
- There are no test projects, lint configs, formatter configs, or CI workflows in this repo.
- `dotnet build` currently succeeds with a large number of existing nullable warnings; do not treat warning cleanup as part of unrelated work.
- `lib/m3u-parser.dll` is a checked-in assembly reference from the project file, not a NuGet package.
- Startup flow is `App.xaml` -> `MainWindow.xaml`; most app behavior is wired through `MainWindowViewModel.cs`.
- Persistence is under `%AppData%\IPTVChannelManager`, not the registry: channel data is `%AppData%\IPTVChannelManager\channeldb.json`, and app settings are JSON files like `%AppData%\IPTVChannelManager\AppSettings.settings` via `Common/AbstractSettings.cs`.
- If docs disagree on settings storage, trust the code (`AbstractSettings.cs`, `MainWindowViewModel.cs`) over `README.md`.
- Player UI has a non-obvious WPF/LibVLC constraint: the on-video HUD is a separate transparent `PlayerOverlayWindow`, kept in sync by `PlayerWindow`, because LibVLC video cannot host a normal WPF overlay.
- Fullscreen double-click on the video surface is handled by a global low-level mouse hook (`WH_MOUSE_LL`) in `PlayerWindow.xaml.cs`; preserve that path when changing player interaction.
- `IPTVChannelManager.csproj` manually enumerates `logos/*` and `tv.ico` as WPF resources. If you add, remove, or rename logo assets or the app icon, update the project file too.
- Preserve the existing repo instruction from `.github/copilot-instructions.md`: do not use PowerShell or Python scripts to modify files; edit files directly.
