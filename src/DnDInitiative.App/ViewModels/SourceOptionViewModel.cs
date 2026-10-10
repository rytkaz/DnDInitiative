using CommunityToolkit.Mvvm.ComponentModel;

using DnDInitiative.Core.StatBlocks;

namespace DnDInitiative.App.ViewModels;

/// <summary>One book in the import page's list, with its tick box.</summary>
public sealed partial class SourceOptionViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isSelected;

    public SourceOptionViewModel(SourceBook book, bool isSelected)
    {
        ArgumentNullException.ThrowIfNull(book);

        Id = book.Id;
        Label = book.Name == book.Id ? book.Id : $"{book.Name} ({book.Id})";
        _isSelected = isSelected;
    }

    public string Id { get; }

    public string Label { get; }
}