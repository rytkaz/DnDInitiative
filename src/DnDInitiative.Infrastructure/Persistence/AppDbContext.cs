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
    }
}