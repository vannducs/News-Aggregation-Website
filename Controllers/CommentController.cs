using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewsAggregator.Data;
using NewsAggregator.Models;
using System.Security.Claims;

namespace NewsAggregator.Controllers
{
    public class CommentController : Controller
    {
        private readonly AppDbContext _db;

        public CommentController(AppDbContext db)
        {
            _db = db;
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(int postId, string content)
        {
            if (string.IsNullOrWhiteSpace(content) || content.Length > 1000)
                return Json(new { success = false, message = "Nội dung bình luận không hợp lệ!" });

            var post = await _db.Posts.FindAsync(postId);
            if (post == null || post.IsDeleted)
                return Json(new { success = false, message = "Bài viết không tồn tại!" });

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out var userId))
                return Json(new { success = false, message = "Vui lòng đăng nhập!" });

            var user = await _db.AppUsers.FindAsync(userId);

            var comment = new Comment
            {
                PostID    = postId,
                UserID    = userId,
                Content   = content.Trim(),
                CreatedAt = DateTime.Now,
                IsApproved = true
            };

            _db.Comments.Add(comment);
            await _db.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = "Đã gửi bình luận!",
                comment = new
                {
                    content    = comment.Content,
                    authorName = user?.FullName ?? "Người dùng",
                    createdAt  = comment.CreatedAt.ToString("dd/MM/yyyy HH:mm")
                }
            });
        }
    }
}
