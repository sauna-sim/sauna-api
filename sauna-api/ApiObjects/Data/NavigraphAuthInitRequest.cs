using System;
namespace SaunaSim.Api.ApiObjects.Data
{
    public class NavigraphAuthInitRequest
    {
        public string CodeChallenge { get; set; }
        public string CodeChallengeMethod { get; set; }
    }
}

