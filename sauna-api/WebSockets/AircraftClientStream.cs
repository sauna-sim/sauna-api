using Microsoft.AspNetCore.SignalR;
using SaunaSim.Api.ApiObjects.Aircraft;
using SaunaSim.Core.Data;
using SaunaSim.Core.Simulator.Aircraft;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace SaunaSim.Api.WebSockets
{
    public class AircraftClientStream
    {
        private WebSocket _ws;
        private bool _cancellationRequested;
        private Task _sendTask;

        public AircraftClientStream(WebSocket ws)
        {
            _ws = ws;
            _cancellationRequested = false;
        }

        public void StartSend()
        {
            _cancellationRequested = false;
            _sendTask = Task.Run(SendWorker);
        }

        public void StopSend()
        {
            _cancellationRequested = true;
            _sendTask?.Wait();
        }

        private async Task SendWorker()
        {
            while (!_cancellationRequested && _ws.State == WebSocketState.Open)
            {
                List<AircraftResponse> pilots = new();
                SimAircraftHandler.PerformOnAircraft((list =>
                {
                    foreach (var pilot in list)
                    {
                        pilots.Add(new AircraftResponse(pilot, true));
                    }
                }));

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
                        Data = pilots,
                        Type = SocketResponseDataType.AIRCRAFT_UPDATE
                    }, options);
                    stream.Position = 0;
                    using var reader = new StreamReader(stream);
                    jsonString = await reader.ReadToEndAsync();
                }

                // Convert to byte array
                await WebSocketHandler.SendString(_ws, jsonString);

                // Wait
                await Task.Delay(AppSettingsManager.PosCalcRate);
            }
        }
    }
}