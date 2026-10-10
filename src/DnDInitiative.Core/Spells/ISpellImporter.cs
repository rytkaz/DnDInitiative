namespace DnDInitiative.Core.Spells;

public interface ISpellImporter
{
    /// <summary>
    /// Imports every spell found in the Markdown notes under a folder. Only notes written
    /// with the app's own spell template are recognised.
    /// </summary>
    Task<SpellSaveResult> ImportFolderAsync(string folderPath, CancellationToken cancellationToken = default);
}
