using System.Xml.Linq;
using HtmlAgilityPack;
using NewsAggregator.Models;

namespace NewsAggregator.Services
{
    public interface IUniversalArticleExtractorService
    {
        Task<List<RssFeedItem>> GetRssFeedItemsAsync(string rssUrl);
        Task<ArticleExtractResult> ExtractArticleAsync(string articleUrl);
    }

    public class UniversalArticleExtractorService : IUniversalArticleExtractorService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<UniversalArticleExtractorService> _logger;

        // Thử lần lượt, lấy node có score (textLength + paragraphCount×80) cao nhất
        private static readonly string[] ContentSelectors =
        {
            "//*[@itemprop='articleBody']",               // Schema.org — ~60% báo
            "//*[contains(@class,'fck_detail')]",          // VnExpress
            "//*[contains(@class,'detail-content')]",      // Tuổi Trẻ, Thanh Niên, PLO
            "//*[contains(@class,'singular-content')]",    // Dân Trí
            "//*[contains(@class,'the-article-body')]",    // Zing/Znews
            "//*[contains(@class,'content-detail')]",      // VietnamNet
            "//*[contains(@class,'article-content')]",     // VTV, VOV, Nhân Dân, QDND
            "//*[contains(@class,'detail-body')]",         // NLD, Tiền Phong
            "//*[contains(@class,'detail__body')]",
            "//*[contains(@class,'post-content')]",
            "//*[contains(@class,'entry-content')]",
            "//*[contains(@class,'news-content')]",
            "//*[contains(@id,'ArticleContent')]",         // báo .NET Webforms cũ
            "//*[@id='noidung']",                          // Dân Trí cũ, Lao Động
            "//*[@id='maincontent']",
            "//article",                                   // HTML5 fallback
        };

        private static readonly string[] ViDateFormats =
        {
            "dd/MM/yyyy HH:mm",
            "dd/MM/yyyy HH:mm:ss",
            "HH:mm dd/MM/yyyy",
            "dd/MM/yyyy",
            "dd-MM-yyyy HH:mm",
            "dd-MM-yyyy",
        };

