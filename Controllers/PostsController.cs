using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using NewsAggregator.Data;
using NewsAggregator.Models;
using NewsAggregator.Models.ViewModels;

namespace NewsAggregator.Controllers
{
    public class PostsController : Controller
    {
        private readonly AppDbContext _context;

        public PostsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var post = await _context.Posts
                .Include(p => p.Menu)
                .FirstOrDefaultAsync(p => p.PostID == id && p.IsActive);

            if (post is null)
            {
                return NotFound();
            }

            post.ViewCount += 1;
            await _context.SaveChangesAsync();

            var viewModel = new PostDetailsViewModel
            {
                Post = post,
                Menus = await _context.Menus
                    .Where(m => m.IsActive && m.Position == 1)
                    .OrderBy(m => m.MenuOrder)
                    .ToListAsync(),
                RelatedPosts = await _context.Posts
                    .Where(p => p.IsActive && p.MenuID == post.MenuID && p.PostID != id)
                    .OrderByDescending(p => p.CreatedDate)
                    .Take(3)
                    .ToListAsync(),
                Comments = await _context.Comments
                    .Where(c => c.PostID == id && c.IsApproved)
                    .OrderByDescending(c => c.CreatedAt)
                    .ToListAsync(),
                CommentForm = new CommentInputModel { PostID = id }
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(CommentInputModel input)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                input.AuthorName = GetCommentAuthorName(input);
                input.AuthorEmail = GetCommentAuthorEmail(input);
                ModelState.Remove(nameof(input.AuthorName));
                ModelState.Remove(nameof(input.AuthorEmail));
            }

            if (!ModelState.IsValid)
            {
                var post = await _context.Posts
                    .Include(p => p.Menu)
                    .FirstOrDefaultAsync(p => p.PostID == input.PostID && p.IsActive);

                if (post is null)
                {
                    return NotFound();
                }

                var viewModel = new PostDetailsViewModel
                {
                    Post = post,
                    Menus = await _context.Menus.Where(m => m.IsActive && m.Position == 1).OrderBy(m => m.MenuOrder).ToListAsync(),
                    RelatedPosts = await _context.Posts
                        .Where(p => p.IsActive && p.MenuID == post.MenuID && p.PostID != input.PostID)
                        .OrderByDescending(p => p.CreatedDate)
                        .Take(3)
                        .ToListAsync(),
                    Comments = await _context.Comments
                        .Where(c => c.PostID == input.PostID && c.IsApproved)
                        .OrderByDescending(c => c.CreatedAt)
                        .ToListAsync(),
                    CommentForm = input
                };

                return View("Details", viewModel);
            }

            _context.Comments.Add(new Comment
            {
                PostID = input.PostID,
                UserID = GetCurrentUserId(),
                AuthorName = GetCommentAuthorName(input),
                AuthorEmail = GetCommentAuthorEmail(input),
                Content = input.Content.Trim(),
                CreatedAt = DateTime.Now,
                IsApproved = true
            });

            await _context.SaveChangesAsync();
            TempData["CommentMessage"] = "Binh luan da duoc gui thanh cong.";

            return RedirectToAction(nameof(Details), new { id = input.PostID });
        }

        private int? GetCurrentUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var userId) ? userId : null;
        }

        private string GetCommentAuthorName(CommentInputModel input)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return User.FindFirst("FullName")?.Value ?? User.Identity.Name ?? input.AuthorName.Trim();
            }

            return input.AuthorName.Trim();
        }

        private string? GetCommentAuthorEmail(CommentInputModel input)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return User.FindFirstValue(ClaimTypes.Email);
            }

            return string.IsNullOrWhiteSpace(input.AuthorEmail) ? null : input.AuthorEmail.Trim();
        }
    }
}
