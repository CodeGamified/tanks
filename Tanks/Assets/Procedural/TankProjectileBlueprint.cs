// Copyright CodeGamified 2025-2026
// MIT License — Tanks: Code Your Tank
using CodeGamified.Procedural;
using UnityEngine;

namespace Tanks.Procedural
{
    /// <summary>
    /// Procedural blueprint for a tank projectile.
    /// Emits a single small sphere, similar to PongBallBlueprint.
    /// </summary>
    public class TankProjectileBlueprint : IProceduralBlueprint
    {
        public string DisplayName => "Projectile";
        public ProceduralLODHint LODHint => ProceduralLODHint.Lightweight;
        public string PaletteId => "tanks";

        public ProceduralPartDef[] GetParts()
        {
            return new[]
            {
                new ProceduralPartDef("body", PrimitiveType.Sphere,
                    Vector3.zero,
                    Vector3.one * 0.2f,
                    "projectile") { Collider = ColliderMode.None }
            };
        }
    }
}
