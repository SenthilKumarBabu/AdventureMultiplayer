using System;
using UnityEngine;
using Unity.Netcode;
using PLAYERTWO.PlatformerProject;
using System.Collections.Generic;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Replaces ClientNetworkTransform with manual position + velocity + state sync.
    ///
    /// Owner:
    ///   Reads Player position, velocity, rotation and state every Update and pushes
    ///   them into a single NetworkVariable struct. Physics and input run normally.
    ///
    /// Non-owner (ghost):
    ///   Disables EntityController and PlayerInputManager so PLAYER TWO physics stops.
    ///   Uses dead reckoning: advances position using the last received velocity each
    ///   frame, then smoothly corrects toward the latest received position when a new
    ///   packet arrives. This means fast motion like stomp fall or dash extrapolates
    ///   correctly instead of lagging behind like a plain interpolating NetworkTransform.
    ///
    /// Remove ClientNetworkTransform from the player prefab and add this instead.
    /// </summary>
    [DefaultExecutionOrder(50)]
    [RequireComponent(typeof(NetworkObject))]
    [AddComponentMenu("Adventure Multiplayer/Networked Movement Sync")]
    public class NetworkedMovementSync : NetworkBehaviour
    {
        [Header("Ghost Correction")]
        [SerializeField] private float positionCorrectionSpeed = 12f;
        [SerializeField] private float rotationCorrectionSpeed = 20f;
        [SerializeField] private float snapDistance            = 3f;

        // ── Events for AnimatorSync and ParticleSync to subscribe ─────────────

        /// <summary>Fires on non-owners when the player state index changes.</summary>
        public event Action<int, int> OnRemoteStateChanged;  // (oldIndex, newIndex)

        /// <summary>Fires on non-owners when the player lands. Carries world position and fall speed.</summary>
        public event Action<Vector3, float> OnRemoteLanded;

        /// <summary>Latest synced state — readable by AnimatorSync etc.</summary>
        public PlayerNetworkState SyncedState => m_netState.Value;

        // ── NetworkVariable ───────────────────────────────────────────────────

        private readonly NetworkVariable<PlayerNetworkState> m_netState = new(
            new PlayerNetworkState(),
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner);

        // ── Internals ─────────────────────────────────────────────────────────

        private Player   m_player;
        private Rigidbody m_rigidbody;

        // Dead reckoning state
        private Vector3    m_extrapolatedPosition;
        private Vector3    m_deadReckonVelocity;
        private Quaternion m_targetRotation;

        private int  m_lastStateIndex = -1;
        private bool m_wasGrounded;

        private bool m_loggedFirstPush;

        // States where dead reckoning must be suppressed — player is constrained to a surface.
        private static readonly HashSet<System.Type> k_surfaceStates = new()
        {
            typeof(WallDragPlayerState),
            typeof(LedgeHangingPlayerState),
            typeof(LedgeClimbingPlayerState),
            typeof(PoleClimbingPlayerState),
        };

        // ── Lifecycle ─────────────────────────────────────────────────────────

        public override void OnNetworkSpawn()
        {
            m_player    = GetComponent<Player>();
            m_rigidbody = GetComponentInChildren<Rigidbody>();

            if (IsOwner)
            {
                // Ensure EC and PIM are enabled — the ghost path (IsOwner=false) may have
                // run first on this same object if NGO assigned ownership after OnNetworkSpawn.
                if (m_player != null) m_player.enabled = true;
                var ec = GetComponent<EntityController>();
                var im = GetComponent<PlayerInputManager>();
                if (ec != null) ec.enabled = true;
                if (im != null) im.enabled = true;

                // Lock input until the race countdown fires Go (0).
                // If the race is already started (e.g. late-join), skip the lock.
                bool raceAlreadyStarted = RaceManager.Instance != null &&
                                          RaceManager.Instance.RaceStarted.Value;
                if (!raceAlreadyStarted && im != null)
                {
                    im.enabled = false;
                    Debug.Log("[NetworkedMovementSync] Input locked — waiting for countdown.");
                    RaceCountdown.OnRaceStart += OnRaceStart;

                    void OnRaceStart()
                    {
                        RaceCountdown.OnRaceStart -= OnRaceStart;
                        if (im != null) im.enabled = true;
                        Debug.Log("[NetworkedMovementSync] Input unlocked — GO!");
                    }
                }

                Debug.Log($"[NetworkedMovementSync] OWNER spawned: " +
                          $"localClientId={NetworkManager.Singleton.LocalClientId} ownerClientId={OwnerClientId} " +
                          $"IsServer={IsServer} IsHost={IsHost} | " +
                          $"Player.enabled={m_player?.enabled} " +
                          $"EntityController.enabled={ec?.enabled} " +
                          $"PlayerInputManager.enabled={im?.enabled} " +
                          $"inputLocked={!raceAlreadyStarted}");
                PushState();
            }
            else
            {
                Debug.Log($"[NetworkedMovementSync] GHOST spawned: " +
                          $"localClientId={NetworkManager.Singleton.LocalClientId} ownerClientId={OwnerClientId}");

                // Kinematic rigidbody so Unity physics won't fight our manual position.
                if (m_rigidbody != null)
                {
                    m_rigidbody.isKinematic = true;
                    m_rigidbody.useGravity  = false;
                }

                // Stop PLAYER TWO physics and input on the ghost.
                // Disable Player (Entity) first — Entity.Update applies gravity via
                // transform.position += velocity*dt when EntityController is disabled,
                // so we must stop Entity from ticking entirely on remote ghosts.
                if (m_player != null) m_player.enabled = false;

                var ec = GetComponent<EntityController>();
                if (ec != null) ec.enabled = false;

                // DO NOT disable or destroy PlayerInputManager on the ghost.
                // Both im.enabled=false and Destroy(im) trigger OnDisable() which calls
                // actions.Disable() on the shared InputActionAsset — killing input for
                // the local owner's player too.
                // It's safe to leave it alive: m_player.enabled=false stops PLAYER TWO
                // from ever calling GetMovementDirection() on the ghost's PIM.

                Debug.Log($"[NetworkedMovementSync] Ghost setup done: " +
                          $"Player.enabled={m_player?.enabled} " +
                          $"EntityController.enabled={ec?.enabled} " +
                          $"PlayerInputManager=left alive (safe, Player is disabled)");

                m_extrapolatedPosition = transform.position;
                m_targetRotation       = transform.rotation;

                m_netState.OnValueChanged += OnNetStateChanged;
            }
        }

        private bool m_loggedUpdatePath;

        private void Update()
        {
            if (!IsSpawned) return;

            if (!m_loggedUpdatePath)
            {
                m_loggedUpdatePath = true;
                Debug.Log($"[MovementSync] Update path: IsOwner={IsOwner} ownerClientId={OwnerClientId} " +
                          $"localClientId={NetworkManager.Singleton.LocalClientId} pos={transform.position}");
            }

            if (IsOwner)
                PushState();
            else
                StepGhost();
        }

        public override void OnNetworkDespawn()
        {
            m_netState.OnValueChanged -= OnNetStateChanged;
        }

        // ── Owner ─────────────────────────────────────────────────────────────

        private void PushState()
        {
            if (m_player == null) return;

            if (!m_loggedFirstPush)
            {
                m_loggedFirstPush = true;
                var ec = GetComponent<EntityController>();
                var im = GetComponent<PlayerInputManager>();
                Debug.Log($"[NetworkedMovementSync] PushState first tick (clientId={OwnerClientId}): " +
                          $"Player.enabled={m_player.enabled} " +
                          $"EntityController.enabled={ec?.enabled} " +
                          $"PlayerInputManager.enabled={im?.enabled} " +
                          $"pos={transform.position} vel={m_player.velocity}");
            }

            m_netState.Value = new PlayerNetworkState
            {
                Position    = transform.position,
                Velocity    = m_player.velocity,
                Rotation    = transform.rotation,
                StateIndex  = m_player.states.index,
                IsGrounded  = m_player.isGrounded,
                JumpCounter = m_player.jumpCounter,
                IsHolding   = m_player.holding,
                IsOnSurface = m_player.states.current != null &&
                              k_surfaceStates.Contains(m_player.states.current.GetType()),
            };

            if (m_netState.Value.IsOnSurface)
                Debug.Log($"[MovementSync] Owner surface state={m_player.states.current?.GetType().Name} pos={transform.position}");
        }

        // ── Ghost dead reckoning ──────────────────────────────────────────────

        private void StepGhost()
        {
            // Advance extrapolated position each frame using the last known velocity.
            // This means a stomping ghost falls at the right speed between packets
            // rather than being linearly interpolated from the last two positions.
            m_extrapolatedPosition += m_deadReckonVelocity * Time.deltaTime;

            float dist = Vector3.Distance(transform.position, m_extrapolatedPosition);

            // On wall/ledge surfaces dead reckoning is zeroed, so snap directly —
            // no lerp, no drift into the air.
            bool onSurface = m_netState.Value.IsOnSurface;
            if (dist > snapDistance || onSurface)
                transform.position = m_extrapolatedPosition;
            else
                transform.position = Vector3.Lerp(
                    transform.position,
                    m_extrapolatedPosition,
                    positionCorrectionSpeed * Time.deltaTime);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                m_targetRotation,
                rotationCorrectionSpeed * Time.deltaTime);
        }

        private void OnNetStateChanged(PlayerNetworkState old, PlayerNetworkState next)
        {
            // Reset dead reckoning from the freshly received position.
            m_extrapolatedPosition = next.Position;
            m_deadReckonVelocity   = next.Velocity;

            // When the owner is grounded, suppress vertical dead reckoning.
            // Otherwise a landing packet with residual downward velocity extrapolates
            // the ghost into the floor before the next correction arrives.
            if (next.IsGrounded)
                m_deadReckonVelocity.y = 0f;

            // When on a wall/ledge surface, kill all dead reckoning — the player is
            // constrained to a surface the ghost doesn't know about. Snapping directly
            // to the authoritative position prevents the ghost from drifting into the air.
            if (next.IsOnSurface)
            {
                m_deadReckonVelocity = Vector3.zero;
                Debug.Log($"[MovementSync] Ghost surface snap pos={next.Position} stateIdx={next.StateIndex}");
            }

            m_targetRotation = next.Rotation;

            // Notify listeners of state transitions.
            if (next.StateIndex != m_lastStateIndex)
            {
                OnRemoteStateChanged?.Invoke(m_lastStateIndex, next.StateIndex);
                m_lastStateIndex = next.StateIndex;
            }

            // Landing: was airborne last tick, now grounded.
            if (!m_wasGrounded && next.IsGrounded)
                OnRemoteLanded?.Invoke(next.Position, Mathf.Abs(old.Velocity.y));

            m_wasGrounded = next.IsGrounded;
        }
    }
}
