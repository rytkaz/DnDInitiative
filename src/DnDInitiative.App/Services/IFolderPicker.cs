namespace DnDInitiative.App.Services;

public interface IFolderPicker
{
    /// <summary>Asks the user for a folder. Returns null if they cancel.</summary>
    string? PickFolder(string title);
}