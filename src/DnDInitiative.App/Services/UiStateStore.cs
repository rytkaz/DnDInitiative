using System.IO;
using System.Text.Json;

namespace DnDInitiative.App.Services;

/// <summary>How the window was arranged when the app was last used.</summary>
public sealed record UiState
{
    public bool IsNavigationCollapsed { get; init; }

    // The sizes are null until the user has dragged something, so each page keeps its own default.

    /// <summary>Width of the stat block panel on the tracker page.</summary>
    public double? StatBlockPanelWidth { get; init; }

    /// <summary>Height of the book list in the 5eTools import window.</summary>
    public double? SourcesPanelHeight { get; init; }

    /// <summary>Whether the tracker page shows its stat block panel. Null means never changed: shown.</summary>
    public bool? ShowStatBlockPanel { get; init; }

    /// <summary>Width of the stat block panel on the encounter builder page.</summary>
    public double? EncounterStatBlockPanelWidth { get; init; }

    /// <summary>Whether the encounter builder shows its stat block panel. Null means never changed: shown.</summary>
    public bool? ShowEncounterStatBlockPanel { get; init; }

    /// <summary>Whether the encounter list is sorted by date, newest first, and not by name.</summary>
    public bool SortEncountersByDate { get; init; }

    /// <summary>The main window's size when it is not maximized.</summary>
    public double? WindowWidth { get; init; }

    public double? WindowHeight { get; init; }

    public bool IsWindowMaximized { get; init; }

    /// <summary>The size of the import windows, shared by all of them.</summary>
    public double? ImportWindowWidth { get; init; }

    public double? ImportWindowHeight { get; init; }

    /// <summary>
    /// The sizes of the panels resized through <see cref="ViewModels.PanelSize"/>, by panel name.
    /// One dictionary rather than a property each, so a new resizable panel needs nothing here.
    /// </summary>
    public IReadOnlyDictionary<string, double>? PanelSizes { get; init; }
}

/// <summary>Remembers the window's arrangement between runs.</summary>
public interface IUiStateStore
{
    /// <summary>Returns what was saved, or the defaults if nothing usable was.</summary>
    UiState Load();

    /// <summary>
    /// Replaces everything that was saved. Several pages share this state, so change only
    /// your own part: <c>Save(Load() with { ... })</c>.
    /// </summary>
    void Save(UiState state);
}

public sealed class JsonUiStateStore : IUiStateStore
{
    private readonly string _filePath;

    // Read from the file once, then kept here. Every page asks for the state when it is
    // created and again each time it saves, and none of them should cost a file read.
    private UiState? _state;

    public JsonUiStateStore(string filePath)
    {
        _filePath = filePath;
    }

    // Both methods are synchronous on purpose. The file is a few bytes, and the main view
    // model needs the answer in its constructor, which cannot await anything.
    public UiState Load()
    {
        return _state ??= ReadFile();
    }

    private UiState ReadFile()
    {
        try
        {
            return File.Exists(_filePath)
                ? JsonSerializer.Deserialize<UiState>(File.ReadAllText(_filePath)) ?? new UiState()
                : new UiState();
        }
        catch (JsonException)
        {
            return new UiState();
        }
        catch (IOException)
        {
            return new UiState();
        }
        catch (UnauthorizedAccessException)
        {
            return new UiState();
        }
    }

    public void Save(UiState state)
    {
        // Losing this only costs the user one click next time, so a failure to write
        // it must never interrupt what they are doing.
        _state = state;

        try
        {
            if (Path.GetDirectoryName(_filePath) is { Length: > 0 } directory)
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(_filePath, JsonSerializer.Serialize(state));
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
