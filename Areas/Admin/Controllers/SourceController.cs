using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewsAggregator.Data;
using NewsAggregator.Models;
using NewsAggregator.Models.ViewModels;
using NewsAggregator.Services;

namespace NewsAggregator.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = UserRoles.StaffRoles)]
public class SourceController(AppDbContext db, IUniversalArticleExtractorService extractor) : Controller
{
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Quản lý nguồn báo";

        var sources = await db.Sources
            .AsNoTracking()
            .Where(s => !s.IsDeleted)
            .OrderBy(s => s.SourceName)
            .Select(s => new SourceListItem
            {
                SourceID        = s.SourceID,
                SourceName      = s.SourceName,
                RssUrl          = s.RssUrl,
                WebsiteUrl      = s.WebsiteUrl,
                LogoUrl         = s.LogoUrl,
                IsActive        = s.IsActive,
                PostCount       = s.Posts.Count(p => !p.IsDeleted && p.IsActive),
                LastCrawledAt   = s.CrawlLogs
                                    .OrderByDescending(l => l.CrawlTime)
                                    .Select(l => (DateTime?)l.CrawlTime)
                                    .FirstOrDefault(),
                LastCrawlStatus = s.CrawlLogs
                                    .OrderByDescending(l => l.CrawlTime)
                                    .Select(l => l.Status)
                                    .FirstOrDefault(),
            })
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
            bool exists = await db.Sources
                .AnyAsync(s => s.SourceName == source.SourceName);

            if (exists)
            {
                ModelState.AddModelError("SourceName",
                    "Tên nguồn báo này đã tồn tại!");
                return View(source);
            }

            source.IsActive = true;
            db.Sources.Add(source);
            await db.SaveChangesAsync();

            Hangfire.BackgroundJob.Enqueue<CrawlerService>(
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

        var source = await db.Sources.FindAsync(id);
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
            db.Sources.Update(source);
            await db.SaveChangesAsync();

            TempData["Success"] = "Cập nhật nguồn báo thành công!";
            return RedirectToAction(nameof(Index));
        }
        return View(source);
    }

    // POST: /Admin/Source/ToggleActive/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var source = await db.Sources.FindAsync(id);
        if (source == null) return NotFound();

        source.IsActive = !source.IsActive;
        db.Sources.Update(source);
        await db.SaveChangesAsync();

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
        var source = await db.Sources.FindAsync(id);
        if (source == null) return NotFound();

        source.IsDeleted = true;
        source.DeletedAt = DateTime.Now;
        source.IsActive = false;
        db.Sources.Update(source);
        await db.SaveChangesAsync();

        TempData["Success"] = $"Đã chuyển '{source.SourceName}' vào thùng rác!";
        return RedirectToAction(nameof(Index));
    }

    // POST: /Admin/Source/CrawlNow/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrawlNow(int id)
    {
        var source = await db.Sources.FindAsync(id);
        if (source == null) return NotFound();

        if (!source.IsActive)
        {
            source.IsActive = true;
            db.Sources.Update(source);
            await db.SaveChangesAsync();
        }

        Hangfire.BackgroundJob.Enqueue<CrawlerService>(
            service => service.CrawlSourceByIdAsync(id));

        TempData["Success"] =
            $"Đã kích hoạt crawl cho '{source.SourceName}'! Kiểm tra tại /hangfire";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Admin/Source/TestRss/5
    // Chạy trực tiếp (không qua Hangfire) để test RSS — hữu ích khi debug nguồn mới
    public async Task<IActionResult> TestRss(int id)
    {
        var source = await db.Sources.FindAsync(id);
        if (source == null) return NotFound();

        var items = await extractor.GetRssFeedItemsAsync(source.RssUrl);

        ViewData["Title"] = $"Test RSS — {source.SourceName}";
        ViewData["SourceName"] = source.SourceName;
        ViewData["RssUrl"] = source.RssUrl;
        return View(items);
    }
}
