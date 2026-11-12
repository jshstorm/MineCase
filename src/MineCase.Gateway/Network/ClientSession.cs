using Microsoft.Extensions.ObjectPool;
using MineCase.Buffers;
using MineCase.Protocol;
using MineCase.Serialization;
using MineCase.Server.Game;
using MineCase.Server.Network;
using Orleans;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace MineCase.Gateway.Network
{
    class ClientSession : IDisposable
    {
        private readonly TcpClient _tcpClient;
        private Stream _remoteStream;
        private readonly IClusterClient _grainFactory;
        private volatile bool _useCompression = false;
        private readonly Guid _sessionId;
        private readonly OutcomingPacketObserver _outcomingPacketObserver;
        private IClientboundPacketObserver _clientboundPacketObserverRef;
        private readonly ActionBlock<object> _outcomingPacketDispatcher;
        private readonly ObjectPool<UncompressedPacket> _uncompressedPacketObjectPool;
        private readonly IBufferPool<byte> _bufferPool;
        private readonly IPacketCompress _packetCompress;

        private uint _compressThreshold;

        public ClientSession(TcpClient tcpClient, IClusterClient grainFactory, IBufferPool<byte> bufferPool, ObjectPool<UncompressedPacket> uncompressedPacketObjectPool, IPacketCompress packetCompress)
        {
            _sessionId = Guid.NewGuid();
            _tcpClient = tcpClient;
            _grainFactory = grainFactory;
            _bufferPool = bufferPool;
            _packetCompress = packetCompress;
            _uncompressedPacketObjectPool = uncompressedPacketObjectPool;
            _outcomingPacketObserver = new OutcomingPacketObserver(this);
            _outcomingPacketDispatcher = new ActionBlock<object>(SendOutcomingPacket);
        }

        public async Task Startup(CancellationToken cancellationToken)
        {
            using (_remoteStream = _tcpClient.GetStream())
            {
                // subscribe observer to get packet from server
                _clientboundPacketObserverRef = _grainFactory.CreateObjectReference<IClientboundPacketObserver>(_outcomingPacketObserver);
                await _grainFactory.GetGrain<IClientboundPacketSink>(_sessionId).Subscribe(_clientboundPacketObserverRef);
                try
                {
                    DateTime expiredTime = DateTime.Now + TimeSpan.FromSeconds(10);
                    while (!cancellationToken.IsCancellationRequested &&
                        !_outcomingPacketDispatcher.Completion.IsCompleted)
                    {
                        await DispatchIncomingPacket();
                        // renew subscribe, 10 sec
                        if (DateTime.Now > expiredTime)
                        {
                            await _grainFactory.GetGrain<IClientboundPacketSink>(_sessionId).Subscribe(_clientboundPacketObserverRef);
                            expiredTime = DateTime.Now + TimeSpan.FromSeconds(10);
                        }
                    }
                }
                catch (EndOfStreamException)
                {
                    var router = _grainFactory.GetGrain<IPacketRouter>(_sessionId);
                    await router.Close();

                    await _outcomingPacketDispatcher.Completion;
                }
            }
        }

        private void OnClosed()
        {
            _outcomingPacketDispatcher.Post(null);
        }

        private async Task DispatchIncomingPacket()
        {
            using (var bufferScope = _bufferPool.CreateScope())
            {
                // Compression is switched on by the outgoing side while this read may already be waiting,
                // so pick the format only once the next packet has started arriving.
                var length = Protocol.Protocol.ReadPacketLength(_remoteStream);
                UncompressedPacket packet;
                if (_useCompression)
                {
                    var compressedPacket = await CompressedPacket.DeserializeAsync(_remoteStream, length, null);
                    packet = _packetCompress.Decompress(compressedPacket, _compressThreshold);
                }
                else
                {
                    packet = await UncompressedPacket.DeserializeAsync(_remoteStream, length, bufferScope);
                }

                await DispatchIncomingPacket(packet);
            }
        }

        private async Task SendOutcomingPacket(object packetOrCommand)
        {
            if (packetOrCommand == null)
            {
                _tcpClient.Client.Shutdown(SocketShutdown.Send);
                _outcomingPacketDispatcher.Complete();
            }
            else if (packetOrCommand is UncompressedPacket packet)
            {
                using (var bufferScope = _bufferPool.CreateScope())
                {
                    if (_useCompression)
                    {
                        var newPacket = _packetCompress.Compress(packet, _compressThreshold);
                        await newPacket.SerializeAsync(_remoteStream);
                    }
                    else
                    {
                        // The client answers SetCompression with compressed packets, so incoming reads switch before it is sent.
                        if (packet.PacketId == Protocol.Protocol.SetCompressionPacketId)
                        {
                            _compressThreshold = GetCompressionThreshold(packet);
                            _useCompression = true;
                        }

                        await packet.SerializeAsync(_remoteStream);
                    }
                }
            }
        }

        private static uint GetCompressionThreshold(UncompressedPacket packet)
        {
            var br = new SpanReader(packet.Data);
            return br.ReadAsVarInt(out _);
        }

        private async Task DispatchIncomingPacket(UncompressedPacket packet)
        {
            var router = _grainFactory.GetGrain<IPacketRouter>(_sessionId);
            await router.SendPacket(packet);
        }

        private async void DispatchOutcomingPacket(object packet)
        {
            try
            {
                if (!_outcomingPacketDispatcher.Completion.IsCompleted)
                    await _outcomingPacketDispatcher.SendAsync(packet);
            }
            catch
            {
                _outcomingPacketDispatcher.Complete();
            }
        }

        // Puts the sink's packets back in sequence order before they reach the socket.
        class OutcomingPacketObserver : IClientboundPacketObserver
        {
            // Give up on a missing packet once this many later ones are waiting (a dropped message would stall the session otherwise).
            private const int MaxPendingPackets = 1024;

            private readonly ClientSession _session;
            private readonly object _lock = new object();
            private readonly SortedDictionary<long, UncompressedPacket> _pending = new SortedDictionary<long, UncompressedPacket>();
            private Guid _stream;
            private long _expected;

            public OutcomingPacketObserver(ClientSession session)
            {
                _session = session;
            }

            public void OnClosed(Guid stream, long sequence)
            {
                Accept(stream, sequence, null);
            }

            public void ReceivePacket(Guid stream, long sequence, UncompressedPacket packet)
            {
                Accept(stream, sequence, packet);
            }

            // A null packet marks the close notification.
            private void Accept(Guid stream, long sequence, UncompressedPacket packet)
            {
                lock (_lock)
                {
                    if (stream != _stream)
                    {
                        // A new sink activation numbers its packets from zero again.
                        _stream = stream;
                        _expected = 0;
                        _pending.Clear();
                    }

                    if (sequence < _expected)
                        return;

                    _pending[sequence] = packet;
                    if (_pending.Count > MaxPendingPackets)
                        _expected = _pending.Keys.First();

                    while (_pending.Remove(_expected, out var next))
                    {
                        _expected++;
                        if (next == null)
                            _session.OnClosed();
                        else
                            _session.DispatchOutcomingPacket(next);
                    }
                }
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // 要检测冗余调用

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _tcpClient.Dispose();
                }
                disposedValue = true;
            }
        }
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}