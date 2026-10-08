using System.Text.Json;

using DnDInitiative.Core.Parties;

namespace DnDInitiative.Infrastructure.Parties;

/// <summary>
/// Stores all parties in a single JSON file.
/// </summary>
public sealed class JsonPartyRepository : IPartyRepository
{
    private static readonly JsonSerializerOptions s_serializerOptions = new() { WriteIndented = true };

    private readonly string _filePath;

    public JsonPartyRepository(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        _filePath = filePath;
    }

    public async Task<IReadOnlyList<Party>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await ReadAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task SaveAsync(Party party, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(party);

        List<Party> parties = await ReadAsync(cancellationToken).ConfigureAwait(false);

        int index = parties.FindIndex(existing => existing.Id == party.Id);
        if (index >= 0)
        {
            parties[index] = party;
        }
        else
        {
            parties.Add(party);
        }

        await WriteAsync(parties, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        List<Party> parties = await ReadAsync(cancellationToken).ConfigureAwait(false);

        if (parties.RemoveAll(party => party.Id == id) > 0)
        {
            await WriteAsync(parties, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<List<Party>> ReadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_filePath))
        {
            return [];
        }

        string json = await File.ReadAllTextAsync(_filePath, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize<List<Party>>(json, s_serializerOptions) ?? [];
    }

    private async Task WriteAsync(List<Party> parties, CancellationToken cancellationToken)
    {
        string? directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Write to a temporary file and swap it in, so a crash mid-write
        // cannot leave a half-written parties file behind.
        string temporaryPath = _filePath + ".tmp";
        string json = JsonSerializer.Serialize(parties, s_serializerOptions);
        await File.WriteAllTextAsync(temporaryPath, json, cancellationToken).ConfigureAwait(false);
        File.Move(temporaryPath, _filePath, overwrite: true);
    }
}