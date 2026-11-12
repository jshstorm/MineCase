using System.Threading.Tasks;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MineCase.Serialization;
using MineCase.Serialization.Serializers;
using Orleans;
using Orleans.Configuration;
using Orleans.Dashboard;
using Orleans.Hosting;
using Orleans.Runtime;
using Orleans.Serialization;

namespace MineCase.Server
{
    partial class Program
    {
        static async Task Main(string[] args)
        {
            var createShardKey = false;
            Serializers.RegisterAll();

            var hostBuilder = new HostBuilder()
                .UseServiceProviderFactory(x => new AutofacServiceProviderFactory(ConfigureAutofac))
                .ConfigureAppConfiguration(ConfigureAppConfiguration)
                .ConfigureServices(ConfigureServices)
                .ConfigureLogging(ConfigureLogging)
                .UseConsoleLifetime()
                .UseOrleans((Microsoft.Extensions.Hosting.HostBuilderContext context, ISiloBuilder builder) =>
                {
                    builder.Configure<ClusterOptions>(options =>
                    {
                        options.ClusterId = "dev";
                        options.ServiceId = "MineCaseService";
                    })
                    .AddOutgoingGrainCallFilter(AllowCallChainReentrancy)
                    .ConfigureServices(services => services.AddSerializer(serializer => serializer.AddMineCaseSerializers()))
                    .ConfigureEndpoints(
                        siloPort: context.Configuration.GetValue("orleans:siloPort", 11111),
                        gatewayPort: context.Configuration.GetValue("orleans:gatewayPort", 30000))
                    .UseMongoDBClient(context.Configuration.GetSection("persistenceOptions")["connectionString"])
                    .AddMemoryStreams("JobsProvider")
                    .AddMemoryStreams("TransientProvider")
                    .UseMongoDBReminders(options =>
                    {
                        options.DatabaseName = context.Configuration.GetSection("persistenceOptions")["databaseName"];
                        options.CreateShardKeyForCosmos = createShardKey;
                    })
                    .UseMongoDBClustering(c =>
                    {
                        c.DatabaseName = context.Configuration.GetSection("persistenceOptions")["databaseName"];
                        c.CreateShardKeyForCosmos = createShardKey;
                    })
                    .AddDashboard()
                    .AddMongoDBGrainStorageAsDefault(options =>
                    {
                        options.DatabaseName = context.Configuration.GetSection("persistenceOptions")["databaseName"];
                        options.CreateShardKeyForCosmos = createShardKey;
                    })
                    .AddMongoDBGrainStorage("PubSubStore", options =>
                    {
                        options.DatabaseName = context.Configuration.GetSection("persistenceOptions")["databaseName"];
                        options.CreateShardKeyForCosmos = createShardKey;
                    });
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseUrls("http://*:8080");
                    webBuilder.Configure(app =>
                    {
                        app.UseRouting();
                        app.UseEndpoints(endpoints => endpoints.MapOrleansDashboard());
                    });
                });

            var host = hostBuilder.Build();
            Serializers.RegisterAll(host.Services);
            await host.RunAsync();
        }

        // Orleans 3 had a silo-wide SchedulingOptions.AllowCallChainReentrancy switch. Orleans 7+ scopes it per call,
        // so every outgoing call opens a reentrant section to keep the A -> B -> A call chains working.
        private static async Task AllowCallChainReentrancy(IOutgoingGrainCallContext context)
        {
            using (RequestContext.AllowCallChainReentrancy())
            {
                await context.Invoke();
            }
        }
    }
}
