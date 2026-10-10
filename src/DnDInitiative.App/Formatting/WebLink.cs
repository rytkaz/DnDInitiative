namespace DnDInitiative.App.Formatting;

/// <summary>Links to web pages, as opposed to links to spells (see <see cref="SpellLink"/>).</summary>
public static class WebLink
{
    /// <summary>Turns a link target into a URI, or returns null if it is not a web address.</summary>
    public static Uri? ToUri(string? target)
    {
        return Uri.TryCreate(target?.Trim(), UriKind.Absolute, out Uri? uri) && IsWebAddress(uri) ? uri : null;
    }

    /// <summary>
    /// True only for http and https. Anything else a URI can be (a file, a program, another
    /// app's link type) is not something to open because imported text said so.
    /// </summary>
    public static bool IsWebAddress(Uri? uri)
    {
        return uri is { IsAbsoluteUri: true }
            && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
    }
}
