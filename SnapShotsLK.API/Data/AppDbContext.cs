using Microsoft.EntityFrameworkCore;
using SnapShotsLK.API.Models;

namespace SnapShotsLK.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        
        public DbSet<User> Users { get; set; }
    }
}