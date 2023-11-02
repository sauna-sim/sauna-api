using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SaunaSim.Core.Simulator.Aircraft;

namespace SaunaSim.Api.Controllers
{
    [ApiController]
    [Route("api/server")]
    public class ServerController : ControllerBase
    {
        private readonly ILogger<ServerController> _logger;
        private readonly IHostApplicationLifetime _appLifetime;
        
        public ServerController(ILogger<ServerController> logger, IHostApplicationLifetime appLifetime)
        {
            _appLifetime = appLifetime;
            _logger = logger;
        }
        

        [HttpPost("shutdown")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult ShutdownServer()
        {
            _logger.LogInformation("Server shutdown requested.");
            _appLifetime.StopApplication();
            return Ok();
        }
    }
}

