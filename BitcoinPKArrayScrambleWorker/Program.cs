using BitcoinPKArrayScrambleWorker.Configuration;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BitcoinPKArrayScrambleWorker
{
    class Program
    {
        static void Main(string[] args)
        {
            CreateHostBuilder(args)
                .Build()
                .Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSystemd()
                .ConfigureLogging( x =>
                {
                    x.ClearProviders();
                    x.AddConsole();
                    x.AddDebug();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    CollectConfigurations(services);

                    services.AddSingleton<IScrambleService, ScrambleService>();
                    
                    services.AddHostedService<BitcoinPkArrayScrambleWorker>();
                });

        private static void CollectConfigurations(IServiceCollection services)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .Build();

            var publisherSettings = new PublisherSettings();
            configuration.Bind("publisherSettings", publisherSettings);
            services.AddSingleton(publisherSettings);

            var subscriberSettings = new SubscriberSettings();
            configuration.Bind("SubscriberSettings", subscriberSettings);
            services.AddSingleton(subscriberSettings);

            var workerSettings = new WorkerSettings();
            configuration.Bind("WorkerSettings", workerSettings);
            services.AddSingleton(workerSettings);
        }
    }
}
