using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using NLog.Web;
using Microsoft.Extensions.Logging;
using System.Net;

namespace DocsChain
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var logger = NLog.Web.NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger();
            try
            {
                logger.Debug("init main");
                BuildWebHost(args).Run();

            }
            catch (Exception ex)
            {
                logger.Error(ex,"Main Crashed");
                throw;
            }
            finally
            {
                // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
                NLog.LogManager.Shutdown();
            }

        }

        public static IWebHost BuildWebHost(string[] args) {
            var logger = NLog.LogManager.LoadConfiguration("nlog.config").GetCurrentClassLogger();
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            var webHost = WebHost.CreateDefaultBuilder(args)
                                                      
                           .UseContentRoot(Directory.GetCurrentDirectory())
                           .ConfigureLogging(logging =>
                           {
                               logging.ClearProviders();
                               logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
                           })  
                           .UseNLog()
                           .ConfigureAppConfiguration((hostingContext, config) =>
                           {
                               var env = hostingContext.HostingEnvironment;
                               config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                                     .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);
                               config.AddEnvironmentVariables();
                               config.AddCommandLine(args);
                           })
                           .UseKestrel((options)=>
                           {
                               // Get host name
                               String strHostName = Dns.GetHostName();

                               // Find host by name
                               IPHostEntry ipHostEntry = Dns.GetHostEntry(strHostName);

                               // Enumerate IP addresses
                               foreach (IPAddress ipaddress in ipHostEntry.AddressList)
                               {
                                   if (ipaddress.AddressFamily== System.Net.Sockets.AddressFamily.InterNetwork)
                                        logger.Info($"Listening on IP {ipaddress.ToString()}");
                               }
                               options.Listen(IPAddress.Any, Convert.ToInt32(configuration["NodeIdentification:ListeningPort"]));
                           }
                           )
                           .UseStartup<Startup>()
                           .Build();


            return webHost;
        }
           
    }
}
