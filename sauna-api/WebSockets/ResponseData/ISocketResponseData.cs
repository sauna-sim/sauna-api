namespace SaunaSim.Api.WebSockets.ResponseData
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

    public interface ISocketResponseData
    {
        public SocketResponseDataType Type { get; }
        public object Data { get; }
    }
}