using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewsAggregator.Data;
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
            RecentPosts   = await _context.Posts
                .Include(p => p.Menu)
                .OrderByDescending(p => p.CreatedDate)
                .Take(5)
                .ToListAsync(),
            RecentComments = await _context.Comments
                .Include(c => c.Post)
                .OrderByDescending(c => c.CreatedAt)
                .Take(5)
                .ToListAsync()
        };

        return View(viewModel);
    }
}