using Microsoft.EntityFrameworkCore;
using Surl.API.Model;

namespace Surl.API.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<UrlShorten> UrlShorten { get; set; }
        public DbSet<UrlShortenAccess> UrlShortenAccess { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite("DataSource=Data/app.db;Cache=Shared");
    }
}
