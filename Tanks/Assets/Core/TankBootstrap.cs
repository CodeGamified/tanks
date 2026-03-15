// Copyright CodeGamified 2025-2026
// MIT License — Tanks: Code Your Tank
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using CodeGamified.Audio;
using CodeGamified.Camera;
using CodeGamified.Procedural;
using CodeGamified.Time;
using CodeGamified.Settings;
using CodeGamified.Quality;
using CodeGamified.Bootstrap;
using Tanks.Game;
using Tanks.Scripting;
using Tanks.AI;
using Tanks.Procedural;
using Tanks.UI;

namespace Tanks.Core
{
    /// <summary>
    /// Bootstrap for Tanks — inspired by the legendary Miniclip game.
    ///
    /// Architecture (same pattern as Pong):
    ///   - Instantiate managers → wire cross-references → configure scene
    ///   - .engine submodule gives us TUI + Code Execution for free
    ///   - Players don't use WASD — they WRITE CODE to control their tank
    ///   - Each tank on the map is its own script executor
    ///   - Each script controls drive, turret angle, and firing
    ///   - Projectiles traced with LineRenderer (like Pong ball trails)
    ///   - Last tank standing wins, or tie if no ammo left
    ///
    /// Attach to a GameObject. Press Play → Tanks appear.
    /// </summary>
    public class TankBootstrap : GameBootstrap
    {
        protected override string LogTag => "TANKS";

        // =================================================================
        // INSPECTOR
        // =================================================================

        [Header("Arena")]
        public float arenaWidth = 20f;
        public float arenaHeight = 14f;

        [Header("Tank Stats")]
        public float tankMaxSpeed = 4f;
        public float tankTurnSpeed = 120f;
        public float turretSpeed = 180f;
        public int tankHP = 3;
        public int tankAmmo = 10;
        public float fireCooldown = 1f;
        public float projectileSpeed = 8f;

        [Header("Match")]
        public bool autoRestart = true;
        public float restartDelay = 3f;

        [Header("AI Opponent")]
        public AIDifficulty aiDifficulty = AIDifficulty.Easy;

        [Header("Time")]
        public bool enableTimeScale = true;

        [Header("Scripting")]
        public bool enableScripting = true;

        [Header("TUI Frontend")]
        [Tooltip("Enable terminal UI overlay (.engine)")]
        public bool enableTUI = true;

        [Header("Camera")]
        public bool configureCamera = true;

        // =================================================================
        // RUNTIME REFERENCES
        // =================================================================

        private TankArena _arena;
        private TankBody _playerTank;
        private TankBody _aiTank;
        private TankMatchManager _match;
        private TankAIController _aiController;
        private TankProgram _playerProgram;
        private System.Collections.Generic.List<TankObstacle> _obstacles
            = new System.Collections.Generic.List<TankObstacle>();
        private TankFogOfWar _fog;

        // Procedural
        private ColorPalette _palette;
        private AssemblyResult _playerTankVisual;
        private AssemblyResult _aiTankVisual;
        private Transform _playerTurretPivot;
        private Transform _aiTurretPivot;

        // Code editor
        private TankCompilerExtension _compilerExt;
        private TankEditorExtension _editorExt;

        // TUI
        private TankTUIManager _tuiManager;

        // Camera
        private CameraAmbientMotion _cameraSway;
        private static readonly Vector3 DefaultCameraPos = new Vector3(0f, 0f, -18f);
        private TankBody _cameraFollowTarget;
        private TankBody _cameraFollowEnemy;
        private float _followDistance = 12f;
        private float _followElevation = 6f;
        private const float FollowLerpSpeed = 5f;
        private const float ZoomSpeed = 8f;
        private const float MinZoom = 4f;
        private const float MaxZoom = 30f;

        // =================================================================
        // UPDATE
        // =================================================================

        private void Update()
        {
            HandleScrollZoom();
            UpdateCameraFollow();
            HandleCameraClick();
            HandleCameraEscape();
            SyncTankVisuals();
        }

        // =================================================================
        // CAMERA — click-to-follow + scroll zoom + Escape to default
        // =================================================================

