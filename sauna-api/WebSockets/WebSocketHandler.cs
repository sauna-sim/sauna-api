using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System;
using SaunaSim.Api.ApiObjects.Server;
using System.Diagnostics;
using System.Reflection;
using System.IO;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Collections;
using System.Collections.Generic;
using SaunaSim.Api.ApiObjects.Aircraft;
using SaunaSim.Core.Data;
using SaunaSim.Core.Simulator.Aircraft;
using SaunaSim.Api.WebSockets.ResponseData;
using SaunaSim.Api.WebSockets.ResponseData.Aircraft;
using System.Runtime.CompilerServices;
using SaunaSim.Api.WebSockets.RequestData;

namespace SaunaSim.Api.WebSockets
{
    public class WebSocketHandler : IDisposable
    {
        private readonly List<ClientStream> _generalClients;
        private readonly SemaphoreSlim _generalClientsLock;
        private readonly Dictionary<string, AircraftWebSocketHandler> _aircraftClientsMap;
        private readonly SimAircraftHandler _simAircraftHandler;

        public WebSocketHandler(SimAircraftHandler handler)
        {
            _generalClients = new();
            _generalClientsLock = new(1);
            _aircraftClientsMap = new();
            _simAircraftHandler = handler;

            // Register event handlers
            _simAircraftHandler.GlobalSimStateChanged += OnSimStateChange;
            _simAircraftHandler.AircraftCreated += SimAircraftHandler_AircraftCreated;
            _simAircraftHandler.AircraftDeleted += SimAircraftHandler_AircraftDeleted;
        }

        private void SimAircraftHandler_AircraftDeleted(object sender, AircraftDeletedEventArgs e)
        {
            var data = new AircraftEventDeleted(e.Callsign);
            SendForAllGeneral(new SocketAircraftUpdateData(data)).ConfigureAwait(false);

            // Handle Aircraft Clients
            Task.Run(async () =>
            {
                bool aircraftExists = _aircraftClientsMap.TryGetValue(e.Callsign, out AircraftWebSocketHandler value);

                if (aircraftExists)
                {
                    await value.RemoveAll();
                    _aircraftClientsMap.Remove(e.Callsign);
                }
            });
        }

        private void SimAircraftHandler_AircraftCreated(object sender, AircraftPositionUpdateEventArgs e)
        {
            var data = new AircraftEventCreated(e.Aircraft.Callsign, new AircraftResponse(e.Aircraft, true));
            SendForAllGeneral(new SocketAircraftUpdateData(data)).ConfigureAwait(false);

            // Handle Aircraft Clients
            Task.Run(async () =>
            {
                bool aircraftExists = _aircraftClientsMap.TryGetValue(e.Aircraft.Callsign, out AircraftWebSocketHandler value);

                if (aircraftExists)
                {
                    await value.RemoveAll();
                    _aircraftClientsMap.Remove(e.Aircraft.Callsign);
                }

                _aircraftClientsMap.Add(e.Aircraft.Callsign, new AircraftWebSocketHandler(e.Aircraft.Callsign, _simAircraftHandler));
            });
        }

        private async Task AddGeneralClient(ClientStream client)
        {
            await _generalClientsLock.WaitAsync();
            _generalClients.Add(client);
            client.StartSend();
            _generalClientsLock.Release();

            // Send server info
            var version = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
            await client.QueueMessage(new SocketServerInfoData(new ApiServerInfoResponse()
            {
                ServerId = "sauna-api",
                Version = new ApiServerInfoResponse.VersionInfo((uint)version.ProductMajorPart, (uint)version.ProductMinorPart, (uint)version.ProductBuildPart)
            }));

            // Send initial sim state
            await client.QueueMessage(new SocketSimStateData(new AircraftStateRequestResponse()
            {
                Paused = _simAircraftHandler.AllPaused,
                SimRate = _simAircraftHandler.SimRate
            }));

            // Send all aircraft initially
            _simAircraftHandler.SimAircraftListLock.WaitOne();

            foreach (SimAircraft pilot in _simAircraftHandler.SimAircraftList)
            {
                await client.QueueMessage(new SocketAircraftUpdateData(new AircraftEventCreated(pilot.Callsign, new AircraftResponse(pilot, true))));
            }

            _simAircraftHandler.SimAircraftListLock.ReleaseMutex();
        }