        public UniversalArticleExtractorService(
            IHttpClientFactory httpClientFactory,
            ILogger<UniversalArticleExtractorService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        // ────────────────────────────────────────────────────────────────
        // RSS — đọc bytes rồi decode UTF-8 để tránh encoding trap
        // ────────────────────────────────────────────────────────────────

        public async Task<List<RssFeedItem>> GetRssFeedItemsAsync(string rssUrl)
        {
            var result = new List<RssFeedItem>();
            try
            {
                var client = _httpClientFactory.CreateClient("ArticleExtractor");
                // GetByteArrayAsync + decode thủ công: AutomaticDecompression đã giải nén
                // nhưng GetStringAsync đôi khi đoán sai encoding → đọc bytes + UTF-8 an toàn hơn
                var bytes = await client.GetByteArrayAsync(rssUrl);
                var xml   = System.Text.Encoding.UTF8.GetString(bytes).TrimStart('﻿');

                var doc = XDocument.Parse(xml);

                XNamespace media   = "http://search.yahoo.com/mrss/";
                XNamespace dc      = "http://purl.org/dc/elements/1.1/";
                XNamespace content = "http://purl.org/rss/1.0/modules/content/";

                foreach (var item in doc.Descendants("item"))
                {
                    var title = item.Element("title")?.Value?.Trim();
                    var link  = item.Element("link")?.Value?.Trim()
                             ?? item.Elements().FirstOrDefault(e => e.Name.LocalName == "link")?.Value?.Trim();

                    if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(link)) continue;

                    var pubDateStr  = item.Element("pubDate")?.Value?.Trim()
                                  ?? item.Element(dc + "date")?.Value?.Trim();
                    var description = item.Element("description")?.Value?.Trim()
                                  ?? item.Element(content + "encoded")?.Value?.Trim();
                    var author      = item.Elements()
                                         .FirstOrDefault(e => e.Name.LocalName is "author" or "creator")
                                         ?.Value?.Trim();
                    var category    = item.Element("category")?.Value?.Trim()
                                  ?? item.Elements()
                                         .FirstOrDefault(e => e.Name.LocalName == "category")
                                         ?.Value?.Trim();

                    var feedItem = new RssFeedItem
                    {
                        Title         = title,
                        Url           = link,
                        Summary       = StripHtml(description ?? ""),
                        Author        = author,
                        Category      = category,
                        PublishedDate = ParseDate(pubDateStr),
                    };

                    // Ảnh: media:thumbnail → enclosure → img đầu tiên trong description
                    feedItem.ImageUrl =
                        item.Element(media + "thumbnail")?.Attribute("url")?.Value
                     ?? item.Elements()
                            .FirstOrDefault(e => e.Name.LocalName is "thumbnail" or "content")
                            ?.Attribute("url")?.Value;

                    if (feedItem.ImageUrl == null)
                    {
                        var enclosure = item.Element("enclosure");
                        var encType   = enclosure?.Attribute("type")?.Value ?? "";
                        if (encType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                            feedItem.ImageUrl = enclosure?.Attribute("url")?.Value;
                    }

                    if (feedItem.ImageUrl == null && !string.IsNullOrEmpty(description))
                        feedItem.ImageUrl = ExtractFirstImageFromHtml(description);

                    result.Add(feedItem);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RSS] Lỗi đọc feed: {Url}", rssUrl);
            }
            return result;
        }

        // ────────────────────────────────────────────────────────────────
        // Crawl bài báo từ URL
        // ────────────────────────────────────────────────────────────────

        public async Task<ArticleExtractResult> ExtractArticleAsync(string articleUrl)
        {
            var result = new ArticleExtractResult { SourceUrl = articleUrl };
            try
            {
                var client = _httpClientFactory.CreateClient("ArticleExtractor");
                var html   = await client.GetStringAsync(articleUrl);

                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                result.Title         = ExtractTitle(doc);
                result.ImageUrl      = ExtractImage(doc);
                result.Author        = ExtractAuthor(doc);
                result.PublishedDate = ExtractDate(doc);
                (result.Content, result.ContentText) = ExtractContent(doc);
                result.IsSuccess = !string.IsNullOrWhiteSpace(result.Content)
                                || !string.IsNullOrWhiteSpace(result.Title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Crawl] Lỗi: {Url}", articleUrl);
                result.IsSuccess    = false;
                result.ErrorMessage = ex.Message;
            }
            return result;
        }

        // ────────────────────────────────────────────────────────────────
        // Private extractors
        // ────────────────────────────────────────────────────────────────

        private static string ExtractTitle(HtmlDocument doc)
        {
            var ogTitle = GetMetaContent(doc, "property", "og:title");
            if (!string.IsNullOrEmpty(ogTitle)) return ogTitle;

            foreach (var xpath in new[]
            {
                "//h1[contains(@class,'title')]",
                "//h1[contains(@class,'detail')]",
                "//h1[contains(@class,'article')]",
                "//h1",
            })
            {
                var text = doc.DocumentNode.SelectSingleNode(xpath)?.InnerText?.Trim();
                if (!string.IsNullOrEmpty(text)) return HtmlEntity.DeEntitize(text);
            }

            return doc.DocumentNode.SelectSingleNode("//title")?.InnerText?.Trim() ?? "";
        }

        private static (string html, string text) ExtractContent(HtmlDocument doc)
        {
            HtmlNode? bestNode = null;
            int bestScore = 0;

            foreach (var sel in ContentSelectors)
            {
                var node = doc.DocumentNode.SelectSingleNode(sel);
                if (node == null) continue;

                var pCount  = node.SelectNodes(".//p")?.Count ?? 0;
                var textLen = node.InnerText?.Length ?? 0;
                var score   = textLen + pCount * 80;

                if (score > bestScore) { bestScore = score; bestNode = node; }
            }

            if (bestNode == null || bestScore < 300) return ("", "");

            CleanNode(bestNode);
            return (bestNode.InnerHtml, StripHtml(bestNode.InnerText ?? ""));
        }

        private static string? ExtractImage(HtmlDocument doc)
        {
            // og:image và twitter:image — 100% báo lớn có
            foreach (var (attrName, attrVal) in new[]
            {
                ("property", "og:image"),
                ("name",     "twitter:image"),
                ("name",     "twitter:image:src"),
            })
            {
                var node = doc.DocumentNode.SelectSingleNode($"//meta[@{attrName}='{attrVal}']");
                var val  = node?.GetAttributeValue("content", "");
                if (IsImageUrl(val)) return val;
            }

            // itemprop=image
            var imgNode = doc.DocumentNode.SelectSingleNode("//*[@itemprop='image']");
            if (imgNode != null)
            {
                var src = imgNode.GetAttributeValue("src", "");
                if (string.IsNullOrEmpty(src)) src = imgNode.GetAttributeValue("content", "");
                if (IsImageUrl(src)) return src;
            }

            return null;
        }

        private static string? ExtractAuthor(HtmlDocument doc)
        {
            // meta[name=author] — ~70% báo
            var metaAuthor = GetMetaContent(doc, "name", "author");
            if (!string.IsNullOrEmpty(metaAuthor) && metaAuthor.Length < 80)
                return metaAuthor;

            // meta[property=article:author]
            var articleAuthor = GetMetaContent(doc, "property", "article:author");
            if (!string.IsNullOrEmpty(articleAuthor) && articleAuthor.Length < 80)
                return articleAuthor;

            // itemprop=author (Schema.org)
            var authorNode = doc.DocumentNode.SelectSingleNode("//*[@itemprop='author']");
            if (authorNode != null)
            {
                var nameNode = authorNode.SelectSingleNode(".//*[@itemprop='name']");
                var text     = (nameNode ?? authorNode).InnerText?.Trim();
                if (!string.IsNullOrEmpty(text) && text.Length < 100)
                    return HtmlEntity.DeEntitize(text);
            }

            // class-based: box-author (VnExpress, Thanh Niên) → author-name (Dân Trí) → author chung
            foreach (var cls in new[] { "box-author", "article-author", "author-name", "reporter", "writer", "author" })
            {
                var node = doc.DocumentNode.SelectSingleNode($"//*[contains(@class,'{cls}')]");
                var txt  = node?.InnerText?.Trim();
                if (!string.IsNullOrEmpty(txt) && txt.Length < 100)
                    return HtmlEntity.DeEntitize(txt);
            }

            return null;
        }

        private static DateTime? ExtractDate(HtmlDocument doc)
        {
            // article:published_time — Open Graph, ~90% báo có
            var ogDate = GetMetaContent(doc, "property", "article:published_time");
            if (TryParseDate(ogDate, out var dt0)) return dt0;

            // <time datetime="..."> — HTML5, ~80% báo
            var timeVal = doc.DocumentNode.SelectSingleNode("//time[@datetime]")
                              ?.GetAttributeValue("datetime", "");
            if (TryParseDate(timeVal, out var dt1)) return dt1;

            // itemprop=datePublished — Schema.org
            var dateNode = doc.DocumentNode.SelectSingleNode("//*[@itemprop='datePublished']");
            if (dateNode != null)
            {
                var val = dateNode.GetAttributeValue("content", "");
                if (string.IsNullOrEmpty(val)) val = dateNode.GetAttributeValue("datetime", "");
                if (string.IsNullOrEmpty(val)) val = dateNode.InnerText?.Trim() ?? "";
                if (TryParseDate(val, out var dt2)) return dt2;
            }

            // meta[name=pubdate]
            var pubDate = GetMetaContent(doc, "name", "pubdate");
            if (TryParseDate(pubDate, out var dt3)) return dt3;

            // class-based date: publish-date, date-publish...
            foreach (var cls in new[] { "publish-date", "date-publish", "publish_date", "post-date" })
            {
                var raw = doc.DocumentNode
                    .SelectSingleNode($"//*[contains(@class,'{cls}')]")?.InnerText?.Trim();
                if (TryParseDate(raw, out var dt4)) return dt4;
            }

            return null;
        }

        // ────────────────────────────────────────────────────────────────
        // Utilities
        // ────────────────────────────────────────────────────────────────

        private static void CleanNode(HtmlNode node)
        {
            var removeSelectors = new[]
            {
                ".//script", ".//style", ".//iframe", ".//noscript",
                ".//*[contains(@class,'ads')]",
                ".//*[contains(@class,'advertisement')]",
                ".//*[contains(@class,'related')]",
                ".//*[contains(@class,'social')]",
                ".//*[contains(@class,'share')]",
                ".//*[contains(@class,'comment')]",
                ".//*[contains(@class,'tags')]",
                ".//*[contains(@id,'ads')]",
                ".//*[contains(@id,'banner')]",
            };
            foreach (var sel in removeSelectors)
            {
                var nodes = node.SelectNodes(sel);
                if (nodes == null) continue;
                foreach (var n in nodes.ToList()) n.Remove();
            }
        }

        private static string? GetMetaContent(HtmlDocument doc, string attrName, string attrValue)
        {
            var val = doc.DocumentNode
                .SelectSingleNode($"//meta[@{attrName}='{attrValue}']")
                ?.GetAttributeValue("content", "");
            return string.IsNullOrEmpty(val) ? null : HtmlEntity.DeEntitize(val);
        }

        private static string StripHtml(string html)
        {
            if (string.IsNullOrEmpty(html)) return "";
            var d = new HtmlDocument();
            d.LoadHtml(html);
            return HtmlEntity.DeEntitize(d.DocumentNode.InnerText).Trim();
        }

        private static string? ExtractFirstImageFromHtml(string html)
        {
            if (string.IsNullOrEmpty(html)) return null;
            var d = new HtmlDocument();
            d.LoadHtml(html);
            var src = d.DocumentNode.SelectSingleNode("//img")?.GetAttributeValue("src", "");
            return string.IsNullOrEmpty(src) ? null : src;
        }

        private static bool IsImageUrl(string? url)
        {
            if (string.IsNullOrEmpty(url)) return false;
            return url.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                && (url.Contains(".jpg") || url.Contains(".jpeg") || url.Contains(".png")
                 || url.Contains(".webp") || url.Contains(".gif")
                 || url.Contains("image") || url.Contains("photo") || url.Contains("thumb"));
        }

        private static DateTime? ParseDate(string? raw)
            => TryParseDate(raw, out var dt) ? dt : null;

        private static bool TryParseDate(string? raw, out DateTime result)
        {
            result = default;
            if (string.IsNullOrWhiteSpace(raw)) return false;

            // ISO 8601, RFC 2822, và các format tự động khác
            if (DateTime.TryParse(raw, null,
                System.Globalization.DateTimeStyles.RoundtripKind, out result))
                return true;

            // Format ngày Việt Nam
            return DateTime.TryParseExact(raw, ViDateFormats,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out result);
        }
    }
}
