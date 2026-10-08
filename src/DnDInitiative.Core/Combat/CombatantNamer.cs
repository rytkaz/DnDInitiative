using System.Globalization;

namespace DnDInitiative.Core.Combat;

/// <summary>
/// Hands out display names for one combat, numbering creatures that share a name.
/// </summary>
public sealed class CombatantNamer
{
    private readonly Dictionary<string, int> _issuedCounts = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<string> Assign(IReadOnlyList<string> baseNames)
    {
        ArgumentNullException.ThrowIfNull(baseNames);

        Dictionary<string, int> countsInBatch = baseNames
            .GroupBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);

        var names = new List<string>(baseNames.Count);
        foreach (string baseName in baseNames)
        {
            _issuedCounts.TryGetValue(baseName, out int alreadyIssued);

            // A name stays plain only if it is the first of its kind in this
            // combat and nothing else in this batch shares it.
            bool needsNumber = alreadyIssued > 0 || countsInBatch[baseName] > 1;
            int number = alreadyIssued + 1;
            _issuedCounts[baseName] = number;

            names.Add(needsNumber
                ? baseName + " " + number.ToString(CultureInfo.InvariantCulture)
                : baseName);
        }

        return names;
    }
}