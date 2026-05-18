using Microsoft.AspNetCore.Mvc;
using NewsAggregator.Services;

namespace NewsAggregator.Controllers.Api
{
    [ApiController]
    [Route("api/weather")]
    public class WeatherApiController(IWeatherService weatherService) : ControllerBase
    {
        [HttpGet("cities")]
        public IActionResult GetCities()
        {
            var cities = weatherService.GetCities()
                .Select(c => new { name = c.Name, lat = c.Lat, lon = c.Lon });
            return Ok(cities);
        }

        [HttpGet]
        public async Task<IActionResult> GetWeather([FromQuery] string city = "Hà Nội")
        {
            var weather = await weatherService.GetWeatherAsync(city);
            return Ok(new
            {
                city        = weather.City,
                temperature = weather.Temperature,
                icon        = weather.Icon,
                description = weather.Description,
            });
        }
    }
}
