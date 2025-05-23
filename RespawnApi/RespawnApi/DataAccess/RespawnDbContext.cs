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

            builder.Entity<UserProfile>()
                .HasOne<IdentityUser>()
                .WithOne()
                .HasForeignKey<UserProfile>(u => u.UserId);

            builder.Entity<Poll>()
                .HasOne(p => p.Creator)
                .WithMany(u => u.CreatedPolls)
                .HasForeignKey(p => p.CreatorUserId)
                .OnDelete(DeleteBehavior.Cascade); // Pokud je UserProfile smazán, smažou se i jeho ankety

            // Oprava unikátního indexu pro PollVote
            // Uživatel může hlasovat pro více možností v jedné anketě (pokud je multiple choice),
            // ale pro každou možnost pouze jednou.
            builder.Entity<PollVote>()
                .HasIndex(v => new { v.PollId, v.UserId, v.OptionId }) // Změněno z { v.PollId, v.UserId }
                .IsUnique();

            // Konfigurace vztahů pro PollVote (explicitní definice může pomoci EF Core)
            builder.Entity<PollVote>()
                .HasOne(pv => pv.Poll)
                .WithMany(p => p.PollVotes)
                .HasForeignKey(pv => pv.PollId)
                .OnDelete(DeleteBehavior.Cascade); // Pokud je Poll smazán, smažou se i jeho hlasy

            builder.Entity<PollVote>()
                .HasOne(pv => pv.Option)
                .WithMany(po => po.PollVotes)
                .HasForeignKey(pv => pv.OptionId)
                .OnDelete(DeleteBehavior.Cascade); // Pokud je PollOption smazána, smažou se i její hlasy

            builder.Entity<PollVote>()
                .HasOne(pv => pv.User)
                .WithMany(u => u.PollVotes)
                .HasForeignKey(pv => pv.UserId)
                .OnDelete(DeleteBehavior.Cascade); // Pokud je UserProfile smazán, smažou se i jeho hlasy
        }
    }
}
