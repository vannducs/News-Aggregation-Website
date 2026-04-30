using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewsAggregator.Data;
using NewsAggregator.Models;
using NewsAggregator.Models.ViewModels;

namespace NewsAggregator.Controllers
{
    public class SearchController : Controller
    {
        private readonly AppDbContext _context;
        private const int PageSize = 8;

        public SearchController(AppDbContext context) => _context = context;

        // GET /Search/Index
        [HttpGet]
        public async Task<IActionResult> Index(
            string keyword   = "",
            int?   categoryID = null,
            string sortBy    = "newest",
            string dateRange = "",
            string dateFrom  = "",
            string dateTo    = "",
            int    page      = 1)
        {
            keyword = keyword?.Trim() ?? string.Empty;

            var categories = await _context.Menus
                .Where(m => m.IsActive).OrderBy(m => m.MenuOrder).ToListAsync();

            var query = _context.Posts.Include(p => p.Menu).Where(p => p.IsActive);

            // ── Fuzzy Search ─────────────────────────────────────────────────────
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var tokens = keyword.ToLower()
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries);

                // Mỗi token phải xuất hiện trong ít nhất 1 field (OR giữa fields, AND giữa tokens)
                foreach (var token in tokens)
                {
                    var t = token;
                    query = query.Where(p =>
                        p.Title.ToLower().Contains(t) ||
                        (p.Abstract != null && p.Abstract.ToLower().Contains(t)) ||
                        (p.Author   != null && p.Author.ToLower().Contains(t)));
                }
            }

            if (categoryID.HasValue && categoryID.Value > 0)
                query = query.Where(p => p.MenuID == categoryID.Value);

            var now = DateTime.Now;
            if (dateRange == "custom"
                && DateTime.TryParse(dateFrom, out var fd)
                && DateTime.TryParse(dateTo,   out var td))
            {
                td    = td.AddDays(1).AddTicks(-1);
                query = query.Where(p => p.CreatedDate >= fd && p.CreatedDate <= td);
            }
            else
            {
                query = dateRange switch
                {
                    "today" => query.Where(p => p.CreatedDate >= now.Date),
                    "week"  => query.Where(p => p.CreatedDate >= now.AddDays(-7)),
                    "month" => query.Where(p => p.CreatedDate >= now.AddMonths(-1)),
                    _       => query
                };
            }

            query = sortBy switch
            {
                "oldest"  => query.OrderBy(p => p.CreatedDate),
                "popular" => query.OrderByDescending(p => p.ViewCount),
                "title"   => query.OrderBy(p => p.Title),
                _         => query.OrderByDescending(p => p.CreatedDate)
            };

            int totalResults = await query.CountAsync();
            int totalPages   = (int)Math.Ceiling(totalResults / (double)PageSize);
            page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

            var results = await query.Skip((page - 1) * PageSize).Take(PageSize).ToListAsync();

            var postIds       = results.Select(p => p.PostID).ToList();
            var commentCounts = await _context.Comments
                .Where(c => postIds.Contains(c.PostID) && c.IsApproved)
                .GroupBy(c => c.PostID)
                .Select(g => new { PostID = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.PostID, x => x.Count);

            var vm = new SearchViewModel
            {
                Keyword       = keyword,
                CategoryID    = categoryID,
                SortBy        = sortBy,
                DateRange     = dateRange,
                DateFrom      = dateFrom,
                DateTo        = dateTo,
                Results       = results,
                Categories    = categories,
                CurrentPage   = page,
                TotalPages    = totalPages,
                TotalResults  = totalResults,
                PageSize      = PageSize,
                CommentCounts = commentCounts
            };

            return View(vm);
        }

        // GET /Search/LiveSearch?keyword=...  (AJAX - live dropdown)
        [HttpGet]
        public async Task<IActionResult> LiveSearch(string keyword = "")
        {
            keyword = keyword?.Trim() ?? string.Empty;
            if (keyword.Length < 2)
                return Json(new { suggestions = Array.Empty<object>(), totalCount = 0 });

            var tokens = keyword.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var query = _context.Posts.Include(p => p.Menu).Where(p => p.IsActive);
            foreach (var t in tokens)
            {
                var tok = t;
                query = query.Where(p =>
                    p.Title.ToLower().Contains(tok) ||
                    (p.Abstract != null && p.Abstract.ToLower().Contains(tok)));
            }

            int totalCount = await query.CountAsync();

            var suggestions = await query
                .OrderByDescending(p => p.ViewCount)
                .ThenByDescending(p => p.CreatedDate)
                .Take(8)
                .Select(p => new
                {
                    id       = p.PostID,
                    title    = p.Title,
                    category = p.Menu != null ? p.Menu.MenuName : "Tin tức",
                    image    = p.Images,
                    date     = p.CreatedDate.ToString("dd/MM/yyyy"),
                    views    = p.ViewCount
                })
                .ToListAsync();

            return Json(new { suggestions, totalCount });
        }

        // GET /Search/Suggestions (legacy, tương thích ngược)
        [HttpGet]
        public async Task<IActionResult> Suggestions(string keyword = "")
        {
            keyword = keyword?.Trim() ?? string.Empty;
            if (keyword.Length < 2) return Json(new List<object>());

            var kw = keyword.ToLower();
            var list = await _context.Posts
                .Where(p => p.IsActive && p.Title.ToLower().Contains(kw))
                .OrderByDescending(p => p.ViewCount).Take(6)
                .Select(p => new { id = p.PostID, title = p.Title,
                    category = p.Menu != null ? p.Menu.MenuName : "Tin tức", image = p.Images })
                .ToListAsync();

            return Json(list);
        }
    }
}
