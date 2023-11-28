using SaunaSim.Api.ApiObjects.Aircraft;
using System;

namespace SaunaSim.Api.WebSockets.ResponseData.Aircraft
{
    public class AircraftEventPosition : ISocketAircraftEvent
    {
        public SocketAircraftEventType Type => SocketAircraftEventType.POSITION;

        public string Callsign { get; private set; }

        public DateTime TimeStamp { get; private set; }

        public object Data { get; private set; }

        public AircraftEventPosition(string callsign, DateTime timeStamp, AircraftResponse data)
        {
            Callsign = callsign;
            Data = data;
            TimeStamp = timeStamp;
        }
    }
}