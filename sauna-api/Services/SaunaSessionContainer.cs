using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using SaunaSim.Api.WebSockets;
using SaunaSim.Core.Simulator.Aircraft;
using SaunaSim.Core.Simulator.Commands;
using SaunaSim.Core.Simulator.Session;

namespace SaunaSim.Api.Services;

public class SaunaSessionContainer : IDisposable
{
    public string SessionId { get; }
    public SimSession Session { get; }
    public WebSocketHandler WebSocketHandler { get; }
    public List<string> CommandsBuffer { get; }
    public Mutex CommandsBufferLock { get; }

    public SaunaSessionContainer(string sessionId, SimSessionDetails details, Action<string, int> logger, CancellationToken cancellationToken)
    {
        SessionId = sessionId;
        Session = new SimSession(
            details,
            Path.Join(AppDomain.CurrentDomain.BaseDirectory, "magnetic", "WMM.COF"),
            Path.Join(Path.GetTempPath(), "sauna-api", "grib-tiles", sessionId),
            logger,
            cancellationToken
        );
        WebSocketHandler = new WebSocketHandler(Session.AircraftHandler);
        CommandsBuffer = new List<string>();
        CommandsBufferLock = new Mutex();
    }

    public void Dispose()
    {
        WebSocketHandler?.Dispose();
        Session?.Dispose();
        CommandsBufferLock?.Dispose();
    }
}