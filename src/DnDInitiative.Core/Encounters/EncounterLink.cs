using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace DnDInitiative.Core.Encounters;

/// <summary>
/// A link that starts an encounter: dndinitiative://encounter/{id}?party={id}
/// </summary>
public sealed record EncounterLink(Guid EncounterId, Guid? PartyId = null)
{
    public const string Scheme = "dndinitiative";

    private const string EncounterHost = "encounter";

    public static bool TryParse(string? text, [NotNullWhen(true)] out EncounterLink? link)
    {
        link = null;

        if (!Uri.TryCreate(text, UriKind.Absolute, out Uri? uri)
            || !string.Equals(uri.Scheme, Scheme, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(uri.Host, EncounterHost, StringComparison.OrdinalIgnoreCase)
            || !Guid.TryParse(uri.AbsolutePath.Trim('/'), out Guid encounterId))
        {
            return false;
        }

        Guid? partyId = null;
        foreach (string pair in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] parts = pair.Split('=', 2);
            if (parts.Length == 2
                && parts[0].Equals("party", StringComparison.OrdinalIgnoreCase)
                && Guid.TryParse(parts[1], out Guid parsedPartyId))
            {
                partyId = parsedPartyId;
            }
        }

        link = new EncounterLink(encounterId, partyId);
        return true;
    }

    public override string ToString()
    {
        return PartyId is { } partyId
            ? string.Create(CultureInfo.InvariantCulture, $"{Scheme}://{EncounterHost}/{EncounterId:D}?party={partyId:D}")
            : string.Create(CultureInfo.InvariantCulture, $"{Scheme}://{EncounterHost}/{EncounterId:D}");
    }
}