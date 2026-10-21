using UnityEngine;
using PLAYERTWO.PlatformerProject;

namespace AdventureMultiplayer
{
    /// <summary>
    /// One tidy "what is in front of me" reading, replacing the scattered ad-hoc raycasts that
    /// each obstacle mode used to do for itself.
    /// </summary>
    public struct BotObstacleReading
    {
        public BotObstacleType Type;
        public Collider        Collider;
        /// <summary>Distance from the bot to the hit, along the scan direction.</summary>
        public float           Distance;
        public Vector3         Point;
        /// <summary>The hint on the hit object, when it has one (this is where bridge markers live).</summary>
        public BotObstacleHint Hint;

        public bool   Valid => Type != BotObstacleType.None && Collider != null;
        public string Name  => Collider != null ? Collider.name : "-";
    }

    /// <summary>
    /// Looks ahead of the bot and reports the nearest thing worth reacting to, already
    /// classified by <see cref="BotObstacleClassifier"/>.
    ///
    /// Deliberately a plain class rather than a MonoBehaviour: the project forbids
    /// AddComponent, and nothing here needs Inspector wiring or its own Update.
    ///
    /// Read-only with respect to the world — it casts and reports, it never steers. That is
    /// what makes it safe to run alongside the existing movement code while the new obstacle
    /// system is being validated.
    /// </summary>
    public sealed class BotObstacleSensor
    {
        private const float k_castRadius = 0.35f;
        private const int   k_maxHits    = 16;

        private readonly RaycastHit[] m_hits = new RaycastHit[k_maxHits];

        private readonly Transform m_self;
        private readonly Player    m_player;
        private readonly LayerMask m_layer;

        public BotObstacleSensor(Transform self, Player player, LayerMask layer)
        {
            m_self   = self;
            m_player = player;
            m_layer  = layer;
        }

        /// <summary>
        /// Sweep <paramref name="distance"/> metres along <paramref name="dir"/> and return the
        /// nearest classified obstacle, or an invalid reading when the way is clear.
        ///
        /// A SphereCast rather than a ray: the thin rails, ropes and spinning arms in these
        /// levels routinely slip between three thin rays, which is how a swinging hammer could
        /// read as "nothing there" right up until it connected.
        /// </summary>
        public BotObstacleReading Scan(Vector3 dir, float distance)
        {
            var result = new BotObstacleReading { Type = BotObstacleType.None };

            if (m_self == null) return result;
            dir = Vector3.ProjectOnPlane(dir, Vector3.up);
            if (dir.sqrMagnitude < 0.0001f) return result;
            dir.Normalize();

            // Chest height: low enough to catch a plank edge, high enough to clear the ground
            // the bot is standing on.
            Vector3 origin = m_self.position + Vector3.up * 0.6f;

            int count = Physics.SphereCastNonAlloc(origin, k_castRadius, dir, m_hits, distance,
                                                   m_layer, QueryTriggerInteraction.Ignore);
            if (count <= 0) return result;

            float best = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                var hit = m_hits[i];
                if (hit.collider == null) continue;

                // Our own colliders. SphereCastNonAlloc reports a zero distance for anything
                // already overlapping the start of the sweep, so this also filters the ground
                // the bot is currently standing on.
                if (hit.collider.transform.IsChildOf(m_self)) continue;

                var type = BotObstacleClassifier.Classify(hit.collider, m_player);
                if (type == BotObstacleType.None) continue;

                // Flat walkable ground is not an obstacle — only count a Wall when it is
                // actually steep enough to block. Everything else (platforms, hazards, bridges)
                // matters regardless of its surface angle.
                if (type == BotObstacleType.Wall && hit.normal.y > 0.7f) continue;

                if (hit.distance >= best) continue;

                best = hit.distance;
                result.Type     = type;
                result.Collider = hit.collider;
                result.Distance = hit.distance;
                result.Point    = hit.point;
                result.Hint     = BotObstacleClassifier.FindHint(hit.collider);
            }

            return result;
        }
    }
}
