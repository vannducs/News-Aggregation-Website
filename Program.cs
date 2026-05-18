using Hangfire;
using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;
using NewsAggregator.Data;
using NewsAggregator.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// DATABASE — chỉ đăng ký 1 lần
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// HTTP CLIENT — crawl (cùng handler với ArticleExtractor để tránh lỗi gzip 0x1F)
builder.Services.AddHttpClient("crawler", client =>
{
    client.DefaultRequestHeaders.Add("User-Agent",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
    client.DefaultRequestHeaders.Add("Accept-Language", "vi-VN,vi;q=0.9,en;q=0.8");
    client.Timeout = TimeSpan.FromSeconds(30);
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    AutomaticDecompression = DecompressionMethods.GZip
                           | DecompressionMethods.Deflate
                           | DecompressionMethods.Brotli,
    AllowAutoRedirect = true,
    MaxAutomaticRedirections = 5,
});

// HTTP CLIENT — universal article extractor (với auto decompression để tránh lỗi 0x1F gzip)
builder.Services.AddHttpClient("ArticleExtractor", client =>
{
    client.DefaultRequestHeaders.Add("User-Agent",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
    client.DefaultRequestHeaders.Add("Accept",
        "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
    client.DefaultRequestHeaders.Add("Accept-Language", "vi-VN,vi;q=0.9,en;q=0.8");
    client.Timeout = TimeSpan.FromSeconds(20);
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    AutomaticDecompression = DecompressionMethods.GZip
                           | DecompressionMethods.Deflate
                           | DecompressionMethods.Brotli,
    AllowAutoRedirect = true,
    MaxAutomaticRedirections = 5,
});

// SERVICES
builder.Services.AddScoped<CrawlerService>();
builder.Services.AddScoped<PasswordService>();
builder.Services.AddScoped<TrashPurgeService>();
builder.Services.AddScoped<PostImageFixService>();
builder.Services.AddScoped<IUniversalArticleExtractorService, UniversalArticleExtractorService>();
builder.Services.AddSingleton<IWeatherService, WeatherService>();

// HANGFIRE
builder.Services.AddHangfire(config =>
    config.UseInMemoryStorage());
builder.Services.AddHangfireServer();

// AUTHENTICATION
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.Name = "NewsAggregator.Auth";
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// MIDDLEWARE PIPELINE
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Authentication phải trước Authorization
app.UseAuthentication();
app.UseAuthorization();

// HANGFIRE — chỉ Admin mới truy cập được dashboard
app.UseHangfireDashboard("/hangfire", new Hangfire.DashboardOptions
{
    Authorization = new[] { new HangfireAdminAuthFilter() }
});
RecurringJob.AddOrUpdate<CrawlerService>(
    "crawl-all-sources",
    service => service.RunAllAsync(),
    "*/30 * * * *"
);
// Tự động xóa vĩnh viễn thùng rác sau 30 ngày — chạy mỗi ngày lúc 3:00 sáng
RecurringJob.AddOrUpdate<TrashPurgeService>(
    "purge-trash",
    service => service.PurgeOldDeletedItemsAsync(),
    "0 3 * * *"
);

// SEED DATA
await SeedData.InitializeAsync(app.Services);

// ROUTING
app.MapControllers(); // attribute-routed API controllers (e.g. /api/weather)
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=News}/{action=Index}/{id?}")
    .WithStaticAssets();

// TỰ MỞ TRÌNH DUYỆT KHI DEV
if (app.Environment.IsDevelopment())
{
    app.Lifetime.ApplicationStarted.Register(() =>
    {
        Task.Run(async () =>
        {
            await Task.Delay(1000);

            var serverAddresses = app.Urls.Any()
                ? app.Urls
                : app.Services.GetRequiredService<IServer>()
                    .Features.Get<IServerAddressesFeature>()?.Addresses
                    ?? Array.Empty<string>();

            var targetUrl = serverAddresses
                .OrderByDescending(a => a.StartsWith("https://",
                    StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(targetUrl))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = targetUrl,
                        UseShellExecute = true
                    });
                }
                catch { }
            }
        });
    });
}

app.Run();