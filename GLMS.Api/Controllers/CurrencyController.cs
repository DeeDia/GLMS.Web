using Microsoft.AspNetCore.Mvc;

namespace GLMS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CurrencyController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;

        public CurrencyController(
            IHttpClientFactory httpClientFactory,
            IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
        }

        // GET: api/currency/rate
        // Returns the current USD to ZAR exchange rate
        [HttpGet("rate")]
        public async Task<IActionResult> GetUsdToZarRate()
        {
            try
            {
                var apiKey = _config["ExchangeRate:ApiKey"];
                var baseUrl = _config["ExchangeRate:BaseUrl"];
                var url = $"{baseUrl}{apiKey}/latest/USD";

                var client = _httpClientFactory.CreateClient();
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();

                // Parse the rate from the JSON response
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                var root = doc.RootElement;
                var rates = root.GetProperty("conversion_rates");
                var zarRate = rates.GetProperty("ZAR").GetDecimal();

                return Ok(new { rate = zarRate, baseCurrency = "USD", targetCurrency = "ZAR" });
            }
            catch
            {
                // Return fallback rate if API is unreachable
                return Ok(new
                {
                    rate = 18.50m,
                    baseCurrency = "USD",
                    targetCurrency = "ZAR",
                    note = "Fallback rate — API unavailable"
                });
            }
        }
    }
}