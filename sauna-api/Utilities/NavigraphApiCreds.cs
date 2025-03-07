namespace SaunaSim.Api.Utilities
{
    public class NavigraphApiCreds
    {
        public NavigraphApiCreds(string clientId,  string clientSecret, string apiAuthUrl)
        {
            ClientId = clientId;
            ClientSecret = clientSecret;
            ApiAuthUrl = apiAuthUrl;
        }

        public string ClientId { get; private set; }
        public string ClientSecret { get; private set; }
        public string ApiAuthUrl { get; private set; }
    }
}
