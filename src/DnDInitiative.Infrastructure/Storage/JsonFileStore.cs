using System.Text.Json;

namespace DnDInitiative.Infrastructure.Storage;

/// <summary>
/// Reads and writes a list of items as a single JSON file.
/// </summary>
internal sealed class JsonFileStore<T>
{
    private static readonly JsonSerializerOptions s_serializerOptions = new() { WriteIndented = true };

    private readonly string _filePath;

    public JsonFileStore(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        _filePath = filePath;
    }

    public async Task<List<T>> ReadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_filePath))
        {
            return [];
        }

        string json = await File.ReadAllTextAsync(_filePath, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize<List<T>>(json, s_serializerOptions) ?? [];
    }

    public async Task WriteAsync(List<T> items, CancellationToken cancellationToken)
    {
        string? directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Write to a temporary file and swap it in, so a crash mid-write
        // cannot leave a half-written file behind.
        string temporaryPath = _filePath + ".tmp";
        string json = JsonSerializer.Serialize(items, s_serializerOptions);
        await File.WriteAllTextAsync(temporaryPath, json, cancellationToken).ConfigureAwait(false);
        File.Move(temporaryPath, _filePath, overwrite: true);
    }
}