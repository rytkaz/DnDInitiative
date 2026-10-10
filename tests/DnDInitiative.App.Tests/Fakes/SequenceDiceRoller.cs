using DnDInitiative.Core.Dice;

namespace DnDInitiative.App.Tests.Fakes;

/// <summary>Returns the given results in order, then keeps repeating the last one.</summary>
internal sealed class SequenceDiceRoller : IDiceRoller
{
    private readonly Queue<int> _results;
    private int _last;

    public SequenceDiceRoller(params int[] results)
    {
        _results = new Queue<int>(results);
    }

    public int Roll(int sides)
    {
        if (_results.TryDequeue(out int next))
        {
            _last = next;
        }

        return _last;
    }
}