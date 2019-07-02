using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;

namespace Launcher.Models
{
    public class ContainerHook : IProcessHook
    {
        private readonly DockerClient client;
        private ContainersListParameters containerListParams;
        private CancellationTokenSource cancelTokenSource = new CancellationTokenSource();
        private readonly Dictionary<string, string> mounts;
        private readonly Dictionary<string, string> envVars;
        private readonly string networkMode;
        private readonly List<string> capabilities;
        private readonly long memoryReservation;
        private readonly long cpuPercent;
        private string configSrc { get; }
        private string configVolSrc { get; }
        private string configVolDest { get; }
        private bool disposedValue = false; // To detect redundant calls
        private readonly bool privileged = false;
        private readonly RestartPolicyKind restartPolicy;
        private readonly AuthConfig authConfig;
        private bool forceUpgrade;
        public string Name { get; private set; }
        public string Tag { get; private set; }
        public string ImageName
        {
            get
            {
                return $"{this.Name}:{this.Tag}";
            }
        }
        public string SafeName { get; private set; }

        public ContainerHook(DockerClient client, ContainerHookConfig config)
        {
            this.client = client;
            this.containerListParams = new Docker.DotNet.Models.ContainersListParameters();
            containerListParams.Filters = new Dictionary<string, IDictionary<string, bool>> {
                {
                    "name",
                    new Dictionary<string, bool> {
                        { config.SafeName, true}
                    }
                }
            };
            containerListParams.Limit = 1;

            this.validate(config);

            this.Name = config.Name;
            this.Tag = config.Tag;
            this.SafeName = config.SafeName;
            this.configSrc = config.ConfigSrc;
            this.configVolSrc = config.ConfigVolSrc;
            this.configVolDest = config.ConfigVolDest;
            this.mounts = config.Mounts;
            this.envVars = config.EnvVariables;
            this.networkMode = config.NetworkMode;
            this.capabilities = config.Capabilities;
            this.memoryReservation = config.MemCap;
            this.cpuPercent = config.CpuPercent;
            this.privileged = config.Privileged;
            this.restartPolicy = config.RestartPolicy;
            this.authConfig = config.AuthConfig;
            this.forceUpgrade = config.ForceUpgrade;
        }

        private void validate(ContainerHookConfig config)
        {
            if (config.SafeName == null || config.SafeName == "")
            {
                throw new InvalidOperationException("Cannot create a ContainerHook with no SafeName");
            }

            if (config.Name == null || config.Name == "")
            {
                throw new InvalidOperationException("Cannot create a ContainerHook with no Name");
            }

            if (config.Tag == null || config.Tag == "")
            {
                throw new InvalidOperationException("Cannot create a ContainerHook with no Tag");
            }

            if (config.NetworkMode == null || config.NetworkMode == "")
            {
                throw new InvalidOperationException("Cannot create a ContainerHook with no NetworkMode");
            }

            if (config.CpuPercent <= 0)
            {
                throw new InvalidOperationException("Cannot create a ContainerHook with CpuPercent < = 0");
            }

            if (config.MemCap <= 0)
            {
                throw new InvalidOperationException("Cannot create a ContainerHook with MemCap < = 0");
            }
        }

        private async Task<ContainerListResponse> container()
        {
            var results = await client.Containers.ListContainersAsync(this.containerListParams, this.cancelTokenSource.Token);
            ContainerListResponse result = null;
            try
            {
                result = results.First();
            }
            catch (InvalidOperationException) { }
            return result;
        }

        public async Task<bool> IsFailed()
        {
            var container = await this.container();
            if (container == null)
            {
                return false;
            }
            else
            {
                return (container.State == "dead" || container.State == "restarting");
            }

        }

        public async Task<bool> IsRunning()
        {
            var container = await this.container();
            if (container == null)
            {
                return false;
            }
            else
            {
                return container.State == "running";
            }
        }

        public async Task<string> ContainerId()
        {
            var container = await this.container();
            if (container == null)
            {
                return null;
            }
            else
            {
                return container.ID;
            }
        }

