using CommunityToolkit.Mvvm.ComponentModel;

using DnDInitiative.Core.StatBlocks;

namespace DnDInitiative.App.ViewModels;

/// <summary>One book in the import page's list, with its tick box.</summary>
public sealed partial class SourceOptionViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isSelected;

    /// <summary>False while the search box is hiding this book. A hidden book keeps its tick.</summary>
    [ObservableProperty]
    private bool _isVisible = true;

    public SourceOptionViewModel(SourceBook book, bool isSelected)
    {
        ArgumentNullException.ThrowIfNull(book);

        Id = book.Id;
        Label = book.Name == book.Id ? book.Id : $"{book.Name} ({book.Id})";
        _isSelected = isSelected;
    }

    public string Id { get; }

    public string Label { get; }

    /// <summary>Whether the search text appears anywhere in the name or abbreviation.</summary>
    public bool Matches(string searchText)
    {
        ArgumentNullException.ThrowIfNull(searchText);
        return searchText.Length == 0 || Label.Contains(searchText, StringComparison.OrdinalIgnoreCase);
    }
}