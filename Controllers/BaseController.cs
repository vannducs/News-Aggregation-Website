using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using NewsAggregator.Data;

namespace NewsAggregator.Controllers
{
    public class BaseController(AppDbContext db) : Controller
    {
        protected readonly AppDbContext _db = db;
        public override async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
        {
            ViewBag.Menus = await _db.Menus
                .AsNoTracking()
                .Where(m => m.IsActive && m.Levels == 1)
                .OrderBy(m => m.MenuOrder)
                .ToListAsync();

            ViewBag.Sources = await _db.Sources
                .AsNoTracking()
                .Where(s => s.IsActive && !s.IsDeleted)
                .OrderBy(s => s.SourceName)
                .ToListAsync();

            await next();
        }
    }
}