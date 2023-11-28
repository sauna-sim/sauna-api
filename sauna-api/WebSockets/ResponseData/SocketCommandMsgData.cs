namespace SaunaSim.Api.WebSockets.ResponseData
{
    public class SocketCommandMsgData : ISocketResponseData
    {
        public SocketResponseDataType Type => SocketResponseDataType.COMMAND_MSG;

        public object Data { get; private set; }

        public SocketCommandMsgData(string data)
        {
            Data = data;
        }
    }
}