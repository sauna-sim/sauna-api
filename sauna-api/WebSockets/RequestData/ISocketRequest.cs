using System;
using System.Text.Json.Serialization;

namespace SaunaSim.Api.WebSockets.RequestData
{
    public enum SocketRequestType
    {
        AIRCRAFT_POS_RATE
    }

    [JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
    [JsonDerivedType(typeof(SocketAircraftPosRateReq), typeDiscriminator: "AIRCRAFT_POS_RATE")]
    public interface ISocketRequest
    {
        SocketRequestType Type { get; }
        object Data { get; }
    }
}