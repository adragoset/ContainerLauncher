using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Launcher.Models
{
    public interface IProcessHook : IDisposable
    {
        string Name { get; }

        string Tag { get; }

        string SafeName { get; }

        Task<bool> IsRunning();

        Task<bool> IsFailed();

        Task<bool> StartProcess(ILogger logger);

        Task<bool> StopProcess();

        Task CleanupProcess();

        Task<Stream> GetLogs();
    }
}