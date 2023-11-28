using SaunaSim.Api.ApiObjects.Aircraft;

namespace SaunaSim.Api.WebSockets.ResponseData.Aircraft
{
    public class AircraftEventDeleted : ISocketAircraftEvent
    {
        public SocketAircraftEventType Type => SocketAircraftEventType.DELETED;

        public string Callsign { get; private set; }

        public object Data { get; private set; }

        public AircraftEventDeleted(string callsign)
        {
            Callsign = callsign;
            Data = null;
        }
    }
}