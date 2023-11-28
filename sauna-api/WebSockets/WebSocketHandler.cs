using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace SaunaSim.Api.WebSockets
{
    public static class WebSocketHandler
    {
        public static async Task SendString(WebSocket ws, string msg)
        {
            var bytes = Encoding.UTF8.GetBytes(msg);
            var arraySegment = new ArraySegment<byte>(bytes, 0, bytes.Length);
            await ws.SendAsync(arraySegment, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        public static async Task HandleSocket(WebSocket ws)
        {
            var acftStream = new AircraftClientStream(ws);
            acftStream.StartSend();

            var buffer = new byte[1024 * 4];
            var receiveResult = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            while (!receiveResult.CloseStatus.HasValue)
            {
                var message = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);

                receiveResult = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }

            acftStream.StopSend();
            await ws.CloseAsync(receiveResult.CloseStatus.Value, receiveResult.CloseStatusDescription, CancellationToken.None);
        }
    }
}