using System.Security.Cryptography;
using System.Text;

namespace DnDInitiative.Infrastructure.Campaigns;

/// <summary>
/// Gives an item that is known by a text key, such as a spell, the Guid a <see cref="JsonFolderStore{T}"/>
/// needs. The same key always gives the same Guid.
/// </summary>
internal static class KeyIdentity
{
    public static Guid For(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        Span<byte> hash = stackalloc byte[SHA256.HashSizeInBytes];
        SHA256.HashData(Encoding.UTF8.GetBytes(key.Trim().ToLowerInvariant()), hash);
        return new Guid(hash[..16]);
    }
}
