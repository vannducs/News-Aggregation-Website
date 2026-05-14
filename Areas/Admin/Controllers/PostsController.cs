using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NewsAggregator.Data;
using NewsAggregator.Models;
using NewsAggregator.Services;

namespace NewsAggregator.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = UserRoles.AdminOrEditor)]
public class PostsController : Controller
{
    private readonly AppDbContext _db;

    public PostsController(AppDbContext db)
    {
        _db = db;
    }

    // GET: /Admin/Posts
    public async Task<IActionResult> Index(string? search, int? menuId, int? sourceId, string? status, int page = 1)
    {
        ViewData["Title"] = "Quản lý bài viết";

        var query = _db.Posts
            .Where(p => !p.IsDeleted)
            .Include(p => p.Menu)
            .Include(p => p.Source)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Title.Contains(search) || (p.Author != null && p.Author.Contains(search)));

        if (menuId.HasValue)
            query = query.Where(p => p.MenuID == menuId);

        if (sourceId.HasValue)
            query = query.Where(p => p.SourceID == sourceId);

        if (status == "active")
            query = query.Where(p => p.IsActive);
        else if (status == "hidden")
            query = query.Where(p => !p.IsActive);

        const int pageSize = 20;
        var total = await query.CountAsync();
        var posts = await query
            .OrderByDescending(p => p.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Search = search;
        ViewBag.MenuId = menuId;
        ViewBag.SourceId = sourceId;
        ViewBag.Status = status;
        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
        ViewBag.Total = total;
        ViewBag.Menus = new SelectList(
            await _db.Menus.Where(m => !m.IsDeleted).OrderBy(m => m.MenuName).ToListAsync(),
            "MenuID", "MenuName", menuId);
        ViewBag.Sources = new SelectList(
            await _db.Sources.Where(s => !s.IsDeleted).OrderBy(s => s.SourceName).ToListAsync(),
            "SourceID", "SourceName", sourceId);

        return View(posts);
    }

    // POST: /Admin/Posts/ToggleActive/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var post = await _db.Posts.FindAsync(id);
        if (post == null) return NotFound();

        post.IsActive = !post.IsActive;
        _db.Posts.Update(post);
        await _db.SaveChangesAsync();

        TempData["Success"] = post.IsActive
            ? $"Đã hiện bài viết!"
            : $"Đã ẩn bài viết!";
        return RedirectToAction(nameof(Index));
    }

    // POST: /Admin/Posts/Delete/5  — soft delete
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var post = await _db.Posts.FindAsync(id);
        if (post == null) return NotFound();

        post.IsDeleted = true;
        post.DeletedAt = DateTime.Now;
        post.IsActive = false;
        _db.Posts.Update(post);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Đã chuyển bài viết vào thùng rác!";
        return RedirectToAction(nameof(Index));
    }

    // POST: /Admin/Posts/FixAllImages — chạy background job sửa ảnh toàn bộ bài cũ
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = UserRoles.Admin)]
    public IActionResult FixAllImages()
    {
        BackgroundJob.Enqueue<PostImageFixService>(s => s.RunAsync());
        TempData["Success"] = "Đã đưa job sửa ảnh vào hàng đợi. Quá trình chạy ngầm, có thể mất vài phút.";
        return RedirectToAction(nameof(Index));
    }
}
