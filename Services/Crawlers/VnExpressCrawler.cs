using System.Xml.Linq;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using NewsAggregator.Data;
using NewsAggregator.Models;

namespace NewsAggregator.Services.Crawlers
{
    public class VnExpressCrawler : BaseCrawler, INewsCrawler
    {
        public VnExpressCrawler(AppDbContext db, HttpClient http)
            : base(db, http)
        {
        }

        private readonly Dictionary<string, int> _rssFeeds = new()
        {
            { "https://vnexpress.net/rss/thoi-su.rss",    2  },
            { "https://vnexpress.net/rss/the-gioi.rss",   3  },
            { "https://vnexpress.net/rss/the-thao.rss",   4  },
            { "https://vnexpress.net/rss/kinh-doanh.rss", 5  },
            { "https://vnexpress.net/rss/so-hoa.rss",     6  },
            { "https://vnexpress.net/rss/suc-khoe.rss",   7  },
            { "https://vnexpress.net/rss/giai-tri.rss",   8  },
            { "https://vnexpress.net/rss/phap-luat.rss",  9  },
            { "https://vnexpress.net/rss/giao-duc.rss",   10 },
            { "https://vnexpress.net/rss/du-lich.rss",    11 },
            { "https://vnexpress.net/rss/oto-xe-may.rss", 12 },
        };

        public async Task<int> CrawlAsync()
        {
            int totalSaved = 0;

            var source = await _db.Sources
                .FirstOrDefaultAsync(s => s.SourceName.Contains("VnExpress"));

            if (source == null) return 0;

            foreach (var (rssUrl, menuId) in _rssFeeds)
            {
                try
                {
                    var xml = await _http.GetStringAsync(rssUrl);
                    var doc = XDocument.Parse(xml);
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
                    Console.WriteLine($"[VnExpress] Lỗi {rssUrl}: {ex.Message}");
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
                    .SelectSingleNode(
                        "//article[contains(@class,'fck_detail')]");

                var authorNode = htmlDoc.DocumentNode
                    .SelectSingleNode(
                        "//p[@style='text-align:right;']//strong");

                var content = contentNode?.InnerHtml ?? "";
                var author  = authorNode?.InnerText?.Trim() ?? "VnExpress";

                return (content, author);
            }
            catch
            {
                return ("", "VnExpress");
            }
        }
    }
}