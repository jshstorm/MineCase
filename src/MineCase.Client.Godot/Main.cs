using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using MineCase.Client.Net;
using MineCase.Client.Net.Rendering;

namespace MineCase.GodotClient
{
    /// <summary>
    /// Connect screen, HUD and the glue between <see cref="MinecraftClient"/> events and the scene.
    /// Command line (after "--"): --host H --port P --name N --view-distance D --autoconnect --fly
    /// --offset-y DY --look-pitch DEGREES --look-yaw DEGREES --screenshot PATH --screenshot-after SECONDS (saves the frame and quits)
    /// --resource-pack PATHS (folders, .zip or a 1.15.2 client .jar, separated like PATH; later ones win).
    /// </summary>
    public partial class Main : Node3D
    {
        private const double PositionSendInterval = 0.05;
        private const int ColumnsPerFrame = 2;

        private readonly Dictionary<string, string> _args = new Dictionary<string, string>();

        private MinecraftClient _client;
        private WorldView _world;
        private PlayerController _player;
        private Control _menu;
        private LineEdit _hostEdit;
        private LineEdit _portEdit;
        private LineEdit _nameEdit;
        private LineEdit _packEdit;
        private string _loadedPacks = string.Empty;
        private Button _connectButton;
        private Label _menuStatus;
        private Label _hud;
        private DirectionalLight3D _sun;
        private double _sendTimer;
        private bool _spawned;
        private byte _viewDistance = 6;
        private float _health = 20;
        private long _timeOfDay = 6000;
        private string _lastError;
        private double _elapsed;

        public override void _Ready()
        {
            ParseArgs();
            BuildEnvironment();
            _world = new WorldView { Name = "World" };
            AddChild(_world);
            _player = new PlayerController { Name = "Player", Position = new Vector3(0, 100, 0) };
            AddChild(_player);
            BuildUi();

            if (_args.TryGetValue("view-distance", out var viewDistance) && byte.TryParse(viewDistance, out var parsed))
                _viewDistance = parsed;
            if (_args.ContainsKey("fly"))
                _player.Flying = true;
            if (_args.ContainsKey("autoconnect"))
                Connect();
        }

        public override void _Process(double delta)
        {
            _elapsed += delta;
            DrainEvents();

            if (_spawned)
            {
                _world.RebuildDirty(_player.GlobalPosition, ColumnsPerFrame);
                _player.Frozen = !_world.HasColumnAt(_player.GlobalPosition);
                _world.ForgetFarColumns(ClientWorld.ToChunkCoordinate(_player.GlobalPosition.X), ClientWorld.ToChunkCoordinate(_player.GlobalPosition.Z), _viewDistance + 3);

                _sendTimer += delta;
                if (_sendTimer >= PositionSendInterval && !_player.Frozen)
                {
                    _sendTimer = 0;
                    var feet = _player.GlobalPosition;
                    _ = SendPositionAsync(feet, _player.MinecraftYaw, _player.MinecraftPitch, _player.IsOnFloor());
                }
            }

            UpdateSun();
            UpdateHud();
            TakeScreenshotIfRequested();
        }

        public override void _ExitTree()
        {
            _client?.Dispose();
        }

        private void Connect() => _ = ConnectWithResourcePacksAsync();

        private async Task ConnectWithResourcePacksAsync()
        {
            _client?.Dispose();
            _client = null;
            _lastError = null;
            var host = _hostEdit.Text.Trim();
            if (!int.TryParse(_portEdit.Text.Trim(), out var port))
            {
                _menuStatus.Text = "포트 번호가 올바르지 않습니다.";
                return;
            }

            _connectButton.Disabled = true;
            if (!await LoadResourcePacksAsync(_packEdit.Text))
            {
                _connectButton.Disabled = false;
                return;
            }

            _client = new MinecraftClient(host, port, _nameEdit.Text.Trim(), _viewDistance);
            if (_args.ContainsKey("trace"))
                _client.Trace = line => GD.Print($"[net {Time.GetTicksMsec() / 1000.0:F2}s] {line}");
            _menuStatus.Text = $"{host}:{port} 에 접속하는 중...";
            await ConnectAsync(_client);
        }