        public async Task CleanupProcess(ILogger logger)
        {
            var container = await this.container();
            logger.LogInformation($"Stopping container:{this.SafeName}");
            await this.stopContainer(container.ID);
            var rmPara = new ContainerRemoveParameters();
            rmPara.Force = true;
            logger.LogInformation($"Removing container:{this.SafeName}");
            await this.client.Containers.RemoveContainerAsync(container.ID, rmPara, this.cancelTokenSource.Token);
            var delPara = new ImageDeleteParameters();
            delPara.Force = true;
            logger.LogInformation($"Removing image:{container.Image}");
            await this.client.Images.DeleteImageAsync(container.Image, delPara, this.cancelTokenSource.Token);
        }

        public async Task<bool> StartProcess(ILogger logger)
        {
            await this.pullImage(logger);
            var updatedConfig = await this.buildConfigFolders(logger);
            var container = await this.container();
            if (container == null)
            {
                var id = await this.createContainer(logger);
                return await this.startContainer(id, logger);
            }
            else
            {
                if (container.Image != this.ImageName || this.forceUpgrade)
                {
                    return await this.upgradeProcess(logger);
                }
                else if (updatedConfig)
                {
                    return await this.restartContainer(await this.ContainerId(), logger);
                }

                return true;
            }
        }

        public async Task<bool> StopProcess()
        {
            return await this.stopContainer(await this.ContainerId());
        }

        public async Task<Stream> GetLogs()
        {
            var para = new ContainerLogsParameters()
            {
                ShowStderr = true,
                ShowStdout = true,
                Follow = true,
                Tail = "25"
            };
            return await this.client.Containers.GetContainerLogsAsync(await this.ContainerId(), para, this.cancelTokenSource.Token);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    this.cancelTokenSource.Cancel();
                }

