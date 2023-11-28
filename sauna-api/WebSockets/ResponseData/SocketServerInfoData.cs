using SaunaSim.Api.ApiObjects.Aircraft;
using SaunaSim.Api.ApiObjects.Server;

namespace SaunaSim.Api.WebSockets.ResponseData
{
    public class SocketServerInfoData : ISocketResponseData
    {
        public SocketResponseDataType Type => SocketResponseDataType.SERVER_INFO;

        public object Data { get; private set; }

        public SocketServerInfoData(ApiServerInfoResponse data)
        {
            Data = data;
        }
    }
}