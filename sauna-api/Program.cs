using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace SaunaSim.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
            CreateHostBuilder(args).Build().Run();
            Console.WriteLine("Shutting Down from Main()");
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => {
                    webBuilder.UseStartup<Startup>();
                    if (CheckForUserDefinedPort(args, out ushort port))
                    {
                        webBuilder.UseUrls($"http://localhost:{port}");
                    }
                });


        static bool CheckForUserDefinedPort(string[] args, out ushort port)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i].ToLower() == "-p")
                {
                    ushort p;
                    if (UInt16.TryParse(args[i + 1], out p))
                    {
                        port = p;
                        return true;
                    }
                }
            }
            port = 0;
            return false;
        }
    }
}
