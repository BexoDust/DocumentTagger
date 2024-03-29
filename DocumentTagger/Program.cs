using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Serilog;

namespace DocumentTagger
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    IConfiguration configuration = hostContext.Configuration;

                    WorkerOptions options = configuration.GetSection("DT").Get<WorkerOptions>();

                    services.AddLogging(builder =>
                    {
                        builder.AddConfiguration(configuration.GetSection("Logging"))
                          .AddSerilog(new LoggerConfiguration().WriteTo.File(options.LogPath).CreateLogger())
                          .AddConsole();
#if DEBUG
                        builder.AddDebug();
#endif
                    });

                    services.AddSingleton(options);
                    services.AddHostedService<Worker>();
                });
    }
}
