using Microsoft.EntityFrameworkCore;
using NewsAggregator.Data;
using NewsAggregator.Models;
using NewsAggregator.Services.Crawlers;

namespace NewsAggregator.Services
{
    public class CrawlerService
    {
        private readonly AppDbContext _db;
        private readonly IHttpClientFactory _httpClientFactory;

        public CrawlerService(
            AppDbContext db,
            IHttpClientFactory httpClientFactory)
        {
            _db = db;
            _httpClientFactory = httpClientFactory;
        }

        public async Task RunAllAsync()
        {
            Console.WriteLine($"[Crawler] Bắt đầu crawl lúc {DateTime.Now}");

            var sources = await _db.Sources
                .Where(s => s.IsActive)
                .ToListAsync();

            foreach (var source in sources)
            {
                await CrawlSourceAsync(source);
            }

            Console.WriteLine($"[Crawler] Hoàn thành lúc {DateTime.Now}");
        }

        private async Task CrawlSourceAsync(Source source)
        {
            var log = new CrawlLog
            {
                SourceID  = source.SourceID,
                CrawlTime = DateTime.Now,
                Status    = "Running"
            };
            _db.CrawlLogs.Add(log);
            await _db.SaveChangesAsync();

            try
            {
                var http = _httpClientFactory.CreateClient("crawler");

                INewsCrawler crawler;

                if (source.SourceName.IndexOf("VnExpress",
                    StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    crawler = new VnExpressCrawler(_db, http);
                }
                else if (
                    source.SourceName.IndexOf("Tuổi Trẻ",
                        StringComparison.OrdinalIgnoreCase) >= 0 ||
                    source.SourceName.IndexOf("Tuoi Tre",
                        StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    crawler = new TuoiTreCrawler(_db, http);
                }
                else if (
                    source.SourceName.IndexOf("Dân Trí",
                        StringComparison.OrdinalIgnoreCase) >= 0 ||
                    source.SourceName.IndexOf("Dan Tri",
                        StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    crawler = new DanTriCrawler(_db, http);
                }
                else
                {
                    throw new NotSupportedException(
                        $"Không hỗ trợ nguồn: {source.SourceName}");
                }

                int count = await crawler.CrawlAsync();

                log.Status       = "Success";
                log.ArticleCount = count;
                Console.WriteLine(
                    $"[{source.SourceName}] Crawl được {count} bài");
            }
            catch (Exception ex)
            {
                log.Status       = "Failed";
                log.ErrorMessage = ex.Message;
                Console.WriteLine(
                    $"[{source.SourceName}] Lỗi: {ex.Message}");
            }
            finally
            {
                await _db.SaveChangesAsync();
            }
        }
    }
}