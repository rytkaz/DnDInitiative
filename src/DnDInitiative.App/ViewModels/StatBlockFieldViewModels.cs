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
    }

    [RelayCommand]
    private void Remove(FeatureEditorViewModel feature)
    {
        Items.Remove(feature);
    }
}