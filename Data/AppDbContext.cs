using Microsoft.EntityFrameworkCore;
using DiplomBackend.Models;

namespace DiplomBackend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Добавьте эту строку, если её не было:
        public DbSet<User> Users { get; set; }

        public DbSet<Profession> Professions { get; set; }
    }
}