        /// <summary>
        /// Loads the packs in the menu field once; an empty field means flat colours.
        /// </summary>
        /// <param name="text">Pack paths separated like PATH.</param>
        /// <returns>False when a pack could not be read.</returns>
        private async Task<bool> LoadResourcePacksAsync(string text)
        {
            text = text.Trim();
            if (text == _loadedPacks)
                return true;
            if (text.Length == 0)
            {
                _world.SetModels(FlatColorBlockModels.Instance);
                _loadedPacks = text;
                return true;
            }

            _menuStatus.Text = "리소스팩을 읽는 중...";
            try
            {
                var paths = text.Split(System.IO.Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                var models = await Task.Run(() =>
                {
                    using var packs = ResourcePackStack.Open(paths);
                    return ResourcePackBlockModels.Load(packs);
                });
                foreach (var problem in models.Problems)
                    GD.Print("[resource pack] " + problem);
                GD.Print($"Resource packs loaded: {models.Atlas.SpriteCount} textures in a {models.Atlas.Width}x{models.Atlas.Height} atlas");
                _world.SetModels(models);
                _loadedPacks = text;
                return true;
            }
            catch (Exception ex)
            {
                GD.PrintErr("Resource pack failed: " + ex);
                _menuStatus.Text = "리소스팩을 읽지 못했습니다: " + ex.Message;
                return false;
            }
        }

        private static string FindInstalledClientJar()
        {
            var home = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
            var appData = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
            var candidates = new[]
            {
                System.IO.Path.Combine(appData, ".minecraft", "versions", "1.15.2", "1.15.2.jar"),
                System.IO.Path.Combine(home, "Library", "Application Support", "minecraft", "versions", "1.15.2", "1.15.2.jar"),
                System.IO.Path.Combine(home, ".minecraft", "versions", "1.15.2", "1.15.2.jar"),
            };
            return Array.Find(candidates, System.IO.File.Exists) ?? string.Empty;
        }

        private async Task ConnectAsync(MinecraftClient client)
        {
            try
            {
                await client.ConnectAsync();
            }
            catch (Exception ex)
            {
                client.Events.Enqueue(new DisconnectedEvent(ex.Message, ex));
            }
        }

        private async Task SendPositionAsync(Vector3 feet, float yaw, float pitch, bool onGround)
        {
            var client = _client;
            if (client == null)
                return;

            try
            {
                await client.SendPositionAsync(feet.X, feet.Y, feet.Z, yaw, pitch, onGround);
            }
            catch (Exception ex)
            {
                _lastError = ex.Message;
            }
        }

        private void DrainEvents()
        {
            if (_client == null)
                return;

            // Bounded so a burst of chunks does not stall a frame.
            for (var i = 0; i < 64 && _client != null && _client.Events.TryDequeue(out var e); i++)
            {
                if (_args.ContainsKey("trace") && e is not ChunkLoadedEvent && e is not TimeUpdatedEvent)
                    GD.Print($"[{_elapsed:F2}s] {e.GetType().Name}");
                switch (e)
                {
                    case LoggedInEvent loggedIn:
                        _menuStatus.Text = $"{loggedIn.Username} 로그인됨";
                        break;
                    case JoinedGameEvent:
                        GD.Print($"Joined game after {_elapsed:F1}s");
                        _menu.Visible = false;
                        Input.MouseMode = Input.MouseModeEnum.Captured;
                        break;
                    case TeleportEvent teleport:
                        // The server teleports more than once while joining, so a fixed view (for screenshots) is applied every time.
                        var pitch = _args.TryGetValue("look-pitch", out var lookPitch) && float.TryParse(lookPitch, out var p) ? p : teleport.Pitch;
                        var yaw = _args.TryGetValue("look-yaw", out var lookYaw) && float.TryParse(lookYaw, out var y) ? y : teleport.Yaw;
                        _player.Teleport(new Vector3((float)teleport.X, (float)teleport.Y, (float)teleport.Z), yaw, pitch);
                        if (!_spawned && _args.TryGetValue("offset-y", out var offset) && float.TryParse(offset, out var dy))
                            _player.GlobalPosition += new Vector3(0, dy, 0);
                        _spawned = true;
                        break;
                    case ChunkLoadedEvent loaded:
                        _world.AddColumn(loaded.Column);
                        break;
                    case ChunkUnloadedEvent unloaded:
                        _world.RemoveColumn(unloaded.ChunkX, unloaded.ChunkZ);
                        break;
                    case TimeUpdatedEvent time:
                        _timeOfDay = time.TimeOfDay;
                        break;
                    case HealthUpdatedEvent health:
                        _health = health.Health;
                        break;
                    case DisconnectedEvent disconnected:
                        OnDisconnected(disconnected);
                        break;
                }
            }
        }

        private void OnDisconnected(DisconnectedEvent disconnected)
        {
            GD.PrintErr($"Disconnected after {_elapsed:F1}s: {disconnected.Reason}");
            _spawned = false;
            _player.Frozen = true;
            foreach (var column in new List<ChunkColumn>(_world.World.Columns))
                _world.RemoveColumn(column.ChunkX, column.ChunkZ);
            Input.MouseMode = Input.MouseModeEnum.Visible;
            _menu.Visible = true;
            _connectButton.Disabled = false;
            _menuStatus.Text = "연결 끊김: " + disconnected.Reason;
            _client?.Dispose();
            _client = null;
        }

        private void UpdateHud()
        {
            if (!_spawned)
            {
                _hud.Text = string.Empty;
                return;
            }

            var p = _player.GlobalPosition;
            _hud.Text =
                $"XYZ {p.X:F1} / {p.Y:F1} / {p.Z:F1}   방향 {_player.MinecraftYaw:F0}° {(_player.Flying ? "[비행]" : string.Empty)}{(_player.Frozen ? " [청크 대기]" : string.Empty)}\n" +
                $"청크 {_world.World.ColumnCount} (대기 {_world.PendingColumns})   삼각형 {_world.TriangleCount:N0}   FPS {Engine.GetFramesPerSecond()}\n" +
                $"체력 {_health:F0}   시각 {(_timeOfDay % 24000) / 1000.0 + 6 % 24:F1}h" +
                (_lastError != null ? $"\n오류: {_lastError}" : string.Empty) +
                "\nWASD 이동 · 마우스 시점 · Space 점프 · F 비행 · Esc 마우스 해제";
        }

        private void UpdateSun()
        {
            // Minecraft time 0 is sunrise, 6000 noon, 12000 sunset.
            var angle = (_timeOfDay % 24000) / 24000.0 * 360.0;
            _sun.RotationDegrees = new Vector3((float)(-angle), 30, 0);
        }

        private void TakeScreenshotIfRequested()
        {
            if (!_args.TryGetValue("screenshot", out var path))
                return;
            var after = _args.TryGetValue("screenshot-after", out var seconds) && double.TryParse(seconds, out var s) ? s : 15;
            if (_elapsed < after)
                return;

            _args.Remove("screenshot");
            GetViewport().GetTexture().GetImage().SavePng(path);
            GD.Print($"Saved screenshot to {path}: spawned={_spawned} columns={_world.World.ColumnCount} pending={_world.PendingColumns} triangles={_world.TriangleCount} position={_player.GlobalPosition} frozen={_player.Frozen} fps={Engine.GetFramesPerSecond()}");
            GetTree().Quit();
        }

        private void BuildEnvironment()
        {
            // Daytime plains sky and fog colours of the game.
            var skyColor = new Color(0.47f, 0.65f, 1.0f);
            var horizonColor = new Color(0.75f, 0.85f, 1.0f);
            var sky = new Sky
            {
                SkyMaterial = new ProceduralSkyMaterial
                {
                    SkyTopColor = skyColor,
                    SkyHorizonColor = horizonColor,
                    GroundHorizonColor = horizonColor,
                    GroundBottomColor = new Color(0.16f, 0.2f, 0.3f),
                },
            };
            var environment = new Godot.Environment
            {
                BackgroundMode = Godot.Environment.BGMode.Sky,
                Sky = sky,
                AmbientLightSource = Godot.Environment.AmbientSource.Sky,
                FogEnabled = true,
                FogLightColor = horizonColor,
                FogDensity = 0.006f,
            };
            AddChild(new WorldEnvironment { Environment = environment });
            _sun = new DirectionalLight3D { RotationDegrees = new Vector3(-50, 30, 0) };
            AddChild(_sun);
        }

        private void BuildUi()
        {
            var layer = new CanvasLayer();
            AddChild(layer);

            _hud = new Label { Position = new Vector2(12, 8) };
            _hud.AddThemeColorOverride("font_color", Colors.White);
            _hud.AddThemeColorOverride("font_shadow_color", Colors.Black);
            _hud.AddThemeConstantOverride("shadow_offset_x", 1);
            _hud.AddThemeConstantOverride("shadow_offset_y", 1);
            layer.AddChild(_hud);

            layer.AddChild(new Label
            {
                Text = "+",
                AnchorLeft = 0.5f,
                AnchorTop = 0.5f,
                AnchorRight = 0.5f,
                AnchorBottom = 0.5f,
                OffsetLeft = -4,
                OffsetTop = -12,
            });

            var center = new CenterContainer { AnchorRight = 1, AnchorBottom = 1 };
            _menu = center;
            layer.AddChild(center);
            var panel = new PanelContainer { CustomMinimumSize = new Vector2(360, 0) };
            center.AddChild(panel);
            var box = new VBoxContainer();
            box.AddThemeConstantOverride("separation", 8);
            panel.AddChild(box);

            box.AddChild(new Label { Text = "MineCase 클라이언트 (Minecraft 1.15.2, 오프라인 모드)" });
            _hostEdit = AddField(box, "서버 주소", _args.GetValueOrDefault("host", "127.0.0.1"));
            _portEdit = AddField(box, "포트", _args.GetValueOrDefault("port", "25565"));
            _nameEdit = AddField(box, "이름", _args.GetValueOrDefault("name", "godot"));
            _packEdit = AddField(box, "리소스팩", _args.GetValueOrDefault("resource-pack", FindInstalledClientJar()));
            _packEdit.PlaceholderText = "비우면 단색 블록 (1.15.2 client.jar, 폴더, zip)";
            _connectButton = new Button { Text = "접속" };
            _connectButton.Pressed += Connect;
            box.AddChild(_connectButton);
            var statusButton = new Button { Text = "서버 상태 확인" };
            statusButton.Pressed += () => _ = QueryStatusAsync();
            box.AddChild(statusButton);
            _menuStatus = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(340, 0) };
            box.AddChild(_menuStatus);
        }

