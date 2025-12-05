using System.Collections.Generic;
using System.Threading.Tasks;

namespace AzureHW.Services
{
    public interface ICurrencyService
    {
        Task<Dictionary<string, double>> GetExchangeRatesAsync(string baseCurrency, string targetCurrencies);
    }
}