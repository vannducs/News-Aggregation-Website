using NewsAggregator.Data;
using NewsAggregator.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace NewsAggregator.Controllers
{
    public class NewsController : BaseController
    {
        public NewsController(AppDbContext db) : base(db) {}
        
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Trang chủ";
            var posts = await PostSummaryQuery()
                .Where(p => p.IsActive && !p.IsDeleted)
                .OrderByDescending(p => p.CreatedDate)
                .Take(44)
                .ToListAsync();

            return View(posts);
        }

        public async Task<IActionResult> Category(int id)
        {
            var menu = await _db.Menus.FindAsync(id);
            if (menu == null) return NotFound();

            ViewData["Title"] = menu.MenuName;
            ViewData["MenuName"] = menu.MenuName;

            var posts = await PostSummaryQuery()
                .Where(p => p.IsActive && !p.IsDeleted && p.MenuID == id)
                .OrderByDescending(p => p.CreatedDate)
                .Take(20)
                .ToListAsync();

            return View(posts);
        }
        public async Task<IActionResult> Detail(int id)
        {
            var post = await _db.Posts
                .Include(p=>p.Menu)
                .Include(p=>p.Source)
                .FirstOrDefaultAsync(p => p.PostID == id && !p.IsDeleted);

            if (post == null) return NotFound();
            ViewData["Title"] = post.Title;
            post.ViewCount++;
            await _db.SaveChangesAsync();

            var relatedPosts = await _db.Posts
                .Where(p => p.MenuID == post.MenuID && p.PostID != post.PostID && p.IsActive && !p.IsDeleted)
                .OrderByDescending(p => p.CreatedDate)
                .Take(5)
                .ToListAsync();

            ViewBag.RelatedPosts = relatedPosts;

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out var userId))
            {
                ViewBag.IsSaved = await _db.SavedPosts
                    .AnyAsync(s => s.UserID == userId && s.PostID == id);
            }

            var comments = await _db.Comments
                .Where(c => c.PostID == id && c.IsApproved)
                .Include(c => c.User)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
            ViewBag.Comments = comments;

            return View(post);
        }
        public async Task<IActionResult> Search(string q)
        {
            ViewData["Title"] = $"Tìm kiếm: {q}";
            ViewBag.Keyword = q;

            if (string.IsNullOrEmpty(q))
                return View(new List<Post>());

            var posts = await PostSummaryQuery()
                .Where(p => p.IsActive && !p.IsDeleted && (p.Title.Contains(q) || (p.Abstract != null && p.Abstract.Contains(q))))
                .OrderByDescending(p => p.CreatedDate)
                .Take(20)
                .ToListAsync();

            return View(posts);
        }

        private IQueryable<Post> PostSummaryQuery() =>
            _db.Posts
                .AsNoTracking()
                .Select(p => new Post
                {
                    PostID      = p.PostID,
                    Title       = p.Title,
                    Abstract    = p.Abstract,
                    Images      = p.Images,
                    Link        = p.Link,
                    Author      = p.Author,
                    CreatedDate = p.CreatedDate,
                    IsActive    = p.IsActive,
                    PostOrder   = p.PostOrder,
                    ViewCount   = p.ViewCount,
                    IsDeleted   = p.IsDeleted,
                    DeletedAt   = p.DeletedAt,
                    MenuID      = p.MenuID,
                    SourceID    = p.SourceID,
                    Menu        = p.Menu,
                    Source      = p.Source,
                });
    }
    
}