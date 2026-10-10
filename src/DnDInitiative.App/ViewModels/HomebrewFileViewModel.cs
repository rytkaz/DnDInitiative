using System.Globalization;

using DnDInitiative.Core.StatBlocks;

namespace DnDInitiative.App.ViewModels;

/// <summary>One homebrew file in the import page's list.</summary>
public sealed class HomebrewFileViewModel
{
    public HomebrewFileViewModel(HomebrewFile file)
    {
        ArgumentNullException.ThrowIfNull(file);

        Path = file.Path;

        // "Flee, Mortals! (FleeMortals) - 340 monsters". Most files hold one book; some hold several.
        string books = string.Join(", ", file.Sources.Select(source => source.Name == source.Id ? source.Id : $"{source.Name} ({source.Id})"));
        Label = string.Create(CultureInfo.InvariantCulture, $"{books} - {file.MonsterCount} monsters");
    }

    /// <summary>Full path of the file; what is saved and what the converter is given.</summary>
    public string Path { get; }

    public string Label { get; }
}
