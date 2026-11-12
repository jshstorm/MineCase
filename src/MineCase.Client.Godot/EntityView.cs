using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MineCase.Client.Net;
using MineCase.Client.Net.Rendering;
using NVector3 = System.Numerics.Vector3;

namespace MineCase.GodotClient
{
    /// <summary>
    /// Mobs and other players: smooths the positions the server sends, faces bodies along their path and swings limbs while walking.
    /// </summary>
    public partial class EntityView : Node3D
    {
        private const float MaxHeadTurn = 70;

        private readonly Dictionary<uint, EntityNode> _entities = new Dictionary<uint, EntityNode>();
        private readonly Dictionary<uint, (StandardMaterial3D Material, MobMesh Mesh, ArrayMesh[] Parts)> _models = new Dictionary<uint, (StandardMaterial3D, MobMesh, ArrayMesh[])>();

        public int Count => _entities.Count;

        public void Spawn(EntitySpawnedEvent e)
        {
            Remove(e.EntityId);
            var (material, mesh, parts) = GetModel(e.EntityType);
            var node = new EntityNode { Name = $"Entity_{e.EntityId}_{mesh.Name}" };
            node.Setup(mesh, parts, material, new Vector3((float)e.X, (float)e.Y, (float)e.Z), e.Yaw);
            _entities[e.EntityId] = node;
            AddChild(node);
        }

        public void Move(EntityMovedEvent e)
        {
            if (_entities.TryGetValue(e.EntityId, out var node))
                node.MoveTo(new Vector3((float)e.X, (float)e.Y, (float)e.Z), e.Pitch);
        }

        public void TurnHead(EntityHeadTurnedEvent e)
        {
            if (_entities.TryGetValue(e.EntityId, out var node))
                node.HeadYaw = e.HeadYaw;
        }

        public void Destroy(EntitiesDestroyedEvent e)
        {
            foreach (var id in e.EntityIds)
                Remove(id);
        }

        public void Clear()
        {
            foreach (var id in _entities.Keys.ToList())
                Remove(id);
        }

        public override void _Process(double delta)
        {
            foreach (var node in _entities.Values)
                node.Animate((float)delta);
        }

        private void Remove(uint id)
        {
            if (_entities.Remove(id, out var node))
                node.QueueFree();
        }

        private (StandardMaterial3D Material, MobMesh Mesh, ArrayMesh[] Parts) GetModel(uint entityType)
        {
            if (_models.TryGetValue(entityType, out var model))
                return model;

            var mesh = MobModels.Build(EntityTypeTable.Get(entityType));
            var image = Image.CreateFromData(mesh.Texture.Width, mesh.Texture.Height, false, Image.Format.Rgba8, mesh.Texture.Pixels);
            var material = new StandardMaterial3D
            {
                AlbedoTexture = ImageTexture.CreateFromImage(image),
                TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest,
                VertexColorUseAsAlbedo = true,
                VertexColorIsSrgb = true,
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            };
            var parts = mesh.Parts.Select(p => WorldView.ToArrayMesh(p.Mesh)).ToArray();
            model = (material, mesh, parts);
            _models[entityType] = model;
            return model;
        }

        private static float ApproachAngle(float current, float target, float maxStep)
        {
            var difference = Mathf.Wrap(target - current, -180f, 180f);
            return current + Mathf.Clamp(difference, -maxStep, maxStep);
        }

        private partial class EntityNode : Node3D
        {
            private readonly List<(Node3D Pivot, MobMeshPart Part)> _parts = new List<(Node3D, MobMeshPart)>();
            private Vector3 _target;
            private float _bodyYaw;
            private float _pitch;
            private float _speed;
            private float _walkPhase;
            private float _time;

            public float HeadYaw { get; set; }

            public void Setup(MobMesh mesh, ArrayMesh[] meshes, Material material, Vector3 position, float yaw)
            {
                Position = position;
                _target = position;
                _bodyYaw = yaw;
                HeadYaw = yaw;
                for (var i = 0; i < mesh.Parts.Count; i++)
                {
                    var part = mesh.Parts[i];
                    var pivot = new Node3D { Position = ToGodot(part.Pivot), RotationDegrees = ToGodot(part.RestRotation) };
                    pivot.AddChild(new MeshInstance3D { Mesh = meshes[i], MaterialOverride = material });
                    AddChild(pivot);
                    _parts.Add((pivot, part));
                }

                RotationDegrees = new Vector3(0, -_bodyYaw, 0);
            }

            public void MoveTo(Vector3 target, float? pitch)
            {
                // Far jumps (teleports, chunk re-sends) snap instead of gliding.
                if (target.DistanceTo(Position) > 8)
                    Position = target;
                _target = target;
                if (pitch.HasValue)
                    _pitch = pitch.Value;
            }

            public void Animate(float delta)
            {
                _time += delta;
                var previous = Position;
                Position = Position.Lerp(_target, 1 - Mathf.Exp(-12 * delta));
                var step = Position - previous;
                var horizontal = new Vector2(step.X, step.Z);
                var speed = delta > 0 ? horizontal.Length() / delta : 0;
                _speed = Mathf.Lerp(_speed, speed, 1 - Mathf.Exp(-8 * delta));

                // Face where the mob walks; when it stands still, turn the body once the head is turned far.
                if (horizontal.Length() > 0.002f)
                    _bodyYaw = ApproachAngle(_bodyYaw, Mathf.RadToDeg(Mathf.Atan2(-step.X, step.Z)), 360 * delta);
                else if (Mathf.Abs(Mathf.Wrap(HeadYaw - _bodyYaw, -180f, 180f)) > 50)
                    _bodyYaw = ApproachAngle(_bodyYaw, HeadYaw, 90 * delta);
                RotationDegrees = new Vector3(0, -_bodyYaw, 0);

                _walkPhase += delta * _speed * 5.5f;
                var swing = Mathf.Clamp(_speed / 2.5f, 0, 1) * 40;
                var headTurn = Mathf.Clamp(Mathf.Wrap(HeadYaw - _bodyYaw, -180f, 180f), -MaxHeadTurn, MaxHeadTurn);
                foreach (var (pivot, part) in _parts)
                {
                    var rest = ToGodot(part.RestRotation);
                    var wave = Mathf.Sin(_walkPhase + (part.Phase * Mathf.Pi));
                    pivot.RotationDegrees = part.Motion switch
                    {
                        PartMotion.Leg => rest + new Vector3(wave * swing, 0, 0),
                        PartMotion.Arm => rest + new Vector3(-wave * swing * 0.7f, 0, 0),
                        PartMotion.Wing => rest + new Vector3(0, 0, (part.Phase == 0 ? 1 : -1) * (swing > 2 ? 35 + (Mathf.Sin(_time * 22) * 35) : 0)),
                        PartMotion.SpiderLeg => rest + new Vector3(0, wave * swing * 0.6f, Mathf.Abs(wave) * swing * 0.3f),
                        PartMotion.Head => rest + new Vector3(_pitch, -headTurn, 0),
                        _ => rest,
                    };
                }
            }

            private static Vector3 ToGodot(NVector3 v) => new Vector3(v.X, v.Y, v.Z);
        }
    }
}
