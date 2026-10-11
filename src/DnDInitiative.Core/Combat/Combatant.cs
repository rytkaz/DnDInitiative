namespace DnDInitiative.Core.Combat;

/// <summary>
/// One participant in a running combat. This is runtime state only: editing a
/// combatant never changes the stat block or saved encounter it came from.
/// </summary>
public sealed class Combatant
{
    public Combatant(
        string name,
        int initiative,
        int? maxHp = null,
        int? armorClass = null,
        Guid? statBlockId = null,
        int initiativeModifier = 0,
        bool isPlayer = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name;
        Initiative = initiative;
        ArmorClass = armorClass;
        StatBlockId = statBlockId;
        InitiativeModifier = initiativeModifier;
        IsPlayer = isPlayer;
        SetMaxHp(maxHp);
    }

    /// <summary>The stat block this combatant was created from, if any.</summary>
    public Guid? StatBlockId { get; }

    public Guid Id { get; } = Guid.NewGuid();

    public string Name { get; private set; }

    public int Initiative { get; set; }

    public int? ArmorClass { get; set; }

    /// <summary>Null when hit points are not tracked for this combatant.</summary>
    public int? MaxHp { get; private set; }

    /// <summary>Null exactly when <see cref="MaxHp"/> is null.</summary>
    public int? CurrentHp { get; private set; }

    public int InitiativeModifier { get; }

    public bool IsPlayer { get; }

    public bool TracksHp => MaxHp is not null;

    /// <summary>
    /// Gives the combatant another name for the rest of the combat, such as "Goblin 2" becoming
    /// "Goblin (burning)". Like every other edit here, it does not touch the stat block.
    /// </summary>
    public void Rename(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
    }

    public void TakeDamage(int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        ChangeHp(-(long)amount);
    }

    public void Heal(int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        ChangeHp(amount);
    }

    public void SetCurrentHp(int value)
    {
        if (MaxHp is not int max)
        {
            throw new InvalidOperationException($"{Name} does not track hit points.");
        }

        CurrentHp = Math.Clamp(value, 0, max);
    }

    public void SetMaxHp(int? maxHp)
    {
        if (maxHp is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxHp), maxHp, "Max HP cannot be negative.");
        }

        if (maxHp is not int max)
        {
            MaxHp = null;
            CurrentHp = null;
            return;
        }

        // Starting to track HP means starting at full; otherwise keep the
        // current value and only pull it down if the new maximum is lower.
        CurrentHp = CurrentHp is int current ? Math.Min(current, max) : max;
        MaxHp = max;
    }

    public override string ToString() => Name;

    private void ChangeHp(long delta)
    {
        if (MaxHp is not int max || CurrentHp is not int current)
        {
            throw new InvalidOperationException($"{Name} does not track hit points.");
        }

        // long arithmetic so a huge amount cannot overflow int and wrap around.
        CurrentHp = (int)Math.Clamp(current + delta, 0, max);
    }
}