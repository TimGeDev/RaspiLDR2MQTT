using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace RaspiLDR2MQTT;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var host = new HostBuilder()
            .ConfigureAppConfiguration(configApp =>
            {
                configApp.SetBasePath(Directory.GetCurrentDirectory());
                configApp.AddJsonFile("appsettings.json", optional: true);
                configApp.AddCommandLine(args);
            })
            .ConfigureServices((hostContext, services) =>
            {
                services.AddLogging();
                services.AddSingleton<MQTTService>();
                services.AddHostedService<LdrService>();
            })
            .ConfigureLogging((hostContext, configLogging) => { configLogging.AddConsole(); })
            .UseConsoleLifetime()
            .Build();

        await host.RunAsync();
    }
}