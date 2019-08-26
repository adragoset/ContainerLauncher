using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;


namespace Launcher
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                var config_path = System.IO.Path.Combine(AppContext.BaseDirectory);
                if (Environment.GetEnvironmentVariable("CONFIG_PATH") != null)
                {
                    System.Console.WriteLine("setting the config path");
                    config_path = Environment.GetEnvironmentVariable("CONFIG_PATH");
                }
                config.SetBasePath(config_path);
            })
            .ConfigureLogging((hostingContext, logging) =>
            {
                // Requires `using Microsoft.Extensions.Logging;`
                logging.AddConfiguration(hostingContext.Configuration.GetSection("logging"));
                logging.AddConsole();
                logging.AddDebug();
                logging.AddEventSourceLogger();
            })
            .UseStartup<Startup>();
    }
}
