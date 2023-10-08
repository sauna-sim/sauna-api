using FsdConnectorNet;
using System.Reflection;
using System;
using System.Runtime.Loader;

namespace SaunaSim.Api.Utilities
{
    public static class PrivateInfoLoader
    {
        public static NavigraphApiCreds GetNavigraphCreds(Action<string> logger)
        {
            // Load Navigraph Info
            string clientId = "";
            string clientSecret = "";
            try
            {
                // Load DLL and ClientInformation class
                string assemblyPath = AppDomain.CurrentDomain.BaseDirectory + "sauna-vatsim-private.dll";
                Assembly clientInfoDll = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);
                Type clientInfoClass = clientInfoDll.GetType("sauna_vatsim_private.NavigraphApiCreds");
                
                // Extract Properties
                foreach (PropertyInfo p in clientInfoClass.GetProperties())
                {
                    object value = p.GetValue(null);
                    switch (p.Name)
                    {
                        case "ClientId":
                            clientId = (string)value;
                            break;
                        case "ClientSecret":
                            clientSecret = (string)value;
                            break;
                    }
                }
            } catch (Exception ex)
            {
                logger("There was an error loading the Navigraph API Credentials. Navigraph API cannot be accessed!");
                return null;
            }

            return new NavigraphApiCreds(clientId, clientSecret);
        }

        public static ClientInfo GetClientInfo(Action<string> logger)
        {
            // Load Client Info
            string clientName = "";
            ushort clientId = 0;
            string privateKey = "";
            (uint, uint, uint) version = (0, 5, 0);
            try
            {
                // Load DLL and ClientInformation class
                string assemblyPath = AppDomain.CurrentDomain.BaseDirectory + "sauna-vatsim-private.dll";
                Assembly clientInfoDll = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);
                Type clientInfoClass = clientInfoDll.GetType("sauna_vatsim_private.ClientInformation");

                // Extract Properties
                foreach (PropertyInfo p in clientInfoClass.GetProperties())
                {
                    object value = p.GetValue(null);
                    switch (p.Name)
                    {
                        case "ClientName":
                            clientName = (string)value;
                            break;
                        case "ClientId":
                            clientId = (ushort)value;
                            break;
                        case "PrivateKey":
                            privateKey = (string)value;
                            break;
                        case "Version":
                            version = ((uint, uint, uint))value;
                            break;
                    }
                }
            } catch (Exception ex)
            {
                logger("There was an error loading the Client Information. Will use default Client Information. This may not allow you to connect to a VATSIM server!");
            }

            return new ClientInfo(clientName, clientId, privateKey, version.Item1, version.Item2, version.Item3);
        }
    }
}
