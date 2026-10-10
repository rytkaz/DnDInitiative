namespace DnDInitiative.Core.StatBlocks;

/// <summary>What a generated converter configuration should cover.</summary>
public sealed record ConverterConfigOptions
{
    /// <summary>The books whose monsters are wanted, by id, such as "XMM".</summary>
    public IReadOnlyCollection<string> Sources { get; init; } = [];

    /// <summary>Full paths of homebrew files. Everything in them is wanted.</summary>
    public IReadOnlyCollection<string> HomebrewFiles { get; init; } = [];

    /// <summary>
    /// Every book that contains spells. Their spells are converted so that stat blocks can
    /// link to them; their monsters are not, unless the book is also in <see cref="Sources"/>.
    /// </summary>
    public IReadOnlyCollection<string> SpellSources { get; init; } = [];
}

public interface IConverterConfigWriter
{
    /// <summary>
    /// Writes a converter configuration that produces importable stat blocks and spells.
    /// </summary>
    /// <returns>The path of the configuration file.</returns>
    Task<string> WriteAsync(ConverterConfigOptions options, CancellationToken cancellationToken = default);
}