namespace DnDInitiative.App.Services;

/// <summary>Asks the user to confirm something that cannot be undone.</summary>
public interface IConfirmationService
{
    /// <returns>True if the user chose to go ahead.</returns>
    bool Confirm(string title, string message);
}