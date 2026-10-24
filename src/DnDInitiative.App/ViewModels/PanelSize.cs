using CommunityToolkit.Mvvm.ComponentModel;

using DnDInitiative.App.Services;

namespace DnDInitiative.App.ViewModels;

/// <summary>
/// The width or height of a panel the user can drag to resize. It starts at the size saved
/// last time, stays between a minimum and a maximum, and is saved when a drag ends.
/// A view binds a panel's size to <see cref="Value"/> and a drag handle to this object
/// through <see cref="Behaviors.ResizeHandle"/>.
/// </summary>
public sealed class PanelSize : ObservableObject
{
    private readonly IUiStateStore _uiState;
    private double _value;

    /// <param name="key">The name the size is saved under. Unique across the app, such as "PartySetup.PartyList".</param>
    public PanelSize(IUiStateStore uiState, string key, double defaultValue, double minimum, double maximum)
    {
        ArgumentNullException.ThrowIfNull(uiState);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        _uiState = uiState;
        Key = key;
        Minimum = minimum;
        Maximum = maximum;

        // Clamped as well, so a size edited by hand in the file cannot make a panel vanish.
        double saved = uiState.Load().PanelSizes is { } sizes && sizes.TryGetValue(key, out double value) ? value : defaultValue;
        _value = Math.Clamp(saved, minimum, maximum);
    }

    public string Key { get; }

    public double Minimum { get; }

    public double Maximum { get; }

    public double Value
    {
        get => _value;
        set => SetProperty(ref _value, Math.Clamp(value, Minimum, Maximum));
    }

    /// <summary>
    /// Remembers the size for the next run. Called when a drag ends rather than on every
    /// change, which happens for every pixel the mouse moves.
    /// </summary>
    public void Save()
    {
        UiState state = _uiState.Load();
        var sizes = state.PanelSizes is null
            ? new Dictionary<string, double>(StringComparer.Ordinal)
            : new Dictionary<string, double>(state.PanelSizes, StringComparer.Ordinal);
        sizes[Key] = Value;

        _uiState.Save(state with { PanelSizes = sizes });
    }
}
