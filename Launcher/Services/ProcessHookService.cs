using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet;
using Launcher.Models;
using Microsoft.Extensions.Logging;

namespace Launcher.Services
{
    public class ProcessHookService : IDisposable
    {
        private ContainerHook processHook;
        private StreamReader logStream;
        private bool disposedValue = false; // To detect redundant calls
        private ILogger<ProcessHookService> logger;
        private SemaphoreSlim logStreamLock = new SemaphoreSlim(1, 1);
        private SemaphoreSlim logForwardLock = new SemaphoreSlim(1, 1);
        public string Name
        {
            get { return this.processHook.SafeName; }
        }

        public ProcessHookService(DockerClient client, ILogger<ProcessHookService> logger, ContainerHookConfig config)
        {
            this.processHook = new ContainerHook(client, config);
            this.logStream = null;
            this.logger = logger;
        }


        public async Task<bool> Start()
        {
            using (this.logger.BeginScope(this.processHook.SafeName))
            {
                return await this.processHook.StartProcess(this.logger);
            }
        }

        public async Task<StreamReader> LogStream()
        {
            await logStreamLock.WaitAsync();
            try
            {
                if (this.logStream == null)
                {
                    if (await this.processHook.IsRunning())
                    {
                        this.logStream = new StreamReader(await this.processHook.GetLogs());
                    }
                }
                else
                {
                    if (await this.processHook.IsRunning() && !this.logStream.BaseStream.CanRead)
                    {
                        this.logStream.Close();
                        this.logStream.Dispose();
                        this.logStream = new StreamReader(await this.processHook.GetLogs());
                    }
                }
                return this.logStream;
            }
            finally
            {
                logStreamLock.Release();
            }
        }

        public async Task<bool> Healthy()
        {
            return await this.processHook.IsFailed();
        }

        public Task ForwardProcessLogs(CancellationToken token)
        {
            var t = new Task(async () =>
            {
                await logForwardLock.WaitAsync();
                try
                {
                    StreamReader logStream = await this.LogStream();
                    if (logStream != null)
                    {
                        string line;
                        using (logger.BeginScope(processHook.SafeName))
                        {
                            while ((line = await logStream.ReadLineAsync()) != null && !token.IsCancellationRequested)
                            {
                                var clean = CleanInput(line);
                                if (clean != null && clean != String.Empty && clean != " ")
                                {
                                    logger.LogInformation(clean);
                                }
                            }
                        }
                    }
                }
                finally
                {
                    await logForwardLock.WaitAsync();
                }
            }, token);
            t.Start();
            return t;
        }

        static string CleanInput(string strIn)
        {
            // Replace invalid characters with empty strings.
            try
            {
                return Regex.Replace(strIn, @"[^\w\.@\: \-]", "", RegexOptions.None, TimeSpan.FromSeconds(1.5));
            }
            // If we timeout when replacing invalid characters, 
            // we should return Empty.
            catch (RegexMatchTimeoutException)
            {
                return String.Empty;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (this.logStream.BaseStream.CanRead)
                    {
                        this.logStream.Close();
                    }

                    this.logStream.Dispose();
                    this.processHook.Dispose();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}