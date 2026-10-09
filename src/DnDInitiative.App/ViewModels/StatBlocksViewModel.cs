using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DnDInitiative.App.Services;
using DnDInitiative.Core.StatBlocks;

namespace DnDInitiative.App.ViewModels;

public sealed partial class StatBlocksViewModel : PageViewModel
{
    private const int SearchLimit = 200;

    private readonly IStatBlockRepository _repository;
    private readonly IStatBlockImporter _importer;
    private readonly IFolderPicker _folderPicker;
    private bool _isLoaded;

    // The full stat block behind the selected list row, loaded when the selection changes.
    private StatBlock? _selectedDetails;

    [ObservableProperty]
    private string _searchText = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditCommand))]
    [NotifyCanExecuteChangedFor(nameof(CopyCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    private StatBlockSummary? _selectedStatBlock;

    [ObservableProperty]
    private StatBlockDetailsViewModel? _details;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditing))]
    [NotifyPropertyChangedFor(nameof(IsBrowsing))]
    private StatBlockEditorViewModel? _editor;

    [ObservableProperty]
    private string _statusMessage = "";

    public StatBlocksViewModel(IStatBlockRepository repository, IStatBlockImporter importer, IFolderPicker folderPicker)
    {
        _repository = repository;
        _importer = importer;
        _folderPicker = folderPicker;
    }

    public override string Title => "Stat Blocks";

    public ObservableCollection<StatBlockSummary> FilteredStatBlocks { get; } = [];

    public bool IsEditing => Editor is not null;

    public bool IsBrowsing => Editor is null;

    private bool HasSelection => SelectedStatBlock is not null;

    partial void OnSearchTextChanged(string value)
    {
        _ = RefreshListAsync(SelectedStatBlock?.Id);
    }

    partial void OnSelectedStatBlockChanged(StatBlockSummary? value)
    {
        _ = LoadDetailsAsync(value);
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (_isLoaded)
        {
            return;
        }

        _isLoaded = true;
        await RefreshListAsync(selectId: null);
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
        if (_selectedDetails is { } selected)
        {
            Editor = new StatBlockEditorViewModel(selected);
            StatusMessage = "";
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task CopyAsync()
    {
        if (_selectedDetails is not { } selected)
        {
            return;
        }

        StatBlock copy = selected with { Id = Guid.NewGuid(), Name = selected.Name + " (Copy)" };
        await _repository.SaveAsync(copy);
        await RefreshListAsync(copy.Id);
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
        await RefreshListAsync(selectId: null);
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
        await RefreshListAsync(statBlock.Id);
        StatusMessage = $"Saved {statBlock.Name}.";
    }

    [RelayCommand]
    private void Cancel()
    {
        Editor = null;
        StatusMessage = "";
    }

    [RelayCommand]
    private async Task ImportAsync()
    {
        string? folder = _folderPicker.PickFolder("Select the folder containing the generated notes");
        if (folder is null)
        {
            return;
        }

        StatusMessage = "Importing...";
        try
        {
            StatBlockImportResult result = await _importer.ImportFolderAsync(folder);
            await RefreshListAsync(SelectedStatBlock?.Id);
            StatusMessage = string.Create(
                CultureInfo.InvariantCulture,
                $"Imported {result.Added} new and updated {result.Updated}, from {result.FilesScanned} files.");
        }
        catch (IOException exception)
        {
            StatusMessage = "Import failed: " + exception.Message;
        }
        catch (UnauthorizedAccessException exception)
        {
            StatusMessage = "Import failed: " + exception.Message;
        }
    }

    /// <summary>
    /// Re-runs the current search and selects the given stat block, or the first result.
    /// </summary>
    private async Task RefreshListAsync(Guid? selectId)
    {
        string search = SearchText;
        IReadOnlyList<StatBlockSummary> results = await _repository.SearchAsync(search, SearchLimit);

        // The user typed again while this search was running: a newer one will fill the list.
        if (search != SearchText)
        {
            return;
        }

        FilteredStatBlocks.Clear();
        foreach (StatBlockSummary summary in results)
        {
            FilteredStatBlocks.Add(summary);
        }

        SelectedStatBlock = FilteredStatBlocks.FirstOrDefault(summary => summary.Id == selectId)
            ?? FilteredStatBlocks.FirstOrDefault();

        // Reload even if the same row is selected again: its contents may just have been saved.
        await LoadDetailsAsync(SelectedStatBlock);
    }

    private async Task LoadDetailsAsync(StatBlockSummary? summary)
    {
        if (summary is null)
        {
            _selectedDetails = null;
            Details = null;
            return;
        }

        StatBlock? statBlock = await _repository.GetAsync(summary.Id);

        // The selection moved on while this one was loading: drop the stale result.
        if (SelectedStatBlock?.Id != summary.Id)
        {
            return;
        }

        _selectedDetails = statBlock;
        Details = statBlock is null ? null : new StatBlockDetailsViewModel(statBlock);
    }
}