using Microsoft.Win32;

namespace DnDInitiative.App.Services;

public sealed class FilePicker : IFilePicker
{
    public string? PickFile(string title, string filter)
    {
        var dialog = new OpenFileDialog { Title = title, Filter = filter, CheckFileExists = true };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}