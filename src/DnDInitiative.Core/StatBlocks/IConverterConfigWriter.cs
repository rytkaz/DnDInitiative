namespace DnDInitiative.Core.StatBlocks;

public interface IConverterConfigWriter
{
    /// <summary>
    /// Writes a converter configuration that produces importable stat blocks for the given books
    /// and homebrew files.
    /// </summary>
    /// <returns>The path of the configuration file.</returns>
    Task<string> WriteAsync(
        IReadOnlyCollection<string> sourceIds,
        IReadOnlyCollection<string> homebrewFiles,
        CancellationToken cancellationToken = default);
}