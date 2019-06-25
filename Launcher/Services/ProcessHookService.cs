using System;
using System.IO;
using System.Threading.Tasks;
using Docker.DotNet;
using Launcher.Models;
using Microsoft.Extensions.Logging;

namespace Launcher.Services
{
    public class ProcessHookService : IDisposable
    {
        private ContainerHook processHook;
        private Stream logStream;
        private bool disposedValue = false; // To detect redundant calls
        private ILogger<ProcessHookService> logger;
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
            using(this.logger.BeginScope(this.processHook.SafeName)) {
                this.logger.LogInformation($"Starting container:{this.processHook.ImageName}");
                return await this.processHook.StartProcess(this.logger);
            }
        }

        public async Task<Stream> LogStream()
        {
            if (this.logStream == null)
            {
                if (await this.processHook.IsRunning())
                {
                    this.logStream = await this.processHook.GetLogs();
                }
            }
            else
            {
                if (!this.logStream.CanRead)
                {
                    this.logStream.Close();
                    this.logStream.Dispose();
                    this.logStream = await this.processHook.GetLogs();
                }
            }
            var result = await new Task<Stream>(() => this.logStream);
            return result;
        }

        public async Task<bool> Healthy()
        {
            return await this.processHook.IsFailed();
        }

        public async void ForwardProcessLogs()
        {
            Stream logStream = await this.LogStream();
            logger.LogInformation($"ForwardProcess Logs for:{processHook.SafeName}");
            if (logStream != null)
            {
                var stream = new StreamReader(logStream);
                string line;
                using (logger.BeginScope(processHook.SafeName))
                {
                    while ((line = stream.ReadLine()) != null)
                    {
                        logger.LogInformation(line);
                    }
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (this.logStream.CanRead)
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