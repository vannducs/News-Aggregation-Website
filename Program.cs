using Hangfire;
using System.Diagnostics;
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

// HTTP CLIENT — crawl
builder.Services.AddHttpClient("crawler", client =>
{
    client.DefaultRequestHeaders.Add(
        "User-Agent",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
        "AppleWebKit/537.36 (KHTML, like Gecko) " +
        "Chrome/120.0.0.0 Safari/537.36");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// SERVICES
builder.Services.AddScoped<CrawlerService>();
builder.Services.AddScoped<PasswordService>();

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

// HANGFIRE
app.UseHangfireDashboard("/hangfire");
RecurringJob.AddOrUpdate<CrawlerService>(
    "crawl-all-sources",
    service => service.RunAllAsync(),
    "*/30 * * * *"
);

// SEED DATA
await SeedData.InitializeAsync(app.Services);

// ROUTING
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