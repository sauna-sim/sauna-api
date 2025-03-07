using FsdConnectorNet;
using System.Reflection;
using System;
using System.Runtime.Loader;
using System.Runtime.InteropServices;

namespace SaunaSim.Api.Utilities
{
    public static class PrivateInfoLoader
    {

        public static NavigraphApiCreds GetNavigraphCreds(Action<string> logger)
        {
            // Load Navigraph Info
            try
            {
                IntPtr navigraphPtr = navigraph_creds_init();
                IntPtr clientIdPtr = get_field_from_store(navigraphPtr, 0, out byte clientIdSize);
                IntPtr clientSecretPtr = get_field_from_store(navigraphPtr, 1, out byte clientSecretSize);
                IntPtr apiAuthUrlPtr = get_field_from_store(navigraphPtr, 2, out byte apiAuthUrlSize);

                string clientId = GetStringFromPtr(clientIdPtr, clientIdSize);
                string clientSecret = GetStringFromPtr(clientSecretPtr, clientSecretSize);
                string apiAuthUrl = GetStringFromPtr(apiAuthUrlPtr, apiAuthUrlSize);

                drop_secret_store(navigraphPtr);

                return new NavigraphApiCreds(clientId, clientSecret, apiAuthUrl);
            } catch (Exception)
            {
                logger("There was an error loading the Navigraph API Credentials. Navigraph API cannot be accessed!");
                return null;
            }

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
                IntPtr vatsimPtr = vatsim_client_info_init();
                IntPtr clientNamePtr = get_field_from_store(vatsimPtr, 0, out byte clientNameSize);
                IntPtr clientIdPtr = get_field_from_store(vatsimPtr, 1, out byte clientIdSize);
                IntPtr privateKeyPtr = get_field_from_store(vatsimPtr, 2, out byte privateKeySize);
                IntPtr version1Ptr = get_field_from_store(vatsimPtr, 3, out byte version1Size);
                IntPtr version2Ptr = get_field_from_store(vatsimPtr, 4, out byte version2Size);
                IntPtr version3Ptr = get_field_from_store(vatsimPtr, 5, out byte version3Size);

                clientName = GetStringFromPtr(clientNamePtr, clientNameSize);
                privateKey = GetStringFromPtr(privateKeyPtr, privateKeySize);

                if (BitConverter.IsLittleEndian)
                {
                    clientId = BitConverter.ToUInt16(GetBytesFromPtrReverse(clientIdPtr, clientIdSize), 0);
                    version.Item1 = BitConverter.ToUInt32(GetBytesFromPtrReverse(version1Ptr, version1Size), 0);
                    version.Item2 = BitConverter.ToUInt32(GetBytesFromPtrReverse(version2Ptr, version2Size), 0);
                    version.Item3 = BitConverter.ToUInt32(GetBytesFromPtrReverse(version3Ptr, version3Size), 0);
                } else
                {
                    clientId = BitConverter.ToUInt16(GetBytesFromPtr(clientIdPtr, clientIdSize), 0);
                    version.Item1 = BitConverter.ToUInt32(GetBytesFromPtr(version1Ptr, version1Size), 0);
                    version.Item2 = BitConverter.ToUInt32(GetBytesFromPtr(version2Ptr, version2Size), 0);
                    version.Item3 = BitConverter.ToUInt32(GetBytesFromPtr(version3Ptr, version3Size), 0);
                }

                drop_secret_store(vatsimPtr);
            } catch (Exception)
            {
                logger("There was an error loading the Client Information. Will use default Client Information. This may not allow you to connect to a VATSIM server!");
            }

            return new ClientInfo(clientName, clientId, privateKey, version.Item1, version.Item2, version.Item3);
        }

        private static byte[] GetBytesFromPtrReverse(IntPtr ptr, byte size)
        {
            byte[] bytes = new byte[size];
            for (int i = size - 1; i >= 0; i--)
            {
                bytes[i] = Marshal.ReadByte(ptr + (size - i - 1));
            }

            drop_vec(ptr, size);

            return bytes;
        }

        private static byte[] GetBytesFromPtr(IntPtr ptr, byte size)
        {
            byte[] bytes = new byte[size];
            for (int i = 0; i < size; i++)
            {
                bytes[i] = Marshal.ReadByte(ptr + i);
            }

            drop_vec(ptr, size);

            return bytes;
        }

        private static String GetStringFromPtr(IntPtr ptr, byte size)
        {
            return System.Text.Encoding.UTF8.GetString(GetBytesFromPtr(ptr, size), 0, size);
        }

        [DllImport("sauna_vatsim_private", CallingConvention = CallingConvention.Cdecl)] private static extern IntPtr vatsim_client_info_init();
        [DllImport("sauna_vatsim_private", CallingConvention = CallingConvention.Cdecl)] private static extern IntPtr navigraph_creds_init();
        [DllImport("sauna_vatsim_private", CallingConvention = CallingConvention.Cdecl)] private static extern void drop_secret_store(IntPtr ptr);
        [DllImport("sauna_vatsim_private", CallingConvention = CallingConvention.Cdecl)] private static extern IntPtr get_field_from_store(IntPtr data, byte fieldIndex, out byte fieldSize);
        [DllImport("sauna_vatsim_private", CallingConvention = CallingConvention.Cdecl)] private static extern void drop_vec(IntPtr ptr, byte size);
    }
}
