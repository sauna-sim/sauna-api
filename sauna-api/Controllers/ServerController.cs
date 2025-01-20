using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SaunaSim.Api.ApiObjects.Server;
using SaunaSim.Api.Services;
using SaunaSim.Api.WebSockets;
using SaunaSim.Core.Simulator.Aircraft;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;

namespace SaunaSim.Api.Controllers
{
    [ApiController]
    [Route("api/server")]
    public class ServerController : ControllerBase
    {
        private readonly ILogger<ServerController> _logger;
        private readonly IHostApplicationLifetime _appLifetime;
        private readonly ISimAircraftService _aircraftService;
        
        public ServerController(ILogger<ServerController> logger, IHostApplicationLifetime appLifetime, ISimAircraftService aircraftService)
        {
            _appLifetime = appLifetime;
            _logger = logger;
            _aircraftService = aircraftService;
        }

        [HttpGet("info")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<ApiServerInfoResponse> GetServerInfo()
        {
            var version = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
            return Ok(new ApiServerInfoResponse()
            {
                ServerId = "sauna-api",
                Version = new ApiServerInfoResponse.VersionInfo((uint) version.ProductMajorPart, (uint) version.ProductMinorPart, (uint) version.ProductBuildPart)
            });
        }

        [HttpGet("websocket")]
        public async Task WebSocketStream()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                try
                {
                    using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                    await _aircraftService.WebSocketHandler.HandleGeneralSocket(webSocket);
                } catch (Exception e)
                {
                    _logger.LogWarning($"Websocket connection failed: {e.Message}");
                }
            } else
            {
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
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

