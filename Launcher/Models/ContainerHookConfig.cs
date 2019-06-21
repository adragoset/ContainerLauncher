using System.Collections.Generic;
using Docker.DotNet.Models;

namespace Launcher.Models {

    public class ContainerHookConfig {
        public string Name;
        public string Tag;
        public string SafeName;
        public string ConfigSrc;
        public string ConfigVolSrc;
        public string ConfigVolDest;
        public List<string> Capabilities;
        public Dictionary<string, string> Mounts;
        public Dictionary<string, string> EnvVariables;
        public string NetworkMode;
        public long MemCap;
        public long CpuPercent;
        public bool Privileged;
        public RestartPolicyKind RestartPolicy { get; private set; }

        public void SetRestartPolicy(string policy) {
             if(policy == null) {
                 this.RestartPolicy = RestartPolicyKind.Undefined;
             } 
             else if(policy.ToLower() == "no") {
                 this.RestartPolicy = RestartPolicyKind.No;
             } 
             else if(policy.ToLower() == "on_failure") {
                 this.RestartPolicy = RestartPolicyKind.OnFailure;
             } 
             else if(policy.ToLower() == "always") {
                 this.RestartPolicy = RestartPolicyKind.Always;
             }
             else if(policy.ToLower() == "unless_stopped") {
                 this.RestartPolicy = RestartPolicyKind.UnlessStopped;
             }
        }
    }
}