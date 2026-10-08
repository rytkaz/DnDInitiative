using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DnDInitiative.Core.StatBlocks;

namespace DnDInitiative.App.ViewModels;

public sealed partial class StatBlocksViewModel : PageViewModel
{
    private readonly IStatBlockRepository _repository;
    private List<StatBlock> _all = [];
    private bool _isLoaded;

    [ObservableProperty]
    private string _searchText = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditCommand))]
    [NotifyCanExecuteChangedFor(nameof(CopyCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    private StatBlock? _selectedStatBlock;

    [ObservableProperty]
    private StatBlockDetailsViewModel? _details;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditing))]
    [NotifyPropertyChangedFor(nameof(IsBrowsing))]
    private StatBlockEditorViewModel? _editor;

    [ObservableProperty]
    private string _statusMessage = "";

    public StatBlocksViewModel(IStatBlockRepository repository)
    {
        _repository = repository;
    }

    public override string Title => "Stat Blocks";

    public ObservableCollection<StatBlock> FilteredStatBlocks { get; } = [];

    public bool IsEditing => Editor is not null;

    public bool IsBrowsing => Editor is null;

    private bool HasSelection => SelectedStatBlock is not null;

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }

    partial void OnSelectedStatBlockChanged(StatBlock? value)
    {
        Details = value is null ? null : new StatBlockDetailsViewModel(value);
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (_isLoaded)
        {
            return;
        }

        _isLoaded = true;
        await ReloadAsync(selectId: null);
    }

    [RelayCommand]
    private void New()
    {
        Editor = new StatBlockEditorViewModel(new StatBlock { Id = Guid.NewGuid(), Name = "" });
        StatusMessage = "";
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void Edit()
    {
        if (SelectedStatBlock is { } selected)
        {
            Editor = new StatBlockEditorViewModel(selected);
            StatusMessage = "";
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task CopyAsync()
    {
        if (SelectedStatBlock is not { } selected)
        {
            return;
        }

        StatBlock copy = selected with { Id = Guid.NewGuid(), Name = selected.Name + " (Copy)" };
        await _repository.SaveAsync(copy);
        await ReloadAsync(copy.Id);
        StatusMessage = $"Copied {selected.Name}.";
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task DeleteAsync()
    {
        if (SelectedStatBlock is not { } selected)
        {
            return;
        }

        await _repository.DeleteAsync(selected.Id);
        await ReloadAsync(selectId: null);
        StatusMessage = $"Deleted {selected.Name}.";
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (Editor is not { } editor)
        {
            return;
        }

        if (!editor.Validate())
        {
            StatusMessage = "Give the stat block a name before saving.";
            return;
        }

        StatBlock statBlock = editor.ToStatBlock();
        await _repository.SaveAsync(statBlock);
        Editor = null;
        await ReloadAsync(statBlock.Id);
        StatusMessage = $"Saved {statBlock.Name}.";
    }

    [RelayCommand]
    private void Cancel()
    {
        Editor = null;
        StatusMessage = "";
    }

    private async Task ReloadAsync(Guid? selectId)
    {
        IReadOnlyList<StatBlock> stored = await _repository.GetAllAsync();
        _all = stored.OrderBy(statBlock => statBlock.Name, StringComparer.OrdinalIgnoreCase).ToList();

        ApplyFilter();
        SelectedStatBlock = FilteredStatBlocks.FirstOrDefault(statBlock => statBlock.Id == selectId)
            ?? FilteredStatBlocks.FirstOrDefault();
    }

    private void ApplyFilter()
    {
        Guid? selectedId = SelectedStatBlock?.Id;
        string search = SearchText.Trim();

        FilteredStatBlocks.Clear();
        foreach (StatBlock statBlock in _all)
        {
            if (search.Length == 0 || statBlock.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
            {
                FilteredStatBlocks.Add(statBlock);
            }
        }

        SelectedStatBlock = FilteredStatBlocks.FirstOrDefault(statBlock => statBlock.Id == selectedId);
    }
}