using System.Text.Json;

using DnDInitiative.Core.Homebrewery;
using DnDInitiative.Infrastructure.Persistence;

namespace DnDInitiative.Infrastructure.Homebrewery;

/// <summary>
/// Keeps the Homebrewery page's choices in their own file, so the two import pages never
/// write over each other's settings.
/// </summary>
internal sealed class JsonHomebrewerySettingsStore : IHomebrewerySettingsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    private readonly string _directory;
    private readonly string _filePath;

    public JsonHomebrewerySettingsStore(StorageOptions options)
    {
        _directory = options.DataDirectory;
        _filePath = Path.Combine(_directory, "homebrewery-settings.json");
    }

    public async Task<HomebrewerySettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
        {
            return new HomebrewerySettings();
        }

        string json = await File.ReadAllTextAsync(_filePath, cancellationToken).ConfigureAwait(false);
        try
        {
            return JsonSerializer.Deserialize<HomebrewerySettings>(json, SerializerOptions) ?? new HomebrewerySettings();
        }
        catch (JsonException)
        {
            // A damaged file only costs a path, some links and a name. Start again from the defaults.
            return new HomebrewerySettings();
        }
    }

    public async Task SaveAsync(HomebrewerySettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        Directory.CreateDirectory(_directory);
        string json = JsonSerializer.Serialize(settings, SerializerOptions);
        await File.WriteAllTextAsync(_filePath, json, cancellationToken).ConfigureAwait(false);
    }
}
