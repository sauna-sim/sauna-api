using Microsoft.AspNetCore.SignalR;
using SaunaSim.Api.ApiObjects.Aircraft;
using SaunaSim.Api.ApiObjects.Server;
using SaunaSim.Core.Data;
using SaunaSim.Core.Simulator.Aircraft;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.WebSockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace SaunaSim.Api.WebSockets
{
    public class ClientStream
    {
        private WebSocket _ws;
        private bool _cancellationRequested;

        public ClientStream(WebSocket ws)
        {
            _ws = ws;
            _cancellationRequested = false;
        }

        private async Task SendString(string msg)
        {
            if (!_cancellationRequested && _ws.State == WebSocketState.Open)
            {
                var bytes = Encoding.UTF8.GetBytes(msg);
                var arraySegment = new ArraySegment<byte>(bytes, 0, bytes.Length);
                await _ws.SendAsync(arraySegment, WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }

        public async Task SendObject(SocketResponseDataType type, object data)
        {
            // Convert to JSON String
            string jsonString = string.Empty;

            using (var stream = new MemoryStream())
            {
                var options = new JsonSerializerOptions();
                options.Converters.Add(new JsonStringEnumConverter());
                options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals;
                await JsonSerializer.SerializeAsync(stream, new SocketResponseData()
                {
                    Data = data,
                    Type = type
                }, options);
                stream.Position = 0;
                using var reader = new StreamReader(stream);
                jsonString = await reader.ReadToEndAsync();
            }

            // Convert to byte array
            await SendString(jsonString);
        }

        public async Task StartSend()
        {
            _cancellationRequested = false;

            // Send server info
            var version = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
            await SendObject(SocketResponseDataType.SERVER_INFO, new ApiServerInfoResponse()
            {
                ServerId = "sauna-api",
                Version = new ApiServerInfoResponse.VersionInfo((uint)version.ProductMajorPart, (uint)version.ProductMinorPart, (uint)version.ProductBuildPart)
            });

            // Send initial sim state
            await SendObject(SocketResponseDataType.SIM_STATE_UPDATE, new AircraftStateRequestResponse()
            {
                Paused = SimAircraftHandler.AllPaused,
                SimRate = SimAircraftHandler.SimRate
            });

            // Send initial position calcluation rate
            await SendObject(SocketResponseDataType.POS_CALC_RATE_UPDATE, AppSettingsManager.PosCalcRate);
        }

        public void StopSend()
        {
            _cancellationRequested = true;
        }
    }
}