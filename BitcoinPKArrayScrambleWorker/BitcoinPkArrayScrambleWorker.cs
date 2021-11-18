using System;
using System.IO;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using BitcoinPKArrayScrambleWorker.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDbQueueService;
using MongoDbQueueService.Configuration;
using WorkerUtilitiesService;

namespace BitcoinPKArrayScrambleWorker
{
    public class BitcoinPkArrayScrambleWorker : BackgroundService
    {
        private readonly IScrambleService _scrambleService;
        private readonly IWorkerLifeCycleService _workerLifeCycleService;
        private readonly ILogger<BitcoinPkArrayScrambleWorker> _logger;
        private PublisherSettings _publisherSettings;
        private SubscriberSettings _subscriberSettings;

        public IPublisher _publisher;
        public ISubscriber _subscriber;

        private int _pkIntanceCount = 1;
        private int _pkCount = 1;

        public BitcoinPkArrayScrambleWorker(
            IScrambleService scrambleService, 
            IWorkerLifeCycleService workerLifeCycleService,
            ILogger<BitcoinPkArrayScrambleWorker> logger)
        {
            this._scrambleService = scrambleService;
            this._workerLifeCycleService = workerLifeCycleService;
            this._logger = logger;

            this.ReadConfigurations();

            this._logger.LogInformation($"Subscriber ConnectionString: {this._subscriberSettings.ConnectionString}");
            this._subscriber = new Subscriber(
                this._subscriberSettings.ConnectionString, 
                this._subscriberSettings.Database, 
                this._subscriberSettings.Queue, 
                this._subscriberSettings.WorkerName);

            this._logger.LogInformation($"Publisher ConnectionString: {this._publisherSettings.ConnectionString}");
            this._publisher = new Publisher(
                this._publisherSettings.ConnectionString, 
                this._publisherSettings.Database, 
                this._publisherSettings.Queue);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            this._logger.LogInformation($"Worker started at: {DateTimeOffset.Now}");

            await this._workerLifeCycleService
                .StartWorker()
                .ConfigureAwait(false);

            this._scrambleService.OnNewByteArray
                .Subscribe(async x => 
                {
                    // Save PK in stream
                    await this._publisher.Send(x);

                    this._pkIntanceCount ++;
                    this._pkCount ++;
                    var message = $"{this._pkIntanceCount.ToString("0000")} | {this._pkCount.ToString("0000")} [{x.ToDescription()}]";
                    await this._workerLifeCycleService
                        .SetWorkerProgress(new WorkerProgress(message))
                        .ConfigureAwait(false);
                    this._logger.LogInformation(message);
                });

            var queueSubscriber = this._subscriber
                .SubscribeQueueCollection<byte[]>(stoppingToken)
                .Subscribe(x => 
                {
                    this.ScramblePK(x);
                });
            

            while(!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000);
            }

            var stopMessage = string.Format($"Worker stopped at: {DateTimeOffset.Now}");

            await this._workerLifeCycleService
                .SetWorkerProgress(new WorkerProgress(stopMessage))
                .ConfigureAwait(false);
            this._logger.LogInformation(stopMessage);
            queueSubscriber.Dispose();
        }

        private void ReadConfigurations()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .Build();

            this._publisherSettings = new PublisherSettings();
            configuration.Bind("publisherSettings", this._publisherSettings);

            this._subscriberSettings = new SubscriberSettings();
            configuration.Bind("SubscriberSettings", this._subscriberSettings);
        }

        private void ScramblePK(byte[] source)
        {
            this._logger.LogInformation($"Start scrambling array [{source.ToDescription()}]");
            this._pkCount = 1;
            this._scrambleService.Scramble(source);
        }
    }
}