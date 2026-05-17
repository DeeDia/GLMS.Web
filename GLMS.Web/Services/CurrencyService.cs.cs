using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace GLMS.Web.Services
{
    // Describes the shape of the JSON the API sends back
    public class ExchangeRateResponse
    {
        public string Result { get; set; } = string.Empty;

        // Maps currency codes to rates e.g. "ZAR" -> 18.50
        public Dictionary<string, decimal> Conversion_Rates { get; set; }
            = new Dictionary<string, decimal>();
    }

    public class CurrencyService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public CurrencyService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
        }

        // Gets the live USD to ZAR rate from the API
        public async Task<decimal> GetUsdToZarRateAsync()
        {
            try
            {
                var apiKey = _config["ExchangeRate:ApiKey"];
                var baseUrl = _config["ExchangeRate:BaseUrl"];

                var url = $"{baseUrl}{apiKey}/latest/USD";

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();

                var data = JsonSerializer.Deserialize<ExchangeRateResponse>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                if (data != null &&
                    data.Conversion_Rates.ContainsKey("ZAR"))
                {
                    return data.Conversion_Rates["ZAR"];
                }

                // Fallback rate if ZAR not found in response
                return 18.50m;
            }
            catch
            {
                // Fallback rate if API is down or no internet
                return 18.50m;
            }
        }

        // Multiplies USD amount by the rate to get ZAR
        public decimal ConvertUsdToZar(decimal usdAmount, decimal rate)
        {
            return Math.Round(usdAmount * rate, 2);
        }
    }
}