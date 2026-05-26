using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using PLAYERTWO.PlatformerProject;

namespace AdventureMultiplayer
{
    /// <summary>
    /// ML-Agents agent that drives a PLAYER TWO bot via AIPlayerInputManager.
    ///
    /// Observation space  : 37 floats  (see CollectObservations)
    /// Continuous actions : 2          (moveX, moveZ  — local space, clamped to unit circle)
    /// Discrete branches  : 6          (jump, spin, dash, glide, stomp, run — each 0/1)
    ///
    /// Setup checklist
    ///   1. Add this component to the bot GameObject (replaces AIBotBrain — do not run both).
    ///   2. Attach a Decision Requester component (Decision Period = 5 recommended).
    ///   3. Add a Behaviour Parameters component:
    ///        - Behaviour Name : AIBot
    ///        - Vector Obs     : 37
    ///        - Continuous     : 2
    ///        - Discrete       : 6  (each branch size = 2)
    ///   4. Optionally add a RayPerceptionSensor3D for wall/floor awareness.
    ///   5. Assign spawnPoint in the Inspector (or leave empty to use start position).
    ///   6. Tag collectibles as "Collectible", hazards as "Hazard", enemies as "Enemy".
    /// </summary>
    [RequireComponent(typeof(AIPlayerInputManager))]
    [AddComponentMenu("Adventure Multiplayer/AI Bot Agent")]
    public class AIBotAgent : Agent
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Spawn")]
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private float     fallKillY = -20f;

        [Header("Observation")]
        [SerializeField] private float observeRadius       = 35f;
        [SerializeField] private int   nearbyCollectibles  = 5;

        [Header("Rewards")]
        [SerializeField] private float rewardStar      = 1.00f;
        [SerializeField] private float rewardHeart     = 0.80f;
        [SerializeField] private float rewardLife      = 0.70f;
        [SerializeField] private float rewardRedCoin   = 0.30f;
        [SerializeField] private float rewardBlueCoin  = 0.25f;
        [SerializeField] private float rewardItemBox   = 0.20f;
        [SerializeField] private float rewardCoin      = 0.10f;
        [SerializeField] private float penaltyDeath    = -1.00f;
        [SerializeField] private float penaltyDamage   = -0.20f;
        [SerializeField] private float penaltyPerStep  = -0.0005f;

        // ── Private references ────────────────────────────────────────────────

        private AIPlayerInputManager m_input;
        private Player               m_player;

        private Vector3    m_spawnPos;
        private Quaternion m_spawnRot;

        private Collectible[] m_allCollectibles;
        private int           m_lastHealth;

        // ─────────────────────────────────────────────────────────────────────
        // Agent lifecycle
        // ─────────────────────────────────────────────────────────────────────

        public override void Initialize()
        {
            m_input  = GetComponent<AIPlayerInputManager>();
            m_player = GetComponent<Player>();

            m_spawnPos = spawnPoint != null ? spawnPoint.position : transform.position;
            m_spawnRot = spawnPoint != null ? spawnPoint.rotation : transform.rotation;

            CacheCollectibles();
        }

