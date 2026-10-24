using Microsoft.EntityFrameworkCore;

namespace DnDInitiative.Infrastructure.Persistence;

internal sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<PartyEntity> Parties => Set<PartyEntity>();

    public DbSet<StatBlockEntity> StatBlocks => Set<StatBlockEntity>();

    public DbSet<EncounterEntity> Encounters => Set<EncounterEntity>();

    public DbSet<SpellEntity> Spells => Set<SpellEntity>();

    public DbSet<ConditionEntity> Conditions => Set<ConditionEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PartyEntity>(party =>
        {
            party.ToTable("Parties");
            party.HasKey(entity => entity.Id);
            party.Property(entity => entity.Name).IsRequired();
            party.HasMany(entity => entity.Members)
                .WithOne()
                .HasForeignKey(member => member.PartyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PlayerCharacterEntity>(member =>
        {
            member.ToTable("PlayerCharacters");
            member.HasKey(entity => entity.Id);
            member.Property(entity => entity.Name).IsRequired();
        });

        modelBuilder.Entity<StatBlockEntity>(statBlock =>
        {
            statBlock.ToTable("StatBlocks");
            statBlock.HasKey(entity => entity.Id);

            // NOCASE makes sorting and comparing names ignore upper and lower case.
            statBlock.Property(entity => entity.Name).IsRequired().UseCollation("NOCASE");
            statBlock.HasIndex(entity => entity.Name);
        });

        modelBuilder.Entity<EncounterEntity>(encounter =>
        {
            encounter.ToTable("Encounters");
            encounter.HasKey(entity => entity.Id);
            encounter.Property(entity => entity.Name).IsRequired();
            encounter.HasMany(entity => entity.Creatures)
                .WithOne()
                .HasForeignKey(creature => creature.EncounterId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EncounterCreatureEntity>(creature =>
        {
            creature.ToTable("EncounterCreatures");
            creature.HasKey(entity => entity.Id);
        });

        modelBuilder.Entity<SpellEntity>(spell =>
        {
            spell.ToTable("Spells");

            // Not called "Id", so EF Core has to be told this is the key.
            spell.HasKey(entity => entity.Key);

            // Looked up by name when a link names a spell instead of giving its key.
            spell.Property(entity => entity.Name).IsRequired().UseCollation("NOCASE");
            spell.HasIndex(entity => entity.Name);
        });

        modelBuilder.Entity<ConditionEntity>(condition =>
        {
            condition.ToTable("Conditions");
            condition.HasKey(entity => entity.Name);

            // On the key itself, so "prone" and "Prone" are the same condition to the database.
            condition.Property(entity => entity.Name).UseCollation("NOCASE");
        });
    }
}