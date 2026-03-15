// Copyright CodeGamified 2025-2026
// MIT License — Tanks: Code Your Tank
using UnityEngine;
using CodeGamified.Time;

namespace Tanks.Game
{
    /// <summary>
    /// A single tank on the battlefield.
    /// Controlled entirely by code — no WASD.
    ///
    /// State exposed to scripts:
    ///   position (x,y), heading (degrees), turret angle (degrees),
    ///   HP, ammo count, alive flag.
    ///
    /// Script API:
    ///   get_my_x/y()        → tank position
    ///   get_my_heading()    → body heading (degrees, 0=right, CCW+)
    ///   get_my_turret()     → turret angle (degrees, relative to body)
    ///   get_my_hp()         → remaining HP
    ///   get_my_ammo()       → remaining ammo
    ///   set_drive(speed)    → drive forward/backward
    ///   set_turn(rate)      → turn body
    ///   set_turret(angle)   → aim turret
    ///   fire()              → fire projectile from turret
    /// </summary>
    public class TankBody : MonoBehaviour
    {
        public TankTeam Team { get; private set; }
        public int TankIndex { get; private set; }
        public bool IsAlive { get; private set; } = true;

        // Config
        public float maxSpeed = 4f;
        public float turnSpeed = 120f;    // degrees/sec
        public float turretSpeed = 180f;  // degrees/sec
        public int maxHP = 3;
        public int maxAmmo = 10;
        public float fireCooldown = 1f;   // sim-seconds between shots
        public float projectileSpeed = 8f;

        // State — exposed to scripts
        public float posX;
        public float posY;
        public float heading;        // degrees, 0=right, CCW positive
        public float turretAngle;    // degrees relative to body
        public int hp;
        public int ammo;

        // Drive commands (set by script each tick)
        public float driveCommand;   // -1..+1 speed fraction
        public float turnCommand;    // angular velocity request
        public float turretTarget;   // target turret angle
        public bool fireRequested;

        // Internal
        private float _cooldownRemaining;
        private TankArena _arena;
        private System.Collections.Generic.List<TankObstacle> _obstacles;

        // Events
        public System.Action<TankBody> OnDestroyed;
        public System.Action<TankBody> OnFired;

        public Vector2 Forward => new Vector2(
            Mathf.Cos(heading * Mathf.Deg2Rad),
            Mathf.Sin(heading * Mathf.Deg2Rad));

        public Vector2 TurretForward
        {
            get
            {
                float worldAngle = heading + turretAngle;
                return new Vector2(
                    Mathf.Cos(worldAngle * Mathf.Deg2Rad),
                    Mathf.Sin(worldAngle * Mathf.Deg2Rad));
            }
        }

        public void Initialize(TankTeam team, int index, TankArena arena,
                               float startX, float startY, float startHeading)
        {
            Team = team;
            TankIndex = index;
            _arena = arena;
            _obstacles = arena?.Obstacles;
            posX = startX;
            posY = startY;
            heading = startHeading;
            turretAngle = 0f;
            hp = maxHP;
            ammo = maxAmmo;
            IsAlive = true;
            _cooldownRemaining = 0f;

            transform.position = new Vector3(posX, posY, 0f);
            transform.rotation = Quaternion.Euler(0f, 0f, heading);
        }

        private void Update()
        {
            if (!IsAlive) return;
            if (SimulationTime.Instance == null || SimulationTime.Instance.isPaused) return;

            float dt = Time.deltaTime * (SimulationTime.Instance?.timeScale ?? 1f);

            // Sub-step for high time scales
            int steps = Mathf.Max(1, Mathf.CeilToInt(dt / 0.004f));
            float subDt = dt / steps;

            for (int i = 0; i < steps && IsAlive; i++)
                StepPhysics(subDt);

            // Cooldown
            _cooldownRemaining = Mathf.Max(0f, _cooldownRemaining - dt);

            // Sync transform
            transform.position = new Vector3(posX, posY, 0f);
            transform.rotation = Quaternion.Euler(0f, 0f, heading);
        }

        private void StepPhysics(float dt)
        {
            // Turn body
            heading += Mathf.Clamp(turnCommand, -1f, 1f) * turnSpeed * dt;
            heading = ((heading % 360f) + 360f) % 360f;

            // Rotate turret toward target
            float turretDiff = Mathf.DeltaAngle(turretAngle, turretTarget);
            turretAngle += Mathf.Clamp(turretDiff, -turretSpeed * dt, turretSpeed * dt);

            // Drive
            float speed = Mathf.Clamp(driveCommand, -1f, 1f) * maxSpeed;
            posX += Forward.x * speed * dt;
            posY += Forward.y * speed * dt;

            // Clamp to arena bounds
            if (_arena != null)
            {
                float halfW = _arena.Width / 2f - 0.5f;
                float halfH = _arena.Height / 2f - 0.5f;
                posX = Mathf.Clamp(posX, -halfW, halfW);
                posY = Mathf.Clamp(posY, -halfH, halfH);
            }

            // Obstacle collision — eject tank from any overlapping obstacle
            if (_obstacles != null)
            {
                const float tankRadius = 0.45f;
                foreach (var obs in _obstacles)
                {
                    if (obs.Overlaps(posX, posY, tankRadius))
                    {
                        var ejected = obs.Eject(posX, posY, tankRadius);
                        posX = ejected.x;
                        posY = ejected.y;
                    }
                }
            }
        }

        public bool TryFire()
        {
            if (!IsAlive || ammo <= 0 || _cooldownRemaining > 0f) return false;
            ammo--;
            _cooldownRemaining = fireCooldown;
            fireRequested = false;
            OnFired?.Invoke(this);
            return true;
        }

        public void TakeDamage(int damage)
        {
            if (!IsAlive) return;
            hp -= damage;
            if (hp <= 0)
            {
                hp = 0;
                IsAlive = false;
                OnDestroyed?.Invoke(this);
            }
        }

        /// <summary>Clear per-tick commands. Called by TankProgram before each script execution.</summary>
        public void ClearCommands()
        {
            driveCommand = 0f;
            turnCommand = 0f;
            fireRequested = false;
            // turretTarget persists — it's a "hold this angle" command
        }

        /// <summary>Reset tank to spawn state for a new round.</summary>
        public void Reset(float startX, float startY, float startHeading)
        {
            posX = startX;
            posY = startY;
            heading = startHeading;
            turretAngle = 0f;
            turretTarget = 0f;
            hp = maxHP;
            ammo = maxAmmo;
            IsAlive = true;
            _cooldownRemaining = 0f;
            ClearCommands();

            transform.position = new Vector3(posX, posY, 0f);
            transform.rotation = Quaternion.Euler(0f, 0f, heading);
        }
    }
}
