using SaunaSim.Api.ApiObjects.Aircraft;

namespace SaunaSim.Api.WebSockets.ResponseData.Aircraft
{
    public class AircraftEventSimState : ISocketAircraftEvent
    {
        public SocketAircraftEventType Type => SocketAircraftEventType.SIM_STATE;

        public string Callsign { get; private set; }

        public object Data { get; private set; }

        public AircraftEventSimState(string callsign, AircraftStateRequestResponse data)
        {
            Callsign = callsign;
            Data = data;
        }
    }
}