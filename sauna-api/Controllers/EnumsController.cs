using System;
using System.Collections.Generic;
using System.Linq;
using FsdConnectorNet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace SaunaSim.Api.Controllers
{
    [Route("api/enums")]
    [ApiController]
    public class EnumsController : ControllerBase
    {
        private readonly ILogger<EnumsController> _logger;

        public EnumsController(ILogger<EnumsController> logger)
        {
            _logger = logger;
        }

        [HttpGet("fsd/protocolRevisions")]
        public List<ProtocolRevision> GetFsdProtocolRevisions()
        {
            return Enum.GetValues(typeof(ProtocolRevision)).Cast<ProtocolRevision>().ToList();
        }
    }
}
