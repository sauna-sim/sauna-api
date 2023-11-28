using SaunaSim.Api.ApiObjects.Aircraft;
using System.Collections.Generic;

namespace SaunaSim.Api.WebSockets.ResponseData
{
    public class SocketAircraftUpdateData : ISocketResponseData
    {
        public SocketResponseDataType Type => SocketResponseDataType.AIRCRAFT_UPDATE;

        public object Data { get; private set; }

        public SocketAircraftUpdateData(List<AircraftResponse> data)
        {
            Data = data;
        }
    }
}