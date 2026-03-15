// Copyright CodeGamified 2025-2026
// MIT License — Tanks: Code Your Tank
using CodeGamified.Procedural;
using UnityEngine;
using System.Collections.Generic;

namespace Tanks.Procedural
{
    /// <summary>
    /// Procedural blueprint for the arena.
    /// Emits 4 walls + floor plane. Mirrors PongCourtBlueprint.
    /// </summary>
    public class TankArenaBlueprint : IProceduralBlueprint
    {
        private readonly float _width;
        private readonly float _height;

        public TankArenaBlueprint(float width, float height)
        {
            _width = width;
            _height = height;
        }

        public string DisplayName => "TankArena";
        public ProceduralLODHint LODHint => ProceduralLODHint.Standard;
        public string PaletteId => "tanks";

        public ProceduralPartDef[] GetParts()
        {
            var parts = new List<ProceduralPartDef>();
            float halfW = _width / 2f;
            float halfH = _height / 2f;
            float wallThickness = 0.3f;
            float wallHeight = 0.5f;

            // Floor
            parts.Add(new ProceduralPartDef("floor", PrimitiveType.Cube,
                new Vector3(0f, 0f, 0.3f),
                new Vector3(_width + 2f, _height + 2f, 0.1f),
                "arena_floor"));

            // Top wall
            parts.Add(new ProceduralPartDef("wall_top", PrimitiveType.Cube,
                new Vector3(0f, halfH + wallThickness / 2f, 0f),
                new Vector3(_width + 2f, wallThickness, wallHeight),
                "wall") { Collider = ColliderMode.Box });

            // Bottom wall
            parts.Add(new ProceduralPartDef("wall_bottom", PrimitiveType.Cube,
                new Vector3(0f, -halfH - wallThickness / 2f, 0f),
                new Vector3(_width + 2f, wallThickness, wallHeight),
                "wall") { Collider = ColliderMode.Box });

            // Left wall
            parts.Add(new ProceduralPartDef("wall_left", PrimitiveType.Cube,
                new Vector3(-halfW - wallThickness / 2f, 0f, 0f),
                new Vector3(wallThickness, _height + 2f, wallHeight),
                "wall") { Collider = ColliderMode.Box });

            // Right wall
            parts.Add(new ProceduralPartDef("wall_right", PrimitiveType.Cube,
                new Vector3(halfW + wallThickness / 2f, 0f, 0f),
                new Vector3(wallThickness, _height + 2f, wallHeight),
                "wall") { Collider = ColliderMode.Box });

            return parts.ToArray();
        }
    }
}
