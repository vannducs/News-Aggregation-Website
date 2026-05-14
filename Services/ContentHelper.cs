using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace NewsAggregator.Services
{
    public static class ContentHelper
    {
        private static readonly Regex HeightStyleRegex = new(
            @"\b(height|min-height|padding-top|padding-bottom|width|float|position|z-index|margin-left|margin-right)\s*:\s*[^;]+;?\s*",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Full article HTML normalization: fix lazy images, absolute URLs,
        /// strip inline styles, remove junk nodes, clean empty paragraphs.
        /// </summary>
        public static string NormalizeArticleHtml(string html, string? baseUrl = null)
        {
            if (string.IsNullOrEmpty(html)) return html;

            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var root = doc.DocumentNode;

            RemoveJunkNodes(root);
            NormalizeImages(root, baseUrl);
            CleanFigures(root);
            StripBlockStyles(root);
            RemoveEmptyParagraphs(root);

            return root.InnerHtml;
        }

        // Backward-compatible wrapper — PostImageFixService and BaseCrawler use this.
        public static string FixContentImages(string html, string? baseUrl = null)
            => NormalizeArticleHtml(html, baseUrl);

        // ── Private helpers ────────────────────────────────────────────────

        private static void RemoveJunkNodes(HtmlNode root)
        {
            var tags = new[] { "script", "style", "noscript", "iframe", "button", "form", "input", "select" };
            foreach (var tag in tags)
            {
                var nodes = root.SelectNodes($"//{tag}");
                if (nodes == null) continue;
                foreach (var n in nodes.ToList()) n.Remove();
            }
        }

        private static void NormalizeImages(HtmlNode root, string? baseUrl)
        {
            var imgs = root.SelectNodes("//img");
            if (imgs == null) return;

            foreach (var img in imgs.ToList())
            {
                // Resolve lazy-load src
                var src = img.GetAttributeValue("src", "").Trim();
                if (IsPlaceholderSrc(src))
                {
                    src = FirstNonEmpty(
                        img.GetAttributeValue("data-src", ""),
                        img.GetAttributeValue("data-original", ""),
                        img.GetAttributeValue("data-lazy", ""),
                        img.GetAttributeValue("data-lazy-src", ""),
                        img.GetAttributeValue("data-echo", "")
                    ) ?? "";
                }

                // Convert relative → absolute
                if (!string.IsNullOrEmpty(src) && !string.IsNullOrEmpty(baseUrl))
                    src = MakeAbsolute(src, baseUrl);

                if (!string.IsNullOrEmpty(src))
                    img.SetAttributeValue("src", src);

                // Remove noisy attributes
                foreach (var attr in new[] {
                    "data-src", "data-original", "data-lazy", "data-lazy-src", "data-echo",
                    "data-srcset", "srcset", "width", "height", "loading",
                    "style", "class", "onclick", "onmouseover" })
                    img.Attributes.Remove(attr);

                // Ensure referrerpolicy so hotlink-protected images load
                img.SetAttributeValue("referrerpolicy", "no-referrer");

                // Keep alt if present, otherwise use empty string
                if (!img.Attributes.Contains("alt"))
                    img.SetAttributeValue("alt", "");
            }
        }

        private static void CleanFigures(HtmlNode root)
        {
            var nodes = root.SelectNodes("//figure | //picture");
            if (nodes == null) return;

            foreach (var el in nodes.ToList())
            {
                el.Attributes.Remove("width");
                el.Attributes.Remove("height");
                el.Attributes.Remove("class");
                el.Attributes.Remove("onclick");

                var style = el.GetAttributeValue("style", "");
                if (!string.IsNullOrEmpty(style))
                {
                    var cleaned = HeightStyleRegex.Replace(style, "").Trim().TrimEnd(';');
                    if (string.IsNullOrWhiteSpace(cleaned))
                        el.Attributes.Remove("style");
                    else
                        el.SetAttributeValue("style", cleaned);
                }
            }
        }

        private static void StripBlockStyles(HtmlNode root)
        {
            const string xPath =
                "//p | //div | //span | //h1 | //h2 | //h3 | //h4 | //h5 | //h6 " +
                "| //blockquote | //li | //ul | //ol | //td | //th | //tr | //table " +
                "| //section | //article | //header | //footer | //aside";

            var nodes = root.SelectNodes(xPath);
            if (nodes == null) return;

            foreach (var el in nodes.ToList())
            {
                el.Attributes.Remove("style");
                el.Attributes.Remove("onclick");
                el.Attributes.Remove("onmouseover");
            }
        }

        private static void RemoveEmptyParagraphs(HtmlNode root)
        {
            var paragraphs = root.SelectNodes("//p");
            if (paragraphs == null) return;

            foreach (var p in paragraphs.ToList())
            {
                var hasText = !string.IsNullOrWhiteSpace(p.InnerText);
                var hasMedia = p.SelectSingleNode(".//img | .//video | .//iframe") != null;
                if (!hasText && !hasMedia)
                    p.Remove();
            }
        }

        private static bool IsPlaceholderSrc(string src)
        {
            if (string.IsNullOrEmpty(src) || src == "about:blank") return true;
            var l = src.ToLowerInvariant();
            return l.StartsWith("data:image/gif") ||
                   l.StartsWith("data:image/png") ||
                   l.StartsWith("data:image/svg") ||
                   l == "javascript:void(0)";
        }

        private static string? FirstNonEmpty(params string[] values)
        {
            foreach (var v in values)
            {
                var t = v?.Trim();
                if (!string.IsNullOrEmpty(t)) return t;
            }
            return null;
        }

        private static string MakeAbsolute(string url, string baseUrl)
        {
            if (string.IsNullOrEmpty(url)) return url;
            if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) return url;
            if (url.StartsWith("//")) return "https:" + url;
            if (url.StartsWith("/") && Uri.TryCreate(baseUrl, UriKind.Absolute, out var base1))
                return $"{base1.Scheme}://{base1.Host}{url}";
            // relative path
            if (Uri.TryCreate(baseUrl, UriKind.Absolute, out var base2) &&
                Uri.TryCreate(base2, url, out var abs))
                return abs.AbsoluteUri;
            return url;
        }

        // Extract scheme+host from any URL — used by crawlers to pass base URL
        public static string ExtractBaseUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return url;
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return $"{uri.Scheme}://{uri.Host}";
            return url;
        }
    }
}
