using FsdConnectorNet;
using SaunaSim.Core.Data.Scenario;

namespace SaunaSim.Api.ApiObjects.Data
{
    public class LoadScenarioFileRequest
    {
        public SaunaScenario Scenario { get; set; }
        public string FileName { get; set; }
        public string Cid { get; set; }
        public string Password { get; set; }
        public string Server { get; set; }
        public int Port { get; set; }
        public ProtocolRevision Protocol { get; set; }
    }
}