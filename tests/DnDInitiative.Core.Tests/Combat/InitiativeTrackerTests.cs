using DnDInitiative.Core.Combat;

namespace DnDInitiative.Core.Tests.CombatTests;

public class InitiativeTrackerTests
{
    // Behaviour 1: Add

    [Fact]
    public void Add_OrdersByInitiativeHighestFirst()
    {
        var low = new Combatant("Low", initiative: 5);
        var high = new Combatant("High", initiative: 20);
        var mid = new Combatant("Mid", initiative: 12);

        var tracker = TrackerWith(low, high, mid);

        AssertOrder(tracker, high, mid, low);
    }

    [Fact]
    public void Add_TiedInitiative_GoesAfterExistingCombatant()
    {
        var first = new Combatant("First", initiative: 12);
        var second = new Combatant("Second", initiative: 12);

        var tracker = TrackerWith(first, second);

        AssertOrder(tracker, first, second);
    }

    [Fact]
    public void Add_BeforeAnyTurnIsTaken_TopOfOrderIsCurrent()
    {
        var low = new Combatant("Low", initiative: 5);
        var high = new Combatant("High", initiative: 20);

        var tracker = TrackerWith(low, high);

        Assert.Same(high, tracker.CurrentCombatant);
    }

    [Fact]
    public void Add_AfterCombatHasAdvanced_DoesNotChangeCurrent()
    {
        var a = new Combatant("A", initiative: 15);
        var b = new Combatant("B", initiative: 10);
        var tracker = TrackerWith(a, b);
        tracker.Next();

        var newcomer = new Combatant("Newcomer", initiative: 20);
        tracker.Add(newcomer);

        AssertOrder(tracker, newcomer, a, b);
        Assert.Same(b, tracker.CurrentCombatant);
    }

    [Fact]
    public void Add_SameCombatantTwice_Throws()
    {
        var a = new Combatant("A", initiative: 15);
        var tracker = TrackerWith(a);

        Assert.Throws<ArgumentException>(() => tracker.Add(a));
    }

    // Behaviour 2: Next

    [Fact]
    public void NewTracker_StartsAtRoundOne()
    {
        var tracker = new InitiativeTracker();

        Assert.Equal(1, tracker.Round);
        Assert.Null(tracker.CurrentCombatant);
    }

    [Fact]
    public void Next_MovesToFollowingCombatant()
    {
        var a = new Combatant("A", initiative: 15);
        var b = new Combatant("B", initiative: 10);
        var tracker = TrackerWith(a, b);

        tracker.Next();

        Assert.Same(b, tracker.CurrentCombatant);
        Assert.Equal(1, tracker.Round);
    }

    [Fact]
    public void Next_PastLastCombatant_WrapsToFirstAndIncrementsRound()
    {
        var a = new Combatant("A", initiative: 15);
        var b = new Combatant("B", initiative: 10);
        var tracker = TrackerWith(a, b);

        tracker.Next();
        tracker.Next();

        Assert.Same(a, tracker.CurrentCombatant);
        Assert.Equal(2, tracker.Round);
    }

    // Behaviour 3: Previous

    [Fact]
    public void Previous_AtFirstCombatantInRoundOne_DoesNothing()
    {
        var a = new Combatant("A", initiative: 15);
        var b = new Combatant("B", initiative: 10);
        var tracker = TrackerWith(a, b);

        tracker.Previous();

        Assert.Same(a, tracker.CurrentCombatant);
        Assert.Equal(1, tracker.Round);
    }

    [Fact]
    public void Previous_AtFirstCombatantInLaterRound_GoesToLastAndDecrementsRound()
    {
        var a = new Combatant("A", initiative: 15);
        var b = new Combatant("B", initiative: 10);
        var tracker = TrackerWith(a, b);
        tracker.Next();
        tracker.Next();

        tracker.Previous();

        Assert.Same(b, tracker.CurrentCombatant);
        Assert.Equal(1, tracker.Round);
    }

    // Behaviour 4: removing someone earlier in the order

    [Fact]
    public void Remove_CombatantBeforeCurrent_KeepsSameCombatantCurrent()
    {
        var a = new Combatant("A", initiative: 15);
        var b = new Combatant("B", initiative: 10);
        var c = new Combatant("C", initiative: 5);
        var tracker = TrackerWith(a, b, c);
        tracker.Next();
        tracker.Next();

        tracker.Remove(a);

        AssertOrder(tracker, b, c);
        Assert.Same(c, tracker.CurrentCombatant);
        Assert.Equal(1, tracker.Round);
    }

