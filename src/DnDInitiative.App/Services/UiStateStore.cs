using System.IO;
using System.Text.Json;

namespace DnDInitiative.App.Services;

/// <summary>How the window was arranged when the app was last used.</summary>
public sealed record UiState
{
    public bool IsNavigationCollapsed { get; init; }
}

/// <summary>Remembers the window's arrangement between runs.</summary>
public interface IUiStateStore
{
    /// <summary>Returns what was saved, or the defaults if nothing usable was.</summary>
    UiState Load();

    void Save(UiState state);
}

public sealed class JsonUiStateStore : IUiStateStore
{
    private readonly string _filePath;

    public JsonUiStateStore(string filePath)
    {
        _filePath = filePath;
    }

    // Both methods are synchronous on purpose. The file is a few bytes, and the main view
    // model needs the answer in its constructor, which cannot await anything.
    public UiState Load()
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
