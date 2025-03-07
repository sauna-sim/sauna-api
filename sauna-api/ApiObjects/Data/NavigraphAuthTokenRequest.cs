using System;
namespace SaunaSim.Api.ApiObjects.Data
{
    public class NavigraphAuthTokenRequest
    {
        public string CodeVerifier { get; set; }
        public string GrantType { get; set; }
        public string DeviceCode { get; set; }
        public string Scope { get; set; }
        public string RefreshToken { get; set; }
    }
}

