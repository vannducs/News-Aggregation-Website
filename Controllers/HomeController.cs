using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using NewsAggregator.Data;
using NewsAggregator.Models;
using NewsAggregator.Models.ViewModels;

namespace NewsAggregator.Controllers;

public class HomeController : BaseController
{
    public HomeController(AppDbContext context) : base(context)
    {
    }

    // Redirect về NewsController vì trang chủ đã xử lý ở đó
    public IActionResult Index()
    {
        return RedirectToAction("Index", "News");
    }

    // Category của Bao — dùng ViewModel
    [Route("Home/Category/{id:int}")]
    public async Task<IActionResult> Category(int id)
    {
        var currentMenu = await _db.Menus
            .FirstOrDefaultAsync(m => m.MenuID == id && m.IsActive);

        if (currentMenu is null) return NotFound();

        var viewModel = new CategoryViewModel
        {
            CurrentMenu = currentMenu,
            Menus = await _db.Menus
                .Where(m => m.IsActive && m.Position == 1)
                .OrderBy(m => m.MenuOrder)
                .ToListAsync(),
            Posts = await _db.Posts
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
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }
}