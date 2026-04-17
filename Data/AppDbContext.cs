using Microsoft.EntityFrameworkCore;
using NewsAggregator.Models;

namespace NewsAggregator.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}

        public DbSet<Menu> Menus {get; set;}
        public DbSet<Post> Posts {get; set;}
        public DbSet<Source> Sources {get; set;}
        public DbSet<CrawlLog> CrawlLogs {get; set;}
        public DbSet<AISummary> AISummaries {get; set;}

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Menu>().ToTable("tblMenu");
            modelBuilder.Entity<Post>().ToTable("tblPost");
            modelBuilder.Entity<Source>().ToTable("tblSources");
            modelBuilder.Entity<CrawlLog>().ToTable("tblCrawlLogs");
            modelBuilder.Entity<AISummary>().ToTable("tblAISummaries");
            modelBuilder.Entity<Post>().HasIndex(p => p.Link).IsUnique();
            modelBuilder.Entity<AISummary>().HasIndex(a => a.PostID).IsUnique();
        }
    }
}