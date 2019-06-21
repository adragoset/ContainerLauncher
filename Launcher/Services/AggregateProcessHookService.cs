using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Docker.DotNet;
using Launcher.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Launcher.Services
{
    public class AggregateProcessHookService : IDisposable
    {
        private Dictionary<string, ProcessHookService> processList;
        private ILogger<AggregateProcessHookService> logger;
        private bool disposedValue = false; // To detect redundant calls

        public AggregateProcessHookService(ILogger<AggregateProcessHookService> logger)
        {
            this.processList = new Dictionary<string, ProcessHookService>();
            this.logger = logger;
        }

        public List<string> Processes()
        {
            return processList.Keys.ToList();
        }

        public ProcessHookService GetProcess(string name)
        {
            if (this.processList.ContainsKey(name))
            {
                return this.processList[name];
            }

            return null;
        }

        public void StartServices() {
            foreach(var key in this.processList.Keys) {
               this.logger.LogInformation($"Starting Service:{key}");
               this.processList[key].Start().Wait();
            }
        }

        public void AddProcess(ProcessHookService service)
        {
            this.processList.Add(service.Name, service);
        }

        public static AggregateProcessHookService AggregateProcessHookServiceFactory(IServiceProvider services) {
            var config = services.GetService<IConfiguration>();
            var configLogger = services.GetService<ILogger<AggregateProcessHookService>>();
            var aggregateServiceList = new AggregateProcessHookService(configLogger);

            configLogger.LogInformation("Read config");
            var rootConfigPath = config.GetValue<string>("ROOT_CONFIG_PATH");
            var destConfigPath = config.GetValue<string>("DEST_CONFIG_PATH");
            var mountConfigPath = config.GetValue<string>("MOUNT_CONFIG_PATH");
            var serviceSection = config.GetSection("services").GetChildren();

            foreach(var child in serviceSection) {
                configLogger.LogInformation($"Reading config for service:{child}");
                var dockerClient = services.GetService<DockerClient>();
                var logger = services.GetService<ILogger<ProcessHookService>>();
                var hookConfig = new ContainerHookConfig();
                hookConfig.Capabilities = child.GetValue<List<string>>("capabilities");
                if(rootConfigPath != null) {
                    hookConfig.ConfigSrc = Path.Join(rootConfigPath, child.Key);
                    hookConfig.ConfigVolSrc = Path.Join(destConfigPath, child.Key);
                    hookConfig.ConfigVolDest = mountConfigPath;
                }
                hookConfig.CpuPercent = child.GetValue<long>("cpu");
                hookConfig.MemCap = child.GetValue<long>("mem");
                hookConfig.Privileged = child.GetValue<bool>("privileged");
                hookConfig.SetRestartPolicy(child.GetValue<string>("restartPolicy"));

                hookConfig.EnvVariables = child.GetSection("env")
                .GetChildren()
                .Select(item => new KeyValuePair<string, string>(item.Key, item.Value))
                .ToDictionary(x => x.Key, x => x.Value);

                hookConfig.Mounts = child.GetSection("mounts")
                .GetChildren()
                .Select(item => new KeyValuePair<string, string>(item.Key, item.Value))
                .ToDictionary(x => x.Key, x => x.Value);

                hookConfig.Name = child.GetValue<string>("imageName");
                hookConfig.NetworkMode = child.GetValue<string>("networkMode");
                hookConfig.Tag = child.GetValue<string>("imageTag");
                hookConfig.SafeName = child.Key;
                aggregateServiceList.AddProcess(new ProcessHookService(dockerClient, logger, hookConfig));
            }
            aggregateServiceList.StartServices();
            return aggregateServiceList;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    foreach (var key in this.processList.Keys)
                    {
                        var process = this.processList[key];
                        process.Dispose();
                    }
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