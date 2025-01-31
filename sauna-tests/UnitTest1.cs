
using AviationCalcUtilNet.Math;
using FsdConnectorNet;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using SaunaSim.Api.Controllers;
using SaunaSim.Api.Utilities;
using System.Diagnostics;

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
       
        Assert.That(response, Is.Not.Empty);
    }

    [Test]
    public void LineEquationTests()
    {
        (double x1, double y1) = (1, 1);
        (double x2, double y2) = (2, 2);
        (double x3, double y3) = (0, 1);
        (double x4, double y4) = (1, 0);

        var p1 = MathUtil.CreateLineEquation(x1, y1, x2, y2);
        var p2 = MathUtil.CreateLineEquation(x3, y3, x4, y4);

        (double intX, double intY) = MathUtil.Find2LinesIntersection(p1.Coefficients[1], p1.Coefficients[0], p2.Coefficients[1], p2.Coefficients[0]);

        Assert.That(intX, Is.EqualTo(0.5));
        Assert.That(intY, Is.EqualTo(0.5));
    }

    [Test]
    public void Test2()
    {
        ClientInfo vatsimInfo = PrivateInfoLoader.GetClientInfo((string s) => { Debug.WriteLine(s); });
        vatsimInfo.ToString();
    }
}
