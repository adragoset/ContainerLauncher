using System;
using System.Collections.Generic;
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
        private readonly CancellationTokenSource _cancelToken;
        private Task _timer;

        private Dictionary<string, Task> _subTasks;
        private AggregateProcessHookService _procHooks;

        public HookLoggingService(AggregateProcessHookService procHooks, ILogger<HookLoggingService> _logger)
        {
            this._procHooks = procHooks;
            this._logger = _logger;
            this._cancelToken = new CancellationTokenSource();
            this._subTasks = new Dictionary<string,Task>();
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Log passthrough Service is stopping.");
            _cancelToken.Cancel();
            return _timer;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
            {
                while (true)
                {
                    DoWork();
                    await Task.Delay(5000, cancellationToken);
                    if (cancellationToken.IsCancellationRequested)
                        break;
                }
            });
        }

        private void DoWork()
        {
            foreach (var serviceKey in this._procHooks.Processes())
            {
                var service = this._procHooks.GetProcess(serviceKey);
                if(this._subTasks.ContainsKey(serviceKey)) {
                    var task = this._subTasks[serviceKey];
                    if(task.IsFaulted || task.IsCompleted || task.IsCompletedSuccessfully) {
                        task.Dispose();
                        this._subTasks[serviceKey] = service.ForwardProcessLogs(this._cancelToken.Token);
                    }
                }
                else {
                    this._subTasks[serviceKey] = service.ForwardProcessLogs(this._cancelToken.Token);
                }
            }
        }
    }
}