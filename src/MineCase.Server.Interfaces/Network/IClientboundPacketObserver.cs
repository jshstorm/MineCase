using System;
using System.Collections.Generic;
using System.Text;
using MineCase.Protocol;
using Orleans;

namespace MineCase.Server.Network
{
    /// <summary>
    /// Receives a session's outgoing packets. Orleans does not keep one-way calls in order, so every call carries
    /// the sink activation's stream id and a sequence number; the observer must deliver in sequence order.
    /// </summary>
    public interface IClientboundPacketObserver : IGrainObserver
    {
        void ReceivePacket(Guid stream, long sequence, UncompressedPacket packet);

        void OnClosed(Guid stream, long sequence);
    }
}
