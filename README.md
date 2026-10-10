# DnD Initiative

[![CI](https://github.com/rytkaz/DnDInitiative/actions/workflows/ci.yml/badge.svg)](https://github.com/rytkaz/DnDInitiative/actions/workflows/ci.yml)

A Windows desktop app for running D&D 5e combat (2024 rules): an initiative tracker, a stat block library, an encounter builder with XP budgets, and party management. Encounters can be started straight from a link in your Obsidian notes.

Built with C# 14, .NET 10 and WPF.

![The initiative tracker during a combat](docs/screenshots/tracker.png)

| Stat block library | Encounter builder |
|---|---|
| ![A stat block in the library](docs/screenshots/stat-blocks.png) | ![An encounter with its difficulty](docs/screenshots/encounter-builder.png) |

## Features

**Initiative tracker**

- Rolls initiative for creatures, with numbered duplicates ("Goblin 1", "Goblin 2") and optional disadvantage
- Tracks rounds, turns, hit points and armor class; damage, healing and direct HP edits from a popup
- Reorder by editing initiative or by dragging rows
- Add creatures by hand or from the library, and read the selected creature's stat block in a side panel

**Stat blocks**

- Searchable library with a viewer styled after the printed books
- Create, edit and copy stat blocks
- Bulk import from Markdown notes produced by [ttrpg-convert-cli](https://github.com/ebullient/ttrpg-convert-cli), either from an existing folder or by running the converter from inside the app with live output and cancel

**Encounter builder**

- Build encounters from the library and see total XP against the party's Low, Moderate and High budgets
- Start an encounter in the tracker with one click
- Copy a Markdown link that opens the app and starts the encounter

**Parties**

- Save parties with each character's level, hit points and armor class
- The selected party joins every new combat automatically

**General**

- Light and dark mode, following the Windows setting
- Confirmation before deleting, and a warning before closing with unsaved changes
- All data is stored locally in a SQLite database

## Install

1. Download the latest `DnDInitiative-vX.Y.Z-win-x64.zip` from [Releases](https://github.com/rytkaz/DnDInitiative/releases).
2. Unzip it anywhere and run `DnDInitiative.App.exe`.

Nothing else needs to be installed; the .NET runtime is included. The executable is not code-signed, so Windows SmartScreen may show a warning on first run (More info, then Run anyway).

Requires 64-bit Windows 10 or later.

## Starting an encounter from Obsidian

In the encounter builder, select an encounter (and optionally a party) and choose **Copy link**. Paste the result into a note:

```markdown
[Goblin Ambush](dndinitiative://encounter/3f2c8a5e-...?party=9b1d...)
```

Clicking the link opens the app, or brings the running one to the front, and starts that encounter with that party. The app registers the `dndinitiative://` link type for the current Windows user every time it starts, so there is nothing to set up.

## Importing stat blocks

The app does not ship any game content. Stat blocks come from your own data, converted by [ttrpg-convert-cli](https://github.com/ebullient/ttrpg-convert-cli).

The importer reads Markdown notes that contain a [Fantasy Statblocks](https://github.com/javalent/fantasy-statblocks) code block, so the converter's configuration must use a monster template that writes one.

On the **5eTools Import** page, choose:

| Setting | Value |
|---|---|
| ttrpg-convert program | `ttrpg-convert.exe` |
| Configuration file | Your converter configuration (`.json`), optional |
| 5eTools data folder | The `data` folder of your 5eTools copy |
| Output folder | Where the converter writes its notes |

**Convert and import** runs the converter, then imports every stat block from the output folder. Importing again updates creatures that have the same name and source instead of duplicating them.

If you already have converted notes, **Import...** on the Stat Blocks page reads a folder directly.

## Where your data lives

Everything is under `%AppData%\DnDInitiative`:

| Path | Contents |
|---|---|
| `dndinitiative.db` | Parties, stat blocks and encounters (SQLite) |
| `conversion-settings.json` | The paths from the import page |
| `logs\` | One log file per day, kept for 14 days |

To back up or move your data, copy that folder.

## Building from source

Requirements: Windows, and the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```powershell
git clone https://github.com/rytkaz/DnDInitiative.git
cd DnDInitiative

dotnet build DnDInitiative.slnx
dotnet test DnDInitiative.slnx
dotnet run --project src/DnDInitiative.App
```

Or open `DnDInitiative.slnx` in Visual Studio 2022 or later.

Warnings are treated as errors, and the recommended .NET analyzers and code style rules are enforced during the build (see `Directory.Build.props` and `.editorconfig`).

## Architecture

The solution has three projects. Dependencies point inward: the app knows about everything, the core knows about nothing.

```
DnDInitiative.App  ──▶  DnDInitiative.Infrastructure  ──▶  DnDInitiative.Core
       └──────────────────────────────────────────────────────────▲
```

| Project | Responsibility | Depends on |
|---|---|---|
| `DnDInitiative.Core` | Game rules and domain types: dice, combatants, turn order, XP budgets, encounter links. Also the interfaces the outer layers implement (repositories, importer, converter). | Nothing |
| `DnDInitiative.Infrastructure` | Everything that touches the outside world: SQLite through EF Core, the Markdown importer, running the converter as a child process, the settings file. | Core |
| `DnDInitiative.App` | The WPF user interface: views, view models, dialogs, startup. | Core, Infrastructure |

Each project has a matching test project under `tests/`.

### Design decisions

- **MVVM with [CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/).** View models hold all behaviour and are tested without any UI. Code-behind is limited to things that are purely visual, such as drag-and-drop and scrolling.
- **Dependency injection through the .NET generic host.** `App.xaml.cs` is the composition root. Nothing else creates services.
- **Anything the user interface asks of Windows sits behind an interface** (file and folder pickers, confirmation dialogs, the clipboard), so tests can answer on the user's behalf.
- **Pages communicate through a messenger, not references.** The encounter builder announces "start this combat"; the tracker and the main window each react. Neither page knows the other exists.
- **Storage entities are separate from domain types.** The EF Core entities are internal to Infrastructure, so database concerns never leak into the rules.
- **Schema changes are EF Core migrations**, applied automatically at startup.
- **Single instance.** A second launch (for example from a link) hands its arguments to the running app over a named pipe and exits.
- **Errors are logged, not swallowed.** Three global handlers catch anything unexpected, write it to the log with [Serilog](https://serilog.net/), and tell the user where to find it.

### Repository layout

```
src/
  DnDInitiative.Core/            Domain and interfaces
  DnDInitiative.Infrastructure/  Persistence, import, conversion
  DnDInitiative.App/             WPF application
tests/
  DnDInitiative.Core.Tests/
  DnDInitiative.Infrastructure.Tests/
  DnDInitiative.App.Tests/
.github/workflows/               CI and release pipelines
Directory.Build.props            Compiler and analyzer settings for every project
Directory.Packages.props         Central NuGet package versions
```

## Development

### Changing the database schema

The schema is defined by the entity classes in `src/DnDInitiative.Infrastructure/Persistence`. After changing them, add a migration:

```powershell
dotnet tool restore
dotnet ef migrations add DescribeTheChange --project src/DnDInitiative.Infrastructure --startup-project src/DnDInitiative.Infrastructure
```

The app applies pending migrations the next time it starts.

### Continuous integration

Every push to `master` and every pull request is built and tested on Windows by GitHub Actions. Dependabot opens pull requests for NuGet and workflow updates weekly.

### Releasing

Pushing a version tag builds a self-contained, single-file executable and publishes it as a GitHub release:

```powershell
git tag v1.0.0
git push origin v1.0.0
```

## Legal

This is an unofficial fan project. It is not affiliated with, endorsed by, or sponsored by Wizards of the Coast. Dungeons & Dragons and D&D are trademarks of Wizards of the Coast LLC.

The app contains no game content. Use the import feature only with content you own.
