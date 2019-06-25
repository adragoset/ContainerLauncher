using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Launcher.Services
{
    public class HookLoggingService : IHostedService, IDisposable
    {
        private readonly ILogger<HookLoggingService> _logger;
        private readonly CancellationTokenSource cancelToken;
        private Task _timer;
        private AggregateProcessHookService procHooks;

        public HookLoggingService(AggregateProcessHookService procHooks, ILogger<HookLoggingService> _logger)
        {
            this.procHooks = procHooks;
            this._logger = _logger;
            this.cancelToken = new CancellationTokenSource();
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Log passthrough Service is stopping.");
            cancelToken.Cancel();
            return _timer;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
            {
                while (true)
                {
                    await DoWork();
                    await Task.Delay(5000, cancellationToken);
                    if (cancellationToken.IsCancellationRequested)
                        break;
                }
            });
        }

        private async Task DoWork()
        {
            foreach (var serviceKey in this.procHooks.Processes())
            {
                var service = this.procHooks.GetProcess(serviceKey);
                await service.ForwardProcessLogs();
            }
        }
    }
}