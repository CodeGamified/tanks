// Copyright CodeGamified 2025-2026
// MIT License — Tanks: Code Your Tank
using System.Collections.Generic;
using UnityEngine;
using CodeGamified.Time;

namespace Tanks.Game
{
    /// <summary>
    /// Voxelized fog of war system.
    ///
    /// Grid of cells covering the arena. Each tank has its own visibility state:
    ///   HIDDEN   — never seen (opaque dark cubes)
    ///   FOGGED   — previously seen but not currently visible (semi-transparent)
    ///   VISIBLE  — in line-of-sight right now (no cube rendered)
    ///
    /// Vision model:
    ///   - Each tank reveals cells within visionRadius
    ///   - Line-of-sight is blocked by obstacles (raycasted per-cell)
    ///   - Calling scan() from script costs 1 op and refreshes visibility
    ///   - get_enemy_x/y/dist return 0/9999 if enemy is not in VISIBLE cells
    ///
    /// Visual representation:
    ///   - One cube per cell, toggled active/color based on fog state
    ///   - Updated each simulation tick (batched, not per-frame for perf)
    /// </summary>
    public class TankFogOfWar : MonoBehaviour
    {
        public enum FogState : byte
        {
            Hidden  = 0,
            Fogged  = 1,
            Visible = 2,
        }

        // Config
        public float cellSize = 1.0f;
        public float visionRadius = 6f;

        // Grid
        private int _gridW;
        private int _gridH;
        private float _originX;
        private float _originY;

        // Per-tank fog grids (indexed by TankBody.TankIndex)
        private Dictionary<int, FogState[,]> _fogGrids = new Dictionary<int, FogState[,]>();

        // Visual cubes (shared — shows player's perspective)
        private GameObject[,] _fogCubes;
        private Renderer[,] _fogRenderers;
        private Material _hiddenMat;
        private Material _foggedMat;

        // References
        private TankArena _arena;
        private List<TankObstacle> _obstacles;
        private int _displayTankIndex; // which tank's fog to visualize

        // Update throttle
        private float _visualUpdateInterval = 0.1f;
        private float _nextVisualUpdate;

        public void Initialize(TankArena arena, List<TankObstacle> obstacles,
                               float cellSize, float visionRadius, int displayTankIndex = 0)
        {
            _arena = arena;
            _obstacles = obstacles;
            this.cellSize = cellSize;
            this.visionRadius = visionRadius;
            _displayTankIndex = displayTankIndex;

            _gridW = Mathf.CeilToInt(arena.Width / cellSize);
            _gridH = Mathf.CeilToInt(arena.Height / cellSize);
            _originX = -arena.Width / 2f;
            _originY = -arena.Height / 2f;

            BuildMaterials();
            BuildFogCubes();
        }

        /// <summary>Register a tank so it gets its own fog grid.</summary>
        public void RegisterTank(TankBody tank)
        {
            if (_fogGrids.ContainsKey(tank.TankIndex)) return;
            _fogGrids[tank.TankIndex] = new FogState[_gridW, _gridH];
        }

        // ═══════════════════════════════════════════════════════════
        // SCAN — called by TankIOHandler when script invokes scan()
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Update visibility for a specific tank. Cells within visionRadius
        /// with clear line-of-sight become VISIBLE; previously VISIBLE cells
        /// outside range become FOGGED.
        /// </summary>
        public void Scan(TankBody tank)
        {
            if (!_fogGrids.TryGetValue(tank.TankIndex, out var grid)) return;

            float visSq = visionRadius * visionRadius;
            Vector2 tankPos = new Vector2(tank.posX, tank.posY);

            // Phase 1: radius-based vision (short range, 360°)
            for (int gx = 0; gx < _gridW; gx++)
            {
                for (int gy = 0; gy < _gridH; gy++)
                {
                    Vector2 cellCenter = CellCenter(gx, gy);
                    float dx = cellCenter.x - tankPos.x;
                    float dy = cellCenter.y - tankPos.y;
                    float distSq = dx * dx + dy * dy;

                    if (distSq <= visSq && HasLineOfSight(tankPos, cellCenter))
                    {
                        grid[gx, gy] = FogState.Visible;
                    }
                    else if (grid[gx, gy] == FogState.Visible)
                    {
                        grid[gx, gy] = FogState.Fogged;
                    }
                }
            }

            // Phase 2: turret ray — infinite range along turret direction
            RevealTurretRay(tank, grid);
        }

