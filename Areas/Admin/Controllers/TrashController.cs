using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewsAggregator.Data;
using NewsAggregator.Models;

namespace NewsAggregator.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = UserRoles.AdminOrEditor)]
public class TrashController : Controller
{
    private readonly AppDbContext _db;

    public TrashController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Thùng rác";

        ViewBag.DeletedPosts = await _db.Posts
            .Where(p => p.IsDeleted)
            .Include(p => p.Menu)
            .Include(p => p.Source)
            .OrderByDescending(p => p.DeletedAt)
            .ToListAsync();

        ViewBag.DeletedMenus = await _db.Menus
            .Where(m => m.IsDeleted)
            .OrderByDescending(m => m.DeletedAt)
            .ToListAsync();

        ViewBag.DeletedSources = await _db.Sources
            .Where(s => s.IsDeleted)
            .OrderByDescending(s => s.DeletedAt)
            .ToListAsync();

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RestorePost(int id)
    {
        var post = await _db.Posts.FindAsync(id);
        if (post == null) return NotFound();

        post.IsDeleted = false;
        post.DeletedAt = null;
        post.IsActive = true;
        await _db.SaveChangesAsync();

        TempData["Success"] = "Đã khôi phục bài viết!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PermanentDeletePost(int id)
    {
        var post = await _db.Posts.FindAsync(id);
        if (post == null) return NotFound();

        _db.Posts.Remove(post);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Đã xóa vĩnh viễn bài viết!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RestoreMenu(int id)
    {
        var menu = await _db.Menus.FindAsync(id);
        if (menu == null) return NotFound();

        menu.IsDeleted = false;
        menu.DeletedAt = null;
        await _db.SaveChangesAsync();

        TempData["Success"] = "Đã khôi phục danh mục!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PermanentDeleteMenu(int id)
    {
        var menu = await _db.Menus.FindAsync(id);
        if (menu == null) return NotFound();

        _db.Menus.Remove(menu);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Đã xóa vĩnh viễn danh mục!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RestoreSource(int id)
    {
        var source = await _db.Sources.FindAsync(id);
        if (source == null) return NotFound();

        source.IsDeleted = false;
        source.DeletedAt = null;
        source.IsActive = true;
        await _db.SaveChangesAsync();

        TempData["Success"] = "Đã khôi phục nguồn báo!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PermanentDeleteSource(int id)
    {
        var source = await _db.Sources.FindAsync(id);
        if (source == null) return NotFound();

        _db.Sources.Remove(source);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Đã xóa vĩnh viễn nguồn báo!";
        return RedirectToAction(nameof(Index));
    }
}
