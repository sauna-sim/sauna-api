namespace SaunaSim.Api.ApiObjects.Server
{
    public class ApiServerInfoResponse
    {
        public string ServerId { get; set; }
        public VersionInfo Version { get; set; }

        public struct VersionInfo
        {
            public uint Major { get; set; }
            public uint Minor { get; set; }
            public uint Revision { get; set; }

            public VersionInfo(uint major, uint minor, uint revision)
            {
                Major = major;
                Minor = minor;
                Revision = revision;
            }
        }
    }
}