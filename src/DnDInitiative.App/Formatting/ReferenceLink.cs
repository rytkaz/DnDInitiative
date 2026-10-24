namespace DnDInitiative.App.Formatting;

/// <summary>
/// A link to something in the app's own library, written as [Fireball](spell:fireball-xphb)
/// or [prone](condition:Prone). On screen such a link is a hyperlink whose address is a URI
/// with a made-up scheme; clicking it reports that URI, and whoever handles the click turns it
/// back into the reference with <see cref="ToReference"/>.
/// </summary>
public sealed class ReferenceLink
{
    private readonly string _prefix;

    private ReferenceLink(string scheme)
    {
        Scheme = scheme;
        _prefix = scheme + ":";
    }

    public static ReferenceLink Spell { get; } = new("spell");

    public static ReferenceLink Condition { get; } = new("condition");

    public string Scheme { get; }

    /// <summary>Turns a link target into a URI, or returns null if it is not this kind of link.</summary>
    public Uri? ToUri(string? target)
    {
        if (target is null || !target.StartsWith(_prefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        string reference = target[_prefix.Length..].Trim();
        if (reference.Length == 0)
        {
            return null;
        }

        // Escaped, so a reference such as "Detect Magic" (with its space) is still a valid URI.
        return Uri.TryCreate(_prefix + Uri.EscapeDataString(reference), UriKind.Absolute, out Uri? uri) ? uri : null;
    }

    /// <summary>Gets the reference back out of a URI made by <see cref="ToUri"/>.</summary>
    /// <returns>The reference, or null if the URI is some other kind of link.</returns>
    public string? ToReference(Uri? uri)
    {
        if (uri is null || !uri.IsAbsoluteUri || !string.Equals(uri.Scheme, Scheme, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        string reference = Uri.UnescapeDataString(uri.AbsolutePath).Trim();
        return reference.Length == 0 ? null : reference;
    }
}
