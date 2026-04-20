using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using NewsAggregator.Data;
using NewsAggregator.Models;
using NewsAggregator.Models.ViewModels;

namespace NewsAggregator.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _context;

    public HomeController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var menus = await _context.Menus
            .Where(m => m.IsActive && m.Position == 1)
            .OrderBy(m => m.MenuOrder)
            .ToListAsync();

        var postsQuery = _context.Posts
            .Include(p => p.Menu)
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.CreatedDate);

        var viewModel = new HomeIndexViewModel
        {
            Menus = menus,
            FeaturedPosts = await postsQuery.Take(3).ToListAsync(),
            LatestPosts = await postsQuery.Take(6).ToListAsync(),
            PopularPosts = await _context.Posts
                .Include(p => p.Menu)
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.ViewCount)
                .Take(4)
                .ToListAsync()
        };

        return View(viewModel);
    }

    [Route("Home/Category/{id:int}")]
    public async Task<IActionResult> Category(int id)
    {
        var currentMenu = await _context.Menus.FirstOrDefaultAsync(m => m.MenuID == id && m.IsActive);
        if (currentMenu is null)
        {
            return NotFound();
        }

        var viewModel = new CategoryViewModel
        {
            CurrentMenu = currentMenu,
            Menus = await _context.Menus
                .Where(m => m.IsActive && m.Position == 1)
                .OrderBy(m => m.MenuOrder)
                .ToListAsync(),
            Posts = await _context.Posts
                .Include(p => p.Menu)
                .Where(p => p.IsActive && p.MenuID == id)
                .OrderByDescending(p => p.CreatedDate)
                .ToListAsync()
        };

        return View(viewModel);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