        private async Task RemoveGeneralClient(ClientStream client)
        {
            await _generalClientsLock.WaitAsync();
            _generalClients.Remove(client);
            client.StopSend();
            _generalClientsLock.Release();
        }

        private async Task AddAircraftClient(string callsign, ClientStream client)
        {
            bool aircraftExists = _aircraftClientsMap.TryGetValue(callsign, out AircraftWebSocketHandler value);

            if (aircraftExists)
            {
                await value.AddClient(client);
            }
        }

        private async Task RemoveAircraftClient(string callsign, ClientStream client)
        {
            bool aircraftExists = _aircraftClientsMap.TryGetValue(callsign, out AircraftWebSocketHandler value);

            if (aircraftExists)
            {
                await value.RemoveClient(client);
            }
        }

        public async Task SendCommandMsg(string msg)
        {
            await SendForAllGeneral(new SocketCommandMsgData(msg));
        }

        public void OnSimStateChange(object sender, SimStateChangedEventArgs e)
        {
            Task.Run(async () =>
            {
                await SendForAllGeneral(new SocketSimStateData(new AircraftStateRequestResponse()
                {
                    Paused = e.AllPaused,
                    SimRate = e.SimRate
                }));
            });
        }

        private async Task SendForAllGeneral(ISocketResponseData data)
        {
            await _generalClientsLock.WaitAsync();
            var tasks = new List<Task>();
            foreach (var client in _generalClients)
            {
                tasks.Add(client.QueueMessage(data));
            }
            _generalClientsLock.Release();

            await Task.WhenAll(tasks);
        }

        public async Task HandleGeneralSocket(WebSocket ws, CancellationToken cancelToken)
        {
            // Create Stream
            var stream = new ClientStream(ws);

            // Add to list
            await AddGeneralClient(stream);
            try
            {
                var buffer = new byte[1024 * 4];
                var receiveResult = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), cancelToken);

                while (!receiveResult.CloseStatus.HasValue && !stream.ShouldClose)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);

                    receiveResult = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), cancelToken);
                }

                // Remove and close client
                await RemoveGeneralClient(stream);
                await ws.CloseAsync(receiveResult.CloseStatus.Value, receiveResult.CloseStatusDescription, cancelToken);
            } catch (Exception e)
            {
                await RemoveGeneralClient(stream);
            }
        }

        public async Task HandleAircraftSocket(string callsign, WebSocket ws, CancellationToken cancelToken)
        {
            // Create Stream
            var stream = new ClientStream(ws);

            // Add to list
            await AddAircraftClient(callsign, stream);
            try
            {
                var buffer = new byte[1024 * 4];
                var receiveResult = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), cancelToken);

                while (!receiveResult.CloseStatus.HasValue && !stream.ShouldClose)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);

                    // Check message
                    var options = new JsonSerializerOptions();
                    options.Converters.Add(new JsonStringEnumConverter());
                    options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    options.NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals;

                    try
                    {
                        ISocketRequest request = (ISocketRequest) JsonSerializer.Deserialize(message, typeof(ISocketRequest), options);

                        if (request is SocketAircraftPosRateReq req)
                        {
                            ((JsonElement)req.Data).TryGetInt32(out int posrepignore);
                            stream.PosRepIgnore = posrepignore;
                        }

                    } catch (Exception) { }

                    receiveResult = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), cancelToken);
                }

                // Remove and close client
                await RemoveAircraftClient(callsign, stream);
                await ws.CloseAsync(receiveResult.CloseStatus.Value, receiveResult.CloseStatusDescription, cancelToken);
            } catch (Exception e)
            {
                await RemoveAircraftClient(callsign, stream);
            }
        }

        public void Dispose()
        {
            _generalClientsLock?.Dispose();
            foreach (var client in _generalClients)
            {
                client.Dispose();
            }

            foreach (var aircraft in _aircraftClientsMap.Values)
            {
                aircraft.Dispose();
            }
        }
    }
}