        private void HandleScrollZoom()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) < 0.001f) return;

            if (_cameraFollowTarget != null)
            {
                _followDistance -= scroll * ZoomSpeed;
                _followDistance = Mathf.Clamp(_followDistance, MinZoom, MaxZoom);
                _followElevation = _followDistance * 0.5f;
            }
            else if (_cameraSway != null && _cameraSway.enabled)
            {
                float currentZ = Mathf.Abs(DefaultCameraPos.z);
                float newZ = currentZ - scroll * ZoomSpeed;
                newZ = Mathf.Clamp(newZ, MinZoom, MaxZoom);
                float ratio = newZ / Mathf.Max(Mathf.Abs(DefaultCameraPos.z), 0.01f);
                Vector3 newBase = new Vector3(
                    DefaultCameraPos.x,
                    DefaultCameraPos.y * ratio,
                    -newZ);
                _cameraSway.SetBasePosition(newBase);
            }
        }

        private void UpdateCameraFollow()
        {
            if (_cameraFollowTarget == null) return;

            var cam = UnityEngine.Camera.main;
            if (cam == null) return;

            Vector3 targetPos = _cameraFollowTarget.transform.position;

            // Position camera behind the tank (in -Z), elevated
            Vector3 desiredCamPos = new Vector3(
                targetPos.x,
                targetPos.y + _followElevation,
                targetPos.z - _followDistance);

            // Look at the enemy tank (or arena center if enemy is dead)
            Vector3 lookPoint = (_cameraFollowEnemy != null && _cameraFollowEnemy.IsAlive)
                ? _cameraFollowEnemy.transform.position
                : Vector3.zero;

            cam.transform.position = Vector3.Lerp(
                cam.transform.position, desiredCamPos,
                Time.unscaledDeltaTime * FollowLerpSpeed);

            Quaternion desiredRot = Quaternion.LookRotation(
                lookPoint - cam.transform.position, Vector3.up);
            cam.transform.rotation = Quaternion.Slerp(
                cam.transform.rotation, desiredRot,
                Time.unscaledDeltaTime * FollowLerpSpeed);
        }

        private void HandleCameraClick()
        {
            if (!Input.GetMouseButtonDown(0)) return;

            var cam = UnityEngine.Camera.main;
            if (cam == null) return;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 100f)) return;

            var tank = hit.transform.GetComponentInParent<TankBody>();
            if (tank == null) return;

            StopAllCoroutines();
            _cameraSway.enabled = false;
            _cameraFollowTarget = tank;
            _cameraFollowEnemy = (tank == _playerTank) ? _aiTank : _playerTank;
            _followDistance = 12f;
            _followElevation = 6f;
            Log($"Camera → following {tank.Team} Tank #{tank.TankIndex}");
        }

        private void HandleCameraEscape()
        {
            if (_cameraFollowTarget == null) return;
            if (!Input.GetKeyDown(KeyCode.Escape)) return;

            _cameraFollowTarget = null;
            _cameraFollowEnemy = null;
            StartCoroutine(LerpToDefaultView());
        }

        private System.Collections.IEnumerator LerpToDefaultView()
        {
            var cam = UnityEngine.Camera.main;
            if (cam == null) yield break;

            _cameraSway.enabled = false;

            Vector3 startPos = cam.transform.position;
            Quaternion startRot = cam.transform.rotation;
            Quaternion targetRot = Quaternion.LookRotation(Vector3.zero - DefaultCameraPos, Vector3.up);

            float duration = 0.6f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = 1f - Mathf.Pow(1f - elapsed / duration, 3f);
                cam.transform.position = Vector3.Lerp(startPos, DefaultCameraPos, t);
                cam.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
                yield return null;
            }

            cam.transform.position = DefaultCameraPos;
            cam.transform.LookAt(Vector3.zero, Vector3.up);

            _cameraSway.SetBasePosition(DefaultCameraPos);
            _cameraSway.enabled = true;
            Log("Camera → default sway");
        }

        private void SyncTankVisuals()
        {
            // Sync procedural visual roots to tank transforms
            if (_playerTankVisual.Root != null && _playerTank != null)
            {
                _playerTankVisual.Root.transform.position = _playerTank.transform.position;
                _playerTankVisual.Root.transform.rotation = _playerTank.transform.rotation;
            }
            if (_aiTankVisual.Root != null && _aiTank != null)
            {
                _aiTankVisual.Root.transform.position = _aiTank.transform.position;
                _aiTankVisual.Root.transform.rotation = _aiTank.transform.rotation;
            }

            // Rotate turret pivots to match turretAngle (relative to body)
            if (_playerTurretPivot != null && _playerTank != null)
                _playerTurretPivot.localRotation = Quaternion.Euler(0f, 0f, _playerTank.turretAngle);
            if (_aiTurretPivot != null && _aiTank != null)
                _aiTurretPivot.localRotation = Quaternion.Euler(0f, 0f, _aiTank.turretAngle);
        }

        // =================================================================
        // BOOTSTRAP
        // =================================================================

        private void Start()
        {
            Log("🎯 Tanks Bootstrap starting...");

            SettingsBridge.Load();
            QualityBridge.SetTier((QualityTier)SettingsBridge.QualityLevel);

            SetupSimulationTime();
            SetupCamera();
            CreatePalette();
            CreateArena();
            CreateObstacles();
            CreateTanks();
            CreateFogOfWar();
            CreateMatchManager();
            CreateAI();

            if (enableScripting) CreatePlayerProgram();
            if (enableScripting) CreateCodeEditor();
            if (enableTUI) CreateTUI();

            WireEvents();
            StartCoroutine(RunBootSequence());
        }

        // =================================================================
        // SIMULATION TIME
        // =================================================================

        private void SetupSimulationTime()
        {
            EnsureSimulationTime<TankSimulationTime>();
        }

        // =================================================================
        // CAMERA — top-down view of the arena
        // =================================================================

        private void SetupCamera()
        {
            if (!configureCamera) return;

            var cam = EnsureCamera();

            // Top-down orthographic-like perspective view
            cam.orthographic = false;
            cam.fieldOfView = 60f;
            cam.transform.position = DefaultCameraPos;
            cam.transform.LookAt(Vector3.zero, Vector3.up);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.02f, 0.04f);
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 100f;

            _cameraSway = cam.gameObject.AddComponent<CameraAmbientMotion>();
            _cameraSway.lookAtTarget = Vector3.zero;

            // Post-processing: bloom
            var camData = cam.GetComponent<UniversalAdditionalCameraData>();
            if (camData == null)
                camData = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;

            var volumeGO = new GameObject("PostProcessVolume");
            var volume = volumeGO.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var bloom = profile.Add<Bloom>();
            bloom.threshold.overrideState = true;
            bloom.threshold.value = 0.9f;
            bloom.intensity.overrideState = true;
            bloom.intensity.value = 2.0f;
            bloom.scatter.overrideState = true;
            bloom.scatter.value = 0.6f;
            volume.profile = profile;

            Log("Camera: top-down perspective + sway + bloom");
        }

        // =================================================================
        // COLOR PALETTE
        // =================================================================

        private void CreatePalette()
        {
            var colors = new Dictionary<string, Color>
            {
                { "wall",            new Color(0.4f, 0.4f, 0.5f) },
                { "arena_floor",     new Color(0.03f, 0.04f, 0.03f) },
                { "tank_player",     new Color(0.1f, 0.7f, 0.2f) },      // green
                { "tank_ai",         new Color(0.8f, 0.15f, 0.1f) },     // red
                { "turret_player",   new Color(0.15f, 0.85f, 0.3f) },    // bright green
                { "turret_ai",       new Color(0.9f, 0.2f, 0.15f) },     // bright red
                { "projectile",      new Color(1f, 0.7f, 0.1f) },        // orange
                { "obstacle",        new Color(0.3f, 0.3f, 0.35f) },     // dark grey
            };
            _palette = ColorPalette.CreateRuntime(colors);
            Log("Created military neon ColorPalette");
        }

        // =================================================================
        // ARENA
        // =================================================================

        private void CreateArena()
        {
            var go = new GameObject("Arena");
            _arena = go.AddComponent<TankArena>();
            _arena.Width = arenaWidth;
            _arena.Height = arenaHeight;
            _arena.Initialize(_palette);
            Log($"Created Arena ({arenaWidth}×{arenaHeight}) via ProceduralAssembler");
        }

        // =================================================================
        // OBSTACLES
        // =================================================================

        private void CreateObstacles()
        {
            float clearRadius = 3.5f;
            int count = 5; // center + 2 symmetric pairs
            var layout = TankObstacle.GenerateLayout(arenaWidth, arenaHeight, count, clearRadius);

            for (int i = 0; i < layout.Count; i++)
            {
                var def = layout[i];
                var go = new GameObject($"Obstacle_{i}");
                go.transform.SetParent(_arena.transform, false);

                var obs = go.AddComponent<TankObstacle>();
                obs.Initialize(def.x, def.y, def.z, def.w);

                // Procedural visual
                var bp = new TankObstacleBlueprint(def.z, def.w, i);
                var visual = ProceduralAssembler.BuildWithVisualState(bp, _palette);
                if (visual.Root != null)
                    visual.Root.transform.SetParent(go.transform, false);

                _obstacles.Add(obs);
                _arena.Obstacles.Add(obs);
            }

            Log($"Created {layout.Count} obstacles (symmetric layout, {clearRadius}u spawn clearance)");
        }

        // =================================================================
        // FOG OF WAR
        // =================================================================

        private void CreateFogOfWar()
        {
            var go = new GameObject("FogOfWar");
            go.transform.SetParent(_arena.transform, false);
            _fog = go.AddComponent<TankFogOfWar>();
            _fog.Initialize(_arena, _obstacles,
                            cellSize: 1.0f, visionRadius: 6f,
                            displayTankIndex: _playerTank.TankIndex);

            _fog.RegisterTank(_playerTank);
            _fog.RegisterTank(_aiTank);

            // Initial scan so tanks see their surroundings at spawn
            _fog.Scan(_playerTank);
            _fog.Scan(_aiTank);

            _arena.FogOfWar = _fog;

            Log("Created voxelized FogOfWar (cell=1.0, vision=6.0)");
        }

        // =================================================================
        // TANKS
        // =================================================================

        private void CreateTanks()
        {
            float spawnOffset = arenaWidth / 4f;

            // Player tank — CODE-controlled
            var playerGo = new GameObject("PlayerTank (CODE)");
            _playerTank = playerGo.AddComponent<TankBody>();
            ConfigureTank(_playerTank);
            _playerTank.Initialize(TankTeam.Player, 0, _arena,
                                   -spawnOffset, 0f, 0f);

            var playerBlueprint = new TankBlueprint(TankTeam.Player, 0);
            _playerTankVisual = ProceduralAssembler.BuildWithVisualState(playerBlueprint, _palette);
            if (_playerTankVisual.Root != null)
                _playerTankVisual.Root.transform.SetParent(playerGo.transform, false);
            _playerTurretPivot = _playerTankVisual.GetPart("turret");

            // AI tank
            var aiGo = new GameObject("AITank");
            _aiTank = aiGo.AddComponent<TankBody>();
            ConfigureTank(_aiTank);
            _aiTank.Initialize(TankTeam.AI, 1, _arena,
                               spawnOffset, 0f, 180f);

            var aiBlueprint = new TankBlueprint(TankTeam.AI, 1);
            _aiTankVisual = ProceduralAssembler.BuildWithVisualState(aiBlueprint, _palette);
            if (_aiTankVisual.Root != null)
                _aiTankVisual.Root.transform.SetParent(aiGo.transform, false);
            _aiTurretPivot = _aiTankVisual.GetPart("turret");

            Log("Created 2 Tanks (Player=CODE, AI) via ProceduralAssembler");
        }

        private void ConfigureTank(TankBody tank)
        {
            tank.maxSpeed = tankMaxSpeed;
            tank.turnSpeed = tankTurnSpeed;
            tank.turretSpeed = turretSpeed;
            tank.maxHP = tankHP;
            tank.maxAmmo = tankAmmo;
            tank.fireCooldown = fireCooldown;
            tank.projectileSpeed = projectileSpeed;
        }

        // =================================================================
        // MATCH MANAGER
        // =================================================================

        private void CreateMatchManager()
        {
            var go = new GameObject("MatchManager");
            _match = go.AddComponent<TankMatchManager>();
            _match.Initialize(_arena, autoRestart, restartDelay);
            _match.RegisterTank(_playerTank);
            _match.RegisterTank(_aiTank);
            Log("Created TankMatchManager (last tank standing)");
        }

        // =================================================================
        // AI
        // =================================================================

        private void CreateAI()
        {
            var go = new GameObject("AIController");
            _aiController = go.AddComponent<TankAIController>();
            _aiController.Initialize(_aiTank, _arena, aiDifficulty);
            Log($"Created AI ({aiDifficulty})");
        }

        // =================================================================
        // PLAYER SCRIPTING
        // =================================================================

        private void CreatePlayerProgram()
        {
            var go = new GameObject("PlayerProgram");
            _playerProgram = go.AddComponent<TankProgram>();
            _playerProgram.Initialize(_playerTank, _arena);
            Log("Created PlayerProgram (code-controlled tank)");
        }

        // =================================================================
        // CODE EDITOR
        // =================================================================

        private void CreateCodeEditor()
        {
            _compilerExt = new TankCompilerExtension();
            _editorExt = new TankEditorExtension();
            Log("Created TankEditorExtension (ready for CodeEditorWindow)");
        }

        // =================================================================
        // TUI (.engine powered)
        // =================================================================

        private void CreateTUI()
        {
            var go = new GameObject("TankTUI");
            _tuiManager = go.AddComponent<TankTUIManager>();
            _tuiManager.Initialize(_match, _playerProgram, _aiController,
                                   _playerTank, _aiTank);
            Log("Created TankTUI");
        }

        // =================================================================
        // EVENT WIRING
        // =================================================================

        private void WireEvents()
        {
            if (SimulationTime.Instance != null)
            {
                SimulationTime.Instance.OnTimeScaleChanged += s => Log($"Time scale → {s:F0}x");
                SimulationTime.Instance.OnPausedChanged += p => Log(p ? "⏸ PAUSED" : "▶ RESUMED");
            }

            if (_match != null)
            {
                _match.OnTankDestroyed += tank =>
                {
                    Log($"DESTROYED! Tank #{tank.TankIndex} ({tank.Team})");
                    // Flash the destroyed tank visual
                    var visual = tank.Team == TankTeam.Player ? _playerTankVisual : _aiTankVisual;
                    visual.VisualState?.Pulse("body", new Color(5f, 1f, 0f), 0.5f);
                };

                _match.OnMatchWon += winner =>
                    Log($"MATCH WON by {winner.Team} Tank #{winner.TankIndex}!");

                _match.OnDraw += () => Log("MATCH DRAW — no ammo remaining!");

                _match.OnProjectileSpawned += proj =>
                {
                    proj.OnBounced += p =>
                    {
                        // Wall flash on bounce
                        _arena.Visual.VisualState?.Pulse("wall_top", Color.white, 0.1f);
                    };

                    proj.OnHitTank += (p, victim) =>
                    {
                        Log($"HIT! Tank #{victim.TankIndex} takes damage (HP: {victim.hp})");
                        var visual = victim.Team == TankTeam.Player ? _playerTankVisual : _aiTankVisual;
                        visual.VisualState?.Pulse("body", new Color(3f, 0f, 0f), 0.3f);
                    };
                };
            }
        }

        // =================================================================
        // BOOT SEQUENCE
        // =================================================================

        private System.Collections.IEnumerator RunBootSequence()
        {
            yield return null;
            yield return null;

            LogDivider();
            Log("🎯 TANKS — Write Code. Destroy Tanks. Last One Standing.");
            LogDivider();
            LogStatus("ARENA", $"{arenaWidth}×{arenaHeight} (3D ProceduralAssembler)");
            LogStatus("TANKS", $"HP={tankHP}, Ammo={tankAmmo}, Speed={tankMaxSpeed}");
            LogStatus("MATCH", "Last tank standing (or tie if no ammo)");
            LogStatus("FOG  ", "Voxelized fog of war — scan() to reveal");
            LogStatus("OBST ", $"{_obstacles.Count} obstacles (blocks movement + projectiles)");
            LogStatus("AI   ", $"{aiDifficulty}");
            LogStatus("TIME ", SimulationTime.Instance?.GetFormattedTimeScale() ?? "1x");
            LogDivider();
            LogEnabled("Scripting", enableScripting);
            LogEnabled("Editor   ", enableScripting, "TankEditorExtension");
            LogEnabled("TUI      ", enableTUI);
            LogEnabled("Procedural", true, "3D arena + tanks");
            LogDivider();
            Log("🎯 Bootstrap complete. Write your tank code!");

            if (_match != null) _match.StartMatch();
        }

        // =================================================================
        // CLEANUP
        // =================================================================

        private void OnDestroy()
        {
            if (SimulationTime.Instance != null)
            {
                SimulationTime.Instance.OnTimeScaleChanged -= s => { };
                SimulationTime.Instance.OnPausedChanged -= p => { };
            }
        }
    }
}
