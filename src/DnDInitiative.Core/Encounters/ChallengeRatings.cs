namespace DnDInitiative.Core.Encounters;

public static class ChallengeRatings
{
    private static readonly Dictionary<string, int> s_xpByRating = new(StringComparer.Ordinal)
    {
        ["0"] = 10,
        ["1/8"] = 25,
        ["1/4"] = 50,
        ["1/2"] = 100,
        ["1"] = 200,
        ["2"] = 450,
        ["3"] = 700,
        ["4"] = 1100,
        ["5"] = 1800,
        ["6"] = 2300,
        ["7"] = 2900,
        ["8"] = 3900,
        ["9"] = 5000,
        ["10"] = 5900,
        ["11"] = 7200,
        ["12"] = 8400,
        ["13"] = 10000,
        ["14"] = 11500,
        ["15"] = 13000,
        ["16"] = 15000,
        ["17"] = 18000,
        ["18"] = 20000,
        ["19"] = 22000,
        ["20"] = 25000,
        ["21"] = 33000,
        ["22"] = 41000,
        ["23"] = 50000,
        ["24"] = 62000,
        ["25"] = 75000,
        ["26"] = 90000,
        ["27"] = 105000,
        ["28"] = 120000,
        ["29"] = 135000,
        ["30"] = 155000,
    };

    /// <summary>
    /// Looks up the XP for a challenge rating such as "1/4" or "5".
    /// Anything after the rating, as in "5 (XP 1,800)", is ignored.
    /// </summary>
    public static bool TryGetXp(string? challengeRating, out int xp)
    {
        xp = 0;
        if (string.IsNullOrWhiteSpace(challengeRating))
        {
            return false;
        }

        string rating = challengeRating.Trim();
        int end = rating.IndexOf(' ');
        if (end > 0)
        {
            rating = rating[..end];
        }

        return s_xpByRating.TryGetValue(rating, out xp);
    }
}