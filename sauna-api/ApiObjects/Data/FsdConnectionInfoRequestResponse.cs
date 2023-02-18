using FsdConnectorNet;

namespace SaunaSim.Api.ApiObjects.Data
{
    public class FsdConnectionInfoRequestResponse
    {
        public string NetworkId { get; set; }
        public string Password { get; set; }
        public string Hostname { get; set; }
        public int Port { get; set; }
        public ProtocolRevision Protocol { get; set; }
    }
}
