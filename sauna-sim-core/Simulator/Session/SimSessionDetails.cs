using FsdConnectorNet;

namespace SaunaSim.Core.Simulator.Session
{
    public enum SimSessionType
    {
        STANDALONE = 0,
        FSD = 1
    }
    
    public struct SimSessionDetails
    {
        public SimSessionType SessionType { get; set; }
        public FsdConnectionDetails? ConnectionDetails { get; set; }
        public ClientInfo? ClientInfo { get; set; }
    }
}