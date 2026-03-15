// Copyright CodeGamified 2025-2026
// MIT License — Tanks: Code Your Tank
using CodeGamified.Engine;
using CodeGamified.Engine.Compiler;
using CodeGamified.Engine.Runtime;
using Tanks.Game;
using UnityEngine;

namespace Tanks.Scripting
{
    /// <summary>
    /// A single onboard computer within a tank.
    /// Each tank can have multiple computers, each running its own script
    /// with its own registers, variables, and instruction pointer.
    /// All computers on a tank share the same TankBody and communicate
    /// via a shared data bus (float[] channels).
    ///
    /// Examples:
    ///   "main"       — default all-in-one controller
    ///   "turret"     — scanning for enemies, aiming, firing
    ///   "navigation" — pathfinding, obstacle avoidance, movement
    ///   "radar"      — fog-of-war scanning, enemy tracking
    /// </summary>
    public class TankComputer
    {
        public string Name { get; private set; }
        public string SourceCode { get; private set; }
        public float OpsPerSecond { get; set; }

        public CompiledProgram Program { get; private set; }
        public CodeExecutor Executor { get; private set; }
        public TankIOHandler IOHandler { get; private set; }

        private readonly TankBody _tank;
        private readonly TankArena _arena;
        private readonly TankCompilerExtension _compilerExt;
        private float _opAccumulator;
        private bool _isPaused;

        public bool IsRunning => Executor?.IsRunning ?? false;
        public MachineState State => Executor?.State;

        public TankComputer(string name, TankBody tank, TankArena arena,
                            float opsPerSecond = TankProgram.OPS_PER_SECOND)
        {
            Name = name;
            _tank = tank;
            _arena = arena;
            _compilerExt = new TankCompilerExtension();
            OpsPerSecond = opsPerSecond;
        }

        /// <summary>
        /// Compile and load source code. The shared bus enables inter-computer communication.
        /// </summary>
        public bool LoadCode(string source, float[] sharedBus)
        {
            SourceCode = source;

            Executor = new CodeExecutor();
            IOHandler = new TankIOHandler(_tank, _arena);
            IOHandler.SetSharedBus(sharedBus);
            Executor.SetIOHandler(IOHandler);

            Program = PythonCompiler.Compile(source, Name, _compilerExt);

            if (!Program.IsValid)
            {
                Debug.LogWarning($"[TankComputer:{Name}] Compile errors:");
                foreach (var err in Program.Errors)
                    Debug.LogWarning($"  {err}");
                return false;
            }

            Executor.LoadProgram(Program);
            _isPaused = false;
            _opAccumulator = 0f;

            Debug.Log($"[TankComputer:{Name}] Loaded: {Program.Instructions.Length} instructions @ {OpsPerSecond} ops/s");
            return true;
        }

        /// <summary>
        /// Execute instructions for this simulation time step.
        /// When script reaches HALT, PC resets to 0 — memory persists.
        /// Returns number of instructions executed.
        /// </summary>
        public int Tick(float simDelta)
        {
            if (Executor == null || Program == null || _isPaused) return 0;
            if (_tank == null || !_tank.IsAlive) return 0;

            _opAccumulator += simDelta * OpsPerSecond;
            int opsToRun = (int)_opAccumulator;
            _opAccumulator -= opsToRun;

            for (int i = 0; i < opsToRun; i++)
            {
                if (Executor.State.IsHalted)
                {
                    Executor.State.PC = 0;
                    Executor.State.IsHalted = false;
                }
                Executor.ExecuteOne();
            }

            if (opsToRun > 0)
                DrainEvents();

            return opsToRun;
        }

        public void Pause() => _isPaused = true;
        public void Resume() => _isPaused = false;

        public void SetFog(TankFogOfWar fog)
        {
            IOHandler?.SetFog(fog);
        }

        private void DrainEvents()
        {
            if (Executor?.State == null) return;
            while (Executor.State.OutputEvents.Count > 0)
                Executor.State.OutputEvents.Dequeue();
        }
    }
}
