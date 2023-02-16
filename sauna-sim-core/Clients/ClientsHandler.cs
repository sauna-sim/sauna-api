using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SaunaSim.Core;
using SaunaSim.Core.Clients;

namespace VatsimAtcTrainingSimulator
{
    public static class ClientsHandler
    {
        public static List<VatsimClientDisplayable> DisplayableList { get; private set; }
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
            DisplayableList = new List<VatsimClientDisplayable>();
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
                        foreach (VatsimClientDisplayable disp in DisplayableList)
                        {
                            if (disp.client == client)
                            {
                                DisplayableList.Remove(disp);
                                break;
                            }
                        }
                        break;
                    }
                }
            }
        }

        public static bool AddClient(IVatsimClient client)
        {
            lock (clientsLock)
            {
                List<int> deletionList = new List<int>();

                for (int i = clients.Count - 1; i >= 0; i--)
                {
                    IVatsimClient c = clients[i];
                    if (c.ConnectionStatus == CONN_STATUS.DISCONNECTED || (c.Callsign.Equals(client.Callsign) && c.ConnectionStatus == CONN_STATUS.WAITING))
                    {
                        foreach (VatsimClientDisplayable disp in DisplayableList)
                        {
                            if (disp.client == clients[i])
                            {
                                DisplayableList.Remove(disp);
                                break;
                            }
                        }
                        _ = clients[i].Disconnect();
                        clients.RemoveAt(i);
                    }
                    else if (c.Callsign.Equals(client.Callsign))
                    {
                        return false;
                    }
                }

                clients.Add(client);
            }

            DisplayableList.Add(new VatsimClientDisplayable(client));
            return true;
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
                }
                catch (InvalidOperationException)
                {
                    return;
                }
            }
        }
    }
}
