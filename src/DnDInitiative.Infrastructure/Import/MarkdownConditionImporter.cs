using DnDInitiative.Core.Conditions;

namespace DnDInitiative.Infrastructure.Import;

internal sealed class MarkdownConditionImporter : IConditionImporter
{
    private readonly IConditionRepository _conditions;

    public MarkdownConditionImporter(IConditionRepository conditions)
    {
        _conditions = conditions;
    }

    public async Task<int> ImportFolderAsync(string folderPath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folderPath);

        // Keyed by name, so a condition found twice is imported once.
        var byName = new Dictionary<string, Condition>(StringComparer.OrdinalIgnoreCase);

        foreach (string file in Directory.EnumerateFiles(folderPath, ConditionNoteParser.FileName, SearchOption.AllDirectories))
        {
            string markdown = await File.ReadAllTextAsync(file, cancellationToken).ConfigureAwait(false);
            foreach (Condition condition in ConditionNoteParser.Parse(markdown))
            {
                byName[condition.Name] = condition;
            }
        }

        if (byName.Count > 0)
        {
            await _conditions.SaveManyAsync(byName.Values, cancellationToken).ConfigureAwait(false);
        }

        return byName.Count;
    }
}
