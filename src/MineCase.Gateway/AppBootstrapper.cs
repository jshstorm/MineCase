using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using MineCase.Buffers;
using MineCase.Gateway.Network;
using MineCase.Protocol;
using MineCase.Server;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Microsoft.Extensions.Hosting;

namespace MineCase.Gateway
{
    partial class Program
    {
        private static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
        {
            services.AddLogging();
            services.AddSingleton<ConnectionRouter>();
            services.AddSingleton<IPacketCompress, PacketCompress>();
            services.AddTransient<ClientSession>();
            services.AddHostedService<ConnectionRouter>();

            ConfigureObjectPools(services);
        }

        private static void ConfigureOrleansClient(HostBuilderContext context, IClientBuilder builder)
        {
            builder.Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "dev";
                    options.ServiceId = "MineCaseService";
                })
                // .UseLocalhostClustering(gatewayPort: 30000)
                .UseMongoDBClient(context.Configuration.GetSection("persistenceOptions")["connectionString"])
                .UseMongoDBClustering(options =>
                {
                    options.DatabaseName = context.Configuration.GetSection("persistenceOptions")["databaseName"];
                })
                .UseConnectionRetryFilter(RetryConnectToCluster);
        }

        private static async Task<bool> RetryConnectToCluster(Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Failed to connect to the MineCase cluster, retrying in 5 seconds: {exception.Message}");
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            return true;
        }

        private static void ConfigureObjectPools(IServiceCollection services)
        {
            services.AddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();
            services.AddSingleton<ObjectPool<UncompressedPacket>>(s =>
            {
                var provider = s.GetRequiredService<ObjectPoolProvider>();
                return provider.Create<UncompressedPacket>();
            });
            services.AddSingleton<IBufferPool<byte>>(s => new BufferPool<byte>(ArrayPool<byte>.Shared));
        }

        private static void ConfigureAppConfiguration(HostBuilderContext context, IConfigurationBuilder builder)
        {
            builder.SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("config.json", false, false);
        }

        private static void ConfigureLogging(ILoggingBuilder loggingBuilder)
        {
            loggingBuilder.AddConsole();
        }
    }
}
