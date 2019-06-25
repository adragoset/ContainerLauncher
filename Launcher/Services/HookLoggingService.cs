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
        private Timer _timer;
        private AggregateProcessHookService procHooks;

        public HookLoggingService(AggregateProcessHookService procHooks, ILogger<HookLoggingService> _logger)
        {
            this.procHooks = procHooks;
            this._logger = _logger;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Log passthrough Service is starting.");

            _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Log passthrough Service is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        private void DoWork(object state)
        {
            _logger.LogInformation("Recording Logs.");
            _timer?.Change(Timeout.Infinite, 0);
            foreach(var serviceKey in this.procHooks.Processes()) {
                var service = this.procHooks.GetProcess(serviceKey);
                service.ForwardProcessLogs();
            }
            _timer?.Change(5000, 5000);
        }
    }
}