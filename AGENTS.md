# AGENTS.md

Guidance for AI coding agents working on DnD Initiative. Read this before changing code. The README covers the app from the user's side; this file covers how to work on it.

## The project

A Windows desktop app for running D&D 5e (2024) combat: initiative tracker, stat block and spell library, encounter builder, parties. C# 14, .NET 10, WPF, MVVM.

It is also a portfolio project. Code should read like a professional team's work: clean, small classes, named for what they do, with comments that explain *why*.

## Commands

Run from the repository root. Windows only, because WPF and some tests need it.

```powershell
dotnet build DnDInitiative.slnx
dotnet test DnDInitiative.slnx
dotnet run --project src/DnDInitiative.App

# After changing an entity in src/DnDInitiative.Infrastructure/Persistence:
dotnet tool restore
dotnet ef migrations add DescribeTheChange --project src/DnDInitiative.Infrastructure --startup-project src/DnDInitiative.Infrastructure
```

- **Warnings are errors**, with the recommended analyzers and code style enforced (`Directory.Build.props`, `.editorconfig`). A change isn't done until it builds without warnings.
- **Changing the EF model means adding a migration.** Without one, every test that uses `TestDatabase` fails, because migrating refuses to run while the model has changes no migration covers. The app applies migrations at startup.
- **CI** (`.github/workflows/ci.yml`) builds and tests in Release on `windows-latest` for every push to `master` and every pull request. A `v*` tag publishes a release through `release.yml`.

If you can't build in your environment, say so plainly. Don't claim code compiles or tests pass unless you ran them.

## Layout

```
src/DnDInitiative.Core            Domain types, rules and interfaces. Depends on nothing.
src/DnDInitiative.Infrastructure  Files, SQLite (EF Core), import, the converter. Depends on Core.
src/DnDInitiative.App             WPF: views, view models, startup. Depends on both.
tests/<Project>.Tests             One test project per project, folders mirroring src.
```

Inside each project, code is grouped **by feature**, not by kind: `Spells/`, `Conditions/`, `Encounters/`, `Parties/`, `StatBlocks/`, `Campaigns/`. A repository goes in its feature folder (`Infrastructure/Spells/SqliteSpellRepository.cs`), not in a shared `Repositories/` folder. The exceptions:
- `Persistence/` holds EF entities, `AppDbContext` and migrations.
- `Import/` holds the converter pipeline.
- The App project uses `ViewModels/`, `Views/`, `Services/`, `Formatting/`, `Behaviors/`, `Messages/`.

## Architecture rules

- **Dependencies point inward.** Core references nothing, not even EF or WPF. Interfaces that the outer layers implement live in Core, in the feature folder.
- **EF entities are `internal`** to Infrastructure and mapped to and from Core records. Database concerns never reach Core or App.
- **Two stores:**
  - **The campaign folder** holds what the user makes: parties, encounters, spellbooks, their own stat blocks. One readable JSON file each, written through `JsonFolderStore<T>` with the options in `CampaignJson`. It is meant to be kept in git.
  - **The SQLite database** (`%AppData%\DnDInitiative`) holds imported content (stat blocks, spells, conditions) and belongs to one computer.
  - `CampaignStatBlockRepository` merges the two. A user's stat block shadows the imported one with the same Id.
- **Imported stat blocks have deterministic Ids** (`StatBlockIdentity`), so encounters keep pointing at the right creature on every computer and after a re-import. Don't change how those Ids are calculated.
- **Campaign JSON is a file format.** A new property must be optional (nullable, or with a default), so files written by older versions still load. `CampaignJson` refuses missing *required* values.
- **Dependency injection** through the generic host. Composition roots:
  - `App.xaml.cs`: app services, view models, windows.
  - `InfrastructureServiceCollectionExtension.cs`: storage and import.
  - Nothing else news up services. Forwarding registrations (`provider => provider.GetRequiredService<X>()`) let several interfaces share one instance.
- **MVVM with CommunityToolkit.Mvvm:**
  - Use `[ObservableProperty]` on fields, `[RelayCommand]`, and the generated `partial void OnXChanged` hooks.
  - View models hold all behaviour and are tested without UI.
  - Code-behind is only for purely visual work: drag-and-drop, resize thumbs, scrolling, focus.
  - Reusable view behaviour goes in an attached property under `Behaviors/`.
