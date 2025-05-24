// DataAccess/RespawnDbContext.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RespawnApi.Domain.Entities;

namespace RespawnApi.Data
{
    public class RespawnDbContext : IdentityDbContext<IdentityUser>
    {
        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<GameServer> GameServers { get; set; }
        public DbSet<GameSession> GameSessions { get; set; }
        public DbSet<PlayerStats> PlayerStats { get; set; }
        public DbSet<Poll> Polls { get; set; }
        public DbSet<PollVote> PollVotes { get; set; }
        public DbSet<PollOption> PollOptions { get; set; }

        public RespawnDbContext(DbContextOptions<RespawnDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // UserProfile a IdentityUser (základní ASP.NET Core Identity tabulky)
            // Vztah mezi UserProfile a IdentityUser (pokud UserProfile rozšiřuje IdentityUser)
            builder.Entity<UserProfile>()
                .HasOne<IdentityUser>() // UserProfile má jednoho IdentityUser
                .WithOne()              // IdentityUser nemá přímou navigační vlastnost zpět na UserProfile (pokud jste ji nepřidali)
                .HasForeignKey<UserProfile>(up => up.UserId) // Cizí klíč v UserProfile je UserId
                .OnDelete(DeleteBehavior.Cascade); // Pokud je smazán IdentityUser, smaže se i UserProfile

            // GameServer - GameSession (One-to-Many)
            // Jeden GameServer může mít mnoho GameSessions
            builder.Entity<GameServer>()
                .HasMany(gs => gs.GameSessions) // GameServer má kolekci GameSessions
                .WithOne(s => s.Server)         // Každá GameSession patří jednomu GameServer (navigační vlastnost 'Server' v GameSession)
                .HasForeignKey(s => s.GameServerId) // Cizí klíč v GameSession je 'GameServerId'
                .OnDelete(DeleteBehavior.Cascade);  // Pokud je GameServer smazán, smažou se i jeho sessions

            // GameServer - PlayerStats (One-to-Many)
            // Jeden GameServer může mít mnoho záznamů PlayerStats
            builder.Entity<GameServer>()
                .HasMany(gs => gs.PlayerStats)
                .WithOne(ps => ps.Server)           // Každý PlayerStats patří jednomu GameServer (navigační vlastnost 'Server' v PlayerStats)
                .HasForeignKey(ps => ps.ServerId)   // Cizí klíč v PlayerStats je 'ServerId' (přejmenováno z GameServerId v PlayerStats)
                .OnDelete(DeleteBehavior.Cascade);  // Pokud je GameServer smazán, smažou se i jeho statistiky

            // GameSession - PlayerStats (One-to-Many)
            // Jedna GameSession může mít mnoho záznamů PlayerStats
            builder.Entity<GameSession>()
                .HasMany(s => s.PlayerStatsInSession) // GameSession má kolekci PlayerStatsInSession
                .WithOne(ps => ps.GameSession)        // Každý PlayerStats může patřit jedné GameSession (navigační vlastnost 'GameSession' v PlayerStats)
                .HasForeignKey(ps => ps.GameSessionId) // Cizí klíč v PlayerStats je 'GameSessionId' (typ Guid?)
                .OnDelete(DeleteBehavior.SetNull)   // Pokud je GameSession smazána, GameSessionId v PlayerStats se nastaví na NULL
                .IsRequired(false);                 // Cizí klíč GameSessionId je nulovatelný

            // UserProfile - PlayerStats (One-to-Many)
            // Jeden UserProfile může mít mnoho záznamů PlayerStats
            builder.Entity<UserProfile>()
                .HasMany(up => up.PlayerStats)      // UserProfile má kolekci PlayerStats
                .WithOne(ps => ps.User)             // Každý PlayerStats patří jednomu UserProfile (navigační vlastnost 'User' v PlayerStats)
                .HasForeignKey(ps => ps.UserId)     // Cizí klíč v PlayerStats je 'UserId'
                .OnDelete(DeleteBehavior.Cascade);  // Pokud je UserProfile smazán, smažou se i jeho statistiky

            // Poll - UserProfile (Creator) (One-to-Many)
            builder.Entity<Poll>()
                .HasOne(p => p.Creator)
                .WithMany(u => u.CreatedPolls) // UserProfile má kolekci CreatedPolls
                .HasForeignKey(p => p.CreatorUserId)
                .OnDelete(DeleteBehavior.Restrict); // Zabrání smazání UserProfile, pokud má vytvořené ankety

            // Poll - PollOption (One-to-Many)
            builder.Entity<Poll>()
                .HasMany(p => p.PollOptions)
                .WithOne(o => o.Poll)           // PollOption patří jedné Poll
                .HasForeignKey(o => o.PollId)
                .OnDelete(DeleteBehavior.Cascade);

            // Poll - PollVote (One-to-Many)
            builder.Entity<Poll>()
                .HasMany(p => p.PollVotes)
                .WithOne(v => v.Poll)           // PollVote patří jedné Poll
                .HasForeignKey(v => v.PollId)
                .OnDelete(DeleteBehavior.Cascade);

            // PollOption - PollVote (One-to-Many)
            builder.Entity<PollOption>()
                .HasMany(o => o.PollVotes)
                .WithOne(v => v.Option)         // PollVote patří jedné PollOption
                .HasForeignKey(v => v.OptionId)
                .OnDelete(DeleteBehavior.Cascade);

            // PollVote - UserProfile (Hlasující uživatel) (One-to-Many)
            builder.Entity<PollVote>()
                .HasOne(v => v.User)
                .WithMany(u => u.PollVotes)     // UserProfile má kolekci PollVotes
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.Cascade); // Smazání UserProfile smaže jeho hlasy

            // Unikátní index pro PollVote (Uživatel může hlasovat pro každou možnost v anketě pouze jednou)
            builder.Entity<PollVote>()
                .HasIndex(v => new { v.PollId, v.UserId, v.OptionId })
                .IsUnique();

            // Konfigurace pro PlayerStats.Data jako JSON sloupec (pokud používáte Pomelo.EntityFrameworkCore.MySql)
            // Pokud používáte jiného providera, syntaxe se může lišit nebo nemusí být podporována přímo.
            // Pro Pomelo:
            // builder.Entity<PlayerStats>()
            //    .Property(p => p.Data)
            //    .HasColumnType("json"); // Již máte atribut [Column(TypeName = "json")] v entitě, takže toto je redundantní, ale pro jistotu.

            // Další konfigurace modelu...
            // Například unikátní indexy pro názvy, pokud jsou potřeba, atd.
        }
    }
}
