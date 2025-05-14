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
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<PollVote>()
                .HasIndex(v => new { v.PollId, v.UserId })
                .IsUnique();
        }
    }
}

