namespace DnDInitiative.Core.StatBlocks;

public interface IConverterConfigWriter
{
    /// <summary>
    /// Writes a converter configuration that produces importable stat blocks for the given books.
    /// </summary>
    /// <returns>The path of the configuration file.</returns>
    Task<string> WriteAsync(IReadOnlyCollection<string> sourceIds, CancellationToken cancellationToken = default);
}