using System.ComponentModel;
using System.Diagnostics;

using DnDInitiative.App.Formatting;

namespace DnDInitiative.App.Services;

/// <summary>Opens a web page in the user's browser.</summary>
public interface ILinkOpener
{
    /// <returns>False if the address is not a web address, or no browser could be started.</returns>
    bool TryOpen(Uri address);
}

public sealed class BrowserLinkOpener : ILinkOpener
{
    public bool TryOpen(Uri address)
    {
        // Checked here as well as where links are made. UseShellExecute hands the text to
        // Windows to open with whatever is registered for it, and for anything other than
        // a web address that could be a program.
        if (!WebLink.IsWebAddress(address))
        {
            return false;
        }

        try
        {
            using Process? process = Process.Start(new ProcessStartInfo(address.AbsoluteUri) { UseShellExecute = true });
            return true;
        }
        catch (Win32Exception)
        {
            // No browser is registered, or Windows refused to start it.
            return false;
        }
    }
}
