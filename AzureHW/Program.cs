using System;
using System.IO;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.WebJobs;
using AzureHW.Services;

namespace AzureHW
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = new HostBuilder();

            // Налаштування Configuration
            builder.ConfigureAppConfiguration((context, config) =>
            {
                var currentDir = Directory.GetCurrentDirectory();
                var projectDir = Path.GetFullPath(Path.Combine(currentDir, "..", "..", ".."));

                Console.WriteLine($"Current Directory: {currentDir}");
                Console.WriteLine($"Project Directory: {projectDir}");

                // Спробуємо знайти appsettings.json
                var settingsPath = Path.Combine(currentDir, "appsettings.json");
                if (!File.Exists(settingsPath))
                {
                    settingsPath = Path.Combine(projectDir, "appsettings.json");
                    Console.WriteLine($"Looking in project dir: {settingsPath}");
                }

                if (File.Exists(settingsPath))
                {
                    config.SetBasePath(Path.GetDirectoryName(settingsPath))
                          .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                }
                else
                {
                    throw new FileNotFoundException($"appsettings.json not found! Searched: {settingsPath}");
                }

                config.AddEnvironmentVariables();
            });

            // Налаштування WebJobs
            builder.ConfigureWebJobs(b =>
            {
                b.AddAzureStorageCoreServices();
                b.AddTimers();
            });

            // Налаштування DI
            builder.ConfigureServices((context, services) =>
            {
                services.AddHttpClient();
                services.AddSingleton(context.Configuration);
            });

            // Налаштування Logging
            builder.ConfigureLogging((context, logging) =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
            });

            var host = builder.Build();

            using (host)
            {
                await host.RunAsync();
            }
        }
    }
}