        /// <summary>
        /// Cast a ray from the tank along its turret direction.
        /// Reveals every cell it passes through until it hits an obstacle or arena edge.
        /// This gives the turret direction "infinite" vision through fog.
        /// </summary>
        private void RevealTurretRay(TankBody tank, FogState[,] grid)
        {
            Vector2 tankPos = new Vector2(tank.posX, tank.posY);
            Vector2 dir = tank.TurretForward;

            float halfW = _arena.Width / 2f;
            float halfH = _arena.Height / 2f;
            float maxDist = Mathf.Sqrt(halfW * halfW + halfH * halfH) * 2f;
            float step = cellSize * 0.5f;

            for (float t = 0f; t < maxDist; t += step)
            {
                float wx = tankPos.x + dir.x * t;
                float wy = tankPos.y + dir.y * t;

                // Out of arena?
                if (wx < -halfW || wx > halfW || wy < -halfH || wy > halfH)
                    break;

                // Blocked by obstacle?
                bool blocked = false;
                if (_obstacles != null)
                {
                    foreach (var obs in _obstacles)
                    {
                        if (obs.Overlaps(wx, wy, 0.1f))
                        { blocked = true; break; }
                    }
                }
                if (blocked) break;

                int gx = WorldToGridX(wx);
                int gy = WorldToGridY(wy);
                grid[gx, gy] = FogState.Visible;
            }
        }

        /// <summary>Is a world position currently VISIBLE for the given tank?</summary>
        public bool IsVisible(int tankIndex, float worldX, float worldY)
        {
            if (!_fogGrids.TryGetValue(tankIndex, out var grid)) return true;
            int gx = WorldToGridX(worldX);
            int gy = WorldToGridY(worldY);
            if (gx < 0 || gx >= _gridW || gy < 0 || gy >= _gridH) return false;
            return grid[gx, gy] == FogState.Visible;
        }

        /// <summary>Get fog state at grid coords for a specific tank.</summary>
        public FogState GetState(int tankIndex, int gx, int gy)
        {
            if (!_fogGrids.TryGetValue(tankIndex, out var grid)) return FogState.Hidden;
            if (gx < 0 || gx >= _gridW || gy < 0 || gy >= _gridH) return FogState.Hidden;
            return grid[gx, gy];
        }

        // ═══════════════════════════════════════════════════════════
        // LINE OF SIGHT
        // ═══════════════════════════════════════════════════════════

        private bool HasLineOfSight(Vector2 from, Vector2 to)
        {
            if (_obstacles == null) return true;
            foreach (var obs in _obstacles)
            {
                if (obs.IntersectsSegment(from, to))
                    return false;
            }
            return true;
        }

        // ═══════════════════════════════════════════════════════════
        // GRID ↔ WORLD
        // ═══════════════════════════════════════════════════════════

        private Vector2 CellCenter(int gx, int gy)
        {
            return new Vector2(
                _originX + (gx + 0.5f) * cellSize,
                _originY + (gy + 0.5f) * cellSize);
        }

        private int WorldToGridX(float wx) => Mathf.Clamp(
            Mathf.FloorToInt((wx - _originX) / cellSize), 0, _gridW - 1);

        private int WorldToGridY(float wy) => Mathf.Clamp(
            Mathf.FloorToInt((wy - _originY) / cellSize), 0, _gridH - 1);

        // ═══════════════════════════════════════════════════════════
        // VISUAL
        // ═══════════════════════════════════════════════════════════

        private void BuildMaterials()
        {
            var shader = Shader.Find("Sprites/Default")
                ?? Shader.Find("Unlit/Color");

            _hiddenMat = new Material(shader);
            _hiddenMat.color = new Color(0.02f, 0.02f, 0.04f, 0.92f);

            _foggedMat = new Material(shader);
            _foggedMat.color = new Color(0.05f, 0.06f, 0.1f, 0.5f);
        }

        private void BuildFogCubes()
        {
            _fogCubes = new GameObject[_gridW, _gridH];
            _fogRenderers = new Renderer[_gridW, _gridH];

            for (int gx = 0; gx < _gridW; gx++)
            {
                for (int gy = 0; gy < _gridH; gy++)
                {
                    var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube.name = $"Fog_{gx}_{gy}";
                    cube.transform.SetParent(transform, false);

                    Vector2 center = CellCenter(gx, gy);
                    cube.transform.localPosition = new Vector3(center.x, center.y, -0.2f);
                    cube.transform.localScale = new Vector3(cellSize * 0.95f, cellSize * 0.95f, 0.15f);

                    var col = cube.GetComponent<Collider>();
                    if (col != null) Destroy(col);

                    var rend = cube.GetComponent<Renderer>();
                    rend.material = _hiddenMat;

                    _fogCubes[gx, gy] = cube;
                    _fogRenderers[gx, gy] = rend;
                }
            }
        }

        private void Update()
        {
            if (Time.time < _nextVisualUpdate) return;
            _nextVisualUpdate = Time.time + _visualUpdateInterval;
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (!_fogGrids.TryGetValue(_displayTankIndex, out var grid)) return;

            for (int gx = 0; gx < _gridW; gx++)
            {
                for (int gy = 0; gy < _gridH; gy++)
                {
                    var cube = _fogCubes[gx, gy];
                    if (cube == null) continue;

                    switch (grid[gx, gy])
                    {
                        case FogState.Hidden:
                            cube.SetActive(true);
                            _fogRenderers[gx, gy].sharedMaterial = _hiddenMat;
                            break;
                        case FogState.Fogged:
                            cube.SetActive(true);
                            _fogRenderers[gx, gy].sharedMaterial = _foggedMat;
                            break;
                        case FogState.Visible:
                            cube.SetActive(false);
                            break;
                    }
                }
            }
        }
    }
}
