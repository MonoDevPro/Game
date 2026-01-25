using Microsoft.EntityFrameworkCore;

namespace Game.Infrastructure.EfCore;

public sealed class GameDbContext : DbContext
{
    public GameDbContext(DbContextOptions<GameDbContext> options) : base(options)
    {
    }

    public DbSet<AccountRow> Accounts => Set<AccountRow>();
    public DbSet<CharacterRow> Characters => Set<CharacterRow>();
    public DbSet<CharacterVocationRow> CharacterVocations => Set<CharacterVocationRow>();
    public DbSet<EnterTicketRow> EnterTickets => Set<EnterTicketRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AccountRow>(builder =>
        {
            builder.ToTable("accounts");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Username).IsRequired();
            builder.Property(x => x.PasswordHash).IsRequired();
            builder.HasIndex(x => x.Username).IsUnique();
        });

        modelBuilder.Entity<CharacterRow>(builder =>
        {
            builder.ToTable("characters");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Name).IsRequired();
            builder.HasIndex(x => new { x.AccountId, x.Name }).IsUnique();
        });
        
        modelBuilder.Entity<CharacterVocationRow>(builder =>
        {
            builder.ToTable("character_vocations");
            builder.HasKey(x => new { x.CharacterId, x.Vocation });
            builder.Property(x => x.Level).HasDefaultValue(1);
            builder.Property(x => x.Experience).HasDefaultValue(0);
            builder.Property(x => x.Strength).HasDefaultValue(10);
            builder.Property(x => x.Endurance).HasDefaultValue(10);
            builder.Property(x => x.Agility).HasDefaultValue(10);
            builder.Property(x => x.Intelligence).HasDefaultValue(10);
            builder.Property(x => x.Willpower).HasDefaultValue(10);
            builder.Property(x => x.HealthPoints).HasDefaultValue(100);
            builder.Property(x => x.ManaPoints).HasDefaultValue(100);
        });

        modelBuilder.Entity<EnterTicketRow>(builder =>
        {
            builder.ToTable("enter_tickets");
            builder.HasKey(x => x.Ticket);
            builder.Property(x => x.Ticket).IsRequired();
            builder.Property(x => x.ExpiresAt).IsRequired();
        });
    }
}
