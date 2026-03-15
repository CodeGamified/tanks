// Copyright CodeGamified 2025-2026
// MIT License — Tanks: Code Your Tank
using System.Collections.Generic;
using UnityEngine;
using CodeGamified.Engine;
using CodeGamified.Engine.Compiler;
using CodeGamified.Engine.Runtime;
using CodeGamified.Time;
using Tanks.Game;

namespace Tanks.Scripting
{
    /// <summary>
    /// TankProgram — code-controlled tank with N onboard computers.
    /// Each computer runs its own script independently.
    /// All computers share the same TankBody and communicate via SharedBus.
    ///
    /// MULTI-COMPUTER MODEL:
    ///   - By default, a tank has one "main" computer (backward compatible)
    ///   - AddComputer("turret", code, ops) adds a dedicated turret computer
    ///   - Each computer has its own PC, registers, variables, and ops budget
    ///   - Computers communicate via send(channel, value) / recv(channel)
    ///   - SharedBus has 16 float channels (0-15)
    ///
    /// EXECUTION MODEL (tick-based, deterministic):
    ///   - Each simulation tick, all computers run in order
    ///   - Each computer has its own instruction budget (OPS_PER_SECOND)
    ///   - Memory (variables) persists across ticks per computer
    ///   - PC resets to 0 when script completes — no while loop needed
    ///   - More computers = more decisions per tick
    ///
    /// BUILTINS (available to ALL computers):
    ///   get_my_x/y()         → tank position
    ///   get_my_heading()     → body heading (degrees, 0=right, CCW+)
    ///   get_my_turret()      → turret angle (degrees, relative to body)
    ///   get_my_hp()          → remaining HP
    ///   get_my_ammo()        → remaining ammo
    ///   get_enemy_x/y()      → nearest VISIBLE enemy position (fog-gated)
    ///   get_enemy_dist()     → distance to nearest visible enemy (9999 if hidden)
    ///   get_obstacle_x/y()   → nearest obstacle center
    ///   get_obstacle_dist()  → distance to nearest obstacle
    ///   scan()               → refresh fog of war visibility
    ///   is_visible(x,y)      → is cell visible? (0/1)
    ///   set_drive(speed)     → drive forward/backward (-1..+1)
    ///   set_turn(rate)       → turn body (-1..+1)
    ///   set_turret(angle)    → aim turret (degrees relative to body)
    ///   fire()               → fire projectile from turret
    ///   send(channel, value) → write to shared data bus (0-15)
    ///   recv(channel)        → read from shared data bus → R0
    /// </summary>
    public class TankProgram : ProgramBehaviour
    {
        private TankBody _tank;
        private TankArena _arena;

        // Multi-computer
        private readonly List<TankComputer> _computers = new List<TankComputer>();

        // Shared data bus for inter-computer communication
        public const int SHARED_BUS_SIZE = 16;
        private float[] _sharedBus = new float[SHARED_BUS_SIZE];

        // Execution rate — THE core gameplay constraint
        public const float OPS_PER_SECOND = 15f;

        // Default starter code
        private const string DEFAULT_CODE = @"# 🎯 TANKS — Write your tank AI!
# Your script runs at 15 ops/sec (sim-time).
# When it finishes, it restarts from the top.
# Variables persist — use them to track state.
# Last tank standing wins. Efficiency IS strategy.
#
# SENSORS:
#   get_my_x/y()        → your position
#   get_my_heading()     → body angle (degrees)
#   get_my_turret()      → turret angle (vs body)
#   get_my_hp()          → remaining HP
#   get_my_ammo()        → remaining ammo
#   get_enemy_x/y()      → nearest VISIBLE enemy pos
#   get_enemy_dist()     → distance (9999 if not visible)
#   get_obstacle_x/y()   → nearest obstacle center
#   get_obstacle_dist()  → distance to nearest obstacle
#
# FOG OF WAR:
#   scan()               → refresh visibility (costs 1 op)
#   is_visible(x, y)     → is cell visible? → 0/1
#   Turret direction = infinite vision ray through fog!
#   Use set_turret() + scan() to look around!
#   Enemy queries return 0/9999 if enemy not in sight!
#
# ORDERS:
#   set_drive(speed)     → forward/back (-1..+1)
#   set_turn(rate)       → turn body (-1..+1)
#   set_turret(angle)    → aim turret (degrees)
#   fire()               → shoot!
#
# DATA BUS (multi-computer):
#   send(channel, value) → write to shared bus (0-15)
#   recv(channel)        → read from shared bus
#   Use to coordinate between onboard computers!
#
# This starter sweeps turret as radar, locks & fires:
# Turret ray gives infinite vision along its direction!
sweep = get_my_turret() + 20
set_turret(sweep)
scan()
dist = get_enemy_dist()
if dist < 9999:
    ex = get_enemy_x()
    dx = ex - get_my_x()
    set_turret(dx)
    set_drive(0.5)
    fire()
if dist > 9000:
    set_drive(0.2)
";

