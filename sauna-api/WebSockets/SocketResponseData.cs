namespace SaunaSim.Api.WebSockets
{
    public enum SocketResponseDataType
    {
        AIRCRAFT,
        MSG_AIRCRAFT,
        MSG_SERVER
    }
    public class SocketResponseData
    {
        public SocketResponseDataType Type { get; set; }
        public object Data { get; set; }
    }
}