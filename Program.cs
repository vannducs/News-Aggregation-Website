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
builder.Services.AddScoped<PasswordService>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.Name = "NewsAggregator.Auth";
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    connectionString = "Server=.\\SQLEXPRESS;Database=NewsAggregatorDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

await SeedData.InitializeAsync(app.Services);

if (app.Environment.IsDevelopment())
{
    app.Lifetime.ApplicationStarted.Register(() =>
    {
        Task.Run(async () =>
        {
            await Task.Delay(1000);

            var serverAddresses = app.Urls.Any()
                ? app.Urls
                : app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()?.Addresses
                    ?? Array.Empty<string>();

            var targetUrl = serverAddresses
                .OrderByDescending(address => address.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(targetUrl))
            {
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = targetUrl,
                    UseShellExecute = true
                });
            }
            catch
            {
                // Ignore browser launch failures and keep the app running.
            }
        });
    });
}

app.Run();
