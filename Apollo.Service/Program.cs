using Apollo.Service.Bluetooth;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Apollo.Service
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = Host.CreateDefaultBuilder(args);
            string serviceUrl = args.FirstOrDefault() ?? Environment.GetEnvironmentVariable("APOLLO_SERVICE_URL");

            builder.UseWindowsService(win => win.ServiceName = "Apollo Light Service");
            builder.ConfigureServices((host, services) =>
            {
                services.AddHostedService<Worker>();
                services.AddSingleton(new ApolloAdvertisementWatcher());
                services.Configure<ApolloOptions>(options =>
                    options.ServiceURL = serviceUrl);
            });

            builder.ConfigureLogging(builder => builder
                .AddConsole()
                .AddFile("Logs/Apollo.Service-{Date}.txt")
            );

            IHost host = builder.Build();
            await host.RunAsync();
        }
    }
}