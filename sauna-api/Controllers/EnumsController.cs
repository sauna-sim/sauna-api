using FsdConnectorNet;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SaunaSim.Api.ApiObjects.Data;
using SaunaSim.Core.Data;
using System;
using System.Collections.Generic;
using System.Linq;

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
