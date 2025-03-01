using Microsoft.AspNetCore.SignalR;
using SaunaSim.Api.ApiObjects.Aircraft;
using SaunaSim.Api.ApiObjects.Server;
using SaunaSim.Api.WebSockets.ResponseData;
using SaunaSim.Api.WebSockets.ResponseData.Aircraft;
using SaunaSim.Core.Data;
using SaunaSim.Core.Simulator.Aircraft;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.WebSockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace SaunaSim.Api.WebSockets
{
    public class ClientStream
    {
        private WebSocket _ws;
        private Queue<ISocketResponseData> _responseQueue;
        private SemaphoreSlim _responseQueueLock;
        private Task _sendWorker;
        private int _posRepCount = 0;
        private const int SendDelayTime = 100;

        public int PosRepIgnore { get; set; } = 0;

        public bool ShouldClose { get; set; }

        public ClientStream(WebSocket ws)
        {
            _ws = ws;
            ShouldClose = false;
            _responseQueue = new Queue<ISocketResponseData>();
            _responseQueueLock = new SemaphoreSlim(1);
        }

        private async Task ResponseWorker()
        {
            while (!ShouldClose && _ws.State == WebSocketState.Open)
            {
                await _responseQueueLock.WaitAsync();
                bool found = _responseQueue.TryDequeue(out var msg);
                _responseQueueLock.Release();
                if (found)
                {
                    await SendObject(msg);
                } else
                {
                    await Task.Delay(SendDelayTime);
                }
            }
        }

        private async Task SendString(string msg)
        {
            if (!ShouldClose && _ws.State == WebSocketState.Open)
            {
                var bytes = Encoding.UTF8.GetBytes(msg);
                var arraySegment = new ArraySegment<byte>(bytes, 0, bytes.Length);
                await _ws.SendAsync(arraySegment, WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }

        public async Task QueueMessage(ISocketResponseData data)
        {
            await _responseQueueLock.WaitAsync();
            _responseQueue.Enqueue(data);
            _responseQueueLock.Release();
        }

        private async Task SendObject(ISocketResponseData data)
        {
            // Ignore PosReps
            if (PosRepIgnore > 0 && data is SocketAircraftUpdateData acftData && acftData.Data is AircraftEventPosition)
            {
                if (_posRepCount >= PosRepIgnore)
                {
                    _posRepCount = 0;
                }
                if (_posRepCount != 0)
                {
                    _posRepCount++;
                    return;
                }
                _posRepCount++;
            }

            // Convert to JSON String
            string jsonString = string.Empty;

            using (var stream = new MemoryStream())
            {
                var options = new JsonSerializerOptions();
                options.Converters.Add(new JsonStringEnumConverter());
                options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals;
                await JsonSerializer.SerializeAsync(stream, data, options);
                stream.Position = 0;
                using var reader = new StreamReader(stream);
                jsonString = await reader.ReadToEndAsync();
            }

            // Convert to byte array
            await SendString(jsonString);
        }

        public void StartSend()
        {
            ShouldClose = false;
            _sendWorker = Task.Run(ResponseWorker);
        }

        public void StopSend()
        {
            ShouldClose = true;
            _sendWorker.Wait();
        }
    }
}