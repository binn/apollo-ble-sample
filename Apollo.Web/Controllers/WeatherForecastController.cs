using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Apollo.Web.Controllers
{
    [ApiController]
    [Route("/api/v1/devices/@apollo")]
    public class ApolloDeviceController : ControllerBase
    {
        private readonly IHubContext<ApolloHub> _ctx;
        private readonly ILogger<ApolloDeviceController> _logger;

        public ApolloDeviceController(ILogger<ApolloDeviceController> logger, IHubContext<ApolloHub> hubContext)
        {
            _ctx = hubContext;
            _logger = logger;
        }

        [HttpPost("color")]
        public async Task<IActionResult> UpdateColorAsync([FromBody] ApolloColor color)
        {
            if(!ModelState.IsValid)
                return ValidationProblem(ModelState);

            await _ctx.Clients.All.SendAsync("COLOR_STATE_UPDATE", color);
            return NoContent();
        }

        [HttpPost("state")]
        public async Task<IActionResult> UpdatePowerState([FromQuery] bool on)
        {
            await _ctx.Clients.All.SendAsync("LIGHT_STATE_UPDATE", on);
            return NoContent();
        }
    }
}
