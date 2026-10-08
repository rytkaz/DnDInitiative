using System.Runtime.InteropServices;
using System.Windows;

namespace DnDInitiative.App.Services;

public sealed class ClipboardService : IClipboardService
{
    public bool TrySetText(string text)
    {
        try
        {
            Clipboard.SetText(text);
            return true;
        }
        catch (ExternalException)
        {
            return false;
        }
    }
}