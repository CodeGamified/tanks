// Copyright CodeGamified 2025-2026
// MIT License — Tanks: Code Your Tank
using UnityEngine;

namespace Tanks.Core
{
    /// <summary>
    /// Tanks-specific simulation time — subclasses the engine's abstract SimulationTime.
    /// Defines: max scale (500x) and time formatting (MM:SS).
    /// Mirrors PongSimulationTime.
    /// </summary>
    public class TankSimulationTime : CodeGamified.Time.SimulationTime
    {
        protected override float MaxTimeScale => 500f;

        protected override void OnInitialize()
        {
            timeScalePresets = new float[]
                { 0f, 0.25f, 0.5f, 1f, 2f, 5f, 10f, 50f, 100f, 500f };
            currentPresetIndex = 3; // Start at 1x
        }

        public override string GetFormattedTime()
        {
            int minutes = (int)(simulationTime / 60.0);
            int seconds = (int)(simulationTime % 60.0);
            return $"{minutes:D2}:{seconds:D2}";
        }
    }
}
