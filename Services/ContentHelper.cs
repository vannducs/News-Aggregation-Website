using HtmlAgilityPack;

namespace NewsAggregator.Services
{
    public static class ContentHelper
    {
        /// <summary>
        /// Fix lazy-loaded images trong HTML content:
        /// - data-src / data-original / data-lazy → src
        /// - Xóa inline width/height trên img và figure
        /// Gọi trước khi lưu Contents vào DB, và dùng để re-process bài cũ.
        /// </summary>
        public static string FixContentImages(string html)
        {
            if (string.IsNullOrEmpty(html)) return html;

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var imgs = doc.DocumentNode.SelectNodes("//img");
            if (imgs != null)
            {
                foreach (var img in imgs)
                {
                    var src = img.GetAttributeValue("src", "").Trim();

                    if (string.IsNullOrEmpty(src) || src == "about:blank"
                        || src.StartsWith("data:image/gif", StringComparison.OrdinalIgnoreCase)
                        || src.StartsWith("data:image/png", StringComparison.OrdinalIgnoreCase))
                    {
                        var realSrc = img.GetAttributeValue("data-src", "").Trim();
                        if (string.IsNullOrEmpty(realSrc))
                            realSrc = img.GetAttributeValue("data-original", "").Trim();
                        if (string.IsNullOrEmpty(realSrc))
                            realSrc = img.GetAttributeValue("data-lazy", "").Trim();
                        if (string.IsNullOrEmpty(realSrc))
                            realSrc = img.GetAttributeValue("data-lazy-src", "").Trim();

                        if (!string.IsNullOrEmpty(realSrc))
                            img.SetAttributeValue("src", realSrc);
                    }

                    img.Attributes.Remove("width");
                    img.Attributes.Remove("height");
                    img.Attributes.Remove("data-src");
                    img.Attributes.Remove("data-original");
                    img.Attributes.Remove("data-lazy");
                    img.Attributes.Remove("data-lazy-src");
                    img.Attributes.Remove("loading");
                }
            }

            var wrappers = doc.DocumentNode.SelectNodes("//figure | //picture");
            if (wrappers != null)
            {
                foreach (var el in wrappers)
                {
                    el.Attributes.Remove("width");
                    el.Attributes.Remove("height");
                    var style = el.GetAttributeValue("style", "");
                    if (!string.IsNullOrEmpty(style))
                    {
                        style = System.Text.RegularExpressions.Regex.Replace(
                            style, @"(height|min-height|padding-top)\s*:\s*[^;]+;?\s*", "",
                            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        el.SetAttributeValue("style", style.Trim().TrimEnd(';'));
                    }
                }
            }

            return doc.DocumentNode.InnerHtml;
        }
    }
}
