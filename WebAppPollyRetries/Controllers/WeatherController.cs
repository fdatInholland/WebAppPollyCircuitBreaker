using Microsoft.AspNetCore.Mvc;

namespace WebAppPollyRetries.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class WeatherController : ControllerBase
    {

        // HttpClient already has the retry pipeline bound to it
        private readonly WeatherApiClient _weatherApiClient;

        public WeatherController(WeatherApiClient client)
        {
            _weatherApiClient = client;
        }

        [HttpGet("{city}")]
        public async Task<IActionResult> GetCityWeather(string city, CancellationToken cancellationToken)
        {
            // 1. Call your client here. 
            // Polly's retry policy handles any transient network issues automatically under the hood.
            var weather = await _weatherApiClient.GetWeatherAsync(city, cancellationToken);

            if (weather == null)
            {
                return NotFound($"Could not find weather data for {city}.");
            }

            return Ok(weather);
        }
    }
}
