using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BitcoinPKArrayScrambleWorker.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Olimpo.RedisProvider;
using StackExchange.Redis;

namespace BitcoinPKArrayScrambleWorker
{
    public class BitcoinPkArrayScrambleWorker : BackgroundService
    {
        private readonly IDatabaseAsync _databaseAsync;
        private readonly IDatabase _database;

        private const string WorkerName = "Worker1";
        private const string GroupName = "NewGroup";
        private const string IncomeStreamName = "BitcoinPkArrayGenerator";
        private const string OutcomeStreamName = "BitcoinPkArrayScrambled";
        private readonly IScrambleService _scrambleService;
        private readonly ILogger<BitcoinPkArrayScrambleWorker> _logger;

        private int _pkCount = 1;

        public BitcoinPkArrayScrambleWorker(IScrambleService scrambleService, ILogger<BitcoinPkArrayScrambleWorker> logger)
        {
            this._scrambleService = scrambleService;
            this._logger = logger;

            var multiplexer = ConnectionMultiplexer.Connect("127.0.0.1:6379,abortConnect=false");
            this._databaseAsync = multiplexer.GetDatabase(0);
            this._database = multiplexer.GetDatabase(0);

            this._scrambleService.OnNewByteArray
                .Subscribe(x => 
                {
                    var message = JsonSerializer.Serialize(x);
                    this._databaseAsync.StreamAddAsync(OutcomeStreamName, "Message", message);
                    this._pkCount ++;

                    if (this._pkCount % 10000000 == 0)
                    {
                        this._logger.LogInformation($"{this._pkCount.ToString("00000000000000")} [{x.ToDescription()}]");
                    }
                });
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            this._logger.LogInformation("Worker started at: {time}", DateTimeOffset.Now);

            await this.TryCreateStreamGroup(IncomeStreamName, GroupName);

            try
            {
                var couldCreateConsumerGroup = await this._databaseAsync
                    .StreamCreateConsumerGroupAsync(OutcomeStreamName, GroupName, StreamPosition.Beginning, true)
                    .ConfigureAwait(false);
            }
            catch
            {
                // user group already created
            }

            await this.TryCreateStreamGroup(OutcomeStreamName, GroupName);

            var streamInfo = await this._databaseAsync.StreamInfoAsync(IncomeStreamName);
            this._logger.LogInformation($"Stream: {IncomeStreamName} | Count: {streamInfo.Length}");

            await this.ProcessPending();

            var channelStreamer = this.CreateObservableStream<byte[]>(stoppingToken)
                .Subscribe(x => 
                {
                    this.ScramblePK(x.Message);
                });


            while(!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000);
            }

            this._logger.LogInformation("Worker stopped at: {time}", DateTimeOffset.Now);
            channelStreamer.Dispose();
        }

        private void ScramblePK(byte[] source)
        {
            this._logger.LogInformation($"Start scrambling array [{source.ToDescription()}]");
            this._scrambleService.Scramble(source);
        }

        private async Task TryCreateStreamGroup(string streamName, string groupName)
        {
            if (await this.IsGroupCreated(streamName, groupName))
            {
                return;
            }
            var couldCreateConsumerGroup = await this._databaseAsync
                .StreamCreateConsumerGroupAsync(streamName, groupName, StreamPosition.Beginning, true)
                .ConfigureAwait(false);
        }

        private async Task<bool> IsGroupCreated(string streamName, string groupName)
        {
            var groupsInfo = await this._databaseAsync
                .StreamGroupInfoAsync(streamName);

            foreach(var group in groupsInfo)
            {
                if (group.Name == groupName)
                {
                    return true;
                }   
            }

            return false;
        } 

        private async Task ProcessPending()
        {
            var pendingInfo = await this._databaseAsync
                .StreamPendingAsync(IncomeStreamName, GroupName)
                .ConfigureAwait(false);

            if (pendingInfo.PendingMessageCount > 0)
            {
                var pendingMessages = await this._databaseAsync
                    .StreamPendingMessagesAsync(
                        IncomeStreamName, 
                        GroupName, 
                        pendingInfo.PendingMessageCount, 
                        WorkerName,
                        minId: pendingInfo.LowestPendingMessageId,
                        maxId: pendingInfo.HighestPendingMessageId)
                    .ConfigureAwait(false);  

                this._logger.LogInformation($"Pending messages {pendingMessages.Count()}.");
                var currentMessageCount = 1;
                foreach(var message in pendingMessages)
                {
                    var claimMessage = await this._databaseAsync
                        .StreamClaimAsync(
                            IncomeStreamName, 
                            GroupName, 
                            WorkerName,
                            minIdleTimeInMs: 0, 
                            messageIds: new[] { message.MessageId })
                        .ConfigureAwait(false);

                    var pkArray = JsonSerializer.Deserialize<byte[]>(claimMessage.First().Values.First().Value);

                    this._logger.LogInformation($"Start scrambling pending message {currentMessageCount}/{pendingMessages.Count()}");
                    this.ScramblePK(pkArray);
                    this._logger.LogInformation($"End scrambling pending message {currentMessageCount}/{pendingMessages.Count()}");

                    await this._databaseAsync.StreamAcknowledgeAsync(IncomeStreamName, GroupName, message.MessageId);
                }  
            }
        }

        private IObservable<RedisStreamItem<T>> CreateObservableStream<T>(CancellationToken cancellationToken)
        {
            var scheduleInstance = ThreadPoolScheduler.Instance;

            return Observable.Create<RedisStreamItem<T>>(obs => 
            {
                var disposable = Observable
                    .Interval(TimeSpan.FromMilliseconds(200), scheduleInstance)
                    .Subscribe(async _ => 
                    {
                        var message = await this._databaseAsync
                            .StreamReadGroupAsync(
                                IncomeStreamName, 
                                GroupName,
                                WorkerName, 
                                StreamPosition.NewMessages,
                                count: 1)
                            .ConfigureAwait(false);

                        if (message.Any())
                        {
                            var redisStreamItem = new RedisStreamItem<T>(message.First()); 

                            try
                            {
                                 obs.OnNext(redisStreamItem);
                                 // await this._databaseAsync.StreamAcknowledgeAsync(IncomeStreamName, GroupName, message.First().Id);
                            }
                            catch
                            {
                                throw;
                            }
                        }
                        
                    });
                cancellationToken.Register(() => disposable.Dispose());

                return Disposable.Empty;
            });
        }
    }
}