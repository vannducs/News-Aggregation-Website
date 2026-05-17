using Microsoft.EntityFrameworkCore;
using NewsAggregator.Data;
using NewsAggregator.Models;

namespace NewsAggregator.Services.Crawlers
{
    public class GenericRssCrawler : BaseCrawler, INewsCrawler
    {
        private readonly Source _source;
        private readonly IUniversalArticleExtractorService _extractor;
        private Dictionary<string, int>? _menuCache;

        public GenericRssCrawler(AppDbContext db, HttpClient http, Source source, IUniversalArticleExtractorService extractor)
            : base(db, http)
        {
            _source = source;
            _extractor = extractor;
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
                // Dùng extractor: xử lý gzip, namespace media/dc/content tự động
                var feedItems = await _extractor.GetRssFeedItemsAsync(_source.RssUrl);
                Console.WriteLine($"[{_source.SourceName}] Tìm thấy {feedItems.Count} items từ RSS");

                foreach (var feedItem in feedItems)
                {
                    if (string.IsNullOrEmpty(feedItem.Url)) continue;

                    bool exists = await _db.Posts.AnyAsync(p => p.Link == feedItem.Url);
                    if (exists) continue;

                    var menuId   = ResolveMenuId(feedItem.Category, feedItem.Title);
                    var baseUrl  = ContentHelper.ExtractBaseUrl(_source.WebsiteUrl ?? _source.RssUrl);
                    var abstract_ = feedItem.Summary ?? "";

                    var post = new Post
                    {
                        Title       = feedItem.Title,
                        Abstract    = abstract_.Length > 500 ? abstract_[..500] : abstract_,
                        Contents    = FixContentImages(feedItem.Summary ?? "", baseUrl),
                        Link        = feedItem.Url,
                        Images      = feedItem.ImageUrl ?? "",
                        Author      = feedItem.Author ?? _source.SourceName,
                        CreatedDate = feedItem.PublishedDate ?? DateTime.Now,
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

        private int ResolveMenuId(string? rssCategory, string title)
        {
            if (!string.IsNullOrEmpty(rssCategory))
            {
                var normalized = rssCategory.ToLowerInvariant().Trim();

                if (_menuCache!.TryGetValue(normalized, out var id))
                    return id;

                foreach (var kv in _menuCache)
                {
                    if (normalized.Contains(kv.Key) || kv.Key.Contains(normalized))
                        return kv.Value;
                }
            }

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
                    foreach (var menuKv in _menuCache!)
                    {
                        if (menuKv.Key.Contains(kv.Key) || kv.Key.Contains(menuKv.Key))
                            return menuKv.Value;
                    }
                }
            }

            return _menuCache!.Values.First();
        }
    }
}
