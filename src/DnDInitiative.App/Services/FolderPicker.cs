using Microsoft.Win32;

namespace DnDInitiative.App.Services;

public sealed class FolderPicker : IFolderPicker
{
    public string? PickFolder(string title)
    {
        var dialog = new OpenFolderDialog { Title = title };
        return dialog.ShowDialog() == true ? dialog.FolderName : null;
    }
}