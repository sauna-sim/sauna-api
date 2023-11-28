namespace SaunaSim.Api.WebSockets.ResponseData.Aircraft
{
    public enum SocketAircraftEventType
    {
        CREATED,
        DELETED,
        FSD_CONNECTION_STATUS,
        POSITION,
        SIM_STATE
    }

    public interface ISocketAircraftEvent
    {
        public SocketAircraftEventType Type { get; }

        public string Callsign { get; }

        public object Data { get; }
    }
}