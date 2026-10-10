using DnDInitiative.App.ViewModels;
using DnDInitiative.Core.StatBlocks;

namespace DnDInitiative.App.Tests.ViewModels;

public class FeatureListEditorViewModelTests
{
    private readonly FeatureListEditorViewModel _list = new(
        "Actions",
        [
            new StatBlockFeature("Multiattack", "Two attacks."),
            new StatBlockFeature("Bite", "Melee."),
            new StatBlockFeature("Claw", "Melee."),
        ]);

    [Fact]
    public void MoveUpCommand_SwapsTheFeatureWithTheOneAboveIt()
    {
        _list.MoveUpCommand.Execute(_list.Items[2]);

        Assert.Equal(["Multiattack", "Claw", "Bite"], Names());
    }

    [Fact]
    public void MoveDownCommand_SwapsTheFeatureWithTheOneBelowIt()
    {
        _list.MoveDownCommand.Execute(_list.Items[0]);

        Assert.Equal(["Bite", "Multiattack", "Claw"], Names());
    }

    [Fact]
    public void MoveCommands_KeepTheSameRowObject()
    {
        FeatureEditorViewModel claw = _list.Items[2];

        _list.MoveUpCommand.Execute(claw);
        _list.MoveUpCommand.Execute(claw);

        Assert.Same(claw, _list.Items[0]);
    }

    [Fact]
    public void MoveCommands_AreOnlyAvailableWhereThereIsSomewhereToGo()
    {
        Assert.False(_list.MoveUpCommand.CanExecute(_list.Items[0]));
        Assert.True(_list.MoveDownCommand.CanExecute(_list.Items[0]));

        Assert.True(_list.MoveUpCommand.CanExecute(_list.Items[1]));
        Assert.True(_list.MoveDownCommand.CanExecute(_list.Items[1]));

        Assert.True(_list.MoveUpCommand.CanExecute(_list.Items[2]));
        Assert.False(_list.MoveDownCommand.CanExecute(_list.Items[2]));
    }

    [Fact]
    public void MoveCommands_FeatureNotInTheListOrNone_CannotMoveAndChangeNothing()
    {
        var stranger = new FeatureEditorViewModel { Name = "Stranger" };

        Assert.False(_list.MoveUpCommand.CanExecute(stranger));
        Assert.False(_list.MoveDownCommand.CanExecute(null));

        _list.MoveUpCommand.Execute(stranger);
        _list.MoveDownCommand.Execute(null);

        Assert.Equal(["Multiattack", "Bite", "Claw"], Names());
    }

    [Fact]
    public void MoveCommands_AnnounceThatWhatCanMoveHasChanged()
    {
        int announcements = 0;
        _list.MoveUpCommand.CanExecuteChanged += (_, _) => announcements++;

        _list.MoveDownCommand.Execute(_list.Items[0]);
        _list.AddCommand.Execute(null);
        _list.RemoveCommand.Execute(_list.Items[0]);

        // Once per change: a moved row, an added row and a removed row each shift the ends of the list.
        Assert.Equal(3, announcements);
    }

    [Fact]
    public void ToFeatures_ReturnsTheFeaturesInTheirNewOrder()
    {
        _list.MoveUpCommand.Execute(_list.Items[1]);

        StatBlockFeature[] expected =
        [
            new("Bite", "Melee."),
            new("Multiattack", "Two attacks."),
            new("Claw", "Melee."),
        ];
        Assert.Equal<StatBlockFeature>(expected, _list.ToFeatures());
    }

    private string[] Names()
    {
        return _list.Items.Select(item => item.Name).ToArray();
    }
}
