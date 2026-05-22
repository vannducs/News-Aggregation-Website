using System.Collections.Concurrent;

namespace NewsAggregator.Services
{
    public class WeatherInfo
    {
        public string City { get; set; } = "Hà Nội";
        public double Temperature { get; set; }
        public string Description { get; set; } = "";
        public string Icon { get; set; } = "🌡️";
    }

    public class CityCoordinate
    {
        public string Name { get; set; } = "";
        public double Lat { get; set; }
        public double Lon { get; set; }
    }

    public static class VietnamCities
    {
        public static readonly List<CityCoordinate> All =
        [
            new() { Name = "Hà Nội",     Lat = 21.0285, Lon = 105.8542 },
            new() { Name = "TP HCM",     Lat = 10.8231, Lon = 106.6297 },
            new() { Name = "Đà Nẵng",    Lat = 16.0544, Lon = 108.2022 },
            new() { Name = "Hải Phòng",  Lat = 20.8449, Lon = 106.6881 },
            new() { Name = "Cần Thơ",    Lat = 10.0452, Lon = 105.7469 },
            new() { Name = "An Giang",   Lat = 10.3860, Lon = 105.4200 },
            new() { Name = "Vũng Tàu",   Lat = 10.3460, Lon = 107.0843 },
            new() { Name = "Nha Trang",  Lat = 12.2388, Lon = 109.1967 },
            new() { Name = "Huế",        Lat = 16.4637, Lon = 107.5909 },
            new() { Name = "Đà Lạt",     Lat = 11.9404, Lon = 108.4583 },
            new() { Name = "Quy Nhơn",   Lat = 13.7765, Lon = 109.2237 },
            new() { Name = "Thanh Hóa",  Lat = 19.8000, Lon = 105.7667 },
            new() { Name = "Nghệ An",    Lat = 18.6796, Lon = 105.6813 },
            new() { Name = "Hà Tĩnh",    Lat = 18.3559, Lon = 105.8877 },
            new() { Name = "Quảng Ninh", Lat = 21.0064, Lon = 107.2925 },
        ];
    }

    public interface IWeatherService
    {
        Task<WeatherInfo> GetWeatherAsync(string cityName = "Hà Nội");
        List<CityCoordinate> GetCities();
    }

    public class WeatherService : IWeatherService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<WeatherService> _logger;

        private readonly ConcurrentDictionary<string, (WeatherInfo Info, DateTime Time)> _cache = new();
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

        public WeatherService(IHttpClientFactory httpClientFactory, ILogger<WeatherService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public List<CityCoordinate> GetCities() => VietnamCities.All;

        public async Task<WeatherInfo> GetWeatherAsync(string cityName = "Hà Nội")
        {
            if (_cache.TryGetValue(cityName, out var cached) &&
                DateTime.Now - cached.Time < CacheDuration)
                return cached.Info;

            var city = VietnamCities.All.FirstOrDefault(c =>
                string.Equals(c.Name, cityName, StringComparison.OrdinalIgnoreCase))
                ?? VietnamCities.All[0];

            var apiUrl = $"https://api.open-meteo.com/v1/forecast" +
                         $"?latitude={city.Lat}&longitude={city.Lon}" +
                         $"&current=temperature_2m,weathercode&timezone=Asia%2FBangkok";
            try
            {
                var client = _httpClientFactory.CreateClient();
                var json   = await client.GetStringAsync(apiUrl);

                using var doc = System.Text.Json.JsonDocument.Parse(json);
                var current   = doc.RootElement.GetProperty("current");
                var temp      = current.GetProperty("temperature_2m").GetDouble();
                var code      = current.GetProperty("weathercode").GetInt32();

                var info = new WeatherInfo
                {
                    City        = city.Name,
                    Temperature = Math.Round(temp),
                    Description = GetDescription(code),
                    Icon        = GetIcon(code),
                };

                _cache[city.Name] = (info, DateTime.Now);
                return info;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không lấy được thời tiết cho {City}", city.Name);
                return new WeatherInfo { City = city.Name, Temperature = 0, Icon = "🌡️", Description = "" };
            }
        }

        private static string GetIcon(int code) => code switch
        {
            0              => "☀️",
            1 or 2         => "⛅",
            3              => "☁️",
            45 or 48       => "🌫️",
            51 or 53 or 55 => "🌦️",
            61 or 63 or 65 => "🌧️",
            71 or 73 or 75 => "❄️",
            80 or 81 or 82 => "🌧️",
            95             => "⛈️",
            96 or 99       => "⛈️",
            _              => "🌡️",
        };

        private static string GetDescription(int code) => code switch
        {
            0              => "Trời quang",
            1 or 2         => "Ít mây",
            3              => "Nhiều mây",
            45 or 48       => "Sương mù",
            51 or 53 or 55 => "Mưa phùn",
            61 or 63 or 65 => "Có mưa",
            71 or 73 or 75 => "Có tuyết",
            80 or 81 or 82 => "Mưa rào",
            95             => "Có giông",
            96 or 99       => "Giông kèm mưa đá",
            _              => "",
        };
    }
}
