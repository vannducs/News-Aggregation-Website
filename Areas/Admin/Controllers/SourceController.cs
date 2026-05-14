using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewsAggregator.Data;
using NewsAggregator.Models;

namespace NewsAggregator.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = UserRoles.StaffRoles)]
public class SourceController : Controller
{
    private readonly AppDbContext _db;

    public SourceController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Quản lý nguồn báo";

        var sources = await _db.Sources
            .Where(s => !s.IsDeleted)
            .OrderBy(s => s.SourceName)
            .ToListAsync();

        return View(sources);
    }

    public IActionResult Create()
    {
        ViewData["Title"] = "Thêm nguồn báo";
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Source source)
    {
        if (ModelState.IsValid)
        {
            bool exists = await _db.Sources
                .AnyAsync(s => s.SourceName == source.SourceName);

            if (exists)
            {
                ModelState.AddModelError("SourceName",
                    "Tên nguồn báo này đã tồn tại!");
                return View(source);
            }

            source.IsActive = true;
            _db.Sources.Add(source);
            await _db.SaveChangesAsync();

            Hangfire.BackgroundJob.Enqueue<Services.CrawlerService>(
                service => service.RunAllAsync());

            TempData["Success"] = 
                $"Thêm nguồn '{source.SourceName}' thành công! Đang crawl bài viết...";
            return RedirectToAction(nameof(Index));
        }
        return View(source);
    }


    // GET: /Admin/Source/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        ViewData["Title"] = "Sửa nguồn báo";

        var source = await _db.Sources.FindAsync(id);
        if (source == null) return NotFound();

        return View(source);
    }

    // POST: /Admin/Source/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Source source)
    {
        if (id != source.SourceID) return NotFound();

        if (ModelState.IsValid)
        {
            _db.Sources.Update(source);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Cập nhật nguồn báo thành công!";
            return RedirectToAction(nameof(Index));
        }
        return View(source);
    }

    // POST: /Admin/Source/ToggleActive/5
    // Bật/tắt trạng thái crawl
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var source = await _db.Sources.FindAsync(id);
        if (source == null) return NotFound();

        source.IsActive = !source.IsActive;
        _db.Sources.Update(source);
        await _db.SaveChangesAsync();

        TempData["Success"] = source.IsActive
            ? $"Đã bật crawl cho {source.SourceName}"
            : $"Đã tắt crawl cho {source.SourceName}";

        return RedirectToAction(nameof(Index));
    }

    // POST: /Admin/Source/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var source = await _db.Sources.FindAsync(id);
        if (source == null) return NotFound();

        source.IsDeleted = true;
        source.DeletedAt = DateTime.Now;
        source.IsActive = false;
        _db.Sources.Update(source);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Đã chuyển '{source.SourceName}' vào thùng rác!";
        return RedirectToAction(nameof(Index));
    }

    // POST: /Admin/Source/CrawlNow/5
    // Trigger crawl ngay lập tức cho 1 nguồn
    // POST: /Admin/Source/CrawlNow/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrawlNow(int id)
    {
        var source = await _db.Sources.FindAsync(id);
        if (source == null) return NotFound();

        if (!source.IsActive)
        {
            source.IsActive = true;
            _db.Sources.Update(source);
            await _db.SaveChangesAsync();
        }

        Hangfire.BackgroundJob.Enqueue<Services.CrawlerService>(
            service => service.CrawlSourceByIdAsync(id));

        TempData["Success"] = 
            $"Đã kích hoạt crawl cho '{source.SourceName}'! Kiểm tra tại /hangfire";
        return RedirectToAction(nameof(Index));
    }
}