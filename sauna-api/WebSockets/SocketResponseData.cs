namespace SaunaSim.Api.WebSockets
{
    public enum SocketResponseDataType
    {
        SERVER_INFO,
        AIRCRAFT_UPDATE,
        POS_CALC_RATE_UPDATE,
        COMMAND_MSG,
        SERVER_MSG,
        SIM_STATE_UPDATE
    }
    public class SocketResponseData
    {
        public SocketResponseDataType Type { get; set; }
        public object Data { get; set; }
    }
}