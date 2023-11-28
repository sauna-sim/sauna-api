using SaunaSim.Api.ApiObjects.Aircraft;
using SaunaSim.Api.WebSockets.ResponseData;
using SaunaSim.Api.WebSockets.ResponseData.Aircraft;
using SaunaSim.Core.Simulator.Aircraft;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SaunaSim.Api.WebSockets
{
    public class AircraftWebSocketHandler
    {
        private string _callsign;
        private SemaphoreSlim _clientsLock;
        private List<ClientStream> _clients;

        public AircraftWebSocketHandler(string callsign)
        {
            _callsign = callsign;
            _clientsLock = new SemaphoreSlim(1);
            _clients = new List<ClientStream>();

            // Register event handlers
            var aircraft = SimAircraftHandler.GetAircraftByCallsign(callsign);

            if (aircraft != null)
            {
                aircraft.PositionUpdated += Aircraft_PositionUpdated;
                aircraft.ConnectionStatusChanged += Aircraft_ConnectionStatusChanged;
                aircraft.SimStateChanged += Aircraft_SimStateChanged;
            }
        }

        private void Aircraft_SimStateChanged(object sender, AircraftSimStateChangedEventArgs e)
        {
            var data = new AircraftEventSimState(e.Callsign, new AircraftStateRequestResponse()
            {
                Paused = e.Paused,
                SimRate = e.SimRate
            });
            SendForAll(data).ConfigureAwait(false);
        }

        private void Aircraft_ConnectionStatusChanged(object sender, AircraftConnectionStatusChangedEventArgs e)
        {
            var data = new AircraftEventFsd(e.Callsign, e.ConnectionStatus);
            SendForAll(data).ConfigureAwait(false);
        }

        private void Aircraft_PositionUpdated(object sender, AircraftPositionUpdateEventArgs e)
        {
            var data = new AircraftEventPosition(e.Aircraft.Callsign, e.TimeStamp, new AircraftResponse(e.Aircraft, true));
            SendForAll(data).ConfigureAwait(false);
        }

        public async Task AddClient(ClientStream client)
        {
            await _clientsLock.WaitAsync();
            _clients.Add(client);
            _clientsLock.Release();
            client.StartSend();

            // Send first position
            var pilot = SimAircraftHandler.GetAircraftByCallsign(_callsign);
            if (pilot != null)
            {
                await client.QueueMessage(new SocketAircraftUpdateData(new AircraftEventPosition(pilot.Callsign, DateTime.UtcNow, new AircraftResponse(pilot, true))));
            }
        }

        public async Task RemoveClient(ClientStream client)
        {
            await _clientsLock.WaitAsync();
            _clients.Remove(client);
            _clientsLock.Release();
            client.StopSend();
        }

        public async Task RemoveAll()
        {
            await _clientsLock.WaitAsync();
            foreach (var client in _clients)
            {
                client.StopSend();
            }
            _clients.Clear();
            _clientsLock.Release();
        }

        private async Task SendForAll(ISocketAircraftEvent data)
        {
            await _clientsLock.WaitAsync();
            var tasks = new List<Task>();
            foreach (var client in _clients)
            {
                tasks.Add(client.QueueMessage(new SocketAircraftUpdateData(data)));
            }
            _clientsLock.Release();

            await Task.WhenAll(tasks);
        }
    }
}