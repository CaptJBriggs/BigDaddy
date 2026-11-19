# BigDaddy (Yes, it is a Nic Cage reference)
<img width="800" height="482" alt="image" src="https://github.com/user-attachments/assets/98030081-b6a3-459e-af83-0dd4badb1f43" />
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

I made this application with the help of Codex because I couldn't find anything quite like it in the Windows space.

BigDaddy is largely inspired by **SafeEyes on Linux**, but takes a slightly more robust approach. Where SafeEyes reminds you to take a break, BigDaddy **ensures that you actually do**.

Needless to say, this application is primarily intended for children.

## The philosophy behind BigDaddy

One of my guiding principles during development has been:

**"If Windows can already do it, why code it?"**

Rather than reinventing the wheel, BigDaddy aims to make use of functionality already available within Windows. Where possible, it relies on existing Windows operating-system features and functions instead of introducing unnecessary custom replacements.

This keeps the application relatively lightweight, avoids duplicating functionality that Windows already provides, and allows BigDaddy to work alongside the operating system rather than trying to replace parts of it.

## My use case

Here's the scenario I wanted to solve:

Steam can handle the scheduling of *when* my son is allowed to play. BigDaddy handles the frequency of his breaks.

On my son's PC, I've configured it so that he can play for two hours at a time, but must then take a two-hour break. During those two hours, BigDaddy also enforces a five-minute break every thirty minutes.

The schedule looks like this:

- 30 minutes — Play
- 5 minutes — Break
- 30 minutes — Play
- 5 minutes — Break
- 30 minutes — Play
- 5 minutes — Break
- 30 minutes — Play
- 120 minutes — Long Break

The important distinction is that the short breaks do **not** count towards the two hours of actual play time.

There are a few other things worth mentioning:

- **Keyboard blocking:** When an emergency PIN is configured, normal keyboard input is blocked during breaks. The PIN provides temporary emergency keyboard access. Without a PIN configured, keyboard input remains available.
- **Idle detection:** If the user walks away from the PC and doesn't interact with the mouse or keyboard, the play timer pauses after the configured idle period.
- **Session recovery:** Restarting the PC does not simply reset the schedule. BigDaddy can recover an interrupted session.
- **Daily reset:** The daily counters reset automatically at midnight.

It isn't foolproof, and there are still loopholes I intend to address as development continues. However, for now, it does exactly what I wanted it to do.

Below is the more formal project overview.

---

## About BigDaddy

BigDaddy is a Windows desktop break-management application built with **C# and Windows Forms on .NET 10**.

It is currently an Alpha project. The source code is publicly available so that people can inspect it, learn from it, experiment with it, report issues, and contribute improvements.

Development is carried out with assistance from Codex, with new features and improvements being introduced as time and resources allow.

## Development Roadmap

The following items represent planned work beyond the current Alpha 2.5 release. Some are defined development milestones, while others remain ideas to investigate.

### 1. Administrator-Controlled Configuration

The next major development milestone.

- Introduce protected, machine-level configuration storage.
- Require Windows UAC administrator approval for changes to settings and schedule controls.
- Protect actions such as Start, Pause, Resume, Stop, and Exit from unauthorised changes by standard users.
- Keep BigDaddy running normally within the signed-in user's Windows session, without requiring administrator privileges for everyday operation.

This is intended to provide administrator-controlled configuration, not to claim that a desktop application is completely tamper-proof.

### 2. Formal Windows Installer

Move beyond the current portable executable.

- Create a per-machine Windows installer.
- Elevate privileges only when installing or updating.
- Install application files in a protected location.
- Provide Start Menu shortcuts and a proper uninstall entry.
- Ensure normal operation does not require administrator privileges.
- Test installation, upgrades, autostart, recovery, and removal.

### 3. Force Break

Introduce an administrator-triggered break outside the normal schedule.

- Add a Force Break action.
- Reuse the existing PIN architecture where appropriate.
- Determine whether entering the PIN should restore keyboard access or end the forced break entirely.
- Keep forced breaks separate from the normal scheduled-break system.

### 4. Optional Break Skipping

An optional feature, currently deferred.

- Allow a break to be skipped for a configurable period.
- Set a maximum number of permitted skips per day.
- Display the remaining skips.
- Reset the allowance according to the local calendar day.

This is not currently a priority and will not be implemented ahead of the more important enforcement work.

### 5. Multi-Monitor and Display Support

Improve reliability across different hardware and display configurations.

- Test break overlays across multiple monitors.
- Determine whether a break should cover every connected display.
- Improve application scaling and layout on smaller screen resolutions.
- Test overlay placement and visibility at different Windows DPI/scaling settings.
- Confirm that controls remain accessible and usable on smaller displays.

### 6. Emergency Access and Overlay Behaviour

Improve the experience when an administrator needs to access the PC during a break.

- Correct the current behaviour where the break overlay remains permanently on top after emergency PIN access is granted.
- Allow the administrator to interact with the desktop and other applications after using the emergency PIN, without unintentionally ending or resetting the active break.
- Preserve the existing break countdown and session state.
- Ensure emergency access works reliably at different screen resolutions.

### 7. Testing and Reliability

Strengthen the application as it grows.

- Introduce automated tests for scheduling, break transitions, play-session calculations, idle handling, manual pause, stopping, and session recovery.
- Maintain a manual regression checklist for important features.
- Add diagnostic logging where it would help investigate problems.

### 8. Source Code and Release Improvements

Improve the project and its distribution.

- Establish consistent source formatting and line-ending rules.
- Improve versioning and release management.
- Add a proper application icon.
- Provide clearer release notes.
- Investigate optional code signing.
- Investigate the planned application process-name spoofing feature in Task Manager.

### 9. Application Monitoring

Investigate whether a separate monitoring mechanism could detect when BigDaddy unexpectedly closes.

The goal would be to explore recovery and reliability options if the application is no longer running. This would need to respect Windows security boundaries and the limitations of applications running within a standard user's session.

### 10. Additional Scheduling Options

Investigate whether BigDaddy should eventually include its own scheduling functionality.

This could allow users to configure when a schedule should start or operate, rather than relying entirely on an external application such as Steam to manage play times.

The scope and behaviour of an integrated scheduler have not yet been defined.

### 11. Additional Long-Break Features

A few ideas for making longer breaks more informative and useful:

- Display the time at which the next Playing session is scheduled to begin during a Long Break.
- Investigate optional power-saving behaviour during longer breaks.
- Consider whether the application should offer a Sleep option or automatically enter a power-saving state after a configurable period.

These features remain exploratory.

### 12. Usage History and Statistics

Investigate keeping a record of computer usage.

Potential features include:

- Daily play-time totals.
- Weekly and monthly usage summaries.
- Historical break and play-session records.
- Configurable log-retention periods.

The aim would be to provide a clearer picture of computer usage over time.

---

## Current Project Status

BigDaddy remains an Alpha application. Features and behaviour may change between releases, and the project has not yet reached its planned administrator-controlled configuration and installation milestones.

The source code is available for anyone interested in following the project, learning from the implementation, reporting issues, or contributing.

## Status and limitations

Big Daddy is not a hardened or tamper-proof enforcement tool. It runs in the signed-in user's Windows session. Development and improvements are introduced as time and resources allow.

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
(I am not a programmer or developer, just a very determined Dad)

## Licence

This project is licensed under the GNU General Public License v3.0. See [LICENSE](LICENSE).

**The goal is simple: SafeEyes reminds. BigDaddy ensures.**
