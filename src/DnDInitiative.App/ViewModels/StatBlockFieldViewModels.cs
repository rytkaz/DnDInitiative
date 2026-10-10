using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DnDInitiative.Core.StatBlocks;

namespace DnDInitiative.App.ViewModels;

public sealed partial class TextFieldViewModel : ObservableObject
{
    [ObservableProperty]
    private string _value = "";

    public TextFieldViewModel(string label, string value)
    {
        Label = label;
        Value = value;
    }

    public string Label { get; }
}

public sealed partial class NumberFieldViewModel : ObservableObject
{
    [ObservableProperty]
    private int _value;

    public NumberFieldViewModel(string label, int value, int minimum, int maximum)
    {
        Label = label;
        Minimum = minimum;
        Maximum = maximum;
        Value = value;
    }

    public string Label { get; }

    public int Minimum { get; }

    public int Maximum { get; }

    public int ClampedValue => Math.Clamp(Value, Minimum, Maximum);
}

public sealed partial class FeatureEditorViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = "";

    [ObservableProperty]
    private string _description = "";
}

public sealed partial class FeatureListEditorViewModel : ObservableObject
{
    public FeatureListEditorViewModel(string title, IEnumerable<StatBlockFeature> features)
    {
        ArgumentNullException.ThrowIfNull(features);

        Title = title;
        foreach (StatBlockFeature feature in features)
        {
            Items.Add(new FeatureEditorViewModel { Name = feature.Name, Description = feature.Description });
        }
    }

    public string Title { get; }

    public ObservableCollection<FeatureEditorViewModel> Items { get; } = [];

    /// <summary>Entries without a name are treated as not filled in and are dropped.</summary>
    public IReadOnlyList<StatBlockFeature> ToFeatures()
    {
        return Items
            .Where(item => !string.IsNullOrWhiteSpace(item.Name))
            .Select(item => new StatBlockFeature(item.Name.Trim(), item.Description.Trim()))
            .ToList();
    }

    [RelayCommand]
    private void Add()
    {
        Items.Add(new FeatureEditorViewModel());
        RefreshMoveCommands();
    }

    [RelayCommand]
    private void Remove(FeatureEditorViewModel feature)
    {
        Items.Remove(feature);
        RefreshMoveCommands();
    }

    // The order of the list is the order the features are saved and shown in.
    [RelayCommand(CanExecute = nameof(CanMoveUp))]
    private void MoveUp(FeatureEditorViewModel? feature)
    {
        MoveBy(feature, -1);
    }

    [RelayCommand(CanExecute = nameof(CanMoveDown))]
    private void MoveDown(FeatureEditorViewModel? feature)
    {
        MoveBy(feature, +1);
    }

    // Each row's buttons pass their own feature, so these answer per row:
    // the first cannot go up, and the last cannot go down.
    private bool CanMoveUp(FeatureEditorViewModel? feature)
    {
        return feature is not null && Items.IndexOf(feature) > 0;
    }

    private bool CanMoveDown(FeatureEditorViewModel? feature)
    {
        int index = feature is null ? -1 : Items.IndexOf(feature);
        return index >= 0 && index < Items.Count - 1;
    }

    private void MoveBy(FeatureEditorViewModel? feature, int offset)
    {
        int index = feature is null ? -1 : Items.IndexOf(feature);
        int target = index + offset;

        if (index < 0 || target < 0 || target >= Items.Count)
        {
            return;
        }

        // Move keeps the same row object, so text being typed in it and its focus survive.
        Items.Move(index, target);
        RefreshMoveCommands();
    }

    // Which rows can move depends on where every row now is, so ask the buttons to check again.
    private void RefreshMoveCommands()
    {
        MoveUpCommand.NotifyCanExecuteChanged();
        MoveDownCommand.NotifyCanExecuteChanged();
    }
}