using System;
using FsdConnectorNet;

namespace SaunaSim.Core.Simulator.Session
{
    public struct FsdConnectionDetails
    {
        public string HostName { get; set; }
        public ushort Port { get; set; }
        public string NetworkId { get; set; }
        public string Password { get; set; }
        public string RealName { get; set; }
        public ProtocolRevision ProtocolRevision { get; set; }
        public string CommandFrequency { get; set; }

        public static string ConvertFrequency((ushort, ushort) frequency)
        {
            return $"{frequency.Item1:000}.{frequency.Item2:000}";
        }

        public LoginInfo ToLoginInfo(string callsign)
        {
            (ushort, ushort) freq;
            try
            {
                var split = CommandFrequency.Split('.');
                ushort mHz = Convert.ToUInt16(split[0]);
                ushort kHz = Convert.ToUInt16(split[1]);
                if (kHz < 10)
                {
                    kHz *= 100;
                } else if (kHz < 100)
                {
                    kHz *= 10;
                }

                freq = (mHz, kHz);
            } catch (Exception ex)
            {
                if (ex is IndexOutOfRangeException || ex is FormatException || ex is OverflowException)
                {
                    freq = (199, 998);
                }
                else
                {
                    throw;
                }
            }

            return new LoginInfo(NetworkId, Password, callsign, RealName, PilotRatingType.Student, HostName, ProtocolRevision, freq, Port);
        }
    }
}