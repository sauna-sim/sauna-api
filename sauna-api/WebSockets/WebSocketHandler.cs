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

namespace SaunaSim.Api.WebSockets
{
    public static class WebSocketHandler
    {
        private static List<ClientStream> _clients = new();
        private static SemaphoreSlim _clientsLock = new(1);

        private static void SimAircraftHandler_AircraftSimStateChanged(object sender, AircraftSimStateChangedEventArgs e)
        {
            var data = new AircraftEventSimState(e.Callsign, new AircraftStateRequestResponse()
            {
                Paused = e.Paused,
                SimRate = e.SimRate
            });
            SendForAll(new SocketAircraftUpdateData(data)).RunSynchronously();
        }

        private static void SimAircraftHandler_AircraftConnectionStatusChanged(object sender, AircraftConnectionStatusChangedEventArgs e)
        {
            var data = new AircraftEventFsd(e.Callsign, e.ConnectionStatus);
            SendForAll(new SocketAircraftUpdateData(data)).RunSynchronously();
        }

        private static void SimAircraftHandler_AircraftPositionChanged(object sender, AircraftPositionUpdateEventArgs e)
        {
            var data = new AircraftEventPosition(e.Aircraft.Callsign, e.TimeStamp, new AircraftResponse(e.Aircraft, true));
            SendForAll(new SocketAircraftUpdateData(data)).RunSynchronously();
        }

        private static void SimAircraftHandler_AircraftDeleted(object sender, AircraftDeletedEventArgs e)
        {
            var data = new AircraftEventDeleted(e.Callsign);
            SendForAll(new SocketAircraftUpdateData(data)).RunSynchronously();
        }

        private static void SimAircraftHandler_AircraftCreated(object sender, AircraftPositionUpdateEventArgs e)
        {
            var data = new AircraftEventCreated(e.Aircraft.Callsign, new AircraftResponse(e.Aircraft, true));
            SendForAll(new SocketAircraftUpdateData(data)).RunSynchronously();
        }

        private static async Task AddClient(ClientStream client)
        {
            await _clientsLock.WaitAsync();
            _clients.Add(client);
            await client.StartSend();

            // Register event handler
            SimAircraftHandler.GlobalSimStateChanged += OnSimStateChange;
            SimAircraftHandler.AircraftCreated += SimAircraftHandler_AircraftCreated;
            SimAircraftHandler.AircraftDeleted += SimAircraftHandler_AircraftDeleted;
            SimAircraftHandler.AircraftPositionChanged += SimAircraftHandler_AircraftPositionChanged;
            SimAircraftHandler.AircraftConnectionStatusChanged += SimAircraftHandler_AircraftConnectionStatusChanged;
            SimAircraftHandler.AircraftSimStateChanged += SimAircraftHandler_AircraftSimStateChanged;

            _clientsLock.Release();
        }

        private static async Task RemoveClient(ClientStream client)
        {
            await _clientsLock.WaitAsync();
            _clients.Remove(client);
            client.StopSend();

            if (_clients.Count == 0)
            {
                // Deregister event handler
                SimAircraftHandler.GlobalSimStateChanged -= OnSimStateChange;
                SimAircraftHandler.AircraftCreated -= SimAircraftHandler_AircraftCreated;
                SimAircraftHandler.AircraftDeleted -= SimAircraftHandler_AircraftDeleted;
                SimAircraftHandler.AircraftPositionChanged -= SimAircraftHandler_AircraftPositionChanged;
                SimAircraftHandler.AircraftConnectionStatusChanged -= SimAircraftHandler_AircraftConnectionStatusChanged;
                SimAircraftHandler.AircraftSimStateChanged -= SimAircraftHandler_AircraftSimStateChanged;
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