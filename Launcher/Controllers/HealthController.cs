using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Launcher.Services;
using Microsoft.AspNetCore.Mvc;

namespace Launcher.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HealthController : ControllerBase
    {
        private AggregateProcessHookService processes;

        public HealthController(AggregateProcessHookService processes) : base()
        {
            this.processes = processes;
        }

        [HttpGet]
        public async Task<JsonResult> Get()
        {
            var results = new Dictionary<string, bool>();
            foreach(var key in this.processes.Processes()) {
                var proc = this.processes.GetProcess(key);
                results.Add(proc.Name, await proc.Healthy());
            }

            return new JsonResult(results);
        }
    }
}
