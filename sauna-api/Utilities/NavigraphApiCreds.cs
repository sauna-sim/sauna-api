namespace SaunaSim.Api.Utilities
{
    public class NavigraphApiCreds
    {
        public NavigraphApiCreds(string clientId,  string clientSecret)
        {
            ClientId = clientId;
            ClientSecret = clientSecret;
        }

        public string ClientId { get; private set; }
        public string ClientSecret { get; private set;}
    }
}
