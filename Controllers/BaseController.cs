using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using NewsAggregator.Data;

namespace NewsAggregator.Controllers
{
    public class BaseController : Controller
    {
        protected readonly AppDbContext _db;
        public BaseController(AppDbContext db)
        {
            _db = db;
        }
        public override async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
        {
            var menus = await _db.Menus
                .Where(m=>m.IsActive && m.Levels == 1)
                .OrderBy(m=>m.MenuOrder)
                .ToListAsync();

            ViewBag.Menus = menus;
            await next();
        }
    }
}