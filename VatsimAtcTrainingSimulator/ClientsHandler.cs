using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VatsimAtcTrainingSimulator.Core;

namespace VatsimAtcTrainingSimulator
{
    public static class ClientsHandler
    {
        private static List<IVatsimClient> clients;
        private static object clientsLock = new object();
        private static bool _allPaused = true;

        public static bool AllPaused
        {
            get => _allPaused; set
            {
                _allPaused = value;
                lock (clientsLock)
                {
                    foreach (IVatsimClient client in clients)
                    {
                        if (client is VatsimClientPilot)
                        {
                            ((VatsimClientPilot)client).Paused = _allPaused;
                        }
                    }
                }
            }
        }

        static ClientsHandler()
        {
            lock (clientsLock)
            {
                clients = new List<IVatsimClient>();
            }
        }

        public static void RemoveClientByCallsign(string callsign)
        {
            lock (clientsLock)
            {
                foreach (IVatsimClient client in clients)
                {
                    if (client.Callsign.Equals(callsign))
                    {
                        clients.Remove(client);
                        break;
                    }
                }
            }
        }

        public static void AddClient(IVatsimClient client)
        {
            lock (clientsLock)
            {
                clients.Add(client);
            }
        }

        public static IVatsimClient GetClientWhichContainsCallsign(string callsignMatch)
        {
            lock (clientsLock)
            {
                foreach (IVatsimClient client in clients)
                {
                    if (client.Callsign.ToLower().Contains(callsignMatch.ToLower()))
                    {
                        return client;
                    }
                }
            }

            return null;
        }

        public static IVatsimClient GetClientByCallsign(string callsign)
        {
            lock (clientsLock)
            {
                foreach (IVatsimClient client in clients)
                {
                    if (client.Callsign.ToLower().Equals(callsign.ToLower()))
                    {
                        return client;
                    }
                }
            }

            return null;
        }

        public static void SendDataForClient(string callsign, string msg)
        {
            lock (clientsLock)
            {
                foreach (IVatsimClient client in clients)
                {
                    if (client.Callsign.ToLower().Equals(callsign.ToLower()))
                    {
                        _ = client.ConnHandler.SendData(msg);
                        break;
                    }
                }
            }
        }

        public static void DisconnectAllClients()
        {
            lock (clientsLock)
            {
                try
                {
                    foreach (IVatsimClient client in clients)
                    {
                        client.Disconnect();
                    }
                } catch (InvalidOperationException)
                {
                    return;
                }
            }
        }
    }
}
