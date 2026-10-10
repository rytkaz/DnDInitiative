namespace DnDInitiative.App.Formatting;

/// <summary>
/// Links to spells are written as [Fireball](spell:fireball-xphb). On screen such a link is a
/// hyperlink whose address is a URI with the made-up scheme "spell"; clicking it reports that
/// URI, and whoever handles the click turns it back into the spell reference.
/// </summary>
public static class SpellLink
{
    public const string Scheme = "spell";

    private const string Prefix = Scheme + ":";

    /// <summary>Turns a link target into a URI, or returns null if it is not a link to a spell.</summary>
    public static Uri? ToUri(string? target)
    {
        if (target is null || !target.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        string reference = target[Prefix.Length..].Trim();
        if (reference.Length == 0)
        {
            return null;
        }

        // Escaped, so a hand-written reference such as "Detect Magic" (with its space) is still a valid URI.
        return Uri.TryCreate(Prefix + Uri.EscapeDataString(reference), UriKind.Absolute, out Uri? uri) ? uri : null;
    }

    /// <summary>Gets the spell reference back out of a URI made by <see cref="ToUri"/>.</summary>
    /// <returns>The reference, or null if the URI is some other kind of link.</returns>
    public static string? ToReference(Uri? uri)
    {
        if (uri is null || !uri.IsAbsoluteUri || !string.Equals(uri.Scheme, Scheme, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        string reference = Uri.UnescapeDataString(uri.AbsolutePath).Trim();
        return reference.Length == 0 ? null : reference;
    }
}
