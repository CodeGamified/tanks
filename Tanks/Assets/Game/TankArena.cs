// Copyright CodeGamified 2025-2026
// MIT License — Tanks: Code Your Tank
using System.Collections.Generic;
using UnityEngine;
using CodeGamified.Quality;

namespace Tanks.Game
{
    /// <summary>
    /// The Tanks arena — bounded rectangle with walls, obstacles, and fog of war.
    /// Built via ProceduralAssembler from TankArenaBlueprint.
    /// Mirrors PongCourt architecture.
    /// </summary>
    public class TankArena : MonoBehaviour, IQualityResponsive
    {
        public float Width { get; set; } = 20f;
        public float Height { get; set; } = 14f;

        public float HalfWidth => Width / 2f;
        public float HalfHeight => Height / 2f;

        public CodeGamified.Procedural.AssemblyResult Visual { get; private set; }

        // Obstacles in the arena
        public List<TankObstacle> Obstacles { get; } = new List<TankObstacle>();

        // Fog of war
        public TankFogOfWar FogOfWar { get; set; }

        private CodeGamified.Procedural.ColorPalette _palette;

        public void Initialize(CodeGamified.Procedural.ColorPalette palette)
        {
            _palette = palette;
            RebuildVisual();
        }

        private void OnEnable()  => QualityBridge.Register(this);
        private void OnDisable() => QualityBridge.Unregister(this);

        public void OnQualityChanged(QualityTier tier) => RebuildVisual();

        private void RebuildVisual()
        {
            if (Visual.Root != null)
                Destroy(Visual.Root);

            var blueprint = new Tanks.Procedural.TankArenaBlueprint(Width, Height);
            Visual = CodeGamified.Procedural.ProceduralAssembler.BuildWithVisualState(blueprint, _palette);

            if (Visual.Root != null)
                Visual.Root.transform.SetParent(transform, false);
        }
    }
}
