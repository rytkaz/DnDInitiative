using System.Text.RegularExpressions;

using DnDInitiative.Core.Conditions;

namespace DnDInitiative.Infrastructure.Import;

/// <summary>
/// Reads the note the converter writes with every condition in it, rules/conditions.md.
/// Each condition is a section: "## Prone", a line "_Source: XPHB p. 367_", then its text.
/// </summary>
internal static partial class ConditionNoteParser
{
    /// <summary>The name of the note. Links to conditions point into it: conditions.md#Prone.</summary>
    public const string FileName = "conditions.md";

    public static IReadOnlyList<Condition> Parse(string markdown)
    {
        ArgumentNullException.ThrowIfNull(markdown);

        var conditions = new List<Condition>();
        string? name = null;
        string source = "";
        var body = new List<string>();
        bool pastFirstLine = false;

        void EndSection()
        {
            if (name is null)
            {
                return;
            }

            // A section without text has nothing to show. That is what the converter writes
            // when rules are split into one note each; the text is then in those notes.
            string description = SpellNoteParser.CleanText(string.Join('\n', body));
            if (description.Length > 0)
            {
                conditions.Add(new Condition { Name = name, Source = source, Description = description });
            }
        }

        foreach (string line in markdown.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            if (SectionHeading().Match(line) is { Success: true } heading)
            {
                EndSection();
                name = ConverterLinks.Simplify(heading.Groups["name"].Value).Trim();
                source = "";
                body.Clear();
                pastFirstLine = false;
                continue;
            }

            // Everything above the first section: the note's properties and its title.
            if (name is null)
            {
                continue;
            }

            // The source, if there is one, is the first line of the section.
            if (!pastFirstLine && line.Trim().Length > 0)
            {
                pastFirstLine = true;
                if (SourceLine().Match(line.Trim()) is { Success: true } sourceLine)
                {
                    source = ConverterLinks.Simplify(sourceLine.Groups["source"].Value).Trim();
                    continue;
                }
            }

            body.Add(line);
        }

        EndSection();
        return conditions;
    }

    // Exactly two # signs: "### Effects" inside a condition is part of its text.
    [GeneratedRegex(@"^##[ \t]+(?<name>.+?)[ \t]*$")]
    private static partial Regex SectionHeading();

    // "_Source: XPHB p. 367_", in italics with underscores or asterisks.
    [GeneratedRegex(@"^[_*]Source:\s*(?<source>.*?)[_*]$")]
    private static partial Regex SourceLine();
}
