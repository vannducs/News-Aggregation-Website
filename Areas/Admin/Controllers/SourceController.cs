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

    // GET: /Admin/Source
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Quản lý nguồn báo";

        var sources = await _db.Sources
            .OrderBy(s => s.SourceName)
            .ToListAsync();

        return View(sources);
    }

    // GET: /Admin/Source/Create
    public IActionResult Create()
    {
        ViewData["Title"] = "Thêm nguồn báo";
        return View();
    }

    // POST: /Admin/Source/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Source source)
    {
        if (ModelState.IsValid)
        {
            // Kiểm tra tên nguồn đã tồn tại chưa
            bool exists = await _db.Sources
                .AnyAsync(s => s.SourceName == source.SourceName);

            if (exists)
            {
                ModelState.AddModelError("SourceName",
                    "Tên nguồn báo này đã tồn tại!");
                return View(source);
            }

            // Lưu vào DB
            source.IsActive = true;
            _db.Sources.Add(source);
            await _db.SaveChangesAsync();

            // Trigger crawl ngay sau khi thêm thành công
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

        _db.Sources.Remove(source);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Đã xóa nguồn báo!";
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

        // Bật IsActive nếu đang tắt
        if (!source.IsActive)
        {
            source.IsActive = true;
            _db.Sources.Update(source);
            await _db.SaveChangesAsync();
        }

        // Trigger crawl chỉ nguồn này
        Hangfire.BackgroundJob.Enqueue<Services.CrawlerService>(
            service => service.CrawlSourceByIdAsync(id));

        TempData["Success"] = 
            $"Đã kích hoạt crawl cho '{source.SourceName}'! Kiểm tra tại /hangfire";
        return RedirectToAction(nameof(Index));
    }
}