    [Fact]
    public void Remove_CombatantAfterCurrent_KeepsSameCombatantCurrent()
    {
        var a = new Combatant("A", initiative: 15);
        var b = new Combatant("B", initiative: 10);
        var c = new Combatant("C", initiative: 5);
        var tracker = TrackerWith(a, b, c);
        tracker.Next();

        tracker.Remove(c);

        Assert.Same(b, tracker.CurrentCombatant);
        Assert.Equal(1, tracker.Round);
    }

    // Behaviour 5: removing the current combatant

    [Fact]
    public void Remove_CurrentCombatant_PassesTurnToNextInOrder()
    {
        var a = new Combatant("A", initiative: 15);
        var b = new Combatant("B", initiative: 10);
        var c = new Combatant("C", initiative: 5);
        var tracker = TrackerWith(a, b, c);
        tracker.Next();

        tracker.Remove(b);

        Assert.Same(c, tracker.CurrentCombatant);
        Assert.Equal(1, tracker.Round);
    }

    [Fact]
    public void Remove_CurrentCombatantWhenLastInOrder_WrapsToFirstAndIncrementsRound()
    {
        var a = new Combatant("A", initiative: 15);
        var b = new Combatant("B", initiative: 10);
        var tracker = TrackerWith(a, b);
        tracker.Next();

        tracker.Remove(b);

        Assert.Same(a, tracker.CurrentCombatant);
        Assert.Equal(2, tracker.Round);
    }

    [Fact]
    public void Remove_OnlyCombatant_LeavesNoCurrentAndKeepsRound()
    {
        var a = new Combatant("A", initiative: 15);
        var tracker = TrackerWith(a);

        tracker.Remove(a);

        Assert.Empty(tracker.Combatants);
        Assert.Null(tracker.CurrentCombatant);
        Assert.Equal(1, tracker.Round);
    }

    [Fact]
    public void Remove_CombatantNotInTracker_ReturnsFalseAndChangesNothing()
    {
        var a = new Combatant("A", initiative: 15);
        var stranger = new Combatant("Stranger", initiative: 10);
        var tracker = TrackerWith(a);

        bool removed = tracker.Remove(stranger);

        Assert.False(removed);
        AssertOrder(tracker, a);
    }

    // Behaviour 6: Move

    [Fact]
    public void Move_CurrentCombatant_StaysCurrentAtNewPosition()
    {
        var a = new Combatant("A", initiative: 15);
        var b = new Combatant("B", initiative: 10);
        var c = new Combatant("C", initiative: 5);
        var tracker = TrackerWith(a, b, c);

        tracker.Move(fromIndex: 0, toIndex: 2);

        AssertOrder(tracker, b, c, a);
        Assert.Same(a, tracker.CurrentCombatant);
    }

    [Fact]
    public void Move_OtherCombatantAcrossCurrent_KeepsSameCombatantCurrent()
    {
        var a = new Combatant("A", initiative: 15);
        var b = new Combatant("B", initiative: 10);
        var c = new Combatant("C", initiative: 5);
        var tracker = TrackerWith(a, b, c);
        tracker.Next();

        tracker.Move(fromIndex: 2, toIndex: 0);

        AssertOrder(tracker, c, a, b);
        Assert.Same(b, tracker.CurrentCombatant);
        Assert.Equal(1, tracker.Round);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(2, 0)]
    [InlineData(0, 2)]
    public void Move_IndexOutsideOrder_Throws(int fromIndex, int toIndex)
    {
        var tracker = TrackerWith(
            new Combatant("A", initiative: 15),
            new Combatant("B", initiative: 10));

        Assert.Throws<ArgumentOutOfRangeException>(() => tracker.Move(fromIndex, toIndex));
    }

    private static InitiativeTracker TrackerWith(params Combatant[] combatants)
    {
        var tracker = new InitiativeTracker();
        foreach (Combatant combatant in combatants)
        {
            tracker.Add(combatant);
        }

        return tracker;
    }

    private static void AssertOrder(InitiativeTracker tracker, params Combatant[] expected)
    {
        Assert.Equal<Combatant>(expected, tracker.Combatants);
    }
}
