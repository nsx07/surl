using Microsoft.EntityFrameworkCore;
using Surl.API.Model;

namespace Surl.API.Data
{
    public class AppDbContext(DbContextOptions options) : DbContext(options)
    {
        public virtual DbSet<UrlShorten> UrlShorten { get; set; }
        public virtual DbSet<UrlShortenAccess> UrlShortenAccess { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite("DataSource=Data/app.db;Cache=Shared");
            }
        }

    }
}
