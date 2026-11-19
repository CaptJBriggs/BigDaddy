# Big Daddy

Big Daddy is a Windows desktop break-management application built with C# and Windows Forms on .NET 10.

It is currently an Alpha project. The source is published so that people can inspect it, learn from it, experiment, report issues, and contribute improvements.

## Windows download (Alpha)

For a ready-to-run Windows build, download [BigDaddy-Alpha.exe](BigDaddy-Alpha.exe).

It is a self-contained 64-bit Windows executable, so a separate .NET installation is not required. Place it in its intended permanent folder before first launch because Big Daddy records its location for Windows autostart. This is an unsigned Alpha build, not an installer; Windows may show its normal warning before running an application downloaded from the internet.

## Current Alpha features

- Configurable short- and long-break schedules.
- Automatic idle handling that pauses Playing time but not break time.
- Tray-based background operation and Windows-sign-in autostart.
- Full-screen break overlay with live countdown.
- Optional emergency PIN protection for keyboard access during breaks.
- Windows notifications, shutdown, sign-out, and switch-user actions.
- Optional attempt to send Escape immediately before a break begins.
- A compact, always-on-top Play OSD with a configurable visual warning.
- Play-session progress and completed-long-break count for the current local day.
- Light, Dark neon, and Follow Windows appearances.
- Session recovery for interrupted schedules.

## Status and limitations

Big Daddy is not a hardened or tamper-proof enforcement tool. It runs in the signed-in user's Windows session and is under active development. Use it at your own risk, particularly when testing shutdown, sign-out, switch-user, keyboard blocking, and recovery behaviour.

The project currently has no formal installer, code signing, or administrator-controlled configuration layer.

## Requirements

- Windows
- .NET 10 SDK for development
- Visual Studio 2026 or another compatible .NET development environment

## Build

Open `BigDaddy.slnx` in Visual Studio and build the `BigDaddy` project, or run:

```powershell
dotnet build .\BigDaddy\BigDaddy.csproj
```

## Contributing

Issues and pull requests are welcome. Please keep changes focused, describe what you tested, and avoid committing generated build output or local settings.

## Licence

This project is licensed under the GNU General Public License v3.0. See [LICENSE](LICENSE).
