using System.Security.Cryptography;
using System.Text;

namespace DnDInitiative.Core.StatBlocks;

/// <summary>
/// Gives an imported stat block the same identity on every computer.
/// </summary>
public static class StatBlockIdentity
{
    /// <summary>What makes two imported stat blocks "the same creature": name and source, ignoring case.</summary>
    public static string Key(string name, string source)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(source);

        return $"statblock|{name.Trim().ToUpperInvariant()}|{source.Trim().ToUpperInvariant()}";
    }

    /// <summary>
    /// The Id of an imported stat block, calculated from its name and source.
    /// The calculation must never change: Ids are stored in encounters and shared between machines.
    /// </summary>
    public static Guid ForImported(string name, string source)
    {
        Span<byte> hash = stackalloc byte[SHA256.HashSizeInBytes];
        SHA256.HashData(Encoding.UTF8.GetBytes(Key(name, source)), hash);

        // A Guid is 16 bytes; the hash is 32. The first half is as unique as needed.
        Span<byte> bytes = hash[..16];

        // Two fields inside a Guid say what kind it is. Marking this one as
        // "version 8, custom" keeps it a well-formed Guid that tools will accept.
        bytes[6] = (byte)((bytes[6] & 0x0F) | 0x80);
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);

        return new Guid(bytes, bigEndian: true);
    }
}