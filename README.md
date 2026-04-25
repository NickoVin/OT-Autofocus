# OT-Autofocus

A mod for `On-Together: Virtual Co-Working` that automatically restarts the current focus session.

The main purpose of this mod is to make it easier to farm in-game achievements that require a certain number of focus sessions with a specific activity type.

## What It Does

- Remembers the currently selected focus mode.
- Takes the character out of focus every 2 minutes.
- Returns the character to the same focus mode after 1 second.
- Stops automatically if the player leaves focus manually.

## Commands

- `/afstart` - start autofocus. You must already be in focus.
- `/afstop` - stop autofocus.
- `/afhelp` - show a short help message.
- `/autofocus` - same as `/afhelp`.

Mod messages in chat are marked with the `[AF]` prefix.

## Limitations

- The mod does not choose a focus mode by itself. It repeats the one that is already active.
- Leaving focus is currently done by simulating a Space key press.
- If a game text input is active, leaving focus through Space may not work.
- Tested on Windows 11 Pro 22H2 and game version v1.0.7.

## Installation

The easiest way to run the mod is through Thunderstore Mod Manager or r2modman:

1. Create a profile for `On-Together: Virtual Co-Working`.
2. Install BepInEx for that profile.
3. Open the profile folder.
4. Copy the built `Autofocus.dll` into `BepInEx/plugins/Autofocus/`.

For manual installation, build the project and copy:

```text
Autofocus/Autofocus/bin/Release/netstandard2.1/Autofocus.dll
```

to:

```text
<game_folder>/BepInEx/plugins/Autofocus/Autofocus.dll
```

## Build

```powershell
dotnet build .\Autofocus\Autofocus\Autofocus.csproj -c Release
```

## Status

Experimental mod. The main mechanic works, but the implementation depends on internal game classes and Harmony patches.
