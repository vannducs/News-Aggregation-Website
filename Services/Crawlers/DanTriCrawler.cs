using System.Xml.Linq;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using NewsAggregator.Data;
using NewsAggregator.Models;

namespace NewsAggregator.Services.Crawlers
{
    public class DanTriCrawler : BaseCrawler, INewsCrawler
    {
        public DanTriCrawler(AppDbContext db, HttpClient http)
            : base(db, http)
        {
        }

        private readonly Dictionary<string, int> _rssFeeds = new()
        {
            { "https://dantri.com.vn/rss/xa-hoi.rss",      2  },
            { "https://dantri.com.vn/rss/the-gioi.rss",    3  },
            { "https://dantri.com.vn/rss/the-thao.rss",    4  },
            { "https://dantri.com.vn/rss/kinh-doanh.rss",  5  },
            { "https://dantri.com.vn/rss/suc-manh-so.rss", 6  },
            { "https://dantri.com.vn/rss/suc-khoe.rss",    7  },
            { "https://dantri.com.vn/rss/giai-tri.rss",    8  },
            { "https://dantri.com.vn/rss/phap-luat.rss",   9  },
            { "https://dantri.com.vn/rss/giao-duc.rss",    10 },
            { "https://dantri.com.vn/rss/du-lich.rss",     11 },
            { "https://dantri.com.vn/rss/o-to-xe-may.rss", 12 },
        };

        public async Task<int> CrawlAsync()
        {
            int totalSaved = 0;

            var source = await _db.Sources
                .FirstOrDefaultAsync(s => s.SourceName.Contains("Dân Trí"));

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
                    Console.WriteLine($"[DanTri] Lỗi {rssUrl}: {ex.Message}");
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
                    .SelectSingleNode("//div[@data-slot='content']");

                var authorNode = htmlDoc.DocumentNode
                    .SelectSingleNode("//a[@rel='author']");

                var content = contentNode?.InnerHtml ?? "";
                var author  = authorNode?.InnerText?.Trim() ?? "Dân Trí";

                return (content, author);
            }
            catch
            {
                return ("", "Dân Trí");
            }
        }
    }
}