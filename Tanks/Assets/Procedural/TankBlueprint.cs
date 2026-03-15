// Copyright CodeGamified 2025-2026
// MIT License — Tanks: Code Your Tank
using CodeGamified.Procedural;
using Tanks.Game;
using UnityEngine;

namespace Tanks.Procedural
{
    /// <summary>
    /// Procedural blueprint for a tank.
    /// Body hull + tracks + turret + barrel + muzzle brake.
    /// Color depends on team (Player = green, AI = red).
    /// </summary>
    public class TankBlueprint : IProceduralBlueprint
    {
        private readonly TankTeam _team;
        private readonly int _index;

        public TankBlueprint(TankTeam team, int index)
        {
            _team = team;
            _index = index;
        }

        public string DisplayName => $"Tank_{_team}_{_index}";
        public ProceduralLODHint LODHint => ProceduralLODHint.Standard;
        public string PaletteId => "tanks";

        public ProceduralPartDef[] GetParts()
        {
            string bodyColor = _team == TankTeam.Player ? "tank_player" : "tank_ai";
            string turretColor = _team == TankTeam.Player ? "turret_player" : "turret_ai";

            return new[]
            {
                // ── Hull ──────────────────────────────────────────────
                new ProceduralPartDef("body", PrimitiveType.Cube,
                    Vector3.zero,
                    new Vector3(1.0f, 0.7f, 0.25f),
                    bodyColor) { Collider = ColliderMode.Box },

                // Front slope
                new ProceduralPartDef("hull_front", PrimitiveType.Cube,
                    new Vector3(0.45f, 0f, -0.05f),
                    new Vector3(0.2f, 0.55f, 0.15f),
                    bodyColor) { ParentId = "body",
                    LocalRot = Quaternion.Euler(0f, 0f, 15f) },

                // Rear plate
                new ProceduralPartDef("hull_rear", PrimitiveType.Cube,
                    new Vector3(-0.48f, 0f, 0f),
                    new Vector3(0.08f, 0.55f, 0.22f),
                    bodyColor) { ParentId = "body" },

                // ── Tracks — along hull bottom, full length ────────
                // Body Z=0.25 → half=0.125. Track below body bottom.
                new ProceduralPartDef("track_left", PrimitiveType.Cube,
                    new Vector3(0f, -0.28f, 0.18f),
                    new Vector3(1.2f, 0.2f, 0.2f),
                    "obstacle") { ParentId = "body" },

                new ProceduralPartDef("track_right", PrimitiveType.Cube,
                    new Vector3(0f, 0.28f, 0.18f),
                    new Vector3(1.2f, 0.2f, 0.2f),
                    "obstacle") { ParentId = "body" },

                // ── Turret — flat slab on top of hull ─────────────────
                // All cubes, no euler issues.
                new ProceduralPartDef("turret", PrimitiveType.Cube,
                    new Vector3(0.05f, 0f, -0.2f),
                    new Vector3(0.5f, 0.45f, 0.12f),
                    turretColor),

                // ── Gun ───────────────────────────────────────────────
                // Barrel — long thin cube along X
                new ProceduralPartDef("barrel", PrimitiveType.Cube,
                    new Vector3(0.5f, 0f, 0f),
                    new Vector3(0.65f, 0.08f, 0.08f),
                    turretColor) { ParentId = "turret" },

                // Muzzle brake — wider block at barrel tip
                new ProceduralPartDef("muzzle", PrimitiveType.Cube,
                    new Vector3(0.35f, 0f, 0f),
                    new Vector3(0.08f, 0.14f, 0.12f),
                    turretColor) { ParentId = "barrel" },

                // Mantlet — shield plate at barrel root
                new ProceduralPartDef("mantlet", PrimitiveType.Cube,
                    new Vector3(0.22f, 0f, 0f),
                    new Vector3(0.06f, 0.18f, 0.14f),
                    turretColor) { ParentId = "turret" },
            };
        }
    }
}
