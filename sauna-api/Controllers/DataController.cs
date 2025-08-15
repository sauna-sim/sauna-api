using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SaunaSim.Api.ApiObjects.Data;
using SaunaSim.Api.Services;
using SaunaSim.Api.Utilities;
using SaunaSim.Core.Data;

namespace SaunaSim.Api.Controllers
{
    [ApiController]
    [Route("api/data")]
    public class DataController : ControllerBase
    {

        private readonly ILogger<DataController> _logger;
        private readonly ISaunaSessionService _sessionService;

        public DataController(ILogger<DataController> logger, ISaunaSessionService sessionService)
        {
            _logger = logger;
            _sessionService = sessionService;
        }

        [HttpPost("navigraphAuthInit")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> NavigraphAuthInit(NavigraphAuthInitRequest authRequest)
        {
            string error = "";
            var navigraphCreds = PrivateInfoLoader.GetNavigraphCreds((string s) =>
            {
                error = s;
            });

            if (navigraphCreds == null)
            {
                return BadRequest(error);
            }

            using var client = new HttpClient();
            client.BaseAddress = new Uri(navigraphCreds.ApiAuthUrl);

            var query = new Dictionary<string, string>
            {
                ["client_id"] = navigraphCreds.ClientId,
                ["client_secret"] = navigraphCreds.ClientSecret,
                ["code_challenge"] = authRequest.CodeChallenge,
                ["code_challenge_method"] = authRequest.CodeChallengeMethod
            };
            var response = await client.PostAsync("/connect/deviceauthorization", new FormUrlEncodedContent(query));

            if (response.IsSuccessStatusCode)
            {
                return Ok(await response.Content.ReadAsStringAsync());
            }

            return new StatusCodeResult((int)response.StatusCode);
        }

        [HttpPost("navigraphAuthToken")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> NavigraphAuthToken(NavigraphAuthTokenRequest authRequest)
        {
            string error = "";
            var navigraphCreds = PrivateInfoLoader.GetNavigraphCreds((string s) =>
            {
                error = s;
            });

            if (navigraphCreds == null)
            {
                return BadRequest(error);
            }

            using var client = new HttpClient();
            client.BaseAddress = new Uri(navigraphCreds.ApiAuthUrl);

            var query = new Dictionary<string, string>
            {
                ["client_id"] = navigraphCreds.ClientId,
                ["client_secret"] = navigraphCreds.ClientSecret
            };

            if (authRequest.CodeVerifier != null)
            {
                query["code_verifier"] = authRequest.CodeVerifier;
            }

            if (authRequest.GrantType != null)
            {
                query["grant_type"] = authRequest.GrantType;
            }

            if (authRequest.DeviceCode != null)
            {
                query["device_code"] = authRequest.DeviceCode;
            }

            if (authRequest.Scope != null)
            {
                query["scope"] = authRequest.Scope;
            }

            if (authRequest.RefreshToken != null)
            {
                query["refresh_token"] = authRequest.RefreshToken;
            }

            var response = await client.PostAsync("/connect/token", new FormUrlEncodedContent(query));

            if (response.IsSuccessStatusCode)
            {
                return Ok(await response.Content.ReadAsStringAsync());
            } else if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                return BadRequest(await response.Content.ReadAsStringAsync());
            }

            return new StatusCodeResult((int)response.StatusCode);
        }

        [HttpGet("hasNavigraphDataLoaded")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<NavigraphLoadedResponse> GetHasNavigraphDataLoaded()
        {
            return Ok(new NavigraphLoadedResponse() { Loaded = DataHandler.HasNavigraphDataLoaded(), Uuid = DataHandler.GetNavigraphFileUuid() });
        }

        [HttpGet("loadedSectorFiles")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<List<string>> GetLoadedSectorFiles()
        {
            return Ok(DataHandler.GetSectorFilesLoaded());
        }

        [HttpPost("loadSectorFile")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult LoadSectorFile(LoadFileRequest request)
        {
            try
            {
                DataHandler.LoadSectorFile(request.FileName);
                return Ok();
            } catch (FileNotFoundException)
            {
                return BadRequest("The file could not be found.");
            } catch (Exception)
            {
                return BadRequest("The file is not a valid Sector file.");
            }
        }

        [HttpPost("loadDFDNavData")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult LoadDfdNavData(LoadDfdFileRequest request)
        {
            try
            {
                DataHandler.LoadNavigraphDataFile(request.FileName, request.Uuid);
                return Ok();
            } catch (FileNotFoundException)
            {
                return BadRequest("The file could not be found.");
            } catch (Exception)
            {
                return BadRequest("The file is not a vaid NavData file.");
            }
        }
    }
}
