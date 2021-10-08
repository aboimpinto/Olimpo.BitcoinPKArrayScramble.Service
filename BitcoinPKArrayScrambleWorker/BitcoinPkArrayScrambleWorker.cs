using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BitcoinPKArrayScrambleWorker
{
    public class BitcoinPkArrayScrambleWorker : BackgroundService
    {
        private readonly ILogger<BitcoinPkArrayScrambleWorker> _logger;

        public BitcoinPkArrayScrambleWorker(ILogger<BitcoinPkArrayScrambleWorker> logger)
        {
            this._logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            this._logger.LogInformation("Worker started at: {time}", DateTimeOffset.Now);

            while(!stoppingToken.IsCancellationRequested)
            {
                this._logger.LogInformation("Doing some work: {time}", DateTimeOffset.Now);
                await Task.Delay(1000);
            }

            this._logger.LogInformation("Worker stopped at: {time}", DateTimeOffset.Now);
        }
    }
}