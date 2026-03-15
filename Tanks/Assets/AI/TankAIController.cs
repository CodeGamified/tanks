// Copyright CodeGamified 2025-2026
// MIT License — Tanks: Code Your Tank
using UnityEngine;
using Tanks.Game;
using Tanks.Scripting;

namespace Tanks.AI
{
    public enum AIDifficulty
    {
        Easy,
        Medium,
        Hard,
        Expert
    }

    /// <summary>
    /// AI tank controller — runs the SAME bytecode engine as the player.
    /// Each difficulty tier is a Python script compiled + executed by TankProgram.
    /// Mirrors PongAIController: no special C# logic — everything runs through scripts.
    /// </summary>
    public class TankAIController : MonoBehaviour
    {
        private TankBody _tank;
        private TankArena _arena;
        private AIDifficulty _difficulty;
        private TankProgram _program;

        public AIDifficulty Difficulty => _difficulty;
        public TankProgram Program => _program;

        public void Initialize(TankBody tank, TankArena arena, AIDifficulty difficulty)
        {
            _tank = tank;
            _arena = arena;

            _program = gameObject.AddComponent<TankProgram>();
            SetDifficulty(difficulty);
        }

        public void SetDifficulty(AIDifficulty difficulty)
        {
            _difficulty = difficulty;

            switch (difficulty)
            {
                case AIDifficulty.Easy:   _tank.maxSpeed = 2.5f; break;
                case AIDifficulty.Medium: _tank.maxSpeed = 3.5f; break;
                case AIDifficulty.Hard:   _tank.maxSpeed = 4f;   break;
                case AIDifficulty.Expert: _tank.maxSpeed = 4.5f; break;
            }

            string code = GetSampleCode(difficulty);
            _program.Initialize(_tank, _arena, code, $"AI_{difficulty}");

            Debug.Log($"[AI] Difficulty → {difficulty} (running bytecode)");
        }

        // =================================================================
        // SAMPLE CODE — actual AI logic in the same Python subset
        // the player uses. What you see IS what runs.
        // =================================================================

        public static string GetSampleCode(AIDifficulty difficulty)
        {
            switch (difficulty)
            {
                case AIDifficulty.Easy:
                    return @"# EASY — ""The Spinner""
# Sweeps turret to search — turret reveals infinite ray through fog
sweep = get_my_turret() + 15
set_turret(sweep)
scan()
dist = get_enemy_dist()
if dist < 9999:
    ex = get_enemy_x()
    dx = ex - get_my_x()
    set_turret(dx)
    set_drive(0.4)
    fire()
if dist > 9000:
    set_drive(0.3)
    set_turn(0.2)";

                case AIDifficulty.Medium:
                    return @"# MEDIUM — ""The Tracker""
# Sweeps turret to search, locks on when found
sweep = get_my_turret() + 20
set_turret(sweep)
scan()
dist = get_enemy_dist()
if dist < 9999:
    ex = get_enemy_x()
    ey = get_enemy_y()
    dx = ex - get_my_x()
    set_turret(dx)
    set_drive(0.6)
    if dist < 5:
        fire()
    if dx > 0:
        set_turn(0.5)
    if dx < 0:
        set_turn(-0.5)
if dist > 9000:
    set_drive(0.3)
    set_turn(0.1)";

                case AIDifficulty.Hard:
                    return @"# HARD — ""The Flanker""
# Uses turret ray as searchlight, orbits when locked on
sweep = get_my_turret() + 25
set_turret(sweep)
scan()
dist = get_enemy_dist()
odist = get_obstacle_dist()
if odist < 2:
    set_turn(0.8)
    set_drive(-0.4)
if dist < 9999:
    ex = get_enemy_x()
    dx = ex - get_my_x()
    set_turret(dx)
    if dist > 6:
        set_drive(0.8)
        set_turn(0.3)
    if dist < 4:
        set_drive(-0.5)
        set_turn(0.6)
    if dist > 3:
        if dist < 8:
            fire()
if dist > 9000:
    set_drive(0.3)";

                case AIDifficulty.Expert:
                    return @"# EXPERT — ""The Tactician""
# Turret sweeps as radar, holds position, precise shots
sweep = get_my_turret() + 30
set_turret(sweep)
scan()
dist = get_enemy_dist()
ammo = get_my_ammo()
odist = get_obstacle_dist()
if odist < 2:
    set_turn(0.9)
    set_drive(-0.3)
if dist < 9999:
    ex = get_enemy_x()
    ey = get_enemy_y()
    dx = ex - get_my_x()
    set_turret(dx)
    if dist > 7:
        set_drive(0.9)
        set_turn(0.2)
    if dist < 3:
        set_drive(-0.8)
        set_turn(0.7)
    if dist > 3:
        if dist < 9:
            if ammo > 2:
                fire()
    if ammo < 3:
        set_drive(0.6)
        set_turn(0.8)
if dist > 9000:
    set_drive(0.2)";

                default:
                    return "# Unknown difficulty";
            }
        }
    }
}
