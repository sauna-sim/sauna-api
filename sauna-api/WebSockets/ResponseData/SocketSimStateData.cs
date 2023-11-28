using SaunaSim.Api.ApiObjects.Aircraft;

namespace SaunaSim.Api.WebSockets.ResponseData
{
    public class SocketSimStateData : ISocketResponseData
    {
        public SocketResponseDataType Type => SocketResponseDataType.SIM_STATE_UPDATE;

        public object Data { get; private set; }

        public SocketSimStateData(AircraftStateRequestResponse data)
        {
            Data = data;
        }
    }
}