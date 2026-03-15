# Tanks: Code Your Tank

A programming game where you write Python-like scripts to control your tank in combat. No WASD — your code **is** the controller. Last tank standing wins.

Built with Unity 6 (6000.0.36f1) on the [CodeGamified](https://codegamified.github.io/) engine.

## Concept

Each tank is driven entirely by code. You write a short script that reads sensors, makes decisions, and issues movement/firing commands. Scripts execute at **15 operations per second** (sim-time), so every instruction counts — efficiency is strategy.

Your tank fights an AI opponent on a procedurally generated arena with destructible obstacles and fog of war. The turret doubles as a radar: its direction casts an infinite vision ray through the fog.

## Script API

### Sensors

| Function | Returns |
|---|---|
| `get_my_x()` / `get_my_y()` | Tank position |
| `get_my_heading()` | Body angle (degrees, 0=right, CCW+) |
| `get_my_turret()` | Turret angle (degrees, relative to body) |
| `get_my_hp()` | Remaining HP |
| `get_my_ammo()` | Remaining ammo |
| `get_enemy_x()` / `get_enemy_y()` | Nearest **visible** enemy position |
| `get_enemy_dist()` | Distance to nearest visible enemy (9999 if hidden) |
| `get_obstacle_x()` / `get_obstacle_y()` | Nearest obstacle center |
| `get_obstacle_dist()` | Distance to nearest obstacle |

### Fog of War

| Function | Description |
|---|---|
| `scan()` | Refresh visibility (costs 1 op) |
| `is_visible(x, y)` | Is cell visible? Returns 0 or 1 |

### Commands

| Function | Description |
|---|---|
| `set_drive(speed)` | Drive forward/backward (−1 to +1) |
| `set_turn(rate)` | Turn body (−1 to +1) |
| `set_turret(angle)` | Aim turret (degrees relative to body) |
| `fire()` | Shoot a projectile |

### Data Bus (Multi-Computer)

Each tank can run multiple onboard computers (e.g. "main", "turret", "radar") that share a 16-channel float bus:

| Function | Description |
|---|---|
| `send(channel, value)` | Write to shared bus (channels 0–15) |
| `recv(channel)` | Read from shared bus → R0 |

## Example Script

```python
# Sweep turret as radar, lock & fire when enemy spotted
sweep = get_my_turret() + 20
set_turret(sweep)
scan()
dist = get_enemy_dist()
if dist < 9999:
    ex = get_enemy_x()
    dx = ex - get_my_x()
    set_turret(dx)
    set_drive(0.5)
    fire()
if dist > 9000:
    set_drive(0.2)
```

## Game Mechanics

- **Arena**: Bounded rectangle with walls and procedurally placed obstacles
- **Projectiles**: Travel in straight lines, bounce off walls (up to 2 bounces), damage tanks on hit
- **Fog of War**: Grid-based visibility — cells are Hidden, Fogged, or Visible. Turret direction reveals an infinite ray through fog
- **HP/Ammo**: Tanks start with 3 HP and 10 ammo. Match ends when one tank is destroyed, or draws if both are alive with no ammo and no active projectiles
- **AI Opponents**: Easy, Medium, Hard, and Expert — all run the same bytecode engine as the player (no special C# logic)
- **Time Control**: Adjustable simulation speed via time warp

## Architecture

```
Tanks/Assets/
├── AI/               # AI controllers (same bytecode engine as player)
├── Core/             # TankBootstrap — scene wiring & initialization
├── Game/             # TankBody, TankArena, TankProjectile, TankFogOfWar, TankObstacle
├── Procedural/       # Blueprints for tanks, arena, obstacles, projectiles
├── Scenes/           # Unity scenes
├── Scripting/        # TankProgram, TankComputer, TankIOHandler, compiler extension
├── UI/               # TUI manager, status bar, code debugger
└── engine/           # Shared CodeGamified engine submodule
    ├── CodeGamified.Audio/        # Audio & haptic abstraction
    ├── CodeGamified.Bootstrap/    # Game bootstrap base class
    ├── CodeGamified.Camera/       # Camera rig & ambient motion
    ├── CodeGamified.Editor/       # In-game code editor (TUI)
    ├── CodeGamified.Engine/       # Bytecode compiler, executor, opcodes
    ├── CodeGamified.Persistence/  # Git-based save system
    ├── CodeGamified.Procedural/   # Procedural mesh generation
    ├── CodeGamified.Quality/      # Quality tier management
    ├── CodeGamified.Settings/     # Settings bridge
    ├── CodeGamified.Time/         # SimulationTime & time warp
    └── CodeGamified.TUI/          # Terminal UI framework
```

## Getting Started

1. Open the `Tanks/` folder in Unity 6 (6000.0.36f1 or compatible)
2. Open `Assets/Scenes/Engine.unity`
3. Press Play
4. Edit your tank's script in the in-game code editor
5. Watch your code fight the AI

## License

MIT — Copyright CodeGamified 2025-2026
