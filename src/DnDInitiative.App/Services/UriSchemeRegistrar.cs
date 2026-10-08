using Microsoft.Win32;

namespace DnDInitiative.App.Services;

/// <summary>
/// Tells Windows to launch this program for links with a given scheme.
/// </summary>
public static class UriSchemeRegistrar
{
    public static void Register(string scheme, string executablePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scheme);
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);

        try
        {
            using RegistryKey schemeKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{scheme}");
            schemeKey.SetValue("", "URL:" + scheme);
            schemeKey.SetValue("URL Protocol", "");

            using RegistryKey commandKey = schemeKey.CreateSubKey(@"shell\open\command");
            commandKey.SetValue("", $"\"{executablePath}\" \"%1\"");
        }
        catch (UnauthorizedAccessException)
        {
            // Links will not open the app, but everything else still works.
        }
    }
}