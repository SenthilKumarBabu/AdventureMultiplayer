using System.Collections.Generic;
using UnityEngine;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Designer-authored racing line for bots. Every child with a <see cref="BotWaypoint"/>
    /// is a point on the route, in hierarchy order. When a scene has one, RaceBotBrain follows
    /// it instead of guessing the track from raycasts — each waypoint's action tells the bot
    /// exactly where to jump, where to walk carefully and where to wait.
    /// </summary>
    [AddComponentMenu("Adventure Multiplayer/AI/Bot Route")]
    public class BotRoute : MonoBehaviour
    {
        private static readonly List<BotRoute> s_all = new();

        /// <summary>
        /// The route a given bot should follow. With several routes in the scene (BotRoute_A/B/C
        /// from the auto-generator) bots are spread across them so they don't all run one line.
        /// </summary>
        public static BotRoute ForBot(ulong botId)
        {
            s_all.RemoveAll(r => r == null || r.Count == 0);
            if (s_all.Count == 0) return null;
            s_all.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            return s_all[(int)(botId % (ulong)s_all.Count)];
        }
        private BotWaypoint[] m_points;

        public int Count => Points.Length;
        public BotWaypoint this[int i] => Points[i];

        public BotWaypoint[] Points
        {
            get
            {
                if (m_points == null || (!Application.isPlaying)) m_points = GetComponentsInChildren<BotWaypoint>();
                return m_points;
            }
        }

        private void OnEnable()
        {
            if (!s_all.Contains(this)) s_all.Add(this);
            m_points = null;
        }

        private void OnDisable()
        {
            s_all.Remove(this);
        }

        /// <summary>
        /// Index of the waypoint a bot at <paramref name="pos"/> should head for: the end of the
        /// route segment it is closest to. Used at race start and after every respawn.
        /// </summary>
        public int FindResumeIndex(Vector3 pos)
        {
            var pts = Points;
            if (pts.Length == 0) return 0;

            int   best     = 0;
            float bestDist = WeightedDist(pos, pts[0].transform.position);
            for (int i = 0; i + 1 < pts.Length; i++)
            {
                Vector3 a = pts[i].transform.position;
                Vector3 b = pts[i + 1].transform.position;
                Vector3 ab = b - a;
                float t = ab.sqrMagnitude > 0.001f ? Mathf.Clamp01(Vector3.Dot(pos - a, ab) / ab.sqrMagnitude) : 0f;
                float d = WeightedDist(pos, a + ab * t);
                if (d < bestDist) { bestDist = d; best = i + 1; }
            }
            return best;
        }

        // Height counts double: a route passing over/under the bot is not "where it is".
        private static float WeightedDist(Vector3 p, Vector3 q)
        {
            Vector3 d = p - q;
            return Mathf.Sqrt(d.x * d.x + d.z * d.z + 4f * d.y * d.y);
        }

        public static Color ActionColor(BotWaypoint.Action a) => a switch
        {
            BotWaypoint.Action.Careful    => new Color(1f, 0.85f, 0.1f),
            BotWaypoint.Action.Jump       => new Color(1f, 0.3f, 0.2f),
            BotWaypoint.Action.DoubleJump => new Color(1f, 0.2f, 0.8f),
            BotWaypoint.Action.Wait       => new Color(0.3f, 0.6f, 1f),
            _                             => new Color(0.2f, 1f, 0.4f),
        };

        private void OnDrawGizmos()
        {
            var pts = GetComponentsInChildren<BotWaypoint>();
            for (int i = 0; i < pts.Length; i++)
            {
                var wp = pts[i];
                Vector3 p = wp.transform.position;
                Gizmos.color = ActionColor(wp.action);
                Gizmos.DrawWireSphere(p, wp.radius);

                if (i + 1 < pts.Length)
                {
                    Vector3 q = pts[i + 1].transform.position;
                    Gizmos.DrawLine(p, q);
                    // Arrow head so the direction of travel is obvious.
                    Vector3 dir = (q - p).normalized;
                    if (dir.sqrMagnitude > 0.01f)
                    {
                        Vector3 mid = Vector3.Lerp(p, q, 0.5f);
                        Vector3 side = Vector3.Cross(Vector3.up, dir).normalized * 0.4f;
                        Gizmos.DrawLine(mid, mid - dir * 0.6f + side);
                        Gizmos.DrawLine(mid, mid - dir * 0.6f - side);
                    }

                    if (wp.action == BotWaypoint.Action.Wait)
                    {
                        Vector3 w = wp.watchPoint != null ? wp.watchPoint.position : Vector3.Lerp(p, q, 0.5f);
                        Gizmos.DrawWireSphere(w, wp.watchRadius);
                    }
                }
#if UNITY_EDITOR
                string tag = name.StartsWith("BotRoute_") ? name.Substring(9) : "";
                UnityEditor.Handles.Label(p + Vector3.up * 0.8f, $"{tag}{i} {wp.action}");
#endif
            }
        }
    }
}
