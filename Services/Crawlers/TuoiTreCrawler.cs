using System.Xml.Linq;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using NewsAggregator.Data;
using NewsAggregator.Models;
using NewsAggregator.Services.Crawlers;

namespace NewsAggregator.Services.Crawlers
{
    public class TuoiTreCrawler : BaseCrawler, INewsCrawler
    {
        public TuoiTreCrawler(AppDbContext db, HttpClient http)
            : base(db, http)
        {
        }

        private readonly Dictionary<string, int> _rssFeeds = new()
        {
            { "https://tuoitre.vn/rss/thoi-su.rss",    2  },
            { "https://tuoitre.vn/rss/the-gioi.rss",   3  },
            { "https://tuoitre.vn/rss/the-thao.rss",   4  },
            { "https://tuoitre.vn/rss/kinh-doanh.rss", 5  },
            { "https://tuoitre.vn/rss/cong-nghe.rss",  6  },
            { "https://tuoitre.vn/rss/suc-khoe.rss",   7  },
            { "https://tuoitre.vn/rss/giai-tri.rss",   8  },
            { "https://tuoitre.vn/rss/phap-luat.rss",  9  },
            { "https://tuoitre.vn/rss/giao-duc.rss",   10 },
            { "https://tuoitre.vn/rss/du-lich.rss",    11 },
            { "https://tuoitre.vn/rss/xe.rss",         12 },
        };

        public async Task<int> CrawlAsync()
        {
            int totalSaved = 0;

            var source = await _db.Sources
                .FirstOrDefaultAsync(s => s.SourceName.Contains("Tuổi Trẻ"));

            if (source == null) return 0;

            foreach (var (rssUrl, menuId) in _rssFeeds)
            {
                try
                {
                    var xml   = await _http.GetStringAsync(rssUrl);
                    var doc   = XDocument.Parse(xml);
                    var items = doc.Descendants("item").ToList();

                    foreach (var item in items)
                    {
                        var title       = item.Element("title")?.Value?.Trim();
                        var link        = item.Element("link")?.Value?.Trim();
                        var pubDateStr  = item.Element("pubDate")?.Value;
                        var description = item.Element("description")?.Value?.Trim();

                        if (string.IsNullOrEmpty(title) ||
                            string.IsNullOrEmpty(link)) continue;

                        bool exists = await _db.Posts
                            .AnyAsync(p => p.Link == link);
                        if (exists) continue;

                        var publishedAt = ParseDate(pubDateStr);
                        var imageUrl    = ExtractImageFromDescription(description ?? "");
                        var (content, author) = await ParseFullContentAsync(link);

                        var post = new Post
                        {
                            Title       = title,
                            Abstract    = StripHtml(description ?? ""),
                            Contents    = content,
                            Link        = link,
                            Images      = imageUrl,
                            Author      = author,
                            CreatedDate = publishedAt,
                            CrawledAt   = DateTime.Now,
                            MenuID      = menuId,
                            SourceID    = source.SourceID,
                            IsActive    = true
                        };

                        _db.Posts.Add(post);
                        totalSaved++;

                        await Task.Delay(1000);
                    }

                    await _db.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[TuoiTre] Lỗi {rssUrl}: {ex.Message}");
                }
            }

            return totalSaved;
        }

        private async Task<(string content, string author)>
            ParseFullContentAsync(string url)
        {
            try
            {
                var html    = await _http.GetStringAsync(url);
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(html);

                var contentNode = htmlDoc.DocumentNode
                    .SelectSingleNode("//div[@data-role='content']");

                var authorNode = htmlDoc.DocumentNode
                    .SelectSingleNode("//a[@class='name']");

                var content = contentNode?.InnerHtml ?? "";
                var author  = authorNode?.InnerText?.Trim() ?? "Tuổi Trẻ";

                return (content, author);
            }
            catch
            {
                return ("", "Tuổi Trẻ");
            }
        }
    }
}