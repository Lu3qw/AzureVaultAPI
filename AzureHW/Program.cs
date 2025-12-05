using System;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AzureHW.Services;
using System.Threading.Tasks;

namespace AzureHW
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = new Microsoft.Extensions.Hosting.HostBuilder();

            builder.ConfigureServices((context, services) =>
            {

                services.AddHttpClient();


                services.AddSingleton<ICurrencyService, FxRatesApiService>();
                services.AddSingleton<Functions>();
            });

            builder.ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
            });

            var host = builder.Build();

            await host.RunAsync();
        }
    }
}