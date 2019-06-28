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

        private Dictionary<string, LoggingTaskRecord> _subTasks;
        private AggregateProcessHookService _procHooks;

        public HookLoggingService(AggregateProcessHookService procHooks, ILogger<HookLoggingService> _logger)
        {
            this._procHooks = procHooks;
            this._logger = _logger;
            this._cancelToken = new CancellationTokenSource();
            this._subTasks = new Dictionary<string, LoggingTaskRecord>();
        }

        public void Dispose()
        {
            _timer.Dispose();
            foreach (var t in this._subTasks.Keys)
            {
                this._subTasks[t].Dispose();
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Log passthrough Service is stopping.");
            _cancelToken.Cancel();
            return _timer;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = Task.Run(async () =>
            {
                while (true)
                {
                    DoWork();
                    await Task.Delay(5000, cancellationToken);
                    if (cancellationToken.IsCancellationRequested)
                        break;
                }
            });

            return _timer;
        }

        private void DoWork()
        {
            foreach (var serviceKey in this._procHooks.Processes())
            {
                var service = this._procHooks.GetProcess(serviceKey);
                if (this._subTasks.ContainsKey(serviceKey))
                {
                    var task = this._subTasks[serviceKey];
                    if (task.LogTask.Status != TaskStatus.Running)
                    {
                        task.Dispose();
                        this._logger.LogInformation($"Restarting logging task for:{serviceKey}");
                        this._subTasks[serviceKey] = new LoggingTaskRecord(service);
                    }
                }
                else
                {
                    this._logger.LogInformation($"Starting logging task for:{serviceKey}");
                    this._subTasks[serviceKey] = new LoggingTaskRecord(service);
                }
            }
        }

        private class LoggingTaskRecord : IDisposable
        {
            public Task LogTask;
            public CancellationTokenSource TokenSource;

            public LoggingTaskRecord(ProcessHookService logTask)
            {
                this.TokenSource = new CancellationTokenSource();
                this.LogTask = logTask.ForwardProcessLogs(this.TokenSource.Token);
            }

            public void Dispose()
            {
                this.TokenSource.Cancel();
                this.LogTask.Dispose();
            }
        }
    }
}