        private async Task QueryStatusAsync()
        {
            _menuStatus.Text = "상태 확인 중...";
            try
            {
                var json = await MinecraftClient.QueryStatusAsync(_hostEdit.Text.Trim(), int.Parse(_portEdit.Text.Trim()));
                var status = Json.ParseString(json).AsGodotDictionary();
                var players = status["players"].AsGodotDictionary();
                var description = status["description"].AsGodotDictionary();
                CallDeferred(nameof(SetMenuStatus), $"{description["text"]} — 접속자 {players["online"]}/{players["max"]}");
            }
            catch (Exception ex)
            {
                CallDeferred(nameof(SetMenuStatus), "상태 확인 실패: " + ex.Message);
            }
        }

        private void SetMenuStatus(string text) => _menuStatus.Text = text;

        private static LineEdit AddField(Container parent, string label, string value)
        {
            var row = new HBoxContainer();
            row.AddChild(new Label { Text = label, CustomMinimumSize = new Vector2(80, 0) });
            var edit = new LineEdit { Text = value, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            row.AddChild(edit);
            parent.AddChild(row);
            return edit;
        }

        private void ParseArgs()
        {
            var args = OS.GetCmdlineUserArgs();
            for (var i = 0; i < args.Length; i++)
            {
                if (!args[i].StartsWith("--", StringComparison.Ordinal))
                    continue;
                var key = args[i].Substring(2);
                var value = i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal) ? args[++i] : "true";
                _args[key] = value;
            }
        }
    }
}
