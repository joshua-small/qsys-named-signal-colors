# Named Signal Colors

Preview and apply role-based colors to selected Named Signals or the entire design. Review the proposed groups before applying; matching tags across pages change together. Open **Tools > Q-SYS Mods > Named Signal Colors**.

**Preview selection** uses selected Named Signal tags and named pins. Components, annotations, shapes and unnamed pins are ignored, even in a mixed selection. Selecting a component does not include its pins. Changing only ignored objects does not invalidate a preview; selected Named Signals and their associated groups are still rechecked before Apply. If no Named Signals are selected, the preview is empty and Apply stays disabled.

Selection and entire-design previews can include more than 256 groups. The remaining bounds are 256 associated pins per group and 4096 associated pins per operation; selection also allows up to 4096 relevant tag/pin graphics. Apply rechecks the preview before making one undoable operation.

**File > Preferences > Named Signal Colors** contains the palette and **Only change default colors**, default **off**. Enabling the filter preserves groups that contain non-default colors. Role and selection/design scope remain in the operation window. Saving Preferences refreshes an existing preview without applying it.

Each role uses the same color picker as Designer 10.5's signal-trace preferences, with a color swatch, popup picker and exact color entry. Transparency is disabled. Scroll to reach every role. Existing palettes keep their opaque `#RRGGBB` format; edits are saved with **OK** and discarded with **Cancel**.

The first preferences read imports `%LOCALAPPDATA%\QsysSchematicTools\Designer-10.5\signal-palette.json` if present. A successful Preferences save persists it in Core settings. The original palette remains unchanged for rollback. Invalid palettes are reported rather than discarded.

Requires [Q-SYS Mods Core](https://github.com/joshua-small/qsys-mods-core) 0.1.x, Mod Manager 0.8+, and the exact supported Designer 10.5 build. Use the manager's Schematic Tools migration. Better Shortcuts is optional.

Clone Core beside this repository, then build `Designer.Modules.NamedSignalColors.csproj` with .NET 10.0.112 and `-p:DesignerDir="C:\Program Files\QSC\Q-SYS Designer 10.5"`. Run `dotnet run --project tests/SignalColorRules.Tests.csproj` for 71 rule/metadata checks. Core's guarded native driver checks the default preference and themed integration. Applying colors retains the existing preview and host undo behavior; no new hardware support is claimed.

`tests/Preferences/Preferences.csproj` checks the actual Windows color controls, saved palette round trips and draft isolation without opening Designer or changing user settings. Build with the same `DesignerDir` and run on Windows. These control checks do not establish in-Designer popup, keyboard or theme acceptance.

Run `dotnet run --project tests/Palette/Palette.Tests.csproj` with .NET 10 to check the production legacy-palette importer and its interaction with the inference rules. This suite uses synthetic temporary files and needs no Core checkout, vendor assemblies or installed Designer. It covers custom and partial palettes, malformed input, byte-for-byte preservation of the legacy file, independent reads, settings JSON round trips, distinct control roles, explicit overrides and mixed-group filtering. These offline checks do not establish native Preferences persistence, preview refresh, physical input, Apply/undo or design save/reopen behavior.

Run `dotnet run --project tests/Selection/Selection.Tests.csproj` to check the production selection reader with synthetic graphic/pin objects. It covers mixed and empty selections, ignored components and unnamed pins, duplicate references, relevant-object bounds and malformed Named Signal representations. It needs no Core or vendor assemblies. Native mixed-selection preview, Apply revalidation and undo still require Designer acceptance.

Run `dotnet run --project tests/Preview/Preview.Tests.csproj` to exercise the production preview and Apply revalidation code against a synthetic host. Cases include 257–4096 groups, mixed selections, ignored selection changes, stale groups/pin evidence/colors and the remaining pin limits. The synthetic host does not establish native color propagation, undo or Designer acceptance.

Version 0.1.3 passed 113 scoped native checks on Designer 10.5.0 with Mods Core 0.1.9: mixed and ignored-only selections, stale-preview rejection, 300 selected groups with 305 associated pins, 303 whole-design groups, one Undo/Redo per color batch, and fresh-process save/reopen. Fixture setup and controlled stale-state changes used in-process APIs; Preview, role choice, Apply, Undo/Redo and Save As used mouse/keyboard input. Names, pin/provider identities and named Tx/Rx membership were preserved. The fixture had no wired routes. Optional 1024-group native testing, oversized single-group rejection and runtime/hardware acceptance are not established by these checks.

The same 0.1.3 DLL also passed 34 scoped checks on Designer 10.5.1 with Mods Core 0.1.11: mixed and ignored-only selections, 300 selected groups/305 associated pins, 303 whole-design groups/309 named pins, grouped Apply/Undo/Redo, and Preferences Cancel. A saved file reopened in a different process with all 618 pin records, all 303 groups and page identities unchanged, a clean document, and empty Undo/Redo history. Seven Core Preferences checks also passed, including page selection, modal ownership and Cancel preserving settings and shortcut bindings. These checks used disconnected Design mode; stale-preview coverage remains the separate 10.5.0 result above. The package host guard is unchanged, so installation on the tested 10.5.1 build still requires the current Mod Manager's exact-build consent.

Designer 10.5 acceptance for 0.1.1 used a separate hash-matched application copy: all 24 controls loaded, native popup changes reached the palette, exact RGB text committed and survived OK/reopen, Cancel preserved the settings file, and scrolling reached the final role. Light/dark preferences and popup renders were inspected. Popup binding, text commit, persistence and scrolling were exercised by an in-process test harness; this does not claim physical keyboard/mouse coverage or hardware validation. [Validation and release tracking](https://github.com/joshua-small/qsys-named-signal-colors/pull/5).

Extracted from Schematic Tools commit `13c52e6`. [Split tracking](https://github.com/joshua-small/qsys-schematic-tools/issues/4).
