namespace DnDInitiative.Core.StatBlocks;

/// <summary>What the converter needs to turn 5eTools data into Markdown notes.</summary>
/// <param name="ToolPath">The ttrpg-convert program: a full path, or just its name if it is on the PATH.</param>
/// <param name="DataDirectory">The 5eTools "data" folder.</param>
/// <param name="ConfigPath">The converter's own configuration file, if any.</param>
public sealed record ConversionRequest(
    string ToolPath,
    string DataDirectory,
    string? ConfigPath = null);

public interface IStatBlockConverter
{
    /// <summary>
    /// Runs the converter and waits for it to finish. The notes go into a working folder
    /// of the converter's own, which is emptied at the start of every run.
    /// </summary>
    /// <param name="progress">Receives each line the converter prints.</param>
    /// <returns>The folder the notes were written to.</returns>
    /// <exception cref="ConversionException">The converter could not be started, or reported a failure.</exception>
    /// <exception cref="OperationCanceledException">The run was cancelled; the converter has been stopped.</exception>
    Task<string> ConvertAsync(
        ConversionRequest request,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the notes of the last run, once they are no longer needed.
    /// Never fails: anything that cannot be deleted now is removed by the next run.
    /// </summary>
    void DiscardOutput();
}