using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MineCase.Protocol;
using MineCase.Server.Network;
using MineCase.Server.Network.Play;
using MineCase.Server.User;
using MineCase.Server.World;
using MineCase.World;
using Orleans;
using Orleans.Concurrency;
using Orleans.Streams;

namespace MineCase.Server.Game
{
    [GenerateSerializer]
    public sealed class SendChunkJob
    {
        [Id(0)]
        public IWorld World { get; set; }

        [Id(1)]
        public ChunkWorldPos ChunkPosition { get; set; }

        [Id(2)]
        public IReadOnlyCollection<IClientboundPacketSink> Clients { get; set; }

        [Id(3)]
        public IReadOnlyCollection<IUserChunkLoader> Loaders { get; set; }
    }

    internal interface IChunkSenderJobWorker : IGrainWithGuidKey
    {
    }

    [ImplicitStreamSubscription(StreamProviders.Namespaces.ChunkSender)]
    [Reentrant]
    internal class ChunkSenderJobWorker : Grain, IChunkSenderJobWorker
    {
        private readonly IPacketPackager _packetPackager;

        public ChunkSenderJobWorker(IPacketPackager packetPackager)
        {
            _packetPackager = packetPackager;
        }

        public override async Task OnActivateAsync(CancellationToken cancellationToken)
        {
            var stream = this.GetStreamProvider(StreamProviders.JobsProvider).GetStream<SendChunkJob>(StreamProviders.Namespaces.ChunkSender, this.GetPrimaryKey());
            await stream.SubscribeAsync(OnNextAsync);
        }

        private async Task OnNextAsync(SendChunkJob job, StreamSequenceToken token)
        {
            var chunkColumn = GrainFactory.GetGrain<IChunkColumn>(job.World.MakeAddressByPartitionKey(job.ChunkPosition));

            var generator = new ClientPlayPacketGenerator(new BroadcastPacketSink(job.Clients, _packetPackager));
            await generator.ChunkData(Dimension.Overworld, job.ChunkPosition.X, job.ChunkPosition.Z, await chunkColumn.GetState());
            foreach (var loader in job.Loaders)
                loader.OnChunkSent(job.ChunkPosition).Ignore();
        }
    }
}