                disposedValue = true;
            }
        }

        private async Task pullImage(ILogger logger)
        {
            logger.LogInformation($"Pulling image:{this.SafeName}");
            await client.Images.CreateImageAsync(new ImagesCreateParameters
            {
                FromImage = this.Name,
                Tag = this.Tag,
            }, this.authConfig, new Progress(logger), this.cancelTokenSource.Token);
            logger.LogInformation($"Completed pulling image:{this.SafeName}");
        }

        private async Task<string> createContainer(ILogger logger)
        {
            logger.LogInformation($"Creating container for:{this.SafeName}");
            var para = new CreateContainerParameters();
            para.Image = this.ImageName;
            para.Name = this.SafeName;

            //set env vars
            para.Env = new List<string>();
            foreach (var env in this.envVars.Keys)
            {
                para.Env.Add($"{env}={this.envVars[env]}");
            }

            para.HostConfig = new HostConfig();
            para.HostConfig.RestartPolicy = new RestartPolicy()
            {
                Name = this.restartPolicy
            };

            //setup the config folder bind mounts
            para.HostConfig.Mounts = new List<Mount>();

            if (this.configSrc != null)
            {
                para.HostConfig.Mounts.Add(new Mount()
                {
                    Type = "bind",
                    Source = this.configVolSrc,
                    Target = this.configVolDest
                });
            }

            //setup the user defined bind mounts.
            foreach (var mount in this.mounts.Keys)
            {
                para.HostConfig.Mounts.Add(new Mount()
                {
                    Type = "bind",
                    Source = mount,
                    Target = this.mounts[mount]
                });
            }

            para.HostConfig.NetworkMode = this.networkMode;

            //set system resource limits
            para.HostConfig.MemoryReservation = this.memoryReservation;
            para.HostConfig.CPUPercent = this.cpuPercent;

            //set capabilities
            para.HostConfig.CapAdd = this.capabilities;
            para.HostConfig.Privileged = this.privileged;

            //create the container
            var result = await this.client.Containers.CreateContainerAsync(para, this.cancelTokenSource.Token);
            logger.LogInformation($"Container cration finished for:{this.SafeName}");
            return result.ID;
        }

        private async Task<bool> startContainer(string id, ILogger logger)
        {
            logger.LogInformation($"Start container:{this.SafeName}");
            var para = new ContainerStartParameters();
            return await this.client.Containers.StartContainerAsync(id, para, this.cancelTokenSource.Token);
        }

        private async Task<bool> restartContainer(string id, ILogger logger)
        {
            var par = new ContainerRestartParameters();
            par.WaitBeforeKillSeconds = 30;
            logger.LogInformation($"Restarting container:{this.SafeName}");
            await this.client.Containers.RestartContainerAsync(id, par, this.cancelTokenSource.Token);
            return true;
        }

        private async Task<bool> upgradeProcess(ILogger logger)
        {
            logger.LogInformation($"Upgrading container:{this.SafeName}");
            await this.CleanupProcess(logger);
            var id = await this.createContainer(logger);
            return await this.startContainer(id, logger);
        }

        private async Task<bool> stopContainer(string id)
        {
            var stopParameters = new ContainerStopParameters();
            stopParameters.WaitBeforeKillSeconds = 30;
            return await this.client.Containers.StopContainerAsync(id, stopParameters, this.cancelTokenSource.Token);
        }

        private static string getMd5Hash(MD5 md5Hash, byte[] input)
        {

            // Convert the input string to a byte array and compute the hash.
            byte[] data = md5Hash.ComputeHash(input);

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            StringBuilder sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data 
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }

        private async Task<bool> buildConfigFolders(ILogger logger)
        {
            bool updated = false;
            logger.LogInformation($"Building configuration for:{this.SafeName}");
            if (this.configSrc != null)
            {
                if (!Directory.Exists(this.configVolSrc))
                {
                    var srcDir = Directory.CreateDirectory(this.configVolSrc);
                }

                var srcFiles = dirSearch(this.configSrc);

                foreach (var file in srcFiles)
                {
                    var relativePath = Path.GetRelativePath(this.configSrc, file);
                    var destPath = Path.Combine(this.configVolSrc, relativePath);
                    if (File.Exists(destPath))
                    {
                        var srcFile = await File.ReadAllBytesAsync(file, this.cancelTokenSource.Token);
                        var destFile = await File.ReadAllBytesAsync(destPath, this.cancelTokenSource.Token);
                        string srcHash;
                        string destHash;
                        using (MD5 md5Hash = MD5.Create())
                        {
                            srcHash = getMd5Hash(md5Hash, srcFile);
                            destHash = getMd5Hash(md5Hash, destFile);
                        }

                        if (srcHash != destHash)
                        {
                            File.Copy(file, destPath, true);
                            updated = true;
                        }
                    }
                    else
                    {
                        var dir = Path.GetDirectoryName(destPath);
                        if (!Directory.Exists(dir))
                        {
                            Directory.CreateDirectory(dir);
                        }
                        File.Copy(file, destPath);
                        updated = true;
                    }
                }


                foreach (var file in dirSearch(this.configVolSrc))
                {
                    var relativePath = Path.GetRelativePath(this.configVolSrc, file);
                    var destPath = Path.Combine(this.configSrc, relativePath);

                    if (!File.Exists(destPath))
                    {
                        File.Delete(file);
                        updated = true;
                    }
                }
            }
            logger.LogInformation($"Configuration built for:{this.SafeName}");
            return updated;
        }

        private static List<string> dirSearch(string sDir)
        {
            var files = new List<string>();

            foreach (string f in Directory.GetFiles(sDir))
            {
                files.Add(f);
            }

            foreach (string d in Directory.GetDirectories(sDir))
            {
                files.AddRange(dirSearch(d));
            }

            return files;
        }

        class Progress : IProgress<JSONMessage>
        {
            private ILogger logger;

            public Progress(ILogger logger)
            {
                this.logger = logger;
            }
            public void Report(JSONMessage value)
            {
                logger.LogInformation($"Progress:{value.ProgressMessage}");
            }
        }
    }
}