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
            _source    = source;
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
                var feedItems = await _extractor.GetRssFeedItemsAsync(_source.RssUrl);
                Console.WriteLine($"[{_source.SourceName}] RSS: {feedItems.Count} items");

                if (feedItems.Count == 0)
                {
                    Console.WriteLine($"[{_source.SourceName}] Không có item nào từ RSS, bỏ qua");
                    return 0;
                }

                var baseUrl = ContentHelper.ExtractBaseUrl(_source.WebsiteUrl ?? _source.RssUrl);

                foreach (var feedItem in feedItems)
                {
                    if (string.IsNullOrEmpty(feedItem.Url)) continue;

                    bool exists = await _db.Posts.AnyAsync(p => p.Link == feedItem.Url);
                    if (exists) continue;

                    var menuId = ResolveMenuId(feedItem.Category, feedItem.Title);

                    string  contents = FixContentImages(feedItem.Summary ?? "", baseUrl);
                    string? imageUrl = feedItem.ImageUrl;
                    string? author   = feedItem.Author;

                    try
                    {
                        var article = await _extractor.ExtractArticleAsync(feedItem.Url);

                        if (article.IsSuccess)
                        {
                            if (!string.IsNullOrWhiteSpace(article.Content))
                                contents = FixContentImages(article.Content, baseUrl);

                            if (string.IsNullOrEmpty(imageUrl) && !string.IsNullOrEmpty(article.ImageUrl))
                                imageUrl = article.ImageUrl;

                            if (string.IsNullOrEmpty(author) && !string.IsNullOrEmpty(article.Author))
                                author = article.Author;
                        }
                        else if (!string.IsNullOrEmpty(article.ErrorMessage))
                        {
                            Console.WriteLine($"  [{_source.SourceName}] Không extract được: {feedItem.Url} — {article.ErrorMessage}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  [{_source.SourceName}] Lỗi crawl article: {ex.Message}");
                    }

                    var summary = feedItem.Summary ?? "";
                    var abstract_ = summary.Length > 500 ? summary[..500] : summary;

                    var post = new Post
                    {
                        Title       = feedItem.Title,
                        Abstract    = abstract_,
                        Contents    = contents,
                        Link        = feedItem.Url,
                        Images      = imageUrl ?? "",
                        Author      = author ?? _source.SourceName,
                        CreatedDate = feedItem.PublishedDate ?? DateTime.Now,
                        CrawledAt   = DateTime.Now,
                        MenuID      = menuId,
                        SourceID    = _source.SourceID,
                        IsActive    = true
                    };

                    _db.Posts.Add(post);
                    totalSaved++;

                    Console.WriteLine($"  [{_source.SourceName}] +{totalSaved}: {feedItem.Title[..Math.Min(60, feedItem.Title.Length)]}");
                }

                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{_source.SourceName}] Lỗi RSS crawler: {ex.Message}");
            }

            Console.WriteLine($"[{_source.SourceName}] Đã lưu {totalSaved} bài mới");
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
                ["the thao"]   = ["bóng đá", "thể thao", "cầu thủ", "giải đấu", "vận động viên"],
                ["kinh doanh"] = ["kinh tế", "doanh nghiệp", "chứng khoán", "ngân hàng", "tài chính", "thị trường"],
                ["cong nghe"]  = ["công nghệ", "điện thoại", "máy tính", "trí tuệ nhân tạo", "phần mềm"],
                ["giai tri"]   = ["giải trí", "phim", "ca sĩ", "nghệ sĩ", "âm nhạc", "showbiz"],
                ["suc khoe"]   = ["sức khỏe", "bệnh viện", "y tế", "thuốc", "bác sĩ", "bệnh nhân"],
                ["thoi su"]    = ["thời sự", "chính phủ", "quốc hội", "chính sách", "hành chính"],
                ["the gioi"]   = ["quốc tế", "thế giới", "nước ngoài", "mỹ", "trung quốc", "châu âu"],
                ["du lich"]    = ["du lịch", "điểm đến", "khách sạn", "tour", "resort"],
                ["giao duc"]   = ["giáo dục", "học sinh", "sinh viên", "trường", "tuyển sinh"],
                ["phap luat"]  = ["pháp luật", "tòa án", "công an", "vụ án", "điều tra"],
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