- **Pages talk through `IMessenger`**, not references. For example, the encounter builder sends `StartCombatMessage`.
- **Anything the UI asks of Windows goes behind an interface** (`IFilePicker`, `IConfirmationService`, `IClipboardService`, `ILinkOpener`, `IUiStateStore`), so tests can fake it. The same applies to time (`TimeProvider`) and dice (`IDiceRoller`).
- **UI layout state** (panel sizes, toggles, window size, sort order) is persisted through `IUiStateStore`:
  - Save with `Save(Load() with { ... })` so other pages' values survive.
  - Restore in the constructor by assigning the field, not the property, so restoring doesn't trigger a save.
  - For drag-resized sizes, save on `DragCompleted`, not on every change.
- **Every panel can be resized.** Any panel, list, side pane, editor or log area with a size of its own gets a drag handle, in new pages and when an existing page is changed. Follow the existing pattern (the book list on the import page, the stat block side panels):
  - A `Thumb` strip on the panel's edge, with a `SizeWE` or `SizeNS` cursor and a tooltip. Its `DragDelta` and `DragCompleted` handlers in code-behind only pass the change to the view model; that counts as purely visual work.
  - The size is a view model property whose setter clamps it between a minimum and a maximum, so a panel can't be dragged shut or past the window.
  - The size is remembered in `UiState` (a new nullable property) and restored as described above.
  - A panel that simply fills whatever space is left needs no handle of its own; the handle on its neighbour resizes it.

## The import pipeline

Content comes from the user's own 5eTools data, converted by [ttrpg-convert-cli](https://github.com/ebullient/ttrpg-convert-cli) into Markdown notes. The app reads those notes into SQLite and then discards them.

1. `GitHubConverterInstaller` downloads the converter and verifies its SHA-256.
2. `ConverterConfigWriter` writes `config.json` plus two templates:
   - `monster-statblock.txt` wraps `{resource.5eStatblockYaml}` in a `statblock` fence.
   - `spell-note.txt` writes a `spell` fence of `name: value` lines that `SpellNoteParser` reads back.

   It also copies homebrew files with `{@link text|url}` tags rewritten (`HomebrewLinks`). Every book with spells is converted, and `excludePattern` keeps the creatures of unticked books out.
3. `TtrpgConvertConverter` runs it through `IProcessRunner`.
4. The importers read the output:
   - `MarkdownStatBlockImporter` uses `FantasyStatblockParser`.
   - `MarkdownSpellImporter` uses `SpellNoteParser`.
   - `MarkdownConditionImporter` reads `rules/conditions.md` with `ConditionNoteParser`.
5. `ConverterLinks.Simplify` processes all imported text:
   - spell links become `[x](spell:key)`
   - condition links become `[x](condition:Name)`
   - `http(s)` links are kept
   - other links become plain text
   - HTML tags and images are removed
6. In the app, `InlineMarkdown`, `MarkdownBlocks` and `MarkdownText` turn that text into WPF inlines. Links resolve through `ReferenceLink` and `WebLink`, and `MainWindow` opens the reference popup or the browser.

The Homebrewery import (`Infrastructure/Homebrewery/`) is a second, separate pipeline:

1. `PdfLinkReader` reads the links out of a PDF with PdfPig; `HomebreweryLinks` keeps the Homebrewery ones.
2. `HomebreweryImporter` downloads each document's Markdown source from `/download/{shareId}`, a few at a time.
3. A document titled "... Conditions" goes to `HomebreweryConditionParser`. Conditions whose stored source is the PHB or SRD (`ConditionSources.IsCoreRules`) are not replaced.
4. Every document goes to `HomebreweryStatBlockParser`, which knows the legacy block-quote layout and the V3 `{{monster}}` layouts. `ConditionLinkRewriter` turns links into the conditions document into `condition:` links, and links condition names written without one (standard conditions in any case, homebrew ones only when capitalised). Creature names get the source appended, "Goblin (Source)", as the converter does for books other than the Monster Manual.
5. `ImportedStatBlocks` saves the stat blocks, with the same rules as the 5eTools import.

The GM Binder import (`Infrastructure/GmBinder/`) brings in martial exploits as `Ability` records. GM Binder keeps a document's Markdown source private, so `MartialExploitImporter` downloads the rendered page from `/share/{id}` and `MartialExploitParser` reads its HTML with AngleSharp. It walks the headings and paragraphs inside the `div.phb` pages in document order, because the pages close in the middle of sections: an h3 "Nth-Degree Exploits" sets the degree, each h4 is an exploit, and a leading `<em>Prerequisite(s): ...</em>` is split off. The document's title (`og:title`, without " by author") is the source.

