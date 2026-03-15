// Copyright CodeGamified 2025-2026
// MIT License — Tanks: Code Your Tank
using CodeGamified.Procedural;
using UnityEngine;

namespace Tanks.Procedural
{
    /// <summary>
    /// Procedural blueprint for a single obstacle block.
    /// Emits one cube, colored as "obstacle".
    /// </summary>
    public class TankObstacleBlueprint : IProceduralBlueprint
    {
        private readonly float _halfW;
        private readonly float _halfH;
        private readonly int _index;

        public TankObstacleBlueprint(float halfW, float halfH, int index)
        {
            _halfW = halfW;
            _halfH = halfH;
            _index = index;
        }

        public string DisplayName => $"Obstacle_{_index}";
        public ProceduralLODHint LODHint => ProceduralLODHint.Standard;
        public string PaletteId => "tanks";

        public ProceduralPartDef[] GetParts()
        {
            return new[]
            {
                new ProceduralPartDef("body", PrimitiveType.Cube,
                    Vector3.zero,
                    new Vector3(_halfW * 2f, _halfH * 2f, 0.5f),
                    "obstacle") { Collider = ColliderMode.Box }
            };
        }
    }
}
