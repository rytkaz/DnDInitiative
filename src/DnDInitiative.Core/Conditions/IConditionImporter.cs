namespace DnDInitiative.Core.Conditions;

public interface IConditionImporter
{
    /// <summary>Imports the conditions from the converter's notes under a folder.</summary>
    /// <returns>How many conditions were imported.</returns>
    Task<int> ImportFolderAsync(string folderPath, CancellationToken cancellationToken = default);
}
