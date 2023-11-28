namespace SaunaSim.Api.WebSockets
{
    public enum SocketResponseDataType
    {
        AIRCRAFT_UPDATE,
        POS_CALC_RATE_UPDATE,
        AIRCRAFT_MSG,
        SERVER_MSG
    }
    public class SocketResponseData
    {
        public SocketResponseDataType Type { get; set; }
        public object Data { get; set; }
    }
}