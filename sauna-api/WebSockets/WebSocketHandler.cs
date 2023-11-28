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

namespace SaunaSim.Api.WebSockets
{
    public static class WebSocketHandler
    {
        private static List<ClientStream> _generalClients = new();
        private static SemaphoreSlim _generalClientsLock = new(1);
        private static Dictionary<string, AircraftWebSocketHandler> _aircraftClientsMap = new();
        //private static Task _acftPosUpdateWorker;
        //private static bool _acftPosUpdate = false;

        static WebSocketHandler()
        {
            // Register event handlers
            SimAircraftHandler.GlobalSimStateChanged += OnSimStateChange;
            SimAircraftHandler.AircraftCreated += SimAircraftHandler_AircraftCreated;
            SimAircraftHandler.AircraftDeleted += SimAircraftHandler_AircraftDeleted;
        }


        //private static void SimAircraftHandler_AircraftSimStateChanged(object sender, AircraftSimStateChangedEventArgs e)
        //{
        //    var data = new AircraftEventSimState(e.Callsign, new AircraftStateRequestResponse()
        //    {
        //        Paused = e.Paused,
        //        SimRate = e.SimRate
        //    });
        //    SendForAll(new SocketAircraftUpdateData(data)).ConfigureAwait(false);
        //}

        //private static void SimAircraftHandler_AircraftConnectionStatusChanged(object sender, AircraftConnectionStatusChangedEventArgs e)
        //{
        //    var data = new AircraftEventFsd(e.Callsign, e.ConnectionStatus);
        //    SendForAll(new SocketAircraftUpdateData(data)).ConfigureAwait(false);
        //}

        //private static void SimAircraftHandler_AircraftPositionChanged(object sender, AircraftPositionUpdateEventArgs e)
        //{
        //    var data = new AircraftEventPosition(e.Aircraft.Callsign, e.TimeStamp, new AircraftResponse(e.Aircraft, true));
        //    SendForAll(new SocketAircraftUpdateData(data)).ConfigureAwait(false);
        //}

        private static void SimAircraftHandler_AircraftDeleted(object sender, AircraftDeletedEventArgs e)
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

        private static void SimAircraftHandler_AircraftCreated(object sender, AircraftPositionUpdateEventArgs e)
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

                _aircraftClientsMap.Add(e.Aircraft.Callsign, new AircraftWebSocketHandler(e.Aircraft.Callsign));
            });
        }

        private static async Task AddGeneralClient(ClientStream client)
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
                Paused = SimAircraftHandler.AllPaused,
                SimRate = SimAircraftHandler.SimRate
            }));

            // Send initial position calcluation rate
            await client.QueueMessage(new SocketPosCalcUpdateData(AppSettingsManager.PosCalcRate));

            // Send all aircraft initially
            SimAircraftHandler.PerformOnAircraft(async (List<SimAircraft> aircraft) =>
            {
                foreach (SimAircraft pilot in aircraft)
                {
                    await client.QueueMessage(new SocketAircraftUpdateData(new AircraftEventCreated(pilot.Callsign, new AircraftResponse(pilot, true))));
                }
            });

            //if (!_acftPosUpdate)
            //{
            //    _acftPosUpdate = true;
            //    _acftPosUpdateWorker = Task.Run(AircraftPositionWorker);
            //}
        }

        private static async Task RemoveGeneralClient(ClientStream client)
        {
            await _generalClientsLock.WaitAsync();
            _generalClients.Remove(client);
            client.StopSend();
            _generalClientsLock.Release();
        }

        private static async Task AddAircraftClient(string callsign, ClientStream client)
        {
            bool aircraftExists = _aircraftClientsMap.TryGetValue(callsign, out AircraftWebSocketHandler value);

            if (aircraftExists)
            {
                await value.AddClient(client);
            }
        }

        private static async Task RemoveAircraftClient(string callsign, ClientStream client)
        {
            bool aircraftExists = _aircraftClientsMap.TryGetValue(callsign, out AircraftWebSocketHandler value);

            if (aircraftExists)
            {
                await value.RemoveClient(client);
            }
        }

        public static async Task SendCommandMsg(string msg)
        {
            await SendForAllGeneral(new SocketCommandMsgData(msg));
        }

        public static void OnSimStateChange(object sender, SimStateChangedEventArgs e)
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

        //private static async Task AircraftPositionWorker()
        //{
        //    while (_acftPosUpdate)
        //    {
        //        List<AircraftResponse> pilots = new List<AircraftResponse>();
        //        SimAircraftHandler.PerformOnAircraft((list =>
        //        {
        //            foreach (var pilot in list)
        //            {
        //                pilots.Add(new AircraftResponse(pilot, true));
        //            }
        //        }));

        //        await SendForAll(new SocketAircraftUpdateData(pilots));

        //        await Task.Delay(AppSettingsManager.PosCalcRate);
        //    }
        //}

        private static async Task SendForAllGeneral(ISocketResponseData data)
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

        public static async Task HandleGeneralSocket(WebSocket ws)
        {
            // Create Stream
            var stream = new ClientStream(ws);

            // Add to list
            await AddGeneralClient(stream);
            try
            {
                var buffer = new byte[1024 * 4];
                var receiveResult = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                while (!receiveResult.CloseStatus.HasValue && !stream.ShouldClose)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);

                    receiveResult = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                }

                // Remove and close client
                await RemoveGeneralClient(stream);
                await ws.CloseAsync(receiveResult.CloseStatus.Value, receiveResult.CloseStatusDescription, CancellationToken.None);
            } catch (Exception e)
            {
                await RemoveGeneralClient(stream);
            }
        }

        public static async Task HandleAircraftSocket(string callsign, WebSocket ws)
        {
            // Create Stream
            var stream = new ClientStream(ws);

            // Add to list
            await AddAircraftClient(callsign, stream);
            try
            {
                var buffer = new byte[1024 * 4];
                var receiveResult = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                while (!receiveResult.CloseStatus.HasValue && !stream.ShouldClose)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);

                    receiveResult = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                }

                // Remove and close client
                await RemoveAircraftClient(callsign, stream);
                await ws.CloseAsync(receiveResult.CloseStatus.Value, receiveResult.CloseStatusDescription, CancellationToken.None);
            } catch (Exception e)
            {
                await RemoveAircraftClient(callsign, stream);
            }
        }
    }
}