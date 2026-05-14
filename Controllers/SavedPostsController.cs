using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewsAggregator.Data;
using NewsAggregator.Models;
using System.Security.Claims;

namespace NewsAggregator.Controllers
{
    [Authorize]
    public class SavedPostsController : Controller
    {
        private readonly AppDbContext _db;

        public SavedPostsController(AppDbContext db)
        {
            _db = db;
        }

        // GET: /SavedPosts
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            var saved = await _db.SavedPosts
                .Where(s => s.UserID == userId)
                .Include(s => s.Post)
                    .ThenInclude(p => p!.Menu)
                .Include(s => s.Post)
                    .ThenInclude(p => p!.Source)
                .OrderByDescending(s => s.SavedAt)
                .ToListAsync();

            return View(saved);
        }

        // POST: /SavedPosts/Save
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(int postId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Json(new { success = false, message = "Vui lòng đăng nhập!" });

            var exists = await _db.SavedPosts
                .AnyAsync(s => s.UserID == userId && s.PostID == postId);

            if (exists)
                return Json(new { success = false, message = "Bài viết đã được lưu trước đó!" });

            var post = await _db.Posts.FindAsync(postId);
            if (post == null || post.IsDeleted)
                return Json(new { success = false, message = "Bài viết không tồn tại!" });

            _db.SavedPosts.Add(new SavedPost
            {
                UserID  = userId.Value,
                PostID  = postId,
                SavedAt = DateTime.Now
            });
            await _db.SaveChangesAsync();

            return Json(new { success = true, message = "Đã lưu bài viết thành công!" });
        }

        // POST: /SavedPosts/Remove
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int postId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Json(new { success = false, message = "Vui lòng đăng nhập!" });

            var saved = await _db.SavedPosts
                .FirstOrDefaultAsync(s => s.UserID == userId && s.PostID == postId);

            if (saved == null)
                return Json(new { success = false, message = "Bài viết chưa được lưu!" });

            _db.SavedPosts.Remove(saved);
            await _db.SaveChangesAsync();

            return Json(new { success = true, message = "Đã xóa bài viết khỏi danh sách!" });
        }

        // POST: /SavedPosts/RemoveFromList (redirect về danh sách)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromList(int postId)
        {
            var userId = GetCurrentUserId();
            if (userId != null)
            {
                var saved = await _db.SavedPosts
                    .FirstOrDefaultAsync(s => s.UserID == userId && s.PostID == postId);
                if (saved != null)
                {
                    _db.SavedPosts.Remove(saved);
                    await _db.SaveChangesAsync();
                }
            }
            TempData["Success"] = "Đã xóa bài viết khỏi danh sách đã lưu!";
            return RedirectToAction(nameof(Index));
        }

        private int? GetCurrentUserId()
        {
            var val = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(val, out var id) ? id : null;
        }
    }
}
