# BigDaddy (Yes it is a Nic Cage reference)

I made this app with the help of Codex. Since I could not find any thing similar in the windows space. This is largely inspired by Safeeyes on Linux. But built slightly more robust.
Where Safeeyes reminds you to take a break, Bigdaddy ENSURES that you actually do. Needless to say this is meant for children. 
Here is my scenario: Steam can handle the scheduling of when to play. Then BigDaddy handles the frequency. On my kid's PC I have it set up so that he can play for 2 hours at a time, but will be forced to take
a 2 hour break after that with three 5min breaks every 30min. 
So it looks like this:
30min Play
5min Break
30min Play
5min Break
30min Play
5min Break
30min play
120min break
So the total actual play time is still 2hours, the 5min breaks are not deducted from the play time. Additionally when you walk away from the PC, eg not touching the mouse or keyboard the timer will pause
after the set time. Reboots won't reset the timer. The timer and count resets daily at midnight. It is not full proof and there are loops holes that I intend to fix in the future, but for now it does exactly what 
I want it to do. Below you'll find the write up that Codex compiled.

Big Daddy is a Windows desktop break-management application built with C# and Windows Forms on .NET 10.

It is currently an Alpha project. The source is published so that people can inspect it, learn from it, experiment, report issues, and contribute improvements.

## Windows download (Alpha)

For a ready-to-run Windows build, download [BigDaddy-Alpha.exe](https://github.com/CaptJBriggs/BigDaddy/releases/download/alpha-2.5/BigDaddy-Alpha.exe) from the [Alpha 2.5 release](https://github.com/CaptJBriggs/BigDaddy/releases/tag/alpha-2.5).

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
