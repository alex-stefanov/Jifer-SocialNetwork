namespace Jifer.Data
{
    using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore;
    using Jifer.Data.Models;
    using Microsoft.EntityFrameworkCore.Metadata;
    using System.Reflection.Emit;
    using Microsoft.Extensions.Hosting;

    /// <summary>
    /// Database Application Context
    /// </summary>
    public class ApplicationDbContext : IdentityDbContext<JUser>
    {
        public ApplicationDbContext() { }
        /// <summary>
        /// Database Application Context Constructor
        /// </summary>
        /// <param name="options">Options for the DbContext</param>
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
            
        }

        /// <summary>
        /// Database Application Context JUser DbSet
        /// </summary>
        public override DbSet<JUser> Users { get; set; } = null!;

        /// <summary>
        /// Database Application Context JShip DbSet
        /// </summary>
        public DbSet<JShip> FriendShips { get; set; } = null!;

        /// <summary>
        /// Database Application Context JInvitation DbSet
        /// </summary>
        public DbSet<JInvitation> Invitations { get; set; } = null!;

        /// <summary>
        /// Database Application Context JGo DbSet
        /// </summary>
        public DbSet<JGo> Posts { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<JShip>()
                .HasOne(f => f.Sender)
                .WithMany(u => u.SentFriendRequests)
                .HasForeignKey(f => f.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<JShip>()
                .HasOne(f => f.Receiver)
                .WithMany(u => u.ReceivedFriendRequests)
                .HasForeignKey(f => f.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<JGo>()
                .HasOne(p => p.Author)
                .WithMany(u => u.JGos)
                .HasForeignKey(p => p.AuthorId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<JInvitation>()
                .HasOne(i => i.Sender)
                .WithMany(u => u.SentJInvitations)
                .HasForeignKey(i => i.SenderId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}