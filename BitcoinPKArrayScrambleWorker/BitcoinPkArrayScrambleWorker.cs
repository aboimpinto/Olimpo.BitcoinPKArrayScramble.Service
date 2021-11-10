using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using BitcoinPKArrayScrambleWorker.Configuration;
using BitcoinPKArrayScrambleWorker.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDbQueueService;

namespace BitcoinPKArrayScrambleWorker
{
    public class BitcoinPkArrayScrambleWorker : BackgroundService
    {
        private readonly IScrambleService _scrambleService;
        private readonly ILogger<BitcoinPkArrayScrambleWorker> _logger;
        
        public IPublisher _publisher;
        public ISubscriber _subscriber;

        private int _pkCount = 1;

        public BitcoinPkArrayScrambleWorker(
            IScrambleService scrambleService, 
            SubscriberSettings subscriberSettings,
            PublisherSettings publisherSettings,
            WorkerSettings workerSettings,
            ILogger<BitcoinPkArrayScrambleWorker> logger)
        {
            this._scrambleService = scrambleService;
            this._logger = logger;

            this._subscriber = new Subscriber(subscriberSettings.ConnectionString, subscriberSettings.Database, subscriberSettings.Queue, "WORKER_1");
            this._publisher = new Publisher(publisherSettings.ConnectionString, publisherSettings.Database, publisherSettings.Queue);

            this._scrambleService.OnNewByteArray
                .Subscribe(x => 
                {
                    // Save PK in stream
                    this._publisher.Send(x);

                    this._pkCount ++;
                    this._logger.LogInformation($"{this._pkCount.ToString("0000")} [{x.ToDescription()}]");
                });
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            this._logger.LogInformation("Worker started at: {time}", DateTimeOffset.Now);

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

            this._logger.LogInformation("Worker stopped at: {time}", DateTimeOffset.Now);
            queueSubscriber.Dispose();
        }

        private void ScramblePK(byte[] source)
        {
            this._logger.LogInformation($"Start scrambling array [{source.ToDescription()}]");
            this._pkCount = 1;
            this._scrambleService.Scramble(source);
        }
    }
}