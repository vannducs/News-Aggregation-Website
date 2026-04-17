using HtmlAgilityPack;
using NewsAggregator.Data;

namespace NewsAggregator.Services.Crawlers
{
    public abstract class BaseCrawler
    {
        protected readonly AppDbContext _db;
        protected readonly HttpClient _http;
        protected BaseCrawler(AppDbContext db, HttpClient http)
        {
            _db = db;
            _http = http;
        }
        protected string ExtractImageFromDescription(string description)
        {
            if (string.IsNullOrEmpty(description)) return "";
            try
            {
                var htmlDoc = new HtmlDocument(); 
                htmlDoc.LoadHtml(description);

                var imgNode = htmlDoc.DocumentNode.SelectSingleNode("//img");
                return imgNode?.GetAttributeValue("src","") ?? "";
            }
            catch
            {
                return "";
            }
        }

        protected string StripHtml(string html)
        {
            if (string.IsNullOrEmpty(html)) return "";

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            return doc.DocumentNode.InnerText.Trim();
        }

        protected DateTime ParseDate(string? dateStr)
        {
            if (string.IsNullOrEmpty(dateStr)) return DateTime.Now;

            return DateTime.TryParse(dateStr, out var result) ? result : DateTime.Now;
        }
    }
}