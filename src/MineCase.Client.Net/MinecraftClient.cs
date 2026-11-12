using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MineCase.Buffers;
using MineCase.Protocol;
using MineCase.Protocol.Handshaking;
using MineCase.Protocol.Login;
using MineCase.Protocol.Play;
using MineCase.Protocol.Status;
using MineCase.Serialization;
using ProtocolInfo = MineCase.Protocol.Protocol;

namespace MineCase.Client.Net
{
    /// <summary>
    /// Offline-mode Minecraft 1.15.2 connection to a MineCase gateway: logs in, keeps the session alive and turns
    /// the packets a renderer needs into <see cref="ClientEvent"/>s on <see cref="Events"/>.
    /// </summary>
    public sealed class MinecraftClient : IDisposable
    {
        private const uint LoginState = 2;
        private const uint StatusState = 1;

        /// <summary>
        /// Entity type id of players in the 1.15.2 registry.
        /// </summary>
        public const uint PlayerEntityType = 101;

        private readonly string _host;
        private readonly int _port;
        private readonly string _username;
        private readonly byte _viewDistance;
        private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);
        private readonly IPacketCompress _compress = new PacketCompress();
        private readonly IBufferPool<byte> _bufferPool = new BufferPool<byte>(ArrayPool<byte>.Shared);
        private readonly CancellationTokenSource _cancellation = new CancellationTokenSource();
        private readonly object _positionLock = new object();
        private readonly Dictionary<uint, (double X, double Y, double Z)> _entities = new Dictionary<uint, (double X, double Y, double Z)>();

        private TcpClient _tcpClient;
        private NetworkStream _stream;
        private Task _receiveLoop;
        private volatile int _compressionThreshold = -1;
        private bool _playing;
        private int _dimension;
        private double _x;
        private double _y;
        private double _z;
        private float _yaw;
        private float _pitch;

        public MinecraftClient(string host, int port, string username, byte viewDistance = 8)
        {
            _host = host;
            _port = port;
            _username = username;
            _viewDistance = viewDistance;
        }

        public ConcurrentQueue<ClientEvent> Events { get; } = new ConcurrentQueue<ClientEvent>();

        /// <summary>
        /// Gets or sets an optional callback receiving one line per packet sent or received, for debugging.
        /// </summary>
        public Action<string> Trace { get; set; }

        public bool IsConnected => _tcpClient?.Connected == true && _receiveLoop?.IsCompleted == false;

        /// <summary>
        /// Asks the server for its status JSON (server list ping) on a separate connection.
        /// </summary>
        /// <param name="host">Gateway host.</param>
        /// <param name="port">Gateway port.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The status JSON.</returns>
        public static async Task<string> QueryStatusAsync(string host, int port, CancellationToken cancellationToken = default)
        {
            using var tcpClient = new TcpClient { NoDelay = true };
            await tcpClient.ConnectAsync(host, port, cancellationToken);
            var stream = tcpClient.GetStream();
            await WritePacketAsync(stream, CreatePacket(new Handshake { ProtocolVersion = ProtocolInfo.Version, ServerAddress = host, ServerPort = (ushort)port, NextState = StatusState }), -1);
            await WritePacketAsync(stream, CreatePacket(new Request()), -1);
            var length = ProtocolInfo.ReadPacketLength(stream);
            var bufferPool = new BufferPool<byte>(ArrayPool<byte>.Shared);
            using var scope = bufferPool.CreateScope();
            var packet = await UncompressedPacket.DeserializeAsync(stream, length, scope);
            return DecodeStatus(packet);
        }

