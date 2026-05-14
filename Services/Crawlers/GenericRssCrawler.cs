using System.Xml.Linq;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using NewsAggregator.Data;
using NewsAggregator.Models;

namespace NewsAggregator.Services.Crawlers
{
    /// <summary>
    /// Crawler tổng quát cho bất kỳ nguồn RSS nào.
    /// Tự động map category từ RSS sang Menu trong database.
    /// </summary>
    public class GenericRssCrawler : BaseCrawler, INewsCrawler
    {
        private readonly Source _source;
        private Dictionary<string, int>? _menuCache;

        public GenericRssCrawler(AppDbContext db, HttpClient http, Source source)
            : base(db, http)
        {
            _source = source;
        }

        public async Task<int> CrawlAsync()
        {
            _menuCache = await _db.Menus
                .Where(m => m.IsActive && !m.IsDeleted)
                .ToDictionaryAsync(
                    m => m.MenuName.ToLowerInvariant().Trim(),
                    m => m.MenuID);

            int totalSaved = 0;

            try
            {
                var xml = await _http.GetStringAsync(_source.RssUrl);
                var doc = XDocument.Parse(xml);
                var ns = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;

                var items = doc.Descendants("item").ToList();
                Console.WriteLine($"[{_source.SourceName}] Tìm thấy {items.Count} items từ RSS");

                foreach (var item in items)
                {
                    var title      = item.Element("title")?.Value?.Trim();
                    var link       = item.Element("link")?.Value?.Trim()
                                  ?? item.Elements().FirstOrDefault(e => e.Name.LocalName == "link")?.Value?.Trim();
                    var pubDateStr = item.Element("pubDate")?.Value;
                    var description = item.Element("description")?.Value?.Trim();

                    // Thử lấy category từ RSS item
                    var rssCategory = item.Element("category")?.Value?.Trim()
                                   ?? item.Elements().FirstOrDefault(e => e.Name.LocalName == "category")?.Value?.Trim();

                    if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(link)) continue;

                    bool exists = await _db.Posts.AnyAsync(p => p.Link == link);
                    if (exists) continue;

                    var menuId = ResolveMenuId(rssCategory, title);
                    var publishedAt = ParseDate(pubDateStr);
                    var imageUrl = ExtractImageFromDescription(description ?? "");
                    var cleanAbstract = StripHtml(description ?? "");

                    var baseUrl = ContentHelper.ExtractBaseUrl(_source.WebsiteUrl ?? _source.RssUrl);
                    var post = new Post
                    {
                        Title       = title,
                        Abstract    = cleanAbstract.Length > 500 ? cleanAbstract[..500] : cleanAbstract,
                        Contents    = FixContentImages(description ?? "", baseUrl),
                        Link        = link,
                        Images      = imageUrl,
                        Author      = _source.SourceName,
                        CreatedDate = publishedAt,
                        CrawledAt   = DateTime.Now,
                        MenuID      = menuId,
                        SourceID    = _source.SourceID,
                        IsActive    = true
                    };

                    _db.Posts.Add(post);
                    totalSaved++;
                }

                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{_source.SourceName}] Lỗi RSS: {ex.Message}");
            }

            return totalSaved;
        }

        // Map RSS category → MenuID. Fallback về menu đầu tiên (Trang chủ) nếu không khớp.
        private int ResolveMenuId(string? rssCategory, string title)
        {
            if (!string.IsNullOrEmpty(rssCategory))
            {
                var normalized = rssCategory.ToLowerInvariant().Trim();

                // Tìm khớp chính xác
                if (_menuCache!.TryGetValue(normalized, out var id))
                    return id;

                // Tìm khớp một phần (RSS category chứa tên menu hoặc ngược lại)
                foreach (var kv in _menuCache)
                {
                    if (normalized.Contains(kv.Key) || kv.Key.Contains(normalized))
                        return kv.Value;
                }
            }

            // Fallback: đoán từ tiêu đề bài viết theo từ khóa
            var titleLower = title.ToLowerInvariant();
            var keywordMap = new Dictionary<string, string[]>
            {
                ["the thao"]   = ["bóng đá", "thể thao", "cầu thủ", "giải đấu", "vận động"],
                ["kinh doanh"] = ["kinh tế", "doanh nghiệp", "chứng khoán", "ngân hàng", "tài chính"],
                ["cong nghe"]  = ["công nghệ", "điện thoại", "máy tính", "ai", "phần mềm"],
                ["giai tri"]   = ["giải trí", "phim", "ca sĩ", "nghệ sĩ", "âm nhạc"],
                ["suc khoe"]   = ["sức khỏe", "bệnh viện", "y tế", "thuốc", "bác sĩ"],
                ["thoi su"]    = ["thời sự", "chính phủ", "quốc hội", "chính sách"],
                ["the gioi"]   = ["quốc tế", "thế giới", "nước ngoài", "mỹ", "trung quốc"],
            };

            foreach (var kv in keywordMap)
            {
                if (kv.Value.Any(kw => titleLower.Contains(kw)))
                {
                    // Tìm menu khớp với key
                    foreach (var menuKv in _menuCache!)
                    {
                        if (menuKv.Key.Contains(kv.Key) || kv.Key.Contains(menuKv.Key))
                            return menuKv.Value;
                    }
                }
            }

            // Trả về menu đầu tiên (Trang chủ) làm fallback cuối
            return _menuCache!.Values.First();
        }
    }
}
