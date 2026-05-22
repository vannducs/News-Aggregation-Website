using Microsoft.EntityFrameworkCore;
using NewsAggregator.Data;

namespace NewsAggregator.Services
{
    public class PostImageFixService(AppDbContext db)
    {
        public async Task RunAsync()
        {
            var posts = await db.Posts
                .Where(p => !p.IsDeleted && !string.IsNullOrEmpty(p.Contents))
                .ToListAsync();

            int fixed_ = 0;
            foreach (var post in posts)
            {
                var updated = ContentHelper.FixContentImages(post.Contents!);
                if (updated != post.Contents)
                {
                    post.Contents = updated;
                    fixed_++;
                }
            }

            if (fixed_ > 0)
                await db.SaveChangesAsync();

            Console.WriteLine($"[PostImageFix] Đã fix ảnh cho {fixed_}/{posts.Count} bài viết.");
        }
    }
}
