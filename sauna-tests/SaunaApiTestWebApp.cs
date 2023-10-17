using System;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;

namespace sauna_tests
{
	public class SaunaApiTestWebApp : WebApplicationFactory<SaunaSim.Api.Program>
	{
        protected override IHost CreateHost(IHostBuilder builder)
        {
            // shared extra set up goes here
            return base.CreateHost(builder);
        }
    }
}

