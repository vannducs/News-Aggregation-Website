using Microsoft.EntityFrameworkCore;
using NewsAggregator.Data;

namespace NewsAggregator.Services
{
    public class TrashPurgeService
    {
        private readonly AppDbContext _db;

        public TrashPurgeService(AppDbContext db)
        {
            _db = db;
        }

        public async Task PurgeOldDeletedItemsAsync()
        {
            var cutoff = DateTime.Now.AddDays(-30);

            var oldPosts = await _db.Posts
                .Where(p => p.IsDeleted && p.DeletedAt <= cutoff)
                .ToListAsync();
            if (oldPosts.Any())
            {
                _db.Posts.RemoveRange(oldPosts);
                Console.WriteLine($"[Trash] Xóa {oldPosts.Count} bài viết cũ");
            }

            var oldMenus = await _db.Menus
                .Where(m => m.IsDeleted && m.DeletedAt <= cutoff)
                .ToListAsync();
            if (oldMenus.Any())
            {
                _db.Menus.RemoveRange(oldMenus);
                Console.WriteLine($"[Trash] Xóa {oldMenus.Count} danh mục cũ");
            }

            var oldSources = await _db.Sources
                .Where(s => s.IsDeleted && s.DeletedAt <= cutoff)
                .ToListAsync();
            if (oldSources.Any())
            {
                _db.Sources.RemoveRange(oldSources);
                Console.WriteLine($"[Trash] Xóa {oldSources.Count} nguồn báo cũ");
            }

            await _db.SaveChangesAsync();
            Console.WriteLine($"[Trash] Hoàn thành purge lúc {DateTime.Now}");
        }
    }
}
