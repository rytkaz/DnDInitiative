using System.Text.RegularExpressions;

namespace DnDInitiative.Infrastructure.GmBinder;

/// <summary>Recognises links to documents on GM Binder and says where to download them.</summary>
internal static partial class GmBinderLinks
{
    private const string Site = "https://www.gmbinder.com";

    /// <summary>
    /// Returns the share id in a link to a GM Binder document, or null for any other link.
    /// ".../share/-MSfA82gv8V69JAoqFVq" gives "-MSfA82gv8V69JAoqFVq". The ids start with a hyphen.
    /// </summary>
    public static string? ShareId(string? link)
    {
        if (string.IsNullOrWhiteSpace(link))
        {
            return null;
        }

        Match match = ShareLink().Match(link.Trim());
        return match.Success ? match.Groups["id"].Value : null;
    }

    /// <summary>
    /// The address of the document's page. GM Binder does not offer a document's source
    /// unless its author shares it, so the importer reads the page as it is shown.
    /// </summary>
    public static Uri PageUri(string shareId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shareId);
        return new Uri($"{Site}/share/{Uri.EscapeDataString(shareId)}");
    }

    // The id ends at anything that cannot be part of it: a "#p2" page anchor, a query, a slash.
    [GeneratedRegex(@"^(?:https?://)?(?:www\.)?gmbinder\.com/share/(?<id>[A-Za-z0-9_-]+)", RegexOptions.IgnoreCase)]
    private static partial Regex ShareLink();
}
