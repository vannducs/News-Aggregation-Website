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
        public DbSet<AppUser> AppUsers { get; set; }
        public DbSet<Comment> Comments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Menu>().ToTable("tblTable");
            modelBuilder.Entity<Post>().ToTable("tblPost");
            modelBuilder.Entity<Source>().ToTable("tblSources");
            modelBuilder.Entity<CrawlLog>().ToTable("tblCrawlLogs");
            modelBuilder.Entity<AISummary>().ToTable("tblAISummary");
            modelBuilder.Entity<AppUser>().ToTable("tblUsers");
            modelBuilder.Entity<Comment>().ToTable("tblComments");
            modelBuilder.Entity<Post>().HasIndex(p => p.Link).IsUnique();
            modelBuilder.Entity<AISummary>().HasIndex(a => a.PostID).IsUnique();
            modelBuilder.Entity<AppUser>().HasIndex(u => u.Email).IsUnique();

            modelBuilder.Entity<Post>()
                .HasOne(p => p.Menu)
                .WithMany(m => m.Posts)
                .HasForeignKey(p => p.MenuID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Comment>()
                .HasOne(c => c.Post)
                .WithMany(p => p.Comments)
                .HasForeignKey(c => c.PostID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Comment>()
                .HasOne(c => c.User)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.UserID)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
