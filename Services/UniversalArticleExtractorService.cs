using System.Text;
using System.Text.RegularExpressions;
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
        private static readonly string[] ContentSelectors =
        {
            // ── Schema.org (~60% báo lớn hiện đại) ──────────────────────
            "//*[@itemprop='articleBody']",

            // ── Báo lớn Việt Nam ─────────────────────────────────────────
            "//*[contains(@class,'fck_detail')]",           
            "//*[contains(@class,'detail-content')]",       
            "//*[contains(@class,'singular-content')]",     
            "//*[contains(@class,'the-article-body')]",     
            "//*[contains(@class,'content-detail')]",      
            "//*[contains(@class,'article-content')]",      
            "//*[contains(@class,'article__content')]",     
            "//*[contains(@class,'article__body')]",        
            "//*[contains(@class,'article-body')]",      
            "//*[contains(@class,'detail-body')]",        
            "//*[contains(@class,'detail__body')]",    
            "//*[contains(@class,'contentdetail')]",      
            "//*[contains(@class,'cms-body')]",         
            "//*[contains(@class,'knc-body')]",           
            "//*[contains(@class,'nd-detail')]",         
            "//*[contains(@class,'detail-content-area')]", 
            // wordpress
            "//*[contains(@class,'post-content')]",        
            "//*[contains(@class,'entry-content')]",       
            "//*[contains(@class,'post-body')]",         
            "//*[contains(@class,'news-content')]",
            "//*[contains(@class,'news-detail')]",         
            "//*[contains(@class,'article-detail')]",      
            "//*[contains(@class,'main-content')]",
            "//*[contains(@class,'body-content')]",
            "//*[contains(@class,'text-content')]",
            "//*[contains(@class,'content-body')]",
            "//*[contains(@class,'article-text')]",
            "//*[contains(@class,'full-article')]",
            "//*[contains(@class,'story-body')]",
            "//*[contains(@class,'body-text')]",

            "//*[@id='ArticleContent']",                    
            "//*[@id='article-content']",
            "//*[@id='content_detail']",
            "//*[@id='noidung']",                         
            "//*[@id='maincontent']",
            "//*[@id='main-content']",
            "//*[@id='main-detail-body']",              
            "//*[@id='content-article']",

            "//article",
            "//main",
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
        private static readonly Dictionary<string, string> HtmlEntityFixes = new(StringComparer.OrdinalIgnoreCase)
        {
            ["nbsp"]   = " ",  ["ndash"]  = "–",  ["mdash"]  = "—",
            ["laquo"]  = "«",  ["raquo"]  = "»",  ["hellip"] = "…",
            ["copy"]   = "©",  ["reg"]    = "®",  ["trade"]  = "™",
            ["euro"]   = "€",  ["pound"]  = "£",  ["yen"]    = "¥",
            ["bull"]   = "•",  ["lsquo"]  = "‘", ["rsquo"] = "’",
            ["ldquo"]  = "“", ["rdquo"] = "”",
            ["deg"]    = "°",  ["times"]  = "×",  ["divide"] = "÷",
            ["prime"]  = "′",  ["Prime"]  = "″",  ["frac12"] = "½",
            ["frac14"] = "¼",  ["frac34"] = "¾",  ["cent"]   = "¢",
            ["plusmn"] = "±",  ["sup2"]   = "²",  ["sup3"]   = "³",
        };

        private static readonly Regex EntityRegex = new(
            @"&(?!(amp|lt|gt|quot|apos|#\d+|#x[\da-fA-F]+);)([A-Za-z]\w*);",
            RegexOptions.Compiled);

        private static readonly Regex InvalidXmlChars = new(
            @"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]",
            RegexOptions.Compiled);

        private static readonly Regex XmlEncodingDecl = new(
            @"encoding\s*=\s*[""']([^""']+)[""']",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public UniversalArticleExtractorService(
            IHttpClientFactory httpClientFactory,
            ILogger<UniversalArticleExtractorService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<List<RssFeedItem>> GetRssFeedItemsAsync(string rssUrl)
        {
            var result = new List<RssFeedItem>();
            try
            {
                var client = _httpClientFactory.CreateClient("ArticleExtractor");
                var bytes  = await client.GetByteArrayAsync(rssUrl);

                var enc = DetectXmlEncoding(bytes);
                var xml = enc.GetString(bytes).TrimStart('﻿');

                xml = InvalidXmlChars.Replace(xml, "");

                xml = StripDoctype(xml);

                if (IsHtmlResponse(xml))
                {
                    Console.WriteLine($"[RSS] → URL trả về trang HTML, không phải RSS/XML: {rssUrl}");
                    Console.WriteLine($"[RSS] → Kiểm tra lại RSS URL. Thử mở URL trong trình duyệt xem có phải feed không.");
                    return result;
                }

                xml = EntityRegex.Replace(xml, m =>
                {
                    var name = m.Groups[2].Value.TrimEnd(';');
                    return HtmlEntityFixes.TryGetValue(name, out var rep) ? rep : " ";
                });

                XDocument doc;
                try
                {
                    doc = XDocument.Parse(xml);
                }
                catch (System.Xml.XmlException xmlEx)
                {
                    Console.WriteLine($"[RSS] XML không chuẩn, thử lenient parser: {xmlEx.Message}");
                    var settings = new System.Xml.XmlReaderSettings
                    {
                        DtdProcessing    = System.Xml.DtdProcessing.Ignore,
                        CheckCharacters  = false,
                        ConformanceLevel = System.Xml.ConformanceLevel.Fragment,
                    };
                    using var sr     = new StringReader(xml);
                    using var reader = System.Xml.XmlReader.Create(sr, settings);
                    doc = XDocument.Load(reader, LoadOptions.None);
                }

                XNamespace media   = "http://search.yahoo.com/mrss/";
                XNamespace dc      = "http://purl.org/dc/elements/1.1/";
                XNamespace content = "http://purl.org/rss/1.0/modules/content/";
                XNamespace atom    = "http://www.w3.org/2005/Atom";

                var rssItems    = doc.Descendants("item").ToList();
                var atomEntries = doc.Descendants(atom + "entry").ToList();
                bool isAtom     = !rssItems.Any() && atomEntries.Any();
                var items       = isAtom ? atomEntries : rssItems;

                Console.WriteLine($"[RSS] {rssUrl}: {items.Count} {(isAtom ? "Atom entries" : "RSS items")}");

                foreach (var item in items)
                {
                    string? title, link, description, author, category, pubDateStr;
                    string? imageUrl = null;

                    if (isAtom)
                    {
                        title = item.Element(atom + "title")?.Value?.Trim();

                        var altLink = item.Elements(atom + "link")
                            .FirstOrDefault(l => l.Attribute("rel")?.Value is null or "alternate");
                        link = altLink?.Attribute("href")?.Value?.Trim()
                            ?? item.Element(atom + "id")?.Value?.Trim();

                        var summaryEl = item.Element(atom + "content")
                                     ?? item.Element(atom + "summary");
                        description = summaryEl?.Value?.Trim();

                        var authorEl = item.Element(atom + "author");
                        author = authorEl?.Element(atom + "name")?.Value?.Trim();

                        category = item.Elements(atom + "category")
                            .FirstOrDefault()?.Attribute("term")?.Value?.Trim()
                            ?? item.Elements(atom + "category").FirstOrDefault()?.Value?.Trim();

                        pubDateStr = item.Element(atom + "published")?.Value?.Trim()
                                  ?? item.Element(atom + "updated")?.Value?.Trim();
                    }
                    else
                    {
                        title = item.Element("title")?.Value?.Trim();

                        var linkEl = item.Elements().FirstOrDefault(e => e.Name.LocalName == "link");
                        link = linkEl?.Value?.Trim();
                        if (string.IsNullOrEmpty(link))
                            link = linkEl?.Attribute("href")?.Value?.Trim();
                        if (string.IsNullOrEmpty(link))
                        {
                            var guid = item.Element("guid");
                            if (guid?.Attribute("isPermaLink")?.Value?.ToLower() != "false")
                                link = guid?.Value?.Trim();
                        }

                        description = item.Element(content + "encoded")?.Value?.Trim()
                                   ?? item.Element("description")?.Value?.Trim();

                        author = item.Elements()
                            .FirstOrDefault(e => e.Name.LocalName is "creator" or "author")
                            ?.Value?.Trim();

                        category = item.Element("category")?.Value?.Trim()
                                ?? item.Elements()
                                       .FirstOrDefault(e => e.Name.LocalName == "category")
                                       ?.Value?.Trim();

                        pubDateStr = item.Element("pubDate")?.Value?.Trim()
                                  ?? item.Element(dc + "date")?.Value?.Trim();

                        imageUrl = item.Element(media + "thumbnail")?.Attribute("url")?.Value;
                        if (imageUrl == null)
                        {
                            imageUrl = item.Elements()
                                .FirstOrDefault(e =>
                                    e.Name.LocalName is "thumbnail" or "content"
                                    && e.Attribute("url") != null
                                    && (e.Attribute("medium")?.Value == "image"
                                        || e.Attribute("type")?.Value?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true
                                        || e.Attribute("url")?.Value?.Contains("image") == true))
                                ?.Attribute("url")?.Value;
                        }
                        if (imageUrl == null)
                        {
                            imageUrl = item.Elements()
                                .FirstOrDefault(e => e.Name.LocalName is "thumbnail" or "content"
                                                  && e.Attribute("url") != null)
                                ?.Attribute("url")?.Value;
                        }
                        if (imageUrl == null)
                        {
                            var enclosure = item.Element("enclosure");
                            var encType   = enclosure?.Attribute("type")?.Value ?? "";
                            if (encType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                                imageUrl = enclosure?.Attribute("url")?.Value;
                        }
                    }

                    if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(link)) continue;
                    if (!link.StartsWith("http", StringComparison.OrdinalIgnoreCase)) continue;

                    if (imageUrl == null && !string.IsNullOrEmpty(description))
                        imageUrl = ExtractFirstImageFromHtml(description);

                    result.Add(new RssFeedItem
                    {
                        Title         = title,
                        Url           = link,
                        Summary       = StripHtml(description ?? ""),
                        ImageUrl      = imageUrl,
                        Author        = author,
                        Category      = category,
                        PublishedDate = ParseDate(pubDateStr),
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RSS] Lỗi đọc feed: {Url}", rssUrl);
                Console.WriteLine($"[RSS] Lỗi đọc feed {rssUrl}: {ex.GetType().Name}: {ex.Message}");
            }

            Console.WriteLine($"[RSS] Đọc được {result.Count} items từ {rssUrl}");
            return result;
        }

        public async Task<ArticleExtractResult> ExtractArticleAsync(string articleUrl)
        {
            var result = new ArticleExtractResult { SourceUrl = articleUrl };
            try
            {
                var client = _httpClientFactory.CreateClient("ArticleExtractor");
                var bytes = await client.GetByteArrayAsync(articleUrl);
                var enc   = DetectHtmlEncoding(bytes);
                var html  = enc.GetString(bytes);

                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                result.Title         = ExtractTitle(doc);
                result.ImageUrl      = ExtractImage(doc);
                result.Author        = ExtractAuthor(doc);
                result.PublishedDate = ExtractDate(doc);
                (result.Content, result.ContentText) = ExtractContent(doc, articleUrl);
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

        private static (string html, string text) ExtractContent(HtmlDocument doc, string? sourceUrl = null)
        {
            HtmlNode? bestNode  = null;
            int       bestScore = 0;

            foreach (var sel in ContentSelectors)
            {
                HtmlNode? node;
                try { node = doc.DocumentNode.SelectSingleNode(sel); }
                catch { continue; }
                if (node == null) continue;

                var pCount  = node.SelectNodes(".//p")?.Count ?? 0;
                var textLen = node.InnerText?.Length ?? 0;
                var score   = textLen + pCount * 80;

                if (score > bestScore) { bestScore = score; bestNode = node; }
            }

            if (bestNode == null || bestScore < 100) return ("", "");

            CleanNode(bestNode);
            return (bestNode.InnerHtml, StripHtml(bestNode.InnerText ?? ""));
        }

        private static string? ExtractImage(HtmlDocument doc)
        {
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
            var metaAuthor = GetMetaContent(doc, "name", "author");
            if (!string.IsNullOrEmpty(metaAuthor) && metaAuthor.Length < 80) return metaAuthor;

            var articleAuthor = GetMetaContent(doc, "property", "article:author");
            if (!string.IsNullOrEmpty(articleAuthor) && articleAuthor.Length < 80) return articleAuthor;

            var authorNode = doc.DocumentNode.SelectSingleNode("//*[@itemprop='author']");
            if (authorNode != null)
            {
                var nameNode = authorNode.SelectSingleNode(".//*[@itemprop='name']");
                var text     = (nameNode ?? authorNode).InnerText?.Trim();
                if (!string.IsNullOrEmpty(text) && text.Length < 100)
                    return HtmlEntity.DeEntitize(text);
            }

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
            var ogDate = GetMetaContent(doc, "property", "article:published_time");
            if (TryParseDate(ogDate, out var dt0)) return dt0;

            var timeVal = doc.DocumentNode.SelectSingleNode("//time[@datetime]")
                              ?.GetAttributeValue("datetime", "");
            if (TryParseDate(timeVal, out var dt1)) return dt1;

            var dateNode = doc.DocumentNode.SelectSingleNode("//*[@itemprop='datePublished']");
            if (dateNode != null)
            {
                var val = dateNode.GetAttributeValue("content", "");
                if (string.IsNullOrEmpty(val)) val = dateNode.GetAttributeValue("datetime", "");
                if (string.IsNullOrEmpty(val)) val = dateNode.InnerText?.Trim() ?? "";
                if (TryParseDate(val, out var dt2)) return dt2;
            }

            var pubDate = GetMetaContent(doc, "name", "pubdate");
            if (TryParseDate(pubDate, out var dt3)) return dt3;

            foreach (var cls in new[] { "publish-date", "date-publish", "publish_date", "post-date", "time-pub" })
            {
                var raw = doc.DocumentNode
                    .SelectSingleNode($"//*[contains(@class,'{cls}')]")?.InnerText?.Trim();
                if (TryParseDate(raw, out var dt4)) return dt4;
            }

            return null;
        }

        private static Encoding DetectXmlEncoding(byte[] bytes)
        {
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                return Encoding.UTF8;
            if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
                return Encoding.Unicode;

            var header = Encoding.ASCII.GetString(bytes, 0, Math.Min(300, bytes.Length));
            var m      = XmlEncodingDecl.Match(header);
            if (m.Success)
            {
                try { return Encoding.GetEncoding(m.Groups[1].Value); }
                catch { /* encoding name lạ — fallback */ }
            }

            return Encoding.UTF8;
        }

        private static Encoding DetectHtmlEncoding(byte[] bytes)
        {
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                return Encoding.UTF8;

            var preview = Encoding.ASCII.GetString(bytes, 0, Math.Min(1000, bytes.Length));

            var m1 = Regex.Match(preview, @"<meta[^>]+charset\s*=\s*[""']?([^""'\s;>]+)", RegexOptions.IgnoreCase);
            if (m1.Success)
            {
                try { return Encoding.GetEncoding(m1.Groups[1].Value); }
                catch { }
            }

            var m2 = Regex.Match(preview, @"charset=([^\s;""']+)", RegexOptions.IgnoreCase);
            if (m2.Success)
            {
                try { return Encoding.GetEncoding(m2.Groups[1].Value); }
                catch { }
            }

            return Encoding.UTF8;
        }


        private static void CleanNode(HtmlNode node)
        {
            var removeSelectors = new[]
            {
                ".//script", ".//style", ".//iframe",
                ".//*[contains(@class,'ads')]",
                ".//*[contains(@class,'advertisement')]",
                ".//*[contains(@class,'related')]",
                ".//*[contains(@class,'social')]",
                ".//*[contains(@class,'share')]",
                ".//*[contains(@class,'comment')]",
                ".//*[contains(@class,'tags')]",
                ".//*[contains(@id,'ads')]",
                ".//*[contains(@id,'banner')]",
                ".//*[contains(@id,'comment')]",
                ".//*[contains(@class,'sidebar')]",
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
            var img = d.DocumentNode.SelectSingleNode("//img");
            if (img == null) return null;
            var src = img.GetAttributeValue("data-src", "");
            if (string.IsNullOrEmpty(src)) src = img.GetAttributeValue("src", "");
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

            if (DateTime.TryParse(raw, null,
                System.Globalization.DateTimeStyles.RoundtripKind, out result))
                return true;

            return DateTime.TryParseExact(raw, ViDateFormats,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out result);
        }
        private static string StripDoctype(string xml)
        {
            var start = xml.IndexOf("<!DOCTYPE", StringComparison.OrdinalIgnoreCase);
            if (start < 0) return xml;

            var i = start + 9; 
            var depth = 0;
            while (i < xml.Length)
            {
                var c = xml[i];
                if (c == '[') depth++;
                else if (c == ']') depth--;
                else if (c == '>' && depth == 0) { i++; break; }
                i++;
            }
            return xml.Remove(start, i - start).TrimStart();
        }

        private static bool IsHtmlResponse(string xml)
        {
            var trimmed = xml.TrimStart();
            if (trimmed.StartsWith("<html", StringComparison.OrdinalIgnoreCase)) return true;
            if (trimmed.StartsWith("<HTML", StringComparison.OrdinalIgnoreCase)) return true;
            var hasHtmlTag  = Regex.IsMatch(trimmed, @"<html[\s>]", RegexOptions.IgnoreCase);
            var hasRssTag   = Regex.IsMatch(trimmed, @"<(rss|feed|channel|item|entry)\b", RegexOptions.IgnoreCase);
            return hasHtmlTag && !hasRssTag;
        }
    }
}
