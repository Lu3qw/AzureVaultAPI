using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace AzureHW.Services
{
    public class FxRatesApiService : ICurrencyService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<FxRatesApiService> _logger;
        private readonly string _apiKey;
        private readonly string _baseUrl;

        public FxRatesApiService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<FxRatesApiService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _apiKey = _configuration["FxRatesApi:ApiKey"];
            _baseUrl = _configuration["FxRatesApi:BaseUrl"]?.TrimEnd('/');
        }

        public async Task<Dictionary<string, double>> GetExchangeRatesAsync(
            string baseCurrency,
            string targetCurrencies)
        {
            try
            {
                var url = $"{_baseUrl}/latest?base={baseCurrency}";

                if (!string.IsNullOrEmpty(targetCurrencies))
                {
                    url += $"&currencies={targetCurrencies}";
                }

                if (!string.IsNullOrEmpty(_apiKey))
                {
                    url += $"&api_key={_apiKey}";
                }

                _logger.LogInformation($"Запит до API: {url}");

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Відповідь API: {content}");

                var jsonResponse = JObject.Parse(content);

                if (jsonResponse["success"] != null && jsonResponse["success"]!.Value<bool>() == false)
                {
                    var error = jsonResponse["error"]?["message"]?.ToString();
                    throw new Exception($"API помилка: {error}");
                }

                var rates = new Dictionary<string, double>();
                var ratesObject = jsonResponse["rates"] as JObject;

                if (ratesObject != null)
                {
                    foreach (var prop in ratesObject.Properties())
                    {
                        if (double.TryParse(prop.Value.ToString(), out var value))
                        {
                            rates[prop.Name] = value;
                        }
                    }
                }

                _logger.LogInformation($"Отримано {rates.Count} курсів валют");
                return rates;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при отриманні курсів валют");
                throw;
            }
        }

        public async Task<Dictionary<string, double>> GetExchangeRatesFromAlternativeApiAsync(
            string baseCurrency)
        {
            try
            {
                var url = $"https://open.er-api.com/v6/latest/{baseCurrency}";

                _logger.LogInformation($"Запит до альтернативного API: {url}");

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var jsonResponse = JObject.Parse(content);

                if (jsonResponse["result"]?.Value<string>() != "success")
                {
                    throw new Exception("API повернув помилку");
                }

                var rates = new Dictionary<string, double>();
                var ratesObject = jsonResponse["rates"] as JObject;

                if (ratesObject != null)
                {
                    foreach (var prop in ratesObject.Properties())
                    {
                        if (double.TryParse(prop.Value.ToString(), out var value))
                        {
                            rates[prop.Name] = value;
                        }
                    }
                }

                return rates;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при роботі з альтернативним API");
                throw;
            }
        }
    }
}