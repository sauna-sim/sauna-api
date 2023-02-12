namespace AselAtcTrainingSim.AselApi.ApiObjects.Data
{
    public class LoadScenarioFileRequest
    {
        public string FileName { get; set; }
        public string Cid { get; set; }
        public string Password { get; set; }
        public string Server { get; set; }
        public int Port { get; set; }
        public bool VatsimServer { get; set; }
        public string Protocol { get; set; }
    }
}