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

namespace SaunaSim.Api.WebSockets
{
    public static class WebSocketHandler
    {
        private static List<ClientStream> _clients = new();
        private static SemaphoreSlim _clientsLock = new(1);
        private static bool _acftTaskShouldRun = false;
        private static Task _acftSendTask;

        static WebSocketHandler()
        {
            // Register event handler
            SimAircraftHandler.SimStateChanged += OnSimStateChange;
        }

        private static async Task AddClient(ClientStream client)
        {
            await _clientsLock.WaitAsync();
            _clients.Add(client);
            await client.StartSend();

            if (!_acftTaskShouldRun)
            {
                _acftTaskShouldRun = true;
                _acftSendTask = Task.Run(AircraftSendWorker);
            }

            _clientsLock.Release();
        }

        private static async Task RemoveClient(ClientStream client)
        {
            await _clientsLock.WaitAsync();
            _clients.Remove(client);
            client.StopSend();

            if (_clients.Count == 0)
            {
                _acftTaskShouldRun = false;
                await _acftSendTask.WaitAsync(Timeout.InfiniteTimeSpan);
            }
            _clientsLock.Release();
        }

        public static async Task SendCommandMsg(string msg)
        {
            await SendForAll(new SocketCommandMsgData(msg));
        }

        public static void OnSimStateChange(object sender, SimStateChangedEventArgs e)
        {
            Task.Run(async () =>
            {
                await SendForAll(new SocketSimStateData(new AircraftStateRequestResponse()
                {
                    Paused = e.AllPaused,
                    SimRate = e.SimRate
                }));
            });
        }

        private static async Task SendForAll(ISocketResponseData data)
        {
            await _clientsLock.WaitAsync();
            var tasks = new List<Task>();
            foreach (var client in _clients)
            {
                tasks.Add(client.SendObject(data));
            }
            _clientsLock.Release();

            await Task.WhenAll(tasks);
        }

        private static async Task AircraftSendWorker()
        {
            while (_acftTaskShouldRun)
            {
                // Create aircraft list
                List<AircraftResponse> pilots = new();
                SimAircraftHandler.PerformOnAircraft((list =>
                {
                    foreach (var pilot in list)
                    {
                        pilots.Add(new AircraftResponse(pilot, true));
                    }
                }));

                // Send to all clients
                await SendForAll(new SocketAircraftUpdateData(pilots));

                // Wait
                await Task.Delay(AppSettingsManager.PosCalcRate);
            }
        }

        public static async Task HandleSocket(WebSocket ws)
        {
            // Create Stream
            var stream = new ClientStream(ws);

            // Add to list
            await AddClient(stream);

            var buffer = new byte[1024 * 4];
            var receiveResult = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            while (!receiveResult.CloseStatus.HasValue)
            {
                var message = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);

                receiveResult = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }

            // Remove and close client
            await RemoveClient(stream);
            await ws.CloseAsync(receiveResult.CloseStatus.Value, receiveResult.CloseStatusDescription, CancellationToken.None);
        }
    }
}