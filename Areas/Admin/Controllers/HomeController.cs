using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewsAggregator.Data;
using NewsAggregator.Models;
using NewsAggregator.Models.ViewModels;

namespace NewsAggregator.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = NewsAggregator.Models.UserRoles.StaffRoles)]
public class HomeController : Controller
{
    private readonly AppDbContext _context;

    public HomeController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var viewModel = new AdminDashboardViewModel
        {
            TotalPosts    = await _context.Posts.CountAsync(),
            TotalMenus    = await _context.Menus.CountAsync(),
            TotalUsers    = await _context.AppUsers.CountAsync(u => !u.IsDeleted),
            TotalComments = await _context.Comments.CountAsync(),

            // Select projection: tránh load cột Contents (NVARCHAR MAX) gây timeout
            RecentPosts = await _context.Posts
                .AsNoTracking()
                .Where(p => !p.IsDeleted)
                .OrderByDescending(p => p.CreatedDate)
                .Take(5)
                .Select(p => new Post
                {
                    PostID      = p.PostID,
                    Title       = p.Title,
                    Author      = p.Author,
                    CreatedDate = p.CreatedDate,
                    MenuID      = p.MenuID,
                    Menu        = p.Menu != null
                        ? new Menu { MenuID = p.Menu.MenuID, MenuName = p.Menu.MenuName }
                        : null,
                })
                .ToListAsync(),

            RecentComments = await _context.Comments
                .AsNoTracking()
                .OrderByDescending(c => c.CreatedAt)
                .Take(5)
                .Select(c => new Comment
                {
                    CommentID  = c.CommentID,
                    Content    = c.Content,
                    CreatedAt  = c.CreatedAt,
                    IsApproved = c.IsApproved,
                    PostID     = c.PostID,
                    UserID     = c.UserID,
                    Post       = c.Post != null
                        ? new Post { PostID = c.Post.PostID, Title = c.Post.Title }
                        : null,
                })
                .ToListAsync(),
        };

        return View(viewModel);
    }
}