When converter behaviour matters, check its source (`Tools5eLinkifier`, `QuteMonster`, `Json2QuteCompose`) rather than guessing. Note which assumptions you couldn't verify against real output.

## Code conventions

- **Analyzer habits:**
  - `string.Create(CultureInfo.InvariantCulture, $"...")` for interpolated numbers.
  - `[LoggerMessage]` source-generated logging.
  - `ConfigureAwait(false)` in Core and Infrastructure, never in App.
  - `ArgumentNullException.ThrowIfNull` and `ArgumentException.ThrowIfNullOrWhiteSpace` on public entry points.
  - Explicit `StringComparison` on every string comparison.
  - Catch specific exception types, never bare `Exception`.
- **`[GeneratedRegex]`** in `partial` classes, never `new Regex(...)`.
- **Records for domain data.** Records compare list properties by reference, so tests compare lists separately, or `SavedState` compares serialized JSON.
- **Comments say why**, in plain sentences. Don't restate what the code does.
- **Names** are full words, and tests read as sentences: `Method_Situation_ExpectedResult`, for example `SaveCommand_BlankName_DoesNotStoreAndStaysInEditMode`.
- **Files are UTF-8 with BOM and CRLF line endings.** Match the existing file when editing.
- **WPF projects don't get `System.IO` as an implicit using.** Files in App or App.Tests that use `Path`, `File` or `Directory` need `using System.IO;`.
- **No line in C# source may start with three backticks.** Build Markdown fences with `new string('`', 3)`, as `ConverterConfigWriter` and the tests do.

## WPF notes

- The theme is Fluent (`ThemeMode="System"` in `App.xaml`). Use theme brushes through `DynamicResource`, for example `TextFillColorSecondaryBrush`, `ControlStrokeColorDefaultBrush`, `AccentFillColorDefaultBrush`, `SystemFillColorCautionBackgroundBrush`.
- Fluent has no implicit `DataGridRow` style. A row style based on `{x:Type DataGridRow}` picks up the old Aero look, with white rows in dark mode. The tracker grid uses its own minimal row template for that reason.
- Shared styles live in `Styles/AppStyles.xaml`:
  - page and section titles
  - the parchment card colours (`StatBlockPaper`, `StatBlockInk`, `StatBlockAccent`, `StatBlockEdge`)
  - `MarkdownLink`
  - the `MarkdownTextBox` style

  The parchment colours are fixed and deliberately ignore dark mode.
- Selectable card text uses `MarkdownTextBox`, a read-only `RichTextBox` with `Label`, `LabelStyle` and `Markdown` properties. Use it for text the user may want to copy, and `TextBlock` elsewhere.
- Inside a data template, reach the page's command with `{Binding DataContext.XCommand, RelativeSource={RelativeSource AncestorType=...}}` and pass the row as `CommandParameter="{Binding}"`.
- Async lookups that fill a panel use a version counter, so a slow result can't overwrite a newer one. Copy the existing `ShowStatBlockAsync` pattern.

## Tests

- **xUnit** (`Xunit` is a global using). View models are tested with the hand-written fakes in `tests/DnDInitiative.App.Tests/Fakes`, not a mocking library. Add a fake there when a new interface needs one.
- **Infrastructure tests** use real files in temporary folders and a real SQLite file through `TestDatabase`, which runs the migrations.
- **Don't write new unit tests.** Changes no longer come with tests of their own.
  - Existing tests still have to build and pass. When a change breaks one, update it to match the new behaviour, or say why it should go.
  - Instead of tests, list in your summary what to check by hand.
- **Use a typed local for collection expressions in assertions:** `string[] expected = ["a", "b"]; Assert.Equal(expected, actual.ToArray());`. An untyped collection expression can make the `Assert.Equal` overload ambiguous.
- **Watcher tests are timing-based.** Keep their waits generous.

## Working with the maintainer

- **Work in small, reviewable steps.** For each one, explain what changed, why, and what was decided along the way. The maintainer reviews every change and builds and tests it himself.
- **Read the current file before editing.** Files are often edited by hand between sessions, so don't work from memory.
- **Ask before:**
  - adding packages
  - changing the campaign file format incompatibly
  - changing how imported stat block Ids are calculated
- **Point out new migrations, and files to delete,** in your summary. Some agent environments can write files but not delete them.
