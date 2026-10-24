using System.Text.RegularExpressions;

namespace DnDInitiative.Infrastructure.Homebrewery;

/// <summary>
/// Recognises links to documents on the Homebrewery and says where their Markdown source can be downloaded.
/// </summary>
internal static partial class HomebreweryLinks
{
    private const string Site = "https://homebrewery.naturalcrit.com";

    /// <summary>
    /// Returns the share id in a link to a Homebrewery document, or null for any other link.
    /// Share, source, download and print links all carry it: ".../share/uDYpiTxHncTE" gives "uDYpiTxHncTE".
    /// Edit links are not accepted, because their id is a different, private one.
    /// </summary>
    public static string? ShareId(string? link)
    {
        if (string.IsNullOrWhiteSpace(link))
        {
            return null;
        }

        Match match = DocumentLink().Match(link.Trim());
        return match.Success ? match.Groups["id"].Value : null;
    }

    /// <summary>The address that answers with the document's Markdown source as plain text.</summary>
    public static Uri SourceUri(string shareId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shareId);
        return new Uri($"{Site}/download/{Uri.EscapeDataString(shareId)}");
    }

    /// <summary>The address people open to read the document. Used in messages about it.</summary>
    public static string ShareLink(string shareId)
    {
        return $"{Site}/share/{shareId}";
    }

    // The id ends at anything that cannot be part of it: a "#p2" page anchor, a query, a slash.
    [GeneratedRegex(@"^(?:https?://)?(?:www\.)?homebrewery\.naturalcrit\.com/(?:share|source|download|print)/(?<id>[A-Za-z0-9_-]+)", RegexOptions.IgnoreCase)]
    private static partial Regex DocumentLink();
}
