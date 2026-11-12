using System;

namespace MineCase.Client.Net
{
    /// <summary>
    /// Something the server told the client; drained by the game loop.
    /// </summary>
    public abstract class ClientEvent
    {
    }

    public sealed class LoggedInEvent : ClientEvent
    {
        public LoggedInEvent(string uuid, string username)
        {
            Uuid = uuid;
            Username = username;
        }

        public string Uuid { get; }

        public string Username { get; }
    }

    public sealed class JoinedGameEvent : ClientEvent
    {
        public JoinedGameEvent(int entityId, byte gameMode, int dimension, uint viewDistance)
        {
            EntityId = entityId;
            GameMode = gameMode;
            Dimension = dimension;
            ViewDistance = viewDistance;
        }

        public int EntityId { get; }

        public byte GameMode { get; }

        public int Dimension { get; }

        public uint ViewDistance { get; }
    }

    public sealed class TeleportEvent : ClientEvent
    {
        public TeleportEvent(double x, double y, double z, float yaw, float pitch)
        {
            X = x;
            Y = y;
            Z = z;
            Yaw = yaw;
            Pitch = pitch;
        }

        public double X { get; }

        public double Y { get; }

        public double Z { get; }

        public float Yaw { get; }

        public float Pitch { get; }
    }

    public sealed class ChunkLoadedEvent : ClientEvent
    {
        public ChunkLoadedEvent(ChunkColumn column)
        {
            Column = column;
        }

        public ChunkColumn Column { get; }
    }

    public sealed class ChunkUnloadedEvent : ClientEvent
    {
        public ChunkUnloadedEvent(int chunkX, int chunkZ)
        {
            ChunkX = chunkX;
            ChunkZ = chunkZ;
        }

        public int ChunkX { get; }

        public int ChunkZ { get; }
    }

    public sealed class TimeUpdatedEvent : ClientEvent
    {
        public TimeUpdatedEvent(long worldAge, long timeOfDay)
        {
            WorldAge = worldAge;
            TimeOfDay = timeOfDay;
        }

        public long WorldAge { get; }

        public long TimeOfDay { get; }
    }

    public sealed class HealthUpdatedEvent : ClientEvent
    {
        public HealthUpdatedEvent(float health, uint food)
        {
            Health = health;
            Food = food;
        }

        public float Health { get; }

        public uint Food { get; }
    }

    /// <summary>
    /// A mob, object or other player appeared. <see cref="EntityType"/> is the 1.15.2 entity registry id.
    /// </summary>
    public sealed class EntitySpawnedEvent : ClientEvent
    {
        public EntitySpawnedEvent(uint entityId, uint entityType, double x, double y, double z, float yaw, float pitch)
        {
            EntityId = entityId;
            EntityType = entityType;
            X = x;
            Y = y;
            Z = z;
            Yaw = yaw;
            Pitch = pitch;
        }

        public uint EntityId { get; }

        public uint EntityType { get; }

        public double X { get; }

        public double Y { get; }

        public double Z { get; }

        public float Yaw { get; }

        public float Pitch { get; }
    }

    /// <summary>
    /// New absolute position of an entity; yaw and pitch are null when the packet did not change them.
    /// </summary>
    public sealed class EntityMovedEvent : ClientEvent
    {
        public EntityMovedEvent(uint entityId, double x, double y, double z, float? yaw, float? pitch, bool onGround)
        {
            EntityId = entityId;
            X = x;
            Y = y;
            Z = z;
            Yaw = yaw;
            Pitch = pitch;
            OnGround = onGround;
        }

        public uint EntityId { get; }

        public double X { get; }

        public double Y { get; }

        public double Z { get; }

        public float? Yaw { get; }

        public float? Pitch { get; }

        public bool OnGround { get; }
    }

    public sealed class EntityHeadTurnedEvent : ClientEvent
    {
        public EntityHeadTurnedEvent(uint entityId, float headYaw)
        {
            EntityId = entityId;
            HeadYaw = headYaw;
        }

        public uint EntityId { get; }

        public float HeadYaw { get; }
    }

    public sealed class EntitiesDestroyedEvent : ClientEvent
    {
        public EntitiesDestroyedEvent(uint[] entityIds)
        {
            EntityIds = entityIds;
        }

        public uint[] EntityIds { get; }
    }

    public sealed class DisconnectedEvent : ClientEvent
    {
        public DisconnectedEvent(string reason, Exception exception = null)
        {
            Reason = reason;
            Exception = exception;
        }

        public string Reason { get; }

        public Exception Exception { get; }
    }
}
