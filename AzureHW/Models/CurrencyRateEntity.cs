using Azure;
using Azure.Data.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureHW.Models
{
    public class CurrencyRateEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = default!;
        public string RowKey { get; set; } = default!;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string BaseCurrency { get; set; } = "UAH";
        public string TargetCurrency { get; set; } = default!;
        public double ExchangeRate { get; set; }
        public DateTime RateDateTime { get; set; }
        public string Source { get; set; } = "FxRatesAPI";

        public string RateType { get; set; } = "Mid"; // Mid, Bid, Ask
        public double? PreviousRate { get; set; }
        public double? ChangePercent { get; set; }

        public CurrencyRateEntity()
        {
        }

        public CurrencyRateEntity(string baseCurrency, string targetCurrency, double rate, DateTime dateTime)
        {
            BaseCurrency = baseCurrency;
            TargetCurrency = targetCurrency;
            ExchangeRate = rate;
            RateDateTime = dateTime;

            PartitionKey = dateTime.ToString("yyyy-MM");

            RowKey = $"{dateTime:yyyy-MM-ddTHH:mm:ss}-{targetCurrency}";
        }
    }
}