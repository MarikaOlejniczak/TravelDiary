
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TravelDiary.Models;

namespace TravelDiary.Data
{
    public class TravelDiaryDbContext : IdentityDbContext<ApplicationUser>
    {
        public TravelDiaryDbContext(
            DbContextOptions<TravelDiaryDbContext> options)
            : base(options)
        {
        }

        public DbSet<Post> Posts { get; set; }
        public DbSet<PostImage> PostImages { get; set; }

        // Zaproszenia i znajomości użytkowników.
        public DbSet<FriendRequest> FriendRequests { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            // Zachowujemy konfigurację ASP.NET Core Identity.
            base.OnModelCreating(builder);

            // Osoba wysyłająca zaproszenie.
            builder.Entity<FriendRequest>()
                .HasOne(f => f.Sender)
                .WithMany()
                .HasForeignKey(f => f.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            // Osoba otrzymująca zaproszenie.
            builder.Entity<FriendRequest>()
                .HasOne(f => f.Receiver)
                .WithMany()
                .HasForeignKey(f => f.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            // Zapobiegamy identycznym zaproszeniom
            // w tym samym kierunku.
            builder.Entity<FriendRequest>()
                .HasIndex(f => new { f.SenderId, f.ReceiverId })
                .IsUnique();
        }
    }
}
