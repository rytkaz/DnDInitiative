# DnD Initiative

[![CI](https://github.com/rytkaz/DnDInitiative/actions/workflows/ci.yml/badge.svg)](https://github.com/rytkaz/DnDInitiative/actions/workflows/ci.yml)

A Windows desktop app for running D&D 5e combat (2024 rules): an initiative tracker, a stat block and spell library, an encounter builder with XP budgets, and party management. Encounters can be started straight from a link in your Obsidian notes, and a campaign is a folder of plain files you can keep in git.

Built with C# 14, .NET 10 and WPF.

## Features

**Initiative tracker**

- Rolls initiative for creatures, with numbered duplicates ("Goblin 1", "Goblin 2") and optional disadvantage
- Tracks rounds, turns, hit points and armor class; damage, healing and direct HP edits from a popup
- Reorder by editing initiative or by dragging rows; reroll initiative for the creatures without touching the players
- Add creatures by hand or from the library in popups that stay out of the way during combat
- Read the selected creature's stat block in a resizable side panel

**Stat blocks and spells**

- Searchable library with a viewer styled after the printed books
- Create, edit and copy stat blocks; your own are marked and saved with the campaign
- Spell names in a stat block are links: click one to read the spell in a popup
- Import whole books, homebrew included, with one button (see [Importing stat blocks and spells](#importing-stat-blocks-and-spells))

**Encounter builder**

- Build encounters from the library and see total XP against the party's Low, Moderate and High budgets
- Start an encounter in the tracker with one click
- Copy a Markdown link that opens the app and starts the encounter

**Parties**

- Save parties with each character's level, hit points and armor class
- The selected party joins every new combat automatically

**Campaigns**

- A campaign is a folder: one readable JSON file per party, encounter and stat block
- Put the folder in git, or any sync service, to share it or move between computers
- Files changed outside the app are picked up while it runs

**General**

- Light and dark mode, following the Windows setting
- The page list folds away to give a page more room
- Window size and panel sizes are remembered between runs
- Confirmation before deleting, and a warning before closing with unsaved changes

## Install

1. Download the latest `DnDInitiative-vX.Y.Z-win-x64.zip` from [Releases](https://github.com/rytkaz/DnDInitiative/releases).
2. Unzip it anywhere and run `DnDInitiative.App.exe`.

Nothing else needs to be installed; the .NET runtime is included. The executable is not code-signed, so Windows SmartScreen may show a warning on first run (More info, then Run anyway).

Requires 64-bit Windows 10 or later.

## Campaigns

The first run creates a campaign in `Documents\DnD Initiative\My Campaign`. **Open campaign...** in the side panel opens another folder, or starts a new campaign in an empty one.

| Path | Contents |
|---|---|
| `campaign.json` | The campaign's name and file format version |
| `parties\` | One file per party |
| `encounters\` | One file per encounter |
| `statblocks\` | One file per stat block you created or edited |

Imported books are not part of the campaign. They stay in a database on each computer (see [Where your data lives](#where-your-data-lives)), so a campaign folder holds only your own work and stays small. An imported stat block you edit is saved to the campaign and takes the place of the imported one.

To use a campaign on a second computer, sync the folder and import the same books there. Imported stat blocks get the same identity on every computer, so encounters keep pointing at the right creatures.

When files in the folder change while the app is open, for example after a `git pull`, the app reloads them. If that would discard unsaved edits or a combat in progress, it offers a **Reload** button instead.

## Starting an encounter from Obsidian

In the encounter builder, select an encounter (and optionally a party) and choose **Copy link**. Paste the result into a note:

```markdown
[Goblin Ambush](dndinitiative://encounter/3f2c8a5e-...?party=9b1d...)
```

Clicking the link opens the app, or brings the running one to the front, and starts that encounter with that party. The app registers the `dndinitiative://` link type for the current Windows user every time it starts, so there is nothing to set up.

## Importing stat blocks and spells

The app does not ship any game content. Stat blocks and spells come from your own data in 5eTools format, converted by [ttrpg-convert-cli](https://github.com/ebullient/ttrpg-convert-cli).

On the **5eTools Import** page:

1. **Choose the data.** Use **Browse...** to pick the `data` folder of your copy, or **From zip...** to unpack it from a zip file you downloaded.
2. **Tick the books** whose monsters you want. The list has a search box, and **Select all** and **Clear** act on the books the search is showing.
3. **Add homebrew files**, if you have any: books in 5eTools homebrew format (`.json`). Every monster in a listed file is imported.
4. Choose **Convert and import**.

Importing again updates what is already in the library instead of duplicating it. Stat blocks you have edited yourself are left alone.

Spells are imported from every book in the data, whichever books are ticked, because creatures cast spells from books other than their own.

### The converter

The first conversion downloads the latest Windows release of ttrpg-convert-cli from its GitHub releases page, checks the download against the checksum published with it, and runs it on your computer. It is a separate open-source project, not part of this app. **Download latest converter** replaces the copy with the newest release.

If you would rather use a copy you downloaded yourself, set its path under **ttrpg-convert program**.

### Using your own configuration

By default the app writes the converter's configuration for you. To take control, set **Configuration file** to a configuration of your own; it then replaces the book list, the homebrew list and the spell handling. Its monster template must write a [Fantasy Statblocks](https://github.com/javalent/fantasy-statblocks) code block, which is what the importer reads.

## Where your data lives

Your campaigns are wherever you put them. Everything else is under `%AppData%\DnDInitiative` and belongs to one computer:

| Path | Contents |
|---|---|
| `dndinitiative.db` | Imported stat blocks and spells (SQLite) |
| `app-state.json` | Which campaign was open last |
| `ui-state.json` | Window size and panel layout |
| `conversion-settings.json` | The choices made on the import page |
| `converter\` | The downloaded converter and the configuration written for it |
| `source-data\` | Data unpacked from a zip file |
| `logs\` | One log file per day, kept for 14 days |

To back up your work, back up the campaign folders. The rest can be rebuilt by importing again.

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
| `DnDInitiative.Core` | Game rules and domain types: dice, combatants, turn order, XP budgets, encounter links, stat blocks, spells, campaigns. Also the interfaces the outer layers implement (repositories, importers, converter). | Nothing |
| `DnDInitiative.Infrastructure` | Everything that touches the outside world: campaign folders and their file watcher, SQLite through EF Core, the Markdown importers, downloading the converter and running it as a child process, settings files. | Core |
| `DnDInitiative.App` | The WPF user interface: views, view models, dialogs, startup. Built on MVVM with [CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/) and the .NET generic host for dependency injection. | Core, Infrastructure |

Each project has a matching test project under `tests/`.

### Repository layout

```
src/
  DnDInitiative.Core/            Domain and interfaces
  DnDInitiative.Infrastructure/  Persistence, campaigns, import, conversion
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

ttrpg-convert-cli is a separate project by its own authors, used under its own license. This app downloads it on request and does not redistribute it.