        public override void OnEpisodeBegin()
        {
            // Teleport back to spawn
            transform.SetPositionAndRotation(m_spawnPos, m_spawnRot);

            // Zero out physics
            m_player.velocity        = Vector3.zero;
            m_player.lateralVelocity = Vector3.zero;
            m_player.verticalVelocity = Vector3.zero;

            // Clear all inputs
            ResetInputs();

            // Refresh collectible cache and re-register callbacks
            CacheCollectibles();

            m_lastHealth = m_player.health.current;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Observations  —  37 floats total
        // ─────────────────────────────────────────────────────────────────────
        //
        //  Self state         (8)
        //  Nearest collectibles  5 × (dir.xyz + dist + typeValue) = 25
        //  Nearest hazard     (4)
        //

        public override void CollectObservations(VectorSensor sensor)
        {
            // ── Self state (8) ────────────────────────────────────────────────
            Vector3 localVel = transform.InverseTransformDirection(m_player.velocity);
            sensor.AddObservation(localVel / 20f);                                         // 3
            sensor.AddObservation(m_player.isGrounded ? 1f : 0f);                          // 1
            sensor.AddObservation(m_player.verticalVelocity.y / 20f);                      // 1
            sensor.AddObservation(m_player.holding ? 1f : 0f);                             // 1
            sensor.AddObservation((float)m_player.health.current
                                  / Mathf.Max(m_player.health.max, 1));                    // 1
            sensor.AddObservation(m_input.glideHeld ? 1f : 0f);                            // 1

            // ── Nearest collectibles (5 × 5 = 25) ────────────────────────────
            var nearest = GetNearestCollectibles(nearbyCollectibles);
            for (int i = 0; i < nearbyCollectibles; i++)
            {
                if (i < nearest.Count)
                {
                    var c   = nearest[i];
                    var diff = c.transform.position - transform.position;
                    var dir  = transform.InverseTransformDirection(diff.normalized);
                    float dist = diff.magnitude;

                    sensor.AddObservation(dir);                                             // 3
                    sensor.AddObservation(Mathf.Clamp01(dist / observeRadius));             // 1
                    sensor.AddObservation(GetCollectibleValue(c) / 100f);                   // 1
                }
                else
                {
                    sensor.AddObservation(Vector3.zero);                                    // 3
                    sensor.AddObservation(1f);                                              // 1
                    sensor.AddObservation(0f);                                              // 1
                }
            }

            // ── Nearest hazard (4) ────────────────────────────────────────────
            Vector3? hazardPos = FindNearestTaggedPosition("Hazard");
            if (hazardPos.HasValue)
            {
                var diff = hazardPos.Value - transform.position;
                sensor.AddObservation(transform.InverseTransformDirection(diff.normalized)); // 3
                sensor.AddObservation(Mathf.Clamp01(diff.magnitude / observeRadius));        // 1
            }
            else
            {
                sensor.AddObservation(Vector3.zero);                                        // 3
                sensor.AddObservation(1f);                                                  // 1
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Actions
        // ─────────────────────────────────────────────────────────────────────
        //
        //  Continuous[0]  moveX  (−1 … +1, local space)
        //  Continuous[1]  moveZ  (−1 … +1, local space)
        //  Discrete[0]    jump   (0=off, 1=on)
        //  Discrete[1]    spin   (0=off, 1=on)
        //  Discrete[2]    dash   (0=off, 1=on)
        //  Discrete[3]    glide  (0=off, 1=on)
        //  Discrete[4]    stomp  (0=off, 1=on)
        //  Discrete[5]    run    (0=off, 1=on)
        //

        public override void OnActionReceived(ActionBuffers actions)
        {
            // Movement — keep within unit circle, convert to world space
            float moveX = Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f);
            float moveZ = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);
            var localDir = new Vector3(moveX, 0f, moveZ);
            if (localDir.sqrMagnitude > 1f) localDir.Normalize();
            m_input.desiredMoveDirection = transform.TransformDirection(localDir);

            // Discrete abilities — queued inputs auto-clear after one frame in AIPlayerInputManager
            if (actions.DiscreteActions[0] == 1) m_input.jumpQueued  = true;
            if (actions.DiscreteActions[1] == 1) m_input.spinQueued  = true;
            if (actions.DiscreteActions[2] == 1) m_input.dashQueued  = true;
            m_input.glideHeld = actions.DiscreteActions[3] == 1;
            if (actions.DiscreteActions[4] == 1) m_input.stompQueued = true;
            m_input.runHeld   = actions.DiscreteActions[5] == 1;

            // Step penalty — encourages the agent to collect faster
            AddReward(penaltyPerStep);

            // Damage detection
            int hp = m_player.health.current;
            if (hp < m_lastHealth)
                AddReward(penaltyDamage * (m_lastHealth - hp));
            m_lastHealth = hp;

            // Death or out-of-bounds fall → end episode
            if (!m_player.isAlive || transform.position.y < fallKillY)
            {
                AddReward(penaltyDeath);
                EndEpisode();
            }
        }

        // Blank heuristic — bot is always neural-net or self-play driven
        public override void Heuristic(in ActionBuffers actionsOut) { }

        // ─────────────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Cache all collectibles and register collection callbacks.</summary>
        private void CacheCollectibles()
        {
            m_allCollectibles = FindObjectsByType<Collectible>(FindObjectsSortMode.None);

            // Re-register onCollect listeners so rewards fire for this agent instance
            foreach (var c in m_allCollectibles)
            {
                if (c == null) continue;
                c.onCollect.RemoveAllListeners();
                var captured = c;
                c.onCollect.AddListener(collector =>
                {
                    if (collector.gameObject == gameObject)
                        AddReward(GetCollectibleValue(captured) / 100f);
                });
            }
        }

        private List<Collectible> GetNearestCollectibles(int count)
        {
            var candidates = new List<(Collectible c, float dist)>();
            foreach (var c in m_allCollectibles)
            {
                if (c == null || !c.isVisible || !c.canCollect) continue;
                float d = Vector3.Distance(transform.position, c.transform.position);
                if (d <= observeRadius)
                    candidates.Add((c, d));
            }
            candidates.Sort((a, b) => a.dist.CompareTo(b.dist));

            var result = new List<Collectible>(count);
            for (int i = 0; i < Mathf.Min(count, candidates.Count); i++)
                result.Add(candidates[i].c);
            return result;
        }

        private float GetCollectibleValue(Collectible c)
        {
            string n = c.gameObject.name.ToLower();
            if (n.Contains("star"))                    return rewardStar     * 100f;
            if (n.Contains("heart"))                   return rewardHeart    * 100f;
            if (n.Contains("life"))                    return rewardLife     * 100f;
            if (n.Contains("red"))                     return rewardRedCoin  * 100f;
            if (n.Contains("blue"))                    return rewardBlueCoin * 100f;
            if (n.Contains("item") || n.Contains("box")) return rewardItemBox * 100f;
            return rewardCoin * 100f;
        }

        private Vector3? FindNearestTaggedPosition(string tag)
        {
            var objects = GameObject.FindGameObjectsWithTag(tag);
            Vector3? nearest = null;
            float minDist = float.MaxValue;
            foreach (var go in objects)
            {
                float d = Vector3.Distance(transform.position, go.transform.position);
                if (d < minDist && d <= observeRadius)
                {
                    minDist = d;
                    nearest = go.transform.position;
                }
            }
            return nearest;
        }

        private void ResetInputs()
        {
            m_input.desiredMoveDirection = Vector3.zero;
            m_input.jumpQueued           = false;
            m_input.spinQueued           = false;
            m_input.dashQueued           = false;
            m_input.glideHeld            = false;
            m_input.stompQueued          = false;
            m_input.airDiveQueued        = false;
            m_input.rollHeld             = false;
            m_input.crouchHeld           = false;
            m_input.runHeld              = false;
            m_input.diveHeld             = false;
            m_input.swimUpwardHeld       = false;
        }
    }
}
