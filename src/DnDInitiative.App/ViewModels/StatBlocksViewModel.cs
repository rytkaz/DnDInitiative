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
    private const int PageSize = 100;

    // Increased whenever the list is rebuilt, so results from an older request can be recognised and dropped.
    private int _listVersion;
    private int _activeRefreshes;
    private bool _hasMore;

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
        RefreshListAsync(SelectedStatBlock?.Id, loadUntilFound: false).Forget();
    }

    partial void OnSelectedStatBlockChanged(StatBlockSummary? value)
    {
        LoadDetailsAsync(value).Forget();
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

    [RelayCommand]
    private async Task LoadMoreAsync()
    {
        if (_hasMore && _activeRefreshes == 0)
        {
            await LoadNextPageAsync(SearchText, _listVersion);
        }
    }

    /// <summary>
    /// Rebuilds the list from the first page and selects the given stat block, or the first row.
    /// </summary>
    /// <param name="selectId">The stat block to select afterwards, if it is in the list.</param>
    /// <param name="loadUntilFound">
    /// Whether to keep loading pages until that stat block appears. Used after saving, so the
    /// saved row stays selected even when it is far down the list.
    /// </param>
    private async Task RefreshListAsync(Guid? selectId, bool loadUntilFound = true)
    {
        int version = ++_listVersion;
        _activeRefreshes++;
        try
        {
            string search = SearchText;
            IReadOnlyList<StatBlockSummary> firstPage = await _repository.SearchAsync(search, PageSize);

            // A newer refresh started while this one was waiting: let it fill the list.
            if (version != _listVersion)
            {
                return;
            }

            FilteredStatBlocks.Clear();
            AppendPage(firstPage);

            while (loadUntilFound
                && selectId is { } id
                && _hasMore
                && FilteredStatBlocks.All(summary => summary.Id != id))
            {
                if (!await LoadNextPageAsync(search, version))
                {
                    return;
                }
            }

            SelectedStatBlock = FilteredStatBlocks.FirstOrDefault(summary => summary.Id == selectId)
                ?? FilteredStatBlocks.FirstOrDefault();

            // Reload even if the same row is selected again: its contents may just have been saved.
            await LoadDetailsAsync(SelectedStatBlock);
        }
        finally
        {
            _activeRefreshes--;
        }
    }

    /// <summary>Appends the next page. Returns false if the list was rebuilt in the meantime.</summary>
    private async Task<bool> LoadNextPageAsync(string search, int version)
    {
        IReadOnlyList<StatBlockSummary> page = await _repository.SearchAsync(search, PageSize, skip: FilteredStatBlocks.Count);
        if (version != _listVersion)
        {
            return false;
        }

        AppendPage(page);
        return true;
    }

    private void AppendPage(IReadOnlyList<StatBlockSummary> page)
    {
        foreach (StatBlockSummary summary in page)
        {
            FilteredStatBlocks.Add(summary);
        }

        // A full page means there may be more; a short one means that was the end.
        _hasMore = page.Count == PageSize;
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