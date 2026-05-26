using UnityEngine;
using Unity.Netcode;
using DG.Tweening;
using PLAYERTWO.PlatformerProject;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Drives the ghost player's Animator from synced velocity and state data.
    ///
    /// Also mirrors the skin offsets applied by surface states (WallDrag, LedgeHanging,
    /// LedgeClimbing) so the ghost's mesh is visually positioned against the wall/ledge.
    ///
    /// Owner:     PlayerAnimator runs as normal. This script does nothing on the owner.
    /// Non-owner: PlayerAnimator and PlayerParticles disabled. This script sets all
    ///            Animator parameters from the synced velocity and state every Update.
    /// </summary>
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(NetworkObject))]
    [AddComponentMenu("Adventure Multiplayer/Networked Animator Sync")]
    public class NetworkedAnimatorSync : NetworkBehaviour
    {
        private Player               m_player;
        private PlayerAnimator       m_playerAnimator;
        private PlayerParticles      m_playerParticles;
        private Animator             m_animator;
        private NetworkedMovementSync m_movementSync;

        // Animator parameter hashes
        private int m_stateHash;
        private int m_lastStateHash;
        private int m_lateralSpeedHash;
        private int m_verticalSpeedHash;
        private int m_lateralAnimSpeedHash;
        private int m_rollAnimSpeedHash;
        private int m_isGroundedHash;
        private int m_jumpCounterHash;
        private int m_onStateChangedHash;
        private int m_isHoldingHash;

        private int m_lastStateIndex = -1;
        private readonly System.Collections.Generic.HashSet<int> m_validParams = new();

        // ── Skin offset tracking for surface states ───────────────────────────

        private GameObject m_skinClimbSlot;

        // Tracks which skin offset is currently applied so we can undo it on exit.
        private enum SurfaceSkinMode { None, WallDrag, LedgeHanging, LedgeClimbing }
        private SurfaceSkinMode m_currentSkinMode = SurfaceSkinMode.None;
        private Quaternion m_surfaceEnterRotation;  // rotation used when applying the offset, needed to undo it exactly

        // Index → surface mode, built once via reflection over m_list.
        private readonly System.Collections.Generic.Dictionary<int, SurfaceSkinMode> m_surfaceModeByIndex = new();

        // ── Lifecycle ─────────────────────────────────────────────────────────

        public override void OnNetworkSpawn()
        {
            m_player        = GetComponent<Player>();
            m_playerAnimator = GetComponent<PlayerAnimator>();
            m_playerParticles = GetComponent<PlayerParticles>();
            m_movementSync  = GetComponent<NetworkedMovementSync>();

            var root = (m_player != null && m_player.skin != null) ? m_player.skin : transform;
            m_animator = root.GetComponentInChildren<Animator>();

            if (m_animator == null)
            {
                Debug.LogWarning("[NetworkedAnimatorSync] No Animator found on player or skin.");
                return;
            }

            foreach (var p in m_animator.parameters)
                m_validParams.Add(p.nameHash);

            if (m_playerAnimator != null)
            {
                m_stateHash            = Animator.StringToHash(m_playerAnimator.stateName);
                m_lastStateHash        = Animator.StringToHash(m_playerAnimator.lastStateName);
                m_lateralSpeedHash     = Animator.StringToHash(m_playerAnimator.lateralSpeedName);
                m_verticalSpeedHash    = Animator.StringToHash(m_playerAnimator.verticalSpeedName);
                m_lateralAnimSpeedHash = Animator.StringToHash(m_playerAnimator.lateralAnimationSpeedName);
                m_rollAnimSpeedHash    = Animator.StringToHash(m_playerAnimator.rollAnimationSpeedName);
                m_isGroundedHash       = Animator.StringToHash(m_playerAnimator.isGroundedName);
                m_jumpCounterHash      = Animator.StringToHash(m_playerAnimator.jumpCounterName);
                m_onStateChangedHash   = Animator.StringToHash(m_playerAnimator.onStateChangedName);
                m_isHoldingHash        = Animator.StringToHash(m_playerAnimator.isHoldingName);
            }

            if (!IsOwner)
            {
                // Stop PlayerAnimator and PlayerParticles — they read stale ghost state.
                // This script replaces them using synced data.
                if (m_playerAnimator  != null) m_playerAnimator.enabled  = false;
                if (m_playerParticles != null) m_playerParticles.enabled = false;

                m_movementSync.OnRemoteStateChanged += OnRemoteStateChanged;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (m_movementSync != null)
                m_movementSync.OnRemoteStateChanged -= OnRemoteStateChanged;

            ResetSkinOffset();
        }

        private void Start()
        {
            // Build surface-mode index cache here, not in OnNetworkSpawn.
            // EntityStateManager.Start() populates m_list before this runs
            // (default execution order 0 < our order 100), so the list is ready.
            if (m_player?.states == null) return;

            var mListField = typeof(PLAYERTWO.PlatformerProject.EntityStateManager<Player>)
                .GetField("m_list",
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);

            if (mListField == null) return;

            var list = mListField.GetValue(m_player.states)
                as System.Collections.Generic.List<PLAYERTWO.PlatformerProject.EntityState<Player>>;

            if (list == null) return;

            for (int i = 0; i < list.Count; i++)
            {
                var s = list[i];
                if      (s is WallDragPlayerState)      m_surfaceModeByIndex[i] = SurfaceSkinMode.WallDrag;
                else if (s is LedgeHangingPlayerState)  m_surfaceModeByIndex[i] = SurfaceSkinMode.LedgeHanging;
                else if (s is LedgeClimbingPlayerState) m_surfaceModeByIndex[i] = SurfaceSkinMode.LedgeClimbing;
            }

        }

        // ── Ghost update ──────────────────────────────────────────────────────

        private void Update()
        {
            if (!IsSpawned || IsOwner || m_animator == null || m_movementSync == null) return;
            DriveGhostAnimator();
        }

        private void DriveGhostAnimator()
        {
            var state    = m_movementSync.SyncedState;
            var velocity = state.Velocity;
            var lateral  = new Vector3(velocity.x, 0f, velocity.z);

            float lateralSpeed = lateral.magnitude;
            float verticalSpeed = velocity.y;
            float topSpeed = (m_player?.stats != null) ? m_player.stats.current.topSpeed : 10f;

            float minLateralAnimSpeed = m_playerAnimator != null
                ? m_playerAnimator.minLateralAnimationSpeed : 0.5f;
            float minRollAnimSpeed = m_playerAnimator != null
                ? m_playerAnimator.minRollAnimationSpeed : 10f;

            float lateralAnimSpeed = Mathf.Max(minLateralAnimSpeed, lateralSpeed / topSpeed);
            float rollAnimSpeed    = state.IsGrounded
                ? lateralSpeed
                : Mathf.Max(minRollAnimSpeed, velocity.magnitude);

            SafeSetFloat(m_lateralSpeedHash,     lateralSpeed);
            SafeSetFloat(m_verticalSpeedHash,    verticalSpeed);
            SafeSetFloat(m_lateralAnimSpeedHash, lateralAnimSpeed);
            SafeSetFloat(m_rollAnimSpeedHash,    rollAnimSpeed);
            SafeSetBool (m_isGroundedHash,       state.IsGrounded);
            SafeSetInt  (m_jumpCounterHash,      state.JumpCounter);
            SafeSetBool (m_isHoldingHash,        state.IsHolding);

            // State transition — handled by OnRemoteStateChanged event
            if (state.StateIndex != m_lastStateIndex)
            {
                SafeSetInt    (m_lastStateHash, m_lastStateIndex);
                SafeSetInt    (m_stateHash,     state.StateIndex);
                SafeSetTrigger(m_onStateChangedHash);
                m_lastStateIndex = state.StateIndex;
            }

            // Walk dust driven by velocity
            HandleWalkDust(lateralSpeed, state.IsGrounded);
        }

        // ── Surface skin offset mirroring ─────────────────────────────────────

        private void OnRemoteStateChanged(int oldIndex, int newIndex)
        {
            if (m_player == null || m_player.skin == null) return;

            var stats = m_player.stats != null ? m_player.stats.current : null;
            if (stats == null) return;

            // Determine new surface mode from state index.
            var newMode = GetSurfaceMode(newIndex);

            // Use the authoritative position/rotation from the synced state — transform.position
            // hasn't been updated yet when this fires (StepGhost runs in the next Update).
            var syncedPos = m_movementSync.SyncedState.Position;
            var syncedRot = m_movementSync.SyncedState.Rotation;

            // Exit old mode first.
            if (m_currentSkinMode != newMode)
            {
                Debug.Log($"[AnimatorSync] EXIT old={m_currentSkinMode}→new={newMode} | " +
                          $"transform={m_player.transform.position} syncedPos={syncedPos} | " +
                          $"skinWorldPos={m_player.skin?.position} skinParent={m_player.skin?.parent?.name}");

                ExitSurfaceSkinMode(stats);

                Debug.Log($"[AnimatorSync] AFTER_EXIT skinWorldPos={m_player.skin?.position} " +
                          $"skinParent={m_player.skin?.parent?.name} transform={m_player.transform.position}");
            }

            // Enter new mode.
            EnterSurfaceSkinMode(newMode, stats, syncedPos, syncedRot);
            m_currentSkinMode = newMode;
        }

        private void EnterSurfaceSkinMode(SurfaceSkinMode mode, PlayerStats stats,
                                          Vector3 syncedPos, Quaternion syncedRot)
        {
            if (m_player.skin == null) return;

            m_surfaceEnterRotation = syncedRot;

            switch (mode)
            {
                case SurfaceSkinMode.WallDrag:
                    m_player.skin.position += syncedRot * stats.wallDragSkinOffset;
                    break;

                case SurfaceSkinMode.LedgeHanging:
                    m_player.skin.position += syncedRot * stats.ledgeHangingSkinOffset;
                    break;

                case SurfaceSkinMode.LedgeClimbing:
                    if (!m_skinClimbSlot)
                        m_skinClimbSlot = new GameObject("GhostSkinClimbSlot");
                    m_skinClimbSlot.transform.position = syncedPos;
                    m_skinClimbSlot.transform.rotation = syncedRot;
                    m_player.SetSkinParent(m_skinClimbSlot.transform, stats.ledgeClimbingSkinOffset);
                    break;
            }
        }

        private void ExitSurfaceSkinMode(PlayerStats stats)
        {
            if (m_player.skin == null) return;

            switch (m_currentSkinMode)
            {
                case SurfaceSkinMode.WallDrag:
                    m_player.skin.position -= m_surfaceEnterRotation * stats.wallDragSkinOffset;
                    break;

                case SurfaceSkinMode.LedgeHanging:
                    m_player.skin.position -= m_surfaceEnterRotation * stats.ledgeHangingSkinOffset;
                    break;

                case SurfaceSkinMode.LedgeClimbing:
                    // Reparent skin to the player transform while keeping its current world
                    // position, then tween local position back to zero. This avoids the
                    // instant 2-unit teleport that ResetSkinParent() would cause.
                    var skinWorldPos = m_player.skin.position;
                    var skinWorldRot = m_player.skin.rotation;
                    m_player.skin.parent = m_player.transform;
                    m_player.skin.position = skinWorldPos;
                    m_player.skin.rotation = skinWorldRot;
                    m_player.skin.DOLocalMove(Vector3.zero, 0.15f).SetEase(Ease.OutQuad);
                    m_player.skin.DOLocalRotate(Vector3.zero, 0.15f).SetEase(Ease.OutQuad);
                    break;
            }
        }

        private void ResetSkinOffset()
        {
            if (m_player == null || m_player.stats == null || m_player.stats.current == null) return;
            ExitSurfaceSkinMode(m_player.stats.current);
            m_currentSkinMode = SurfaceSkinMode.None;
        }

        private SurfaceSkinMode GetSurfaceMode(int stateIndex)
        {
            return m_surfaceModeByIndex.TryGetValue(stateIndex, out var mode)
                ? mode
                : SurfaceSkinMode.None;
        }

        private void HandleWalkDust(float lateralSpeed, bool isGrounded)
        {
            if (m_playerParticles == null || m_playerParticles.walkDust == null) return;
            if (isGrounded && lateralSpeed > m_playerParticles.walkDustMinSpeed)
                m_playerParticles.Play(m_playerParticles.walkDust);
            else
                m_playerParticles.Stop(m_playerParticles.walkDust);
        }

        // ── Animator helpers ──────────────────────────────────────────────────

        private void SafeSetFloat  (int hash, float val) { if (m_validParams.Contains(hash)) m_animator.SetFloat(hash, val); }
        private void SafeSetBool   (int hash, bool val)  { if (m_validParams.Contains(hash)) m_animator.SetBool(hash, val); }
        private void SafeSetInt    (int hash, int val)   { if (m_validParams.Contains(hash)) m_animator.SetInteger(hash, val); }
        private void SafeSetTrigger(int hash)            { if (m_validParams.Contains(hash)) m_animator.SetTrigger(hash); }
    }
}
