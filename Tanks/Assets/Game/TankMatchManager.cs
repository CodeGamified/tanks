// Copyright CodeGamified 2025-2026
// MIT License — Tanks: Code Your Tank
using System.Collections.Generic;
using UnityEngine;
using CodeGamified.Time;

namespace Tanks.Game
{
    /// <summary>
    /// Match manager — last tank standing wins, or tie if all alive tanks are out of ammo
    /// and all projectiles have expired.
    ///
    /// Mirrors PongMatchManager: event-driven, time-scale aware, supports auto-restart.
    /// </summary>
    public class TankMatchManager : MonoBehaviour
    {
        private TankArena _arena;
        private readonly List<TankBody> _tanks = new List<TankBody>();
        private readonly List<TankProjectile> _activeProjectiles = new List<TankProjectile>();

        private bool _autoRestart;
        private float _restartDelay;

        public bool MatchInProgress { get; private set; }
        public int MatchesPlayed { get; private set; }
        public int PlayerWins { get; private set; }
        public int AIWins { get; private set; }
        public int Draws { get; private set; }

        // Events
        public System.Action OnMatchStarted;
        public System.Action<TankBody> OnTankDestroyed;        // which tank died
        public System.Action<TankBody> OnMatchWon;             // winner (null = draw)
        public System.Action OnDraw;
        public System.Action<TankProjectile> OnProjectileSpawned;

        public IReadOnlyList<TankBody> Tanks => _tanks;

        public void Initialize(TankArena arena, bool autoRestart, float restartDelay)
        {
            _arena = arena;
            _autoRestart = autoRestart;
            _restartDelay = restartDelay;
        }

        public void RegisterTank(TankBody tank)
        {
            _tanks.Add(tank);
            tank.OnDestroyed += HandleTankDestroyed;
            tank.OnFired += HandleTankFired;
        }

        public void StartMatch()
        {
            MatchInProgress = true;
            _activeProjectiles.Clear();
            OnMatchStarted?.Invoke();
        }

        private void HandleTankDestroyed(TankBody tank)
        {
            if (!MatchInProgress) return;
            OnTankDestroyed?.Invoke(tank);
            CheckWinCondition();
        }

        private void HandleTankFired(TankBody tank)
        {
            if (!MatchInProgress) return;

            // Spawn projectile
            var go = new GameObject($"Projectile_{tank.TankIndex}");
            var proj = go.AddComponent<TankProjectile>();

            Vector2 spawnPos = new Vector2(tank.posX, tank.posY) + tank.TurretForward * 0.6f;
            proj.Initialize(tank, _arena, spawnPos, tank.TurretForward,
                           tank.projectileSpeed, 2, 1);

            // Procedural visual — small sphere
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.SetParent(go.transform, false);
            sphere.transform.localScale = Vector3.one * 0.2f;
            var col = sphere.GetComponent<Collider>();
            if (col != null) Destroy(col);

            _activeProjectiles.Add(proj);
            proj.OnExpired += p => _activeProjectiles.Remove(p);
            proj.OnHitTank += (p, _) => _activeProjectiles.Remove(p);

            OnProjectileSpawned?.Invoke(proj);
        }

        private void Update()
        {
            if (!MatchInProgress) return;
            CheckTieCondition();
        }

        private void CheckWinCondition()
        {
            int aliveCount = 0;
            TankBody lastAlive = null;

            foreach (var tank in _tanks)
            {
                if (tank.IsAlive)
                {
                    aliveCount++;
                    lastAlive = tank;
                }
            }

            if (aliveCount <= 1)
            {
                if (aliveCount == 1)
                    EndMatch(lastAlive);
                else
                    EndMatch(null); // All dead simultaneously = draw
            }
        }

        private void CheckTieCondition()
        {
            // Tie: all alive tanks out of ammo AND no projectiles in flight
            if (_activeProjectiles.Count > 0) return;

            bool anyAliveHasAmmo = false;
            int aliveCount = 0;

            foreach (var tank in _tanks)
            {
                if (!tank.IsAlive) continue;
                aliveCount++;
                if (tank.ammo > 0) anyAliveHasAmmo = true;
            }

            // Need at least 2 alive tanks for a tie (otherwise win/loss already triggered)
            if (aliveCount >= 2 && !anyAliveHasAmmo)
            {
                EndMatch(null); // Draw
            }
        }

        private void EndMatch(TankBody winner)
        {
            MatchInProgress = false;
            MatchesPlayed++;

            if (winner != null)
            {
                if (winner.Team == TankTeam.Player) PlayerWins++;
                else AIWins++;
                OnMatchWon?.Invoke(winner);
                Debug.Log($"[TANKS] Match over — {winner.Team} Tank #{winner.TankIndex} wins!");
            }
            else
            {
                Draws++;
                OnDraw?.Invoke();
                Debug.Log("[TANKS] Match over — DRAW!");
            }

            if (_autoRestart)
                StartCoroutine(RestartCoroutine());
        }

        private System.Collections.IEnumerator RestartCoroutine()
        {
            float waited = 0f;
            while (waited < _restartDelay)
            {
                if (SimulationTime.Instance != null && !SimulationTime.Instance.isPaused)
                    waited += Time.deltaTime * (SimulationTime.Instance?.timeScale ?? 1f);
                yield return null;
            }

            // Destroy all remaining projectiles
            foreach (var proj in _activeProjectiles)
            {
                if (proj != null)
                    Destroy(proj.gameObject);
            }
            _activeProjectiles.Clear();

            // Reset tanks to spawn positions
            float spawnOffset = _arena != null ? _arena.Width / 4f : 5f;
            foreach (var tank in _tanks)
            {
                if (tank == null) continue;
                float x = tank.Team == TankTeam.Player ? -spawnOffset : spawnOffset;
                float heading = tank.Team == TankTeam.Player ? 0f : 180f;
                tank.Reset(x, 0f, heading);
            }

            StartMatch();
        }

        private void OnDestroy()
        {
            foreach (var tank in _tanks)
            {
                if (tank != null)
                {
                    tank.OnDestroyed -= HandleTankDestroyed;
                    tank.OnFired -= HandleTankFired;
                }
            }
        }
    }
}
