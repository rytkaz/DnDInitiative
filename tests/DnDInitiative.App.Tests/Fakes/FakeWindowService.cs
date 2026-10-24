using DnDInitiative.App.Services;

namespace DnDInitiative.App.Tests.Fakes;

internal sealed class FakeWindowService : IWindowService
{
    private readonly List<ImportKind> _shownImports = [];

    public IReadOnlyList<ImportKind> ShownImports => _shownImports;

    public void ShowImport(ImportKind kind)
    {
        _shownImports.Add(kind);
    }

    public void CloseAll()
    {
    }
}
