namespace DnDInitiative.Core.Combat;

/// <summary>
/// Owns the turn order, whose turn it is, and the round counter.
/// </summary>
public sealed class InitiativeTracker
{
    private readonly List<Combatant> _combatants = [];
    private int _currentIndex;

    public IReadOnlyList<Combatant> Combatants => _combatants;

    public int Round { get; private set; } = 1;

    /// <summary>Null only while the tracker is empty.</summary>
    public Combatant? CurrentCombatant => _combatants.Count > 0 ? _combatants[_currentIndex] : null;

    public void Add(Combatant combatant)
    {
        ArgumentNullException.ThrowIfNull(combatant);
        if (_combatants.Contains(combatant))
        {
            throw new ArgumentException("Combatant is already in the tracker.", nameof(combatant));
        }

        // First slot whose occupant has a strictly lower initiative, so a tie
        // lands after everyone already holding that value.
        int insertAt = _combatants.FindIndex(c => c.Initiative < combatant.Initiative);
        if (insertAt < 0)
        {
            insertAt = _combatants.Count;
        }

        // Until someone has taken a turn, "current" simply means the top of
        // the order. Once combat has moved on, a newcomer must not steal the turn.
        bool combatHasAdvanced = Round > 1 || _currentIndex > 0;
        bool shiftsCurrent = _combatants.Count > 0 && combatHasAdvanced && insertAt <= _currentIndex;

        _combatants.Insert(insertAt, combatant);

        if (shiftsCurrent)
        {
            _currentIndex++;
        }
    }

    public bool Remove(Combatant combatant)
    {
        int index = _combatants.IndexOf(combatant);
        if (index < 0)
        {
            return false;
        }

        _combatants.RemoveAt(index);

        if (index < _currentIndex)
        {
            // Everything after the removed slot moved up by one, including the current combatant.
            _currentIndex--;
        }
        else if (_currentIndex == _combatants.Count)
        {
            // The current combatant was last in the order. Their turn is over,
            // so this is the same as pressing Next: back to the top, new round.
            _currentIndex = 0;
            if (_combatants.Count > 0)
            {
                Round++;
            }
        }

        // Otherwise either a later combatant was removed (nothing to do), or the
        // current one was removed and the next combatant has already slid into its slot.
        return true;
    }

    public void Next()
    {
        if (_combatants.Count == 0)
        {
            return;
        }

        _currentIndex++;
        if (_currentIndex == _combatants.Count)
        {
            _currentIndex = 0;
            Round++;
        }
    }

    public void Previous()
    {
        if (_combatants.Count == 0)
        {
            return;
        }

        if (_currentIndex > 0)
        {
            _currentIndex--;
        }
        else if (Round > 1)
        {
            _currentIndex = _combatants.Count - 1;
            Round--;
        }
    }

    /// <summary>Moves a combatant so that it ends up at <paramref name="toIndex"/>.</summary>
    public void Move(int fromIndex, int toIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(fromIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(fromIndex, _combatants.Count);
        ArgumentOutOfRangeException.ThrowIfNegative(toIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(toIndex, _combatants.Count);

        Combatant current = _combatants[_currentIndex];
        Combatant moved = _combatants[fromIndex];

        _combatants.RemoveAt(fromIndex);
        _combatants.Insert(toIndex, moved);

        // Reordering changes positions, never whose turn it is.
        _currentIndex = _combatants.IndexOf(current);
    }

    /// <summary>
    /// Changes a combatant's initiative and moves it to the matching place in the order.
    /// </summary>
    public void SetInitiative(Combatant combatant, int initiative)
    {
        ArgumentNullException.ThrowIfNull(combatant);

        int fromIndex = _combatants.IndexOf(combatant);
        if (fromIndex < 0)
        {
            throw new ArgumentException("Combatant is not in the tracker.", nameof(combatant));
        }

        combatant.Initiative = initiative;

        // Same rule as Add, applied to everyone else: go before the first
        // combatant with a strictly lower initiative.
        int toIndex = 0;
        foreach (Combatant other in _combatants)
        {
            if (ReferenceEquals(other, combatant))
            {
                continue;
            }

            if (other.Initiative < initiative)
            {
                break;
            }

            toIndex++;
        }

        Move(fromIndex, toIndex);
    }
}