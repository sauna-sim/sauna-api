namespace SaunaSim.Api.WebSockets.ResponseData
{
    public class SocketPosCalcUpdateData : ISocketResponseData
    {
        public SocketResponseDataType Type => SocketResponseDataType.POS_CALC_RATE_UPDATE;

        public object Data { get; private set; }

        public SocketPosCalcUpdateData(int data)
        {
            Data = data;
        }
    }
}