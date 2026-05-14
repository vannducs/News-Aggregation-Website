using Microsoft.EntityFrameworkCore;
using NewsAggregator.Data;
using NewsAggregator.Models;
using NewsAggregator.Services.Crawlers;

namespace NewsAggregator.Services
{
    public class CrawlerService(AppDbContext db, IHttpClientFactory httpClientFactory)
    {
        public async Task RunAllAsync()
        {
            Console.WriteLine($"[Crawler] Bắt đầu crawl lúc {DateTime.Now}");

            var sources = await db.Sources
                .Where(s => s.IsActive && !s.IsDeleted)
                .ToListAsync();

            foreach (var source in sources)
                await CrawlSourceAsync(source);

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
            db.CrawlLogs.Add(log);
            await db.SaveChangesAsync();

            try
            {
                var http = httpClientFactory.CreateClient("crawler");

                INewsCrawler crawler = source.SourceName.Contains("VnExpress", StringComparison.OrdinalIgnoreCase)
                    ? new VnExpressCrawler(db, http)
                    : source.SourceName.Contains("Tuổi Trẻ", StringComparison.OrdinalIgnoreCase)
                      || source.SourceName.Contains("Tuoi Tre", StringComparison.OrdinalIgnoreCase)
                        ? new TuoiTreCrawler(db, http)
                        : source.SourceName.Contains("Dân Trí", StringComparison.OrdinalIgnoreCase)
                          || source.SourceName.Contains("Dan Tri", StringComparison.OrdinalIgnoreCase)
                            ? new DanTriCrawler(db, http)
                            : new GenericRssCrawler(db, http, source);

                int count = await crawler.CrawlAsync();

                log.Status       = "Success";
                log.ArticleCount = count;
                Console.WriteLine($"[{source.SourceName}] Crawl được {count} bài");
            }
            catch (Exception ex)
            {
                log.Status       = "Failed";
                log.ErrorMessage = ex.Message;
                Console.WriteLine($"[{source.SourceName}] Lỗi: {ex.Message}");
            }
            finally
            {
                await db.SaveChangesAsync();
            }
        }

        public async Task CrawlSourceByIdAsync(int sourceId)
        {
            var source = await db.Sources.FindAsync(sourceId);
            if (source == null || !source.IsActive || source.IsDeleted) return;

            Console.WriteLine($"[CrawlNow] Bắt đầu crawl {source.SourceName}");
            await CrawlSourceAsync(source);
            Console.WriteLine($"[CrawlNow] Hoàn thành crawl {source.SourceName}");
        }
    }
}
