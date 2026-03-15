// Copyright CodeGamified 2025-2026
// MIT License — Tanks: Code Your Tank
using CodeGamified.Engine;
using CodeGamified.Time;
using Tanks.Game;
using UnityEngine;

namespace Tanks.Scripting
{
    /// <summary>
    /// Game I/O handler for Tanks — bridges CUSTOM opcodes to tank state.
    /// Each tank gets its own TankIOHandler wired to its own TankBody.
    /// Mirrors PongIOHandler architecture.
    /// </summary>
    public class TankIOHandler : IGameIOHandler
    {
        private readonly TankBody _tank;
        private readonly TankArena _arena;
        private TankFogOfWar _fog;
        private float[] _sharedBus;

        public TankIOHandler(TankBody tank, TankArena arena)
        {
            _tank = tank;
            _arena = arena;
            _fog = arena?.FogOfWar;
        }

        /// <summary>Late-bind fog reference (created after IOHandler).</summary>
        public void SetFog(TankFogOfWar fog) { _fog = fog; }

        /// <summary>Wire up the shared data bus for inter-computer communication.</summary>
        public void SetSharedBus(float[] bus) { _sharedBus = bus; }

        public bool PreExecute(Instruction inst, MachineState state)
        {
            return true;
        }

        public void ExecuteIO(Instruction inst, MachineState state)
        {
            int tankOp = (int)inst.Op - (int)OpCode.CUSTOM_0;

            switch ((TankOpCode)tankOp)
            {
                // ── Queries → R0 ──
                case TankOpCode.GET_MY_X:
                    state.SetRegister(0, _tank.posX);
                    break;
                case TankOpCode.GET_MY_Y:
                    state.SetRegister(0, _tank.posY);
                    break;
                case TankOpCode.GET_MY_HEADING:
                    state.SetRegister(0, _tank.heading);
                    break;
                case TankOpCode.GET_MY_TURRET:
                    state.SetRegister(0, _tank.turretAngle);
                    break;
                case TankOpCode.GET_MY_HP:
                    state.SetRegister(0, _tank.hp);
                    break;
                case TankOpCode.GET_MY_AMMO:
                    state.SetRegister(0, _tank.ammo);
                    break;
                case TankOpCode.GET_NEAREST_ENEMY_X:
                    state.SetRegister(0, FindNearestEnemyX());
                    break;
                case TankOpCode.GET_NEAREST_ENEMY_Y:
                    state.SetRegister(0, FindNearestEnemyY());
                    break;
                case TankOpCode.GET_NEAREST_ENEMY_DIST:
                    state.SetRegister(0, FindNearestEnemyDist());
                    break;
                case TankOpCode.GET_ARENA_W:
                    state.SetRegister(0, _arena?.Width ?? 20f);
                    break;
                case TankOpCode.GET_ARENA_H:
                    state.SetRegister(0, _arena?.Height ?? 14f);
                    break;

                // ── Orders ──
                case TankOpCode.SET_DRIVE:
                    _tank.driveCommand = state.Registers[0];
                    break;
                case TankOpCode.SET_TURN:
                    _tank.turnCommand = state.Registers[0];
                    break;
                case TankOpCode.SET_TURRET:
                    _tank.turretTarget = state.Registers[0];
                    break;
                case TankOpCode.FIRE:
                    _tank.fireRequested = true;
                    _tank.TryFire();
                    break;

                // ── Fog of War ──
                case TankOpCode.SCAN:
                    if (_fog != null) _fog.Scan(_tank);
                    break;
                case TankOpCode.IS_VISIBLE:
                {
                    float qx = state.Registers[0];
                    float qy = state.Registers[1];
                    bool vis = _fog == null || _fog.IsVisible(_tank.TankIndex, qx, qy);
                    state.SetRegister(0, vis ? 1f : 0f);
                    break;
                }

                // ── Obstacle awareness ──
                case TankOpCode.GET_NEAREST_OBSTACLE_X:
                    state.SetRegister(0, FindNearestObstacleX());
                    break;
                case TankOpCode.GET_NEAREST_OBSTACLE_Y:
                    state.SetRegister(0, FindNearestObstacleY());
                    break;
                case TankOpCode.GET_NEAREST_OBSTACLE_DIST:
                    state.SetRegister(0, FindNearestObstacleDist());
                    break;

                // ── Inter-computer data bus ──
                case TankOpCode.SEND:
                {
                    if (_sharedBus != null)
                    {
                        int ch = Mathf.Clamp((int)state.Registers[0], 0, _sharedBus.Length - 1);
                        _sharedBus[ch] = state.Registers[1];
                    }
                    break;
                }
                case TankOpCode.RECV:
                {
                    if (_sharedBus != null)
                    {
                        int ch = Mathf.Clamp((int)state.Registers[0], 0, _sharedBus.Length - 1);
                        state.SetRegister(0, _sharedBus[ch]);
                    }
                    else
                    {
                        state.SetRegister(0, 0f);
                    }
                    break;
                }
            }
        }

        public float GetTimeScale()
        {
            return SimulationTime.Instance?.timeScale ?? 1f;
        }

        public double GetSimulationTime()
        {
            return SimulationTime.Instance?.simulationTime ?? 0.0;
        }

        // ── Nearest enemy helpers (fog-gated) ──

        private TankBody FindNearestEnemy()
        {
            TankBody nearest = null;
            float bestDistSq = float.MaxValue;
            var tanks = Object.FindObjectsByType<TankBody>(FindObjectsSortMode.None);

            foreach (var other in tanks)
            {
                if (other == _tank || !other.IsAlive) continue;

                // Fog gate: enemy must be in a VISIBLE cell
                if (_fog != null && !_fog.IsVisible(_tank.TankIndex, other.posX, other.posY))
                    continue;

                float dx = other.posX - _tank.posX;
                float dy = other.posY - _tank.posY;
                float distSq = dx * dx + dy * dy;
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    nearest = other;
                }
            }
            return nearest;
        }

        private float FindNearestEnemyX()
        {
            var e = FindNearestEnemy();
            return e != null ? e.posX : 0f;
        }

        private float FindNearestEnemyY()
        {
            var e = FindNearestEnemy();
            return e != null ? e.posY : 0f;
        }

        private float FindNearestEnemyDist()
        {
            var e = FindNearestEnemy();
            if (e == null) return 9999f;
            float dx = e.posX - _tank.posX;
            float dy = e.posY - _tank.posY;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        // ── Nearest obstacle helpers (always available — obstacles are local) ──

        private TankObstacle FindNearestObstacle()
        {
            if (_arena?.Obstacles == null) return null;
            TankObstacle nearest = null;
            float bestDistSq = float.MaxValue;
            foreach (var obs in _arena.Obstacles)
            {
                float dx = obs.CenterX - _tank.posX;
                float dy = obs.CenterY - _tank.posY;
                float distSq = dx * dx + dy * dy;
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    nearest = obs;
                }
            }
            return nearest;
        }

        private float FindNearestObstacleX()
        {
            var o = FindNearestObstacle();
            return o != null ? o.CenterX : 0f;
        }

        private float FindNearestObstacleY()
        {
            var o = FindNearestObstacle();
            return o != null ? o.CenterY : 0f;
        }

        private float FindNearestObstacleDist()
        {
            var o = FindNearestObstacle();
            if (o == null) return 9999f;
            float dx = o.CenterX - _tank.posX;
            float dy = o.CenterY - _tank.posY;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }
    }
}
