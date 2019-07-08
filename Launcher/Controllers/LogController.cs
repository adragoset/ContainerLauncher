using System.Collections.Generic;
using System.Threading.Tasks;
using Launcher.Services;
using Microsoft.AspNetCore.Mvc;

namespace Launcher.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LogController : ControllerBase
    {
        private AggregateProcessHookService processes;

        public LogController(AggregateProcessHookService processes) : base()
        {
            this.processes = processes;
        }

        [HttpGet]
        [Route("{name}")]
        public async Task<JsonResult> Get(string name)
        {
            var service = this.processes.GetProcess(name);
            var result = await service.GetLogs(false, null);
            return new JsonResult(result);
        }
    }
}
