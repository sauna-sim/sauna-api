using SaunaSim.Api.ApiObjects.Aircraft;
using SaunaSim.Api.WebSockets.ResponseData.Aircraft;
using System.Collections.Generic;

namespace SaunaSim.Api.WebSockets.ResponseData
{
    public class SocketAircraftUpdateData : ISocketResponseData
    {
        public SocketResponseDataType Type => SocketResponseDataType.AIRCRAFT_UPDATE;

        public object Data { get; private set; }

        public SocketAircraftUpdateData(ISocketAircraftEvent data)
        {
            Data = data;
        }
    }
}