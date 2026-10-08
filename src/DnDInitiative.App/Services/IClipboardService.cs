namespace DnDInitiative.App.Services;

public interface IClipboardService
{
    /// <summary>Returns false if the clipboard could not be written, for example while another program holds it.</summary>
    bool TrySetText(string text);
}