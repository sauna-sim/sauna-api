using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using VatsimAtcTrainingSimulator.Core.Simulator;

namespace VatsimAtcTrainingSimulator.Core
{
    public enum CONN_STATUS
    {
        DISCONNECTED,
        CONNECTED,
        CONNECTING_TO_SERVER
    }

    public enum CLIENT_TYPE
    {
        PILOT,
        ATC
    }

    public class VatsimClientConnectionHandler
    {
        private Thread recvThread;
        private CONN_STATUS _status;
        private SemaphoreSlim streamLock = new SemaphoreSlim(1, 1);
        private string callsign;
        private IVatsimClient parentClient;

        public TcpClient Client { get; private set; }
        public StreamReader Reader { get; private set; }
        public StreamWriter Writer { get; private set; }
        public Action<CONN_STATUS> StatusChangeAction { get; set; }
        public Action<string, string, string> RequestCommand { get; set; }
        public Action<string> Logger { get; set; }

        public CONN_STATUS Status
        {
            get => _status;
            private set
            {
                _status = value;
                StatusChangeAction?.Invoke(_status);
            }
        }

        public VatsimClientConnectionHandler(IVatsimClient parentClient)
        {
            this.parentClient = parentClient;
            this.callsign = parentClient.Callsign;
            _status = CONN_STATUS.DISCONNECTED;
        }

        public async Task Connect(string hostname, int port)
        {
            // Disconnect first
            if (Status == CONN_STATUS.CONNECTED || (Client != null && Client.Connected))
            {
                Disconnect();
            }

            // Change Status to Connecting and Create Client
            Logger("STATUS: Connecting");
            Status = CONN_STATUS.CONNECTING_TO_SERVER;
            Client = new TcpClient();

            // Try Connection
            try
            {
                await Client.ConnectAsync(hostname, port);
            }
            catch (Exception ex)
            {
                if (ex is SocketException || ex is NullReferenceException)
                {
                    Logger?.Invoke("ERROR: Connection Failed: " + ex.Message + " - " + ex.StackTrace.ToString());
                    Disconnect();
                    return;
                }

                throw ex;
            }

            // Create Reader and Writer
            Reader = new StreamReader(Client.GetStream());
            Writer = new StreamWriter(Client.GetStream())
            {
                AutoFlush = true
            };

            // Set Status to Connected
            Logger?.Invoke("STATUS: Connected");
            Status = CONN_STATUS.CONNECTED;

            // Start Receive Thread
            recvThread = new Thread(new ThreadStart(RecvData))
            {
                Name = $"{callsign} TCP Receiver"
            };
            recvThread.Start();
        }

        private void RecvData()
        {
            try
            {
                string line;

                while (Status == CONN_STATUS.CONNECTED && (line = Reader.ReadLine()) != null)
                {
                    if (line.StartsWith("$") || line.StartsWith("#"))
                    {
                        HandleData(line);
                        Logger?.Invoke(line);
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex is ThreadAbortException || ex is IOException)
                {
                    Logger?.Invoke($"Error Recieving Data: {ex.Message} - {ex.StackTrace}");
                    Disconnect();
                    return;
                }
                Logger?.Invoke($"Error Recieving Data: {ex.Message} - {ex.StackTrace}");
                throw ex;
            }
        }

        private void HandleData(string line)
        {
            string cmdFreqStr = $"@{((Properties.Settings.Default.commandFrequency - 100) * 1000).ToString("00000")}";

            // Handle request
            if (line.StartsWith("$CQ"))
            {
                string[] items = line.Split(':');
                string requester = items[0].Replace("$CQ", "");
                string command = items[2];

                RequestCommand?.Invoke(command, items[1], requester);
            }
            else if (line.StartsWith("#TM"))
            {
                string[] items = line.Split(':');

                // Handle text commands sent to pilot
                if (items.Length >= 3 && items[1].Equals(cmdFreqStr) && parentClient is VatsimClientPilot && items[2].StartsWith($"{callsign}, "))
                {
                    List<string> split = items[2].Replace($"{callsign}, ", "").Split(' ').ToList();

                    // Loop through command list
                    while (split.Count > 0)
                    {
                        // Get command name
                        string command = split[0].ToLower();
                        split.RemoveAt(0);

                        split = CommandHandler.HandleCommand(command, (VatsimClientPilot)parentClient, split, (string msg) =>
                        {
                            _ = SendData($"#TM{callsign}:{cmdFreqStr}:{msg.Replace($"{callsign} ", "")}");
                        });
                    }
                }

            }
        }

        public async Task SendData(string msg)
        {
            // Check if connected
            if (Status == CONN_STATUS.CONNECTED)
            {
                await streamLock.WaitAsync();
                try
                {
                    // Send data
                    await Writer.WriteLineAsync(msg);

                }
                catch (Exception ex)
                {
                    if (ex is ObjectDisposedException || ex is InvalidOperationException || ex is IOException)
                    {
                        Logger?.Invoke($"Error Sending Data: {ex.Message} - {ex.StackTrace}");
                        Disconnect();
                        return;
                    }

                    Logger?.Invoke($"Error Sending Data: {ex.Message} - {ex.StackTrace}");
                    throw ex;
                }
                finally
                {
                    streamLock.Release();
                }
            }
        }

        public async Task AddClient(CLIENT_TYPE type, string callsign, string fullname, string networkId, string password)
        {
            if (type == CLIENT_TYPE.PILOT)
                await SendData($"#AP{callsign}:SERVER:{networkId}:{password}:1:{Properties.Settings.Default.protocol}:1:{fullname}");
            else if (type == CLIENT_TYPE.ATC)
                await SendData($"#AA{callsign}:SERVER:{fullname}:{networkId}:{password}:1:{Properties.Settings.Default.protocol}");

        }

        public async Task RemoveClient(CLIENT_TYPE type, string callsign, string networkId)
        {
            if (type == CLIENT_TYPE.PILOT)
                await SendData($"#DP{callsign}:{networkId}");
            else if (type == CLIENT_TYPE.ATC)
                await SendData($"#DA{callsign}:{networkId}");
        }

        public void Disconnect()
        {
            // Disconnect
            if (Status != CONN_STATUS.DISCONNECTED)
            {
                Logger?.Invoke("STATUS: Disconnected");
                Status = CONN_STATUS.DISCONNECTED;
            }

            if (Reader != null)
            {
                Reader.Close();
            }

            if (Writer != null)
            {
                Writer.Close();
            }

            if (Client != null)
            {
                Client.Close();
            }
        }

        ~VatsimClientConnectionHandler()
        {
            // Disconnect
            Disconnect();
        }
    }
}
