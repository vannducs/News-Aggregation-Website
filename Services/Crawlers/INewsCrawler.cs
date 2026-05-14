namespace NewsAggregator.Services.Crawlers
{
    public interface INewsCrawler
    {
        Task<int> CrawlAsync();
    }
}