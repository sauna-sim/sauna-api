using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

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

    public class VatsimConnectionHandler
    {
        private Thread recvThread;
        private CONN_STATUS _status;
        private bool writingData;

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

        public VatsimConnectionHandler()
        {
            Status = CONN_STATUS.DISCONNECTED;
            writingData = false;
        }

        public async Task Connect(string hostname, int port)
        {
            // Disconnect first
            await Disconnect();

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
                    Status = CONN_STATUS.DISCONNECTED;
                    return;
                }

                throw;
            }

            // Create Reader and Writer
            Reader = new StreamReader(Client.GetStream());
            Writer = new StreamWriter(Client.GetStream())
            {
                AutoFlush = true
            };

            // Start Receive Thread
            recvThread = new Thread(new ThreadStart(RecvData));
            recvThread.Start();

            // Set Status to Connected
            Logger?.Invoke("STATUS: Connected");
            Status = CONN_STATUS.CONNECTED;
        }

        private void HandleData(string line)
        {
            // Handle request
            if (line.StartsWith("$CQ"))
            {
                string[] items = line.Split(':');
                string requester = items[0].Replace("$CQ", "");
                string command = items[2];

                RequestCommand?.Invoke(command, items[1], requester);
            }
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
                        Logger?.Invoke(line);
                    }
                }
            } catch (Exception ex)
            {
                if (ex is ThreadAbortException || ex is IOException)
                {
                    return;
                }

                throw ex;
            }
        }

        public async Task SendData(string msg)
        {
            // Check if connected
            if (Status == CONN_STATUS.CONNECTED)
            {
                // Wait until lock opens up
                await Task.Run(() =>
                {
                    while (writingData) { Thread.Sleep(100); }
                });

                // acquire lock
                writingData = true;

                try
                {
                    // Send data
                    await Writer.WriteLineAsync(msg);
                }
                catch (Exception ex)
                {
                    if (ex is ObjectDisposedException || ex is InvalidOperationException)
                    {

                        writingData = false;
                        return;
                    }

                    throw ex;
                }
            }

            // Release Lock
            writingData = false;
        }

        public async Task AddClient(CLIENT_TYPE type, string callsign, string fullname, string networkId, string password)
        {
            if (type == CLIENT_TYPE.PILOT)
                await SendData($"#AP{callsign}:SERVER:{networkId}:{password}:1:9:1:{fullname}");
            else if (type == CLIENT_TYPE.ATC)
                await SendData($"#AA{callsign}:SERVER:{fullname}:{networkId}:{password}:1:9");

        }

        public async Task RemoveClient(CLIENT_TYPE type, string callsign, string networkId)
        {
            if (type == CLIENT_TYPE.PILOT)
                await SendData($"#DP{callsign}:{networkId}");
            else if (type == CLIENT_TYPE.ATC)
                await SendData($"#DA{callsign}:{networkId}");
        }

        public async Task Disconnect()
        {
            // Disconnect
            Logger?.Invoke("STATUS: Disconnected");
            Status = CONN_STATUS.DISCONNECTED;

            // Wait until lock opens up and thread has finished
            await Task.Run(() =>
            {
                while (writingData) { Thread.Sleep(100); }
            });

            // acquire lock
            writingData = true;

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

            // Release Lock
            writingData = false;
        }

        ~VatsimConnectionHandler()
        {
            // Disconnect
            Disconnect().ConfigureAwait(false);
        }
    }
}
