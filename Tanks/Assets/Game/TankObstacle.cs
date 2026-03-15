// Copyright CodeGamified 2025-2026
// MIT License — Tanks: Code Your Tank
using System.Collections.Generic;
using UnityEngine;

namespace Tanks.Game
{
    /// <summary>
    /// Axis-aligned rectangular obstacle in the arena.
    /// Blocks tank movement and projectile travel.
    /// Built procedurally by TankBootstrap — no prefabs.
    /// </summary>
    public class TankObstacle : MonoBehaviour
    {
        public float MinX { get; private set; }
        public float MaxX { get; private set; }
        public float MinY { get; private set; }
        public float MaxY { get; private set; }
        public float CenterX => (MinX + MaxX) / 2f;
        public float CenterY => (MinY + MaxY) / 2f;
        public float HalfW => (MaxX - MinX) / 2f;
        public float HalfH => (MaxY - MinY) / 2f;

        public void Initialize(float cx, float cy, float halfW, float halfH)
        {
            MinX = cx - halfW;
            MaxX = cx + halfW;
            MinY = cy - halfH;
            MaxY = cy + halfH;
            transform.position = new Vector3(cx, cy, 0f);
        }

        /// <summary>Does a circle at (px,py) with given radius overlap this obstacle?</summary>
        public bool Overlaps(float px, float py, float radius)
        {
            float closestX = Mathf.Clamp(px, MinX, MaxX);
            float closestY = Mathf.Clamp(py, MinY, MaxY);
            float dx = px - closestX;
            float dy = py - closestY;
            return (dx * dx + dy * dy) <= radius * radius;
        }

        /// <summary>Does the line segment from a to b intersect this AABB?</summary>
        public bool IntersectsSegment(Vector2 a, Vector2 b)
        {
            // Liang-Barsky clipping
            float dx = b.x - a.x;
            float dy = b.y - a.y;
            float tMin = 0f;
            float tMax = 1f;

            float[] p = { -dx, dx, -dy, dy };
            float[] q = { a.x - MinX, MaxX - a.x, a.y - MinY, MaxY - a.y };

            for (int i = 0; i < 4; i++)
            {
                if (Mathf.Abs(p[i]) < 1e-8f)
                {
                    if (q[i] < 0f) return false;
                }
                else
                {
                    float t = q[i] / p[i];
                    if (p[i] < 0f)
                        tMin = Mathf.Max(tMin, t);
                    else
                        tMax = Mathf.Min(tMax, t);
                    if (tMin > tMax) return false;
                }
            }
            return true;
        }

        /// <summary>Push a circle out of this obstacle, returning corrected position.</summary>
        public Vector2 Eject(float px, float py, float radius)
        {
            float closestX = Mathf.Clamp(px, MinX, MaxX);
            float closestY = Mathf.Clamp(py, MinY, MaxY);
            float dx = px - closestX;
            float dy = py - closestY;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);

            if (dist < 1e-6f)
            {
                // Center inside obstacle — push out shortest axis
                float pushLeft  = px - MinX;
                float pushRight = MaxX - px;
                float pushDown  = py - MinY;
                float pushUp    = MaxY - py;
                float min = Mathf.Min(pushLeft, Mathf.Min(pushRight, Mathf.Min(pushDown, pushUp)));

                if (min == pushLeft)  return new Vector2(MinX - radius, py);
                if (min == pushRight) return new Vector2(MaxX + radius, py);
                if (min == pushDown)  return new Vector2(px, MinY - radius);
                return new Vector2(px, MaxY + radius);
            }

            float penetration = radius - dist;
            if (penetration <= 0f) return new Vector2(px, py);

            float nx = dx / dist;
            float ny = dy / dist;
            return new Vector2(px + nx * penetration, py + ny * penetration);
        }

        // ═══════════════════════════════════════════════════════════
        // STATIC: generate random obstacle layout
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Generate a symmetric obstacle layout.
        /// Keeps spawn zones clear. Returns obstacle definitions (cx, cy, halfW, halfH).
        /// </summary>
        public static List<Vector4> GenerateLayout(float arenaW, float arenaH,
                                                    int count, float clearRadius)
        {
            var obstacles = new List<Vector4>();
            float halfW = arenaW / 2f;
            float halfH = arenaH / 2f;

            // Always place center obstacle
            obstacles.Add(new Vector4(0f, 0f, 1.2f, 0.4f));

            // Symmetric pairs
            int pairs = (count - 1) / 2;
            int attempts = 0;
            int placed = 0;

            while (placed < pairs && attempts < 200)
            {
                attempts++;
                float cx = Random.Range(1.5f, halfW - 2f);
                float cy = Random.Range(-halfH + 2f, halfH - 2f);
                float hw = Random.Range(0.4f, 1.2f);
                float hh = Random.Range(0.3f, 0.8f);

                // Check spawn zone clearance (left spawn at -arenaW/4, right at +arenaW/4)
                bool tooCloseToSpawn =
                    (cx - hw < clearRadius && cx + hw > -clearRadius) ||
                    Vector2.Distance(new Vector2(cx, cy), new Vector2(halfW / 2f, 0f)) < clearRadius ||
                    Vector2.Distance(new Vector2(-cx, cy), new Vector2(-halfW / 2f, 0f)) < clearRadius;
                if (tooCloseToSpawn) continue;

                // Check overlap with existing
                bool overlaps = false;
                foreach (var o in obstacles)
                {
                    if (Mathf.Abs(cx - o.x) < hw + o.z + 0.5f &&
                        Mathf.Abs(cy - o.y) < hh + o.w + 0.5f)
                    { overlaps = true; break; }
                    if (Mathf.Abs(-cx - o.x) < hw + o.z + 0.5f &&
                        Mathf.Abs(cy - o.y) < hh + o.w + 0.5f)
                    { overlaps = true; break; }
                }
                if (overlaps) continue;

                obstacles.Add(new Vector4(cx, cy, hw, hh));
                obstacles.Add(new Vector4(-cx, cy, hw, hh));   // mirror
                placed++;
            }

            return obstacles;
        }
    }
}
