using Godot;

namespace MineCase.GodotClient
{
    /// <summary>
    /// First-person player: WASD to move, mouse to look, Space to jump, F to toggle flying (Space/Shift up/down).
    /// The node origin is at the feet, like a Minecraft player position.
    /// </summary>
    public partial class PlayerController : CharacterBody3D
    {
        private const float WalkSpeed = 4.3f;
        private const float SprintSpeed = 5.6f;
        private const float FlySpeed = 11f;
        private const float JumpVelocity = 8.4f;
        private const float Gravity = 28f;
        private const float MouseSensitivity = 0.15f;
        private const float EyeHeight = 1.62f;

        private Camera3D _camera;
        private float _yawDegrees;
        private float _pitchDegrees;

        /// <summary>
        /// Gets or sets a value indicating whether physics is paused, e.g. until the chunk under the player arrives.
        /// </summary>
        public bool Frozen { get; set; } = true;

        public bool Flying { get; set; }

        /// <summary>
        /// Gets the Minecraft yaw: 0 looks towards +Z and the angle grows clockwise seen from above.
        /// </summary>
        public float MinecraftYaw => Mathf.PosMod(180f - _yawDegrees, 360f);

        /// <summary>
        /// Gets the Minecraft pitch: positive looks down.
        /// </summary>
        public float MinecraftPitch => -_pitchDegrees;

        public override void _Ready()
        {
            AddChild(new CollisionShape3D
            {
                Shape = new CapsuleShape3D { Radius = 0.3f, Height = 1.8f },
                Position = new Vector3(0, 0.9f, 0),
            });
            _camera = new Camera3D { Position = new Vector3(0, EyeHeight, 0), Current = true, Fov = 75, Far = 600 };
            AddChild(_camera);
            FloorMaxAngle = Mathf.DegToRad(50);
        }

        public void Teleport(Vector3 feet, float minecraftYaw, float minecraftPitch)
        {
            GlobalPosition = feet;
            Velocity = Vector3.Zero;
            SetLook(180f - minecraftYaw, -minecraftPitch);
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            if (@event is InputEventMouseMotion motion && Input.MouseMode == Input.MouseModeEnum.Captured)
            {
                SetLook(_yawDegrees - (motion.Relative.X * MouseSensitivity), _pitchDegrees - (motion.Relative.Y * MouseSensitivity));
            }
            else if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
            {
                Input.MouseMode = Input.MouseModeEnum.Captured;
            }
            else if (@event is InputEventKey { Pressed: true, Echo: false } key)
            {
                if (key.Keycode == Key.Escape)
                    Input.MouseMode = Input.MouseModeEnum.Visible;
                else if (key.Keycode == Key.F)
                    Flying = !Flying;
            }
        }

        public override void _PhysicsProcess(double delta)
        {
            if (Frozen)
                return;

            var input = Vector2.Zero;
            if (Input.IsKeyPressed(Key.W))
                input.Y -= 1;
            if (Input.IsKeyPressed(Key.S))
                input.Y += 1;
            if (Input.IsKeyPressed(Key.A))
                input.X -= 1;
            if (Input.IsKeyPressed(Key.D))
                input.X += 1;

            var speed = Flying ? FlySpeed : (Input.IsKeyPressed(Key.Ctrl) ? SprintSpeed : WalkSpeed);
            var direction = (Transform.Basis * new Vector3(input.X, 0, input.Y)).Normalized();
            var velocity = Velocity;
            velocity.X = direction.X * speed;
            velocity.Z = direction.Z * speed;

            if (Flying)
            {
                velocity.Y = 0;
                if (Input.IsKeyPressed(Key.Space))
                    velocity.Y = FlySpeed;
                if (Input.IsKeyPressed(Key.Shift))
                    velocity.Y = -FlySpeed;
            }
            else
            {
                velocity.Y -= Gravity * (float)delta;
                if (IsOnFloor() && Input.IsKeyPressed(Key.Space))
                    velocity.Y = JumpVelocity;
            }

            Velocity = velocity;
            MoveAndSlide();
        }

        private void SetLook(float yawDegrees, float pitchDegrees)
        {
            _yawDegrees = Mathf.PosMod(yawDegrees, 360f);
            _pitchDegrees = Mathf.Clamp(pitchDegrees, -89f, 89f);
            RotationDegrees = new Vector3(0, _yawDegrees, 0);
            if (_camera != null)
                _camera.RotationDegrees = new Vector3(_pitchDegrees, 0, 0);
        }
    }
}
