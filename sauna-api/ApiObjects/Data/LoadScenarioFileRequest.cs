using FsdConnectorNet;

namespace SaunaSim.Api.ApiObjects.Data
{
    public class LoadScenarioFileRequest
    {
        public string FileName { get; set; }
        public string Cid { get; set; }
        public string Password { get; set; }
        public string Server { get; set; }
        public int Port { get; set; }
        public ProtocolRevision Protocol { get; set; }
    }
}