        public async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            _tcpClient = new TcpClient { NoDelay = true };
            await _tcpClient.ConnectAsync(_host, _port, cancellationToken);
            _stream = _tcpClient.GetStream();
            await SendAsync(new Handshake { ProtocolVersion = ProtocolInfo.Version, ServerAddress = _host, ServerPort = (ushort)_port, NextState = LoginState });
            await SendAsync(new LoginStart { Name = _username });
            _receiveLoop = Task.Run(ReceiveLoopAsync);
        }

        /// <summary>
        /// Reports the player's position; <paramref name="feetY"/> is the bottom of the player's bounding box.
        /// </summary>
        /// <param name="x">Block X.</param>
        /// <param name="feetY">Block Y of the feet.</param>
        /// <param name="z">Block Z.</param>
        /// <param name="yaw">Minecraft yaw in degrees (0 faces +Z, 90 faces -X).</param>
        /// <param name="pitch">Minecraft pitch in degrees (positive looks down).</param>
        /// <param name="onGround">Whether the player stands on a block.</param>
        /// <returns>A task completing when the packet is written.</returns>
        public Task SendPositionAsync(double x, double feetY, double z, float yaw, float pitch, bool onGround)
        {
            if (!_playing)
                return Task.CompletedTask;

            lock (_positionLock)
            {
                _x = x;
                _y = feetY;
                _z = z;
                _yaw = yaw;
                _pitch = pitch;
            }

            return SendAsync(new ServerboundPositionAndLook { X = x, FeetY = feetY, Z = z, Yaw = yaw, Pitch = pitch, OnGround = onGround });
        }

        public void Dispose()
        {
            _cancellation.Cancel();
            _tcpClient?.Dispose();
            _cancellation.Dispose();
        }

        private static UncompressedPacket CreatePacket<TPacket>(TPacket packet)
            where TPacket : IPacket
        {
            using var memory = new MemoryStream();
            using (var writer = new BinaryWriter(memory, Encoding.UTF8, true))
                packet.Serialize(writer);
            return new UncompressedPacket { PacketId = PacketId<TPacket>.Value, Data = memory.ToArray() };
        }

        private static async Task WritePacketAsync(Stream stream, UncompressedPacket packet, int compressionThreshold)
        {
            if (compressionThreshold >= 0)
                await new PacketCompress().Compress(packet, (uint)compressionThreshold).SerializeAsync(stream);
            else
                await packet.SerializeAsync(stream);
        }

        private static string DecodeStatus(UncompressedPacket packet)
        {
            var reader = new SpanReader(packet.Data);
            return PacketDeserializer.Deserialize<Response>(ref reader).JsonResponse;
        }

        private async Task SendAsync<TPacket>(TPacket packet)
            where TPacket : IPacket
        {
            var raw = CreatePacket(packet);
            Trace?.Invoke($"send {typeof(TPacket).Name} 0x{raw.PacketId:X2}");
            await _sendLock.WaitAsync();
            try
            {
                await WritePacketAsync(_stream, raw, _compressionThreshold);
            }
            finally
            {
                _sendLock.Release();
            }
        }

        private async Task ReceiveLoopAsync()
        {
            try
            {
                while (!_cancellation.IsCancellationRequested)
                {
                    using var scope = _bufferPool.CreateScope();
                    var length = ProtocolInfo.ReadPacketLength(_stream);
                    UncompressedPacket packet;
                    if (_compressionThreshold >= 0)
                        packet = _compress.Decompress(await CompressedPacket.DeserializeAsync(_stream, length), (uint)_compressionThreshold);
                    else
                        packet = await UncompressedPacket.DeserializeAsync(_stream, length, scope);

                    Trace?.Invoke($"recv 0x{packet.PacketId:X2} ({packet.Data.Count} bytes)");
                    var reply = _playing ? HandlePlayPacket(packet) : HandleLoginPacket(packet);
                    if (reply != null)
                        await reply();
                }
            }
            catch (Exception ex) when (_cancellation.IsCancellationRequested)
            {
                Events.Enqueue(new DisconnectedEvent("Disconnected.", ex));
            }
            catch (EndOfStreamException ex)
            {
                Events.Enqueue(new DisconnectedEvent("The server closed the connection.", ex));
            }
            catch (Exception ex)
            {
                Events.Enqueue(new DisconnectedEvent(ex.Message, ex));
            }
        }

        private Func<Task> HandleLoginPacket(UncompressedPacket packet)
        {
            var reader = new SpanReader(packet.Data);
            switch (packet.PacketId)
            {
                case 0x00:
                    throw new InvalidDataException("Login refused: " + PacketDeserializer.Deserialize<LoginDisconnect>(ref reader).Reason);
                case 0x01:
                    throw new NotSupportedException("The server is in online mode; only offline-mode servers are supported.");
                case 0x02:
                    var success = PacketDeserializer.Deserialize<LoginSuccess>(ref reader);
                    _playing = true;
                    Events.Enqueue(new LoggedInEvent(success.UUID, success.Username));
                    return null;
                case ProtocolInfo.SetCompressionPacketId:
                    _compressionThreshold = (int)PacketDeserializer.Deserialize<SetCompression>(ref reader).Threshold;
                    return null;
                default:
                    throw new InvalidDataException($"Unexpected login packet 0x{packet.PacketId:X2}.");
            }
        }

        private Func<Task> HandlePlayPacket(UncompressedPacket packet)
        {
            var reader = new SpanReader(packet.Data);
            switch (packet.PacketId)
            {
                case 0x26:
                    var join = PacketDeserializer.Deserialize<JoinGame>(ref reader);
                    _dimension = join.Dimension;
                    Events.Enqueue(new JoinedGameEvent(join.EID, join.GameMode, join.Dimension, join.ViewDistance));
                    return () => SendAsync(new ClientSettings { Locale = "en_us", ViewDistance = _viewDistance, ChatMode = 0, ChatColors = true, DisplayedSkinParts = 0x7F, MainHand = 1 });
                case 0x21:
                    var keepAliveId = PacketDeserializer.Deserialize<ClientboundKeepAlive>(ref reader).KeepAliveId;
                    return () => SendAsync(new ServerboundKeepAlive { KeepAliveId = keepAliveId });
                case 0x36:
                    var teleport = PacketDeserializer.Deserialize<ClientboundPositionAndLook>(ref reader);
                    Events.Enqueue(ApplyTeleport(teleport));
                    return () => SendAsync(new TeleportConfirm { TeleportId = teleport.TeleportId });
                case 0x22:
                    var chunk = ChunkData.Deserialize(ref reader, _dimension == 0);
                    Events.Enqueue(new ChunkLoadedEvent(ChunkColumn.FromPacket(chunk)));
                    return null;
                case 0x1E:
                    var unload = PacketDeserializer.Deserialize<UnloadChunk>(ref reader);
                    Events.Enqueue(new ChunkUnloadedEvent(unload.ChunkX, unload.ChunkZ));
                    return null;
                case 0x4F:
                    var time = PacketDeserializer.Deserialize<TimeUpdate>(ref reader);
                    Events.Enqueue(new TimeUpdatedEvent(time.WorldAge, time.TimeOfDay));
                    return null;
                case 0x49:
                    var health = PacketDeserializer.Deserialize<UpdateHealth>(ref reader);
                    Events.Enqueue(new HealthUpdatedEvent(health.Health, health.Food));
                    return null;
                case 0x00:
                    var spawnObject = PacketDeserializer.Deserialize<SpawnObject>(ref reader);
                    Events.Enqueue(TrackSpawn(spawnObject.EID, spawnObject.Type, spawnObject.X, spawnObject.Y, spawnObject.Z, spawnObject.Yaw.Degrees, spawnObject.Pitch.Degrees));
                    return null;
                case 0x03:
                    var spawnMob = PacketDeserializer.Deserialize<SpawnMob>(ref reader);
                    Events.Enqueue(TrackSpawn(spawnMob.EID, spawnMob.Type, spawnMob.X, spawnMob.Y, spawnMob.Z, spawnMob.Yaw.Degrees, spawnMob.Pitch.Degrees));
                    return null;
                case 0x05:
                    var spawnPlayer = PacketDeserializer.Deserialize<SpawnPlayer>(ref reader);
                    Events.Enqueue(TrackSpawn(spawnPlayer.EntityId, PlayerEntityType, spawnPlayer.X, spawnPlayer.Y, spawnPlayer.Z, spawnPlayer.Yaw.Degrees, spawnPlayer.Pitch.Degrees));
                    return null;
                case 0x29:
                    var move = PacketDeserializer.Deserialize<EntityRelativeMove>(ref reader);
                    EnqueueMove(move.EID, move.DeltaX, move.DeltaY, move.DeltaZ, null, null, move.OnGround);
                    return null;
                case 0x2A:
                    var moveLook = PacketDeserializer.Deserialize<EntityLookAndRelativeMove>(ref reader);
                    EnqueueMove(moveLook.EID, moveLook.DeltaX, moveLook.DeltaY, moveLook.DeltaZ, moveLook.Yaw.Degrees, moveLook.Pitch.Degrees, moveLook.OnGround);
                    return null;
                case 0x2B:
                    var look = PacketDeserializer.Deserialize<EntityLook>(ref reader);
                    EnqueueMove(look.EID, 0, 0, 0, look.Yaw.Degrees, look.Pitch.Degrees, look.OnGround);
                    return null;
                case 0x3C:
                    var headLook = PacketDeserializer.Deserialize<EntityHeadLook>(ref reader);
                    if (_entities.ContainsKey(headLook.EID))
                        Events.Enqueue(new EntityHeadTurnedEvent(headLook.EID, headLook.Yaw.Degrees));
                    return null;
                case 0x57:
                    var entityTeleport = PacketDeserializer.Deserialize<EntityTeleport>(ref reader);
                    _entities[entityTeleport.EID] = (entityTeleport.X, entityTeleport.Y, entityTeleport.Z);
                    Events.Enqueue(new EntityMovedEvent(entityTeleport.EID, entityTeleport.X, entityTeleport.Y, entityTeleport.Z, entityTeleport.Yaw.Degrees, entityTeleport.Pitch.Degrees, entityTeleport.OnGround));
                    return null;
                case 0x38:
                    var destroy = PacketDeserializer.Deserialize<DestroyEntities>(ref reader);
                    foreach (var id in destroy.EntityIds)
                        _entities.Remove(id);
                    Events.Enqueue(new EntitiesDestroyedEvent(destroy.EntityIds));
                    return null;
                default:
                    return null;
            }
        }

        private EntitySpawnedEvent TrackSpawn(uint entityId, uint entityType, double x, double y, double z, float yaw, float pitch)
        {
            _entities[entityId] = (x, y, z);
            return new EntitySpawnedEvent(entityId, entityType, x, y, z, yaw, pitch);
        }

        // Relative moves are in 1/4096 block; entities the client never saw spawn are ignored.
        private void EnqueueMove(uint entityId, short deltaX, short deltaY, short deltaZ, float? yaw, float? pitch, bool onGround)
        {
            if (!_entities.TryGetValue(entityId, out var position))
                return;

            position = (position.X + (deltaX / 4096.0), position.Y + (deltaY / 4096.0), position.Z + (deltaZ / 4096.0));
            _entities[entityId] = position;
            Events.Enqueue(new EntityMovedEvent(entityId, position.X, position.Y, position.Z, yaw, pitch, onGround));
        }

        private TeleportEvent ApplyTeleport(ClientboundPositionAndLook teleport)
        {
            lock (_positionLock)
            {
                _x = (teleport.Flags & 0x01) != 0 ? _x + teleport.X : teleport.X;
                _y = (teleport.Flags & 0x02) != 0 ? _y + teleport.Y : teleport.Y;
                _z = (teleport.Flags & 0x04) != 0 ? _z + teleport.Z : teleport.Z;
                _yaw = (teleport.Flags & 0x08) != 0 ? _yaw + teleport.Yaw : teleport.Yaw;
                _pitch = (teleport.Flags & 0x10) != 0 ? _pitch + teleport.Pitch : teleport.Pitch;
                return new TeleportEvent(_x, _y, _z, _yaw, _pitch);
            }
        }

        private static class PacketId<TPacket>
        {
            public static readonly uint Value = typeof(TPacket).GetCustomAttribute<PacketAttribute>()?.PacketId
                ?? throw new InvalidOperationException($"{typeof(TPacket).Name} has no [Packet] id.");
        }
    }
}
