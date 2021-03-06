// See https://aka.ms/new-console-template for more information

using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CemantixFinder;

static class Program
{
    static async Task<int> Main(string[] args)
    {
        string? word = "";
        while (string.IsNullOrEmpty(word))
        {
            Console.WriteLine("Choisissez un mot de départ:");
            word = Console.ReadLine();
        }
        
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((_, services) =>
            {
                services.AddTransient<ILexicalFieldFinder, FrenchLexicalFieldFinder>();
                services.AddHttpClient(FrenchLexicalFieldFinder.HttpClientName).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                });
                services.AddHttpClient(GameClient.ClientName).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                });
                services.AddTransient<Runner>();
                services.AddTransient<GameClient>();
                services.AddHttpClient();
            }).ConfigureLogging((_, logging) =>
            {
                logging.ClearProviders();
                logging.AddSimpleConsole(options =>
                {
                    options.IncludeScopes = false;
                });
                logging.SetMinimumLevel(LogLevel.Warning);
            })
            .Build();


        using (var serviceScope = host.Services.CreateScope())
        {
            var services = serviceScope.ServiceProvider;

            var myService = services.GetRequiredService<Runner>();
            await myService.Run(word);
        }

        Console.ReadLine();
        return 0;
    }
}