using SaunaSim.Api.ApiObjects.Aircraft;
using SaunaSim.Core.Simulator.Aircraft;

namespace SaunaSim.Api.WebSockets.ResponseData.Aircraft
{
    public class AircraftEventFsd : ISocketAircraftEvent
    {
        public SocketAircraftEventType Type => SocketAircraftEventType.FSD_CONNECTION_STATUS;

        public string Callsign { get; private set; }

        public object Data { get; private set; }

        public AircraftEventFsd(string callsign, ConnectionStatusType data)
        {
            Callsign = callsign;
            Data = data;
        }
    }
}