using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace NewsAggregator.Services
{
    public static class ContentHelper
    {
        // Strips layout-breaking properties from figure/picture inline styles.
        private static readonly Regex FigureHeightRegex = new(
            @"\b(height|min-height|padding-top|padding-bottom)\s*:\s*[^;]+;?\s*",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Normalizes article HTML for storage and display:
        /// - Removes non-content nodes (script, style, iframe)
        /// - Resolves lazy-loaded images (data-src → src)
        /// - Converts relative/protocol-relative image URLs to absolute
        /// - Removes fixed-height inline styles on figure/picture (prevents blank space)
        /// - Removes dimension attributes on img and figure
        ///
        /// Intentionally does NOT:
        /// - Remove class or style from img (preserves site-specific display logic)
        /// - Remove class from figure/picture
        /// - Strip styles from block elements (too aggressive, breaks layouts)
        /// </summary>
        public static string NormalizeArticleHtml(string html, string? baseUrl = null)
        {
            if (string.IsNullOrEmpty(html)) return html;

            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var root = doc.DocumentNode;

            RemoveNonContentNodes(root);
            FixLazyImages(root, baseUrl);
            CleanFigureStyles(root);

            return root.InnerHtml;
        }

        // Backward-compatible wrapper used by BaseCrawler and PostImageFixService.
        public static string FixContentImages(string html, string? baseUrl = null)
            => NormalizeArticleHtml(html, baseUrl);

        // Extract scheme://host from any URL — used by crawlers.
        public static string ExtractBaseUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return url;
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return $"{uri.Scheme}://{uri.Host}";
            return url;
        }

        // ── Private helpers ────────────────────────────────────────────────

        private static void RemoveNonContentNodes(HtmlNode root)
        {
            // Only remove nodes that are never legitimate article content.
            // Do NOT remove noscript (some sites put fallback <img> inside noscript).
            foreach (var tag in new[] { "script", "style", "iframe" })
            {
                var nodes = root.SelectNodes($"//{tag}");
                if (nodes == null) continue;
                foreach (var n in nodes.ToList()) n.Remove();
            }
        }

        private static void FixLazyImages(HtmlNode root, string? baseUrl)
        {
            var imgs = root.SelectNodes("//img");
            if (imgs == null) return;

            foreach (var img in imgs.ToList())
            {
                // --- Resolve lazy-load src ---
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

                // --- Convert relative / protocol-relative to absolute ---
                if (!string.IsNullOrEmpty(src) && !string.IsNullOrEmpty(baseUrl))
                    src = MakeAbsolute(src, baseUrl);

                if (!string.IsNullOrEmpty(src))
                    img.SetAttributeValue("src", src);

                // --- Remove lazy-load helper attributes (already consumed above) ---
                // Also remove srcset — it conflicts with our single-src approach.
                // Keep: class, style, alt, title, referrerpolicy — do NOT touch them.
                foreach (var attr in new[]
                {
                    "data-src", "data-original", "data-lazy", "data-lazy-src", "data-echo",
                    "data-srcset", "srcset", "width", "height", "loading"
                })
                    img.Attributes.Remove(attr);
            }
        }

        private static void CleanFigureStyles(HtmlNode root)
        {
            var nodes = root.SelectNodes("//figure | //picture");
            if (nodes == null) return;

            foreach (var el in nodes.ToList())
            {
                // Remove dimension attributes — let CSS control sizing.
                el.Attributes.Remove("width");
                el.Attributes.Remove("height");

                // Strip only the height-related properties from inline style;
                // leave other style properties (e.g. text-align) untouched.
                var style = el.GetAttributeValue("style", "");
                if (!string.IsNullOrEmpty(style))
                {
                    var cleaned = FigureHeightRegex.Replace(style, "").Trim().TrimEnd(';');
                    if (string.IsNullOrWhiteSpace(cleaned))
                        el.Attributes.Remove("style");
                    else
                        el.SetAttributeValue("style", cleaned);
                }
                // Keep class — removing it would break site-specific CSS selectors
                // that are targeted by our own .entry-content rules in Detail.cshtml.
            }
        }

        private static bool IsPlaceholderSrc(string src)
        {
            if (string.IsNullOrEmpty(src) || src == "about:blank") return true;
            var l = src.ToLowerInvariant();
            return l.StartsWith("data:image/gif")
                || l.StartsWith("data:image/png")
                || l.StartsWith("data:image/svg")
                || l == "javascript:void(0)";
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

            // Protocol-relative: //cdn.example.com/img.jpg
            if (url.StartsWith("//")) return "https:" + url;

            // Root-relative: /uploads/img.jpg
            if (url.StartsWith("/") && Uri.TryCreate(baseUrl, UriKind.Absolute, out var b1))
                return $"{b1.Scheme}://{b1.Host}{url}";

            // Path-relative: images/img.jpg
            if (Uri.TryCreate(baseUrl, UriKind.Absolute, out var b2) &&
                Uri.TryCreate(b2, url, out var abs))
                return abs.AbsoluteUri;

            return url;
        }
    }
}
