using System.Text.Json;

using DnDInitiative.Core.StatBlocks;
using DnDInitiative.Infrastructure.Persistence;

namespace DnDInitiative.Infrastructure.Import;

internal sealed class JsonConversionSettingsStore : IConversionSettingsStore
{
    // Options objects are expensive to build and safe to share, so there is one for the class.
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    private readonly string _directory;
    private readonly string _filePath;

    public JsonConversionSettingsStore(StorageOptions options)
    {
        _directory = options.DataDirectory;
        _filePath = Path.Combine(_directory, "conversion-settings.json");
    }

    public async Task<ConversionSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
        {
            return new ConversionSettings();
        }

        string json = await File.ReadAllTextAsync(_filePath, cancellationToken).ConfigureAwait(false);
        try
        {
            return JsonSerializer.Deserialize<ConversionSettings>(json, SerializerOptions) ?? new ConversionSettings();
        }
        catch (JsonException)
        {
            // A damaged settings file only costs the user four paths. Start again from empty
            // rather than refusing to open the page.
            return new ConversionSettings();
        }
    }

    public async Task SaveAsync(ConversionSettings settings, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_directory);
        string json = JsonSerializer.Serialize(settings, SerializerOptions);
        await File.WriteAllTextAsync(_filePath, json, cancellationToken).ConfigureAwait(false);
    }
}