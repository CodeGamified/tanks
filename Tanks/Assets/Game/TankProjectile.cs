// Copyright CodeGamified 2025-2026
// MIT License — Tanks: Code Your Tank
using UnityEngine;
using CodeGamified.Time;

namespace Tanks.Game
{
    /// <summary>
    /// A projectile fired by a tank.
    /// Travels in a straight line, bounces off arena walls (like Pong ball),
    /// then damages any tank it hits. Uses LineRenderer for trajectory trace.
    ///
    /// Design mirrors PongBall: sub-stepping for high time scales,
    /// SimulationTime-aware, event-driven.
    /// </summary>
    public class TankProjectile : MonoBehaviour
    {
        // Config
        private float _speed;
        private float _radius = 0.15f;
        private int _maxBounces = 2;
        private int _damage = 1;
        private TankBody _owner;

        // State
        public Vector2 Position { get; private set; }
        public Vector2 Velocity { get; private set; }
        public bool IsActive { get; private set; }
        public TankBody Owner => _owner;

        private int _bouncesRemaining;
        private float _lifetime;
        private const float MAX_LIFETIME = 8f;

        // Trail — LineRenderer
        private LineRenderer _trail;
        private int _trailPointCount;
        private const int MAX_TRAIL_POINTS = 512;
        private const float TRAIL_MIN_DIST_SQ = 0.01f;

        // Arena reference for wall collision
        private TankArena _arena;
        private System.Collections.Generic.List<TankObstacle> _obstacles;

        // Events
        public System.Action<TankProjectile, TankBody> OnHitTank;  // (projectile, victim)
        public System.Action<TankProjectile> OnBounced;
        public System.Action<TankProjectile> OnExpired;

        public void Initialize(TankBody owner, TankArena arena,
                               Vector2 startPos, Vector2 direction, float speed,
                               int maxBounces, int damage)
        {
            _owner = owner;
            _arena = arena;
            _obstacles = arena?.Obstacles;
            _speed = speed;
            _maxBounces = maxBounces;
            _damage = damage;

            Position = startPos;
            Velocity = direction.normalized * _speed;
            IsActive = true;
            _bouncesRemaining = maxBounces;
            _lifetime = 0f;

            transform.position = new Vector3(startPos.x, startPos.y, 0f);

            SetupTrail();
        }

        private void SetupTrail()
        {
            _trail = GetComponent<LineRenderer>();
            if (_trail == null)
                _trail = gameObject.AddComponent<LineRenderer>();

            var shader = Shader.Find("Sprites/Default")
                ?? Shader.Find("Unlit/Color");
            _trail.material = new Material(shader);
            _trail.startColor = new Color(1f, 0.6f, 0.1f, 0.9f);
            _trail.endColor = new Color(1f, 0.3f, 0f, 0.3f);
            _trail.startWidth = 0.08f;
            _trail.endWidth = 0.03f;
            _trail.positionCount = 1;
            _trail.SetPosition(0, new Vector3(Position.x, Position.y, 0.01f));
            _trailPointCount = 1;
        }

        private void Update()
        {
            if (!IsActive) return;
            if (SimulationTime.Instance == null || SimulationTime.Instance.isPaused) return;

            float dt = Time.deltaTime * (SimulationTime.Instance?.timeScale ?? 1f);
            _lifetime += dt;

            if (_lifetime >= MAX_LIFETIME)
            {
                Expire();
                return;
            }

            // Sub-step
            int steps = Mathf.Max(1, Mathf.CeilToInt(dt / 0.002f));
            float subDt = dt / steps;

            for (int i = 0; i < steps && IsActive; i++)
                StepPhysics(subDt);

            if (IsActive)
            {
                transform.position = new Vector3(Position.x, Position.y, 0f);
                AppendTrailPoint();
            }
        }

        private void StepPhysics(float dt)
        {
            Position += Velocity * dt;

            if (_arena == null) return;
            float halfW = _arena.Width / 2f;
            float halfH = _arena.Height / 2f;

            // Wall bounce
            bool bounced = false;
            if (Position.x + _radius >= halfW)
            {
                Position = new Vector2(halfW - _radius, Position.y);
                Velocity = new Vector2(-Mathf.Abs(Velocity.x), Velocity.y);
                bounced = true;
            }
            else if (Position.x - _radius <= -halfW)
            {
                Position = new Vector2(-halfW + _radius, Position.y);
                Velocity = new Vector2(Mathf.Abs(Velocity.x), Velocity.y);
                bounced = true;
            }

            if (Position.y + _radius >= halfH)
            {
                Position = new Vector2(Position.x, halfH - _radius);
                Velocity = new Vector2(Velocity.x, -Mathf.Abs(Velocity.y));
                bounced = true;
            }
            else if (Position.y - _radius <= -halfH)
            {
                Position = new Vector2(Position.x, -halfH + _radius);
                Velocity = new Vector2(Velocity.x, Mathf.Abs(Velocity.y));
                bounced = true;
            }

            if (bounced)
            {
                _bouncesRemaining--;
                OnBounced?.Invoke(this);

                if (_bouncesRemaining < 0)
                {
                    Expire();
                    return;
                }
            }

            // Obstacle collision — projectile absorbed on hit
            if (_obstacles != null)
            {
                foreach (var obs in _obstacles)
                {
                    if (obs.Overlaps(Position.x, Position.y, _radius))
                    {
                        Expire();
                        return;
                    }
                }
            }

            // Tank collision — check all tanks in scene
            var tanks = FindObjectsByType<TankBody>(FindObjectsSortMode.None);
            foreach (var tank in tanks)
            {
                if (!tank.IsAlive) continue;
                if (tank == _owner) continue;
                float dx = Position.x - tank.posX;
                float dy = Position.y - tank.posY;
                float distSq = dx * dx + dy * dy;
                float hitRadius = _radius + 0.4f; // tank body radius

                if (distSq <= hitRadius * hitRadius)
                {
                    tank.TakeDamage(_damage);
                    OnHitTank?.Invoke(this, tank);
                    Expire();
                    return;
                }
            }
        }

        private void AppendTrailPoint()
        {
            if (_trail == null || _trailPointCount >= MAX_TRAIL_POINTS) return;

            Vector3 newPos = new Vector3(Position.x, Position.y, 0.01f);
            Vector3 lastPos = _trail.GetPosition(_trailPointCount - 1);

            if ((newPos - lastPos).sqrMagnitude < TRAIL_MIN_DIST_SQ) return;

            _trail.positionCount = _trailPointCount + 1;
            _trail.SetPosition(_trailPointCount, newPos);
            _trailPointCount++;
        }

        private void Expire()
        {
            IsActive = false;
            OnExpired?.Invoke(this);
            // Destroy after short delay to let trail linger
            Destroy(gameObject, 0.5f);
        }
    }
}
