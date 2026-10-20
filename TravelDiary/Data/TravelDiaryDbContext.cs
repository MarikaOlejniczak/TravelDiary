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
    }
}