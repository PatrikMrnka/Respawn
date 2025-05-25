using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RespawnApi.Domain.Entities;

namespace RespawnApi.Data
{
    /// <summary>
    /// Represents the database context for the Respawn application, inheriting from IdentityDbContext to manage user authentication and authorization.
    /// </summary>
    public class RespawnDbContext : IdentityDbContext<IdentityUser>
    {
        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<GameServer> GameServers { get; set; }
        public DbSet<GameSession> GameSessions { get; set; }
        public DbSet<PlayerStats> PlayerStats { get; set; }
        public DbSet<Poll> Polls { get; set; }
        public DbSet<PollVote> PollVotes { get; set; }
        public DbSet<PollOption> PollOptions { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RespawnDbContext"/> class with the specified options.
        /// </summary>
        /// <param name="options"></param>
        public RespawnDbContext(DbContextOptions<RespawnDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // UserProfile a IdentityUser (extended user profile)
            builder.Entity<UserProfile>()
                .HasOne<IdentityUser>()
                .WithOne()
                .HasForeignKey<UserProfile>(up => up.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // GameServer - GameSession (One-to-Many)
            builder.Entity<GameServer>()
                .HasMany(gs => gs.GameSessions)
                .WithOne(s =>
                    s.Server)
                .HasForeignKey(s => s.GameServerId)
                .OnDelete(DeleteBehavior.Cascade);

            // GameServer - PlayerStats (One-to-Many)
            builder.Entity<GameServer>()
                .HasMany(gs => gs.PlayerStats)
                .WithOne(ps =>
                    ps.Server)
                .HasForeignKey(ps =>
                    ps.ServerId)
                .OnDelete(DeleteBehavior.Cascade);

            // GameSession - PlayerStats (One-to-Many)
            builder.Entity<GameSession>()
                .HasMany(s => s.PlayerStatsInSession)
                .WithOne(ps =>
                    ps.GameSession)
                .HasForeignKey(ps => ps.GameSessionId)
                .OnDelete(DeleteBehavior
                    .SetNull)
                .IsRequired(false);

            // UserProfile - PlayerStats (One-to-Many)
            builder.Entity<UserProfile>()
                .HasMany(up => up.PlayerStats) 
                .WithOne(ps =>
                    ps.User)
                .HasForeignKey(ps => ps.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Poll - UserProfile (Creator) (One-to-Many)
            builder.Entity<Poll>()
                .HasOne(p => p.Creator)
                .WithMany(u => u.CreatedPolls)
                .HasForeignKey(p => p.CreatorUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Poll - PollOption (One-to-Many)
            builder.Entity<Poll>()
                .HasMany(p => p.PollOptions)
                .WithOne(o => o.Poll)
                .HasForeignKey(o => o.PollId)
                .OnDelete(DeleteBehavior.Cascade);

            // Poll - PollVote (One-to-Many)
            builder.Entity<Poll>()
                .HasMany(p => p.PollVotes)
                .WithOne(v => v.Poll)
                .HasForeignKey(v => v.PollId)
                .OnDelete(DeleteBehavior.Cascade);

            // PollOption - PollVote (One-to-Many)
            builder.Entity<PollOption>()
                .HasMany(o => o.PollVotes)
                .WithOne(v => v.Option)
                .HasForeignKey(v => v.OptionId)
                .OnDelete(DeleteBehavior.Cascade);

            // PollVote - UserProfile (One-to-Many)
            builder.Entity<PollVote>()
                .HasOne(v => v.User)
                .WithMany(u => u.PollVotes)
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // user can vote for each option in a poll only once
            builder.Entity<PollVote>()
                .HasIndex(v => new { v.PollId, v.UserId, v.OptionId })
                .IsUnique();
        }
    }
}