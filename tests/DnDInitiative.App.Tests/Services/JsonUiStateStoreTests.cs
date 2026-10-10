using System.IO;

using DnDInitiative.App.Services;

namespace DnDInitiative.App.Tests.Services;

public sealed class JsonUiStateStoreTests : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("dndinitiative-tests-").FullName;

    private string FilePath => Path.Combine(_folder, "nested", "ui-state.json");

    public void Dispose()
    {
        Directory.Delete(_folder, recursive: true);
    }

    [Fact]
    public void Load_NothingSaved_ReturnsTheDefaults()
    {
        Assert.False(new JsonUiStateStore(FilePath).Load().IsNavigationCollapsed);
    }

    [Fact]
    public void Save_ThenLoadWithANewStore_ReturnsWhatWasSaved()
    {
        new JsonUiStateStore(FilePath).Save(new UiState { IsNavigationCollapsed = true });

        Assert.True(new JsonUiStateStore(FilePath).Load().IsNavigationCollapsed);
    }

    [Fact]
    public void Save_PanelSizes_AreReadBackByANewStore()
    {
        var saved = new UiState { IsNavigationCollapsed = true, StatBlockPanelWidth = 512.5, SourcesPanelHeight = 300 };

        new JsonUiStateStore(FilePath).Save(saved);

        Assert.Equal(saved, new JsonUiStateStore(FilePath).Load());
    }

    [Fact]
    public void Load_FileWrittenBeforePanelSizesExisted_LeavesThemUnset()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, """{"IsNavigationCollapsed":true}""");

        UiState state = new JsonUiStateStore(FilePath).Load();

        Assert.True(state.IsNavigationCollapsed);
        Assert.Null(state.StatBlockPanelWidth);
        Assert.Null(state.SourcesPanelHeight);
    }

    [Fact]
    public void Load_AfterSave_ReturnsTheSavedStateEvenIfTheFileCouldNotBeWritten()
    {
        // A folder where the file should be makes every write fail.
        Directory.CreateDirectory(FilePath);
        var store = new JsonUiStateStore(FilePath);

        store.Save(new UiState { SourcesPanelHeight = 300 });

        Assert.Equal(300, store.Load().SourcesPanelHeight);
    }

    [Fact]
    public void Load_DamagedFile_ReturnsTheDefaults()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, "{ not json");

        Assert.False(new JsonUiStateStore(FilePath).Load().IsNavigationCollapsed);
    }
}
