# Named Signal Colors

Preview and apply role-based colors to selected Named Signals or the entire design. Review the proposed groups before applying; matching tags across pages change together. Open **Tools > Q-SYS Mods > Named Signal Colors**.

**File > Preferences > Named Signal Colors** contains the palette and **Only change default colors**, default **off**. Enabling the filter preserves groups that contain non-default colors. Role and selection/design scope remain in the operation window. Saving Preferences refreshes an existing preview without applying it.

The first preferences read imports `%LOCALAPPDATA%\QsysSchematicTools\Designer-10.5\signal-palette.json` if present. A successful Preferences save persists it in Core settings. The original palette remains unchanged for rollback. Invalid palettes are reported rather than discarded.

Requires [Q-SYS Mods Core](https://github.com/joshua-small/qsys-mods-core) 0.1.x, Mod Manager 0.8+, and the exact supported Designer 10.5 build. Use the manager's Schematic Tools migration. Better Shortcuts is optional.

Clone Core beside this repository, then build `Designer.Modules.NamedSignalColors.csproj` with .NET 10.0.112 and `-p:DesignerDir="C:\Program Files\QSC\Q-SYS Designer 10.5"`. Run `dotnet run --project tests/SignalColorRules.Tests.csproj` for 71 rule/metadata checks. Core's guarded native driver checks the default preference and themed integration. Applying colors retains the existing preview and host undo behavior; no new hardware support is claimed.

Extracted from Schematic Tools commit `13c52e6`. [Split tracking](https://github.com/joshua-small/qsys-schematic-tools/issues/4).
