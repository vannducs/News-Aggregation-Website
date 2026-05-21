using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewsAggregator.Data;
using NewsAggregator.Models;

namespace NewsAggregator.ViewComponents;

public class SourceNavViewComponent(AppDbContext db) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var sources = await db.Sources
            .AsNoTracking()
            .Where(s => s.IsActive && !s.IsDeleted)
            .OrderBy(s => s.SourceName)
            .ToListAsync();

        return View(sources);
    }
}
