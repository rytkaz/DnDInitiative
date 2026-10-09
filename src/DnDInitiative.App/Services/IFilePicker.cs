namespace DnDInitiative.App.Services;

public interface IFilePicker
{
    /// <summary>Asks the user for an existing file. Returns null if they cancel.</summary>
    /// <param name="filter">A dialog filter such as "Programs (*.exe)|*.exe".</param>
    string? PickFile(string title, string filter);
}