        public string CurrentSourceCode => MainComputer?.SourceCode ?? _sourceCode;
        public System.Action OnCodeChanged;

        /// <summary>The first (default) computer. Null if not initialized.</summary>
        public TankComputer MainComputer => _computers.Count > 0 ? _computers[0] : null;

        /// <summary>All onboard computers.</summary>
        public IReadOnlyList<TankComputer> Computers => _computers;

        /// <summary>The shared data bus (16 float channels).</summary>
        public float[] SharedBus => _sharedBus;

        public void Initialize(TankBody tank, TankArena arena,
                               string initialCode = null, string programName = "TankAI")
        {
            _tank = tank;
            _arena = arena;
            _programName = programName;
            _sourceCode = initialCode ?? DEFAULT_CODE;
            _autoRun = false; // prevent base Start from double-loading

            LoadAndRun(_sourceCode);
        }

        // ═══════════════════════════════════════════════════════════════
        // MULTI-COMPUTER API
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Add a new onboard computer with its own script.
        /// Returns the computer, or null if name is duplicate or compilation failed.
        /// </summary>
        public TankComputer AddComputer(string name, string code,
                                        float opsPerSecond = OPS_PER_SECOND)
        {
            for (int i = 0; i < _computers.Count; i++)
                if (_computers[i].Name == name) return null;

            var computer = new TankComputer(name, _tank, _arena, opsPerSecond);
            if (!computer.LoadCode(code, _sharedBus))
                return null;

            _computers.Add(computer);
            Debug.Log($"[TankAI] Added computer '{name}' ({_computers.Count} total)");
            return computer;
        }

        /// <summary>Get a computer by name. Null if not found.</summary>
        public TankComputer GetComputer(string name)
        {
            for (int i = 0; i < _computers.Count; i++)
                if (_computers[i].Name == name) return _computers[i];
            return null;
        }

        /// <summary>Remove a computer by name. Cannot remove "main".</summary>
        public bool RemoveComputer(string name)
        {
            if (name == "main") return false;
            for (int i = 0; i < _computers.Count; i++)
            {
                if (_computers[i].Name == name)
                {
                    _computers.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        // ═══════════════════════════════════════════════════════════════
        // LIFECYCLE OVERRIDES
        // ═══════════════════════════════════════════════════════════════

        protected override void Start()
        {
            // Don't call base — Initialize already handles setup
        }

        public override bool LoadAndRun(string source)
        {
            _sourceCode = source;
            _sharedBus = new float[SHARED_BUS_SIZE];
            _computers.Clear();

            var main = new TankComputer("main", _tank, _arena, OPS_PER_SECOND);
            bool ok = main.LoadCode(source, _sharedBus);
            _computers.Add(main);

            // Sync base class fields for backward compatibility
            _executor = main.Executor;
            _program = main.Program;
            _isPaused = false;

            return ok;
        }

        /// <summary>
        /// Tick all onboard computers. Each gets its own ops budget.
        /// Drive commands are cleared once before all computers run —
        /// the last computer to set a command "wins" that tick.
        /// </summary>
        protected override void Update()
        {
            if (_computers.Count == 0 || _isPaused) return;
            if (_tank == null || !_tank.IsAlive) return;

            float timeScale = SimulationTime.Instance?.timeScale ?? 1f;
            if (SimulationTime.Instance != null && SimulationTime.Instance.isPaused) return;

            float simDelta = UnityEngine.Time.deltaTime * timeScale;

            // Clear drive commands once before all computers run
            _tank.ClearCommands();

            // Tick all computers
            for (int i = 0; i < _computers.Count; i++)
                _computers[i].Tick(simDelta);
        }

        protected override IGameIOHandler CreateIOHandler()
        {
            // Fallback — in multi-computer mode, each TankComputer creates its own
            var handler = new TankIOHandler(_tank, _arena);
            handler.SetSharedBus(_sharedBus);
            return handler;
        }

        protected override CompiledProgram CompileSource(string source, string name)
        {
            return PythonCompiler.Compile(source, name, new TankCompilerExtension());
        }

        protected override void ProcessEvents()
        {
            // Each computer drains its own events in Tick()
        }

        /// <summary>Upload new code to the main computer (backward compatible).</summary>
        public void UploadCode(string newSource)
        {
            UploadCode("main", newSource);
        }

        /// <summary>Upload new code to a specific computer by name.</summary>
        public void UploadCode(string computerName, string newSource)
        {
            var computer = GetComputer(computerName);
            if (computer == null) return;

            string code = newSource ?? DEFAULT_CODE;
            computer.LoadCode(code, _sharedBus);

            // Keep base class in sync if it's the main computer
            if (computerName == "main")
            {
                _sourceCode = code;
                _executor = computer.Executor;
                _program = computer.Program;
            }

            Debug.Log($"[TankAI] Uploaded code to '{computerName}' ({computer.Program?.Instructions?.Length ?? 0} instructions)");
            OnCodeChanged?.Invoke();
        }
    }
}
