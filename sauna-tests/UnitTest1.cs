using AviationCalcUtilNet.GeoTools.MagneticTools;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using SaunaSim.Api.Controllers;

namespace sauna_tests;

public class Tests
{

    [SetUp]
    public async Task Setup()
    {
    }

    [Test]
    public async Task Test1()
    {
        var application = new SaunaApiTestWebApp();
        var client = application.CreateClient();


        var response = await client.GetStringAsync("/api/data/settings");
       
        Assert.IsNotEmpty(response);
    }
}
