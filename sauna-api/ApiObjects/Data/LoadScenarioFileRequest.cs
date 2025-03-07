using FsdConnectorNet;
using SaunaSim.Core.Data.Scenario;

namespace SaunaSim.Api.ApiObjects.Data
{
    public class LoadScenarioFileRequest
    {
        public SaunaScenario Scenario { get; set; }
        public string FileName { get; set; }
    }
}