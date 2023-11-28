using SaunaSim.Api.ApiObjects.Aircraft;

namespace SaunaSim.Api.WebSockets.ResponseData.Aircraft
{
    public class AircraftEventCreated : ISocketAircraftEvent
    {
        public SocketAircraftEventType Type => SocketAircraftEventType.CREATED;

        public string Callsign { get; private set; }

        public object Data { get; private set; }

        public AircraftEventCreated(string callsign, AircraftResponse data)
        {
            Callsign = callsign;
            Data = data;
        }
    }
}