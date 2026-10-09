using Microsoft.EntityFrameworkCore;

namespace DnDInitiative.Infrastructure.Persistence;

internal sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<PartyEntity> Parties => Set<PartyEntity>();

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
    }
}