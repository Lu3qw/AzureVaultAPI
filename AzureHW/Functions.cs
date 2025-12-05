using Azure.Data.Tables;
using AzureHW.Models;
using AzureHW.Services;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AzureHW
{
    public class Functions
    {
        private readonly ICurrencyService _currencyService;
        private readonly IConfiguration _configuration;
        private readonly TableClient _tableClient;

        public Functions(
            ICurrencyService currencyService,
            IConfiguration configuration)
        {
            _currencyService = currencyService;
            _configuration = configuration;

            var connectionString = _configuration.GetConnectionString("TableStorage");
            var tableName = _configuration["Settings:TableName"];

            _tableClient = new TableClient(connectionString, tableName);
            _tableClient.CreateIfNotExists();
        }

        /// 
        /// Функція, що виконується щогодини
        /// CRON вираз: "0 0 * * * *" - кожну годину о 0 хвилин
        /// 
        [FunctionName("UpdateCurrencyRatesHourly")]
        public async Task UpdateCurrencyRatesHourly(
            [TimerTrigger("0 * * * * *")] TimerInfo timer,
            ILogger logger)
        {
            logger.LogInformation($"Початок оновлення курсів валют: {DateTime.Now}");

            try
            {
                var baseCurrency = _configuration["Settings:BaseCurrency"];
                var targetCurrencies = _configuration["Settings:TargetCurrencies"];

                logger.LogInformation($"Базова валюта: {baseCurrency}");
                logger.LogInformation($"Цільові валюти: {targetCurrencies}");

 
                Dictionary<string, double> rates;

                try
                {
                    rates = await _currencyService.GetExchangeRatesAsync(
                        baseCurrency,
                        targetCurrencies);
                }
                catch (Exception ex)
                {
                    logger.LogWarning($"Основний API недоступний: {ex.Message}");
                    logger.LogInformation("Використовуємо альтернативний API...");

                    if (_currencyService is FxRatesApiService service)
                    {
                        rates = await service.GetExchangeRatesFromAlternativeApiAsync(baseCurrency);
                    }
                    else
                    {
                        throw;
                    }
                }

                if (rates == null || rates.Count == 0)
                {
                    logger.LogWarning("Не отримано курсів валют");
                    return;
                }

                var currentDateTime = DateTime.UtcNow;
                var savedCount = 0;

                foreach (var rate in rates)
                {
                    try
                    {
                        var entity = new CurrencyRateEntity(
                            baseCurrency,
                            rate.Key,
                            rate.Value,
                            currentDateTime);

                        await _tableClient.UpsertEntityAsync(entity);
                        savedCount++;

                        logger.LogInformation(
                            $"Збережено: {baseCurrency}/{rate.Key} = {rate.Value:F4}");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex,
                            $"Помилка при збереженні курсу {rate.Key}");
                    }
                }

                logger.LogInformation(
                    $"Успішно збережено {savedCount} курсів валют в Azure Table Storage");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Критична помилка при оновленні курсів валют");
                throw;
            }
        }


        [FunctionName("ManualUpdateCurrencyRates")]
        [NoAutomaticTrigger]
        public async Task ManualUpdateCurrencyRates(
            ILogger logger)
        {
            logger.LogInformation("Ручне оновлення курсів валют");
            await UpdateCurrencyRatesHourly(null, logger);
        }

        [FunctionName("CleanupOldRates")]
        public async Task CleanupOldRates(
            [TimerTrigger("0 0 2 * * *")] TimerInfo timer,
            ILogger logger)
        {
            logger.LogInformation($"Початок очищення старих курсів: {DateTime.Now}");

            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-90);
                var cutoffPartitionKey = cutoffDate.ToString("yyyy-MM");

                logger.LogInformation($"Видалення даних старіших за: {cutoffDate}");

                var query = _tableClient.QueryAsync<CurrencyRateEntity>(
                    e => e.PartitionKey.CompareTo(cutoffPartitionKey) < 0);

                var deletedCount = 0;

                await foreach (var entity in query)
                {
                    await _tableClient.DeleteEntityAsync(
                        entity.PartitionKey,
                        entity.RowKey);
                    deletedCount++;
                }

                logger.LogInformation($"Видалено {deletedCount} старих записів");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Помилка при очищенні старих даних");
            }
        }
    }
}