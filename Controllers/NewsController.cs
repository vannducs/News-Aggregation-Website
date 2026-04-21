using NewsAggregator.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace NewsAggregator.Controllers
{
    public class NewsController : BaseController
    {
        public NewsController(AppDbContext db) : base(db) {}
        //Lay bai viet
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Trang chủ";
            var posts = await _db.Posts
                .Include(p=>p.Menu)
                .Include(p=>p.Source)
                .Where(p=>p.IsActive)
                .OrderByDescending(p=>p.CreatedDate)
                .Take(20)
                .ToListAsync();
            
            return View(posts);
        }
        //Lay ten danh muc
        public async Task<IActionResult> Category(int id)
        {
            var menu = await _db.Menus.FindAsync(id);
            if (menu == null) return NotFound();

            ViewData["Title"] = menu.MenuName;
            ViewData["MenuName"] = menu.MenuName;

            var posts = await _db.Posts
                .Include(p=>p.Menu)
                .Include(p=>p.Source)
                .Where(p=>p.IsActive && p.MenuID == id)
                .OrderByDescending(p=>p.CreatedDate)
                .Take(20)
                .ToListAsync();

            return View(posts);
        }
        //CHi tiet bai viet
        public async Task<IActionResult> Detail(int id)
        {
            var post = await _db.Posts
                .Include(p=>p.Menu)
                .Include(p=>p.Source)
                .FirstOrDefaultAsync(p => p.PostID == id);
            
            if (post==null) return NotFound();
            ViewData["Title"] = post.Title;
            post.ViewCount++;
            await _db.SaveChangesAsync();

            var relatedPosts = await _db.Posts
                .Where(p=>p.MenuID==post.MenuID && p.PostID != post.PostID && p.IsActive)
                .OrderByDescending(p=>p.CreatedDate)
                .Take(5)
                .ToListAsync();
            
            ViewBag.RelatedPosts = relatedPosts;
            return View(post);
        }
        //Tìm kiếm bài viết
        public async Task<IActionResult> Search(string q)
        {
            ViewData["Title"] = $"Tìm kiếm: {q}";
            ViewBag.Keyword = q;

            if (string.IsNullOrEmpty(q))
                return View(new List<NewsAggregator.Models.Post>());

            var posts = await _db.Posts
                .Include(p=>p.Menu)
                .Include(p=>p.Source)
                .Where(p=>p.IsActive && (p.Title.Contains(q) || p.Abstract.Contains(q)))
                .OrderByDescending(p=>p.CreatedDate)
                .Take(20)
                .ToListAsync();

            return View(posts);
        }

    }
    
}