using AviationCalcUtilNet.GeoTools.MagneticTools;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SaunaSim.Core.Simulator.Aircraft;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;


namespace SaunaSim.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers()
                // Add JSON Serialization Options
                .AddJsonOptions(options => {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                    options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals;
                });
        }

        private void OnShutdown()
        {
            SimAircraftHandler.DeleteAllAircraft();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime applicationLifetime, ILogger<Startup> logger)
        {
            // Load Magnetic File
            try
            {
                MagneticUtil.LoadData();
                logger.LogInformation("Magnetic File Loaded");
            }
            catch (Exception)
            {
                logger.LogError("There was an error loading the WMM.COF file. Ensure that WMM.COF is placed in the 'magnetic' folder.");
            }

            // Register shutdown
            applicationLifetime.ApplicationStopping.Register(OnShutdown);

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints => {
                endpoints.MapControllers();
            });
        }
    }
}
