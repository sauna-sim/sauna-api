using System.Text.Json.Serialization;

namespace SaunaSim.Api.WebSockets.RequestData
{
    public class SocketAircraftPosRateReq : ISocketRequest
    {
        public SocketRequestType Type { get; set; }
        public int PosRepIgnore => (int)Data;
        public object Data { get; set; }
    }
}