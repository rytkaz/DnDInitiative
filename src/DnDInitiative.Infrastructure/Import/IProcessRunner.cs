namespace DnDInitiative.Infrastructure.Import;

internal interface IProcessRunner
{
    /// <summary>Runs a program to completion and returns its exit code.</summary>
    /// <param name="onOutput">
    /// Called for every line the program prints, to either output stream.
    /// Calls arrive on background threads and may overlap.
    /// </param>
    Task<int> RunAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        string? workingDirectory,
        Action<string> onOutput,
        CancellationToken cancellationToken);
}