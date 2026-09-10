using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using PLAYERTWO.PlatformerProject;
using UnityEngine;

namespace AdventureMultiplayer
{
    /// <summary>
    /// A log that rolls off a platform like a boulder and falls into the water below.
    ///
    /// One cycle: sit at the start point → roll down the slope along
    /// <see cref="rollDirection"/>, hugging the ground, spinning about its long axis →
    /// when the ground drops away at the platform edge, switch to real physics (gravity
    /// + the roll's own momentum) and tumble into the water → once it's under the water
    /// line, snap back to the start, wait <see cref="idleInterval"/> seconds, repeat.
    ///
    /// The roll is DOTween-driven (one progress tween per run so the ground-follow and
    /// edge check can run each frame). The Rigidbody is kinematic while rolling and goes
    /// dynamic only for the fall. The MeshCollider must be Convex (the RollingLog prefab
    /// sets this). See CLAUDE.md, "Physics".
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    [AddComponentMenu("Adventure Multiplayer/Obstacles/Rolling Log Obstacle")]
    public class RollingLogObstacle : MonoBehaviour
    {
        public enum Axis { X, Y, Z }

        [Header("Roll Path")]
        [Tooltip("Direction the log rolls, in its OWN local space. For the RollingLog " +
                 "instance (world-aligned, log lying along X) local -Z points back toward " +
                 "the race start / down the slope toward the water.")]
        [SerializeField] private Vector3 rollDirection = new Vector3(0f, 0f, -1f);

        [Tooltip("Local axis the log SPINS about — its long axis. The obstacle_2_002 log " +
                 "is longest on X.")]
        [SerializeField] private Axis spinAxis = Axis.X;

        [Tooltip("Max distance the log rolls before the run ends (safety cap — normally " +
                 "the run ends when it reaches the edge and falls).")]
        [SerializeField] private float rollDistance = 25f;

        [Tooltip("How long the roll to the edge takes, in seconds.")]
        [SerializeField] private float rollDuration = 2.2f;

        [Tooltip("Seconds the log sits at the start point between runs.")]
        [SerializeField] private float idleInterval = 3f;

        [Tooltip("Ease curve for the roll. InQuad accelerates like a boulder off a ledge.")]
        [SerializeField] private Ease rollEase = Ease.InQuad;

        [Tooltip("Log radius, used to couple spin to travel and to sit it on the ground. " +
                 "0 = auto from the collider bounds.")]
        [SerializeField] private float logRadius = 0f;

        [Tooltip("Flip the spin if the log looks like it's rolling backwards.")]
        [SerializeField] private bool flipSpin = false;

        [Tooltip("Random 0..this second offset before the first run so several logs " +
                 "don't roll in lockstep.")]
        [SerializeField] private float startStagger = 0.5f;

        [Header("Ground / Edge")]
        [Tooltip("Layers treated as ground for the slope-follow and edge raycasts.")]
        [SerializeField] private LayerMask groundMask = Physics.DefaultRaycastLayers;

        [Tooltip("How far above the path the ground raycast starts.")]
        [SerializeField] private float groundProbeHeight = 4f;

        [Tooltip("If the ground under the log's path drops more than this (or vanishes), " +
                 "the log has reached the edge and starts falling.")]
        [SerializeField] private float edgeDropThreshold = 2.5f;

        [Header("Fall Into Water")]
        [Tooltip("World Y of the water surface. When the falling log sinks this far below " +
                 "it, the run resets.")]
        [SerializeField] private float waterLevel = -7.5f;

        [Tooltip("How far below the water line the log sinks before the run resets.")]
        [SerializeField] private float sinkDepth = 2f;

        [Tooltip("Hard cap on the fall — reset after this many seconds even if the water " +
                 "check never triggers.")]
        [SerializeField] private float fallTimeout = 4f;

        [Tooltip("Optional splash effect spawned where the log crosses the water line.")]
        [SerializeField] private GameObject splashEffect;

        [Header("Player Hit")]
        [Tooltip("Speed the log shoves a player it rolls into, along the roll direction.")]
        [SerializeField] private float hitForce = 14f;

        [Tooltip("Upward pop given to a player the log rolls into.")]
        [SerializeField] private float hitUpForce = 4f;

        [Tooltip("How close a player must be to the log surface to get hit.")]
        [SerializeField] private float hitRadius = 1.4f;

        private Rigidbody  _rb;
        private Collider   _collider;
        private Vector3    _startPos;
        private Quaternion _startRot;
        private Vector3    _worldRollDir;
        private float      _radius;
        private Sequence   _seq;
        private bool       _rolling;
        private bool       _falling;
        private float      _fallStart;
        private bool       _splashed;
        private Vector3    _prevPos;

        private Vector3 SpinAxisLocal => spinAxis switch
        {
            Axis.X => Vector3.right,
            Axis.Y => Vector3.up,
            _      => Vector3.forward
        };

        private void Awake()
        {
            _rb       = GetComponent<Rigidbody>();
            _collider = GetComponent<Collider>();
            GoKinematic();

            _startPos = transform.position;
            _startRot = transform.rotation;
            _worldRollDir = transform.TransformDirection(rollDirection.sqrMagnitude > 0.0001f
                ? rollDirection.normalized : Vector3.back);

            _radius = logRadius > 0f
                ? logRadius
                : Mathf.Max(0.25f, Mathf.Min(_collider.bounds.extents.x, _collider.bounds.extents.z));
        }

        private void OnEnable() => RunAsync().Forget();

        private void OnDisable()
        {
            _rolling = false;
            _falling = false;
            _seq?.Kill();
        }

        private async UniTaskVoid RunAsync()
        {
            var token = this.GetCancellationTokenOnDestroy();
            try
            {
                if (startStagger > 0f)
                    await UniTask.Delay(TimeSpan.FromSeconds(UnityEngine.Random.Range(0f, startStagger)),
                                        cancellationToken: token);

                while (!token.IsCancellationRequested)
                {
                    StartRoll();

                    // wait out the whole run: the roll, then (if it reached the edge) the fall
                    await UniTask.WaitWhile(() => _rolling || _falling, cancellationToken: token);

                    ResetToStart();
                    await UniTask.Delay(TimeSpan.FromSeconds(Mathf.Max(0f, idleInterval)),
                                        cancellationToken: token);
                }
            }
            catch (OperationCanceledException) { }
            finally { _seq?.Kill(); }
        }

        // ── Roll ──────────────────────────────────────────────────────────────

        private void StartRoll()
        {
            _seq?.Kill();
            GoKinematic();
            transform.SetPositionAndRotation(_startPos, _startRot);
            _prevPos = _startPos;

            float turns   = rollDistance / (2f * Mathf.PI * _radius);
            float spinDeg = turns * 360f * (flipSpin ? -1f : 1f);

            _rolling = true;
            _falling = false;
            _splashed = false;

            float prog = 0f;
            _seq = DOTween.Sequence();
            _seq.Append(DOTween.To(() => prog, v =>
            {
                prog = v;
                if (_rolling) ApplyRoll(prog, spinDeg);
            }, 1f, rollDuration).SetEase(rollEase)
            .OnComplete(() => { if (_rolling) _rolling = false; }));   // rolled the full cap without an edge
        }

        private void ApplyRoll(float prog, float spinDeg)
        {
            Vector3 flat = _startPos + _worldRollDir * (prog * rollDistance);

            // where's the ground under this point?
            Vector3 probe = flat + Vector3.up * groundProbeHeight;
            _collider.enabled = false;
            bool hit = Physics.Raycast(probe, Vector3.down, out RaycastHit h,
                groundProbeHeight + edgeDropThreshold + _radius + 2f,
                groundMask, QueryTriggerInteraction.Ignore);
            _collider.enabled = true;

            float expectedY = _prevPos.y;                    // roughly where the log is now
            if (!hit || h.point.y < expectedY - edgeDropThreshold)
            {
                BeginFall();
                return;
            }

            Vector3 pos = new Vector3(flat.x, h.point.y + _radius, flat.z);
            Quaternion spin = _startRot * Quaternion.AngleAxis(spinDeg * prog, SpinAxisLocal);
            transform.SetPositionAndRotation(pos, spin);
            _prevPos = pos;
        }

        // ── Fall ──────────────────────────────────────────────────────────────

        private void BeginFall()
        {
            _seq?.Kill();
            _rolling = false;
            _falling = true;
            _fallStart = Time.time;

            // carry the roll's momentum into the physics fall
            Vector3 vel = (transform.position - _prevPos) / Mathf.Max(Time.deltaTime, 0.0001f);
            if (vel.sqrMagnitude < 1f) vel = _worldRollDir * (rollDistance / Mathf.Max(0.1f, rollDuration));

            _rb.isKinematic = false;
            _rb.useGravity  = true;
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _rb.linearVelocity  = vel;

            Vector3 axisWorld = transform.TransformDirection(SpinAxisLocal);
            _rb.angularVelocity = axisWorld * (vel.magnitude / _radius) * (flipSpin ? -1f : 1f);
        }

        private void GoKinematic()
        {
            _rb.linearVelocity  = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.isKinematic     = true;
            _rb.useGravity      = false;
            _rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        }

        private void ResetToStart()
        {
            GoKinematic();
            transform.SetPositionAndRotation(_startPos, _startRot);
            _prevPos = _startPos;
        }

        // ── Per-frame ─────────────────────────────────────────────────────────

        private void Update()
        {
            if (_rolling)
            {
                HitPlayers();
                return;
            }

            if (_falling)
            {
                float y = transform.position.y;

                if (!_splashed && y <= waterLevel && splashEffect != null)
                {
                    _splashed = true;
                    var pos = transform.position;
                    Instantiate(splashEffect, new Vector3(pos.x, waterLevel, pos.z), Quaternion.identity);
                }

                if (y < waterLevel - sinkDepth || Time.time - _fallStart > fallTimeout)
                    _falling = false;   // RunAsync's WaitWhile unblocks → ResetToStart
            }
        }

        private void HitPlayers()
        {
            Vector3 center = _collider.bounds.center;
            float   scan   = hitRadius + _collider.bounds.extents.magnitude;
            var hits = Physics.OverlapSphere(center, scan, ~0, QueryTriggerInteraction.Ignore);

            foreach (var col in hits)
            {
                var player = col.GetComponentInParent<Player>();
                if (player == null || !player.isAlive) continue;

                Vector3 nearest = _collider.ClosestPoint(player.position);
                if (Vector3.Distance(player.position, nearest) > hitRadius) continue;

                Vector3 shove = _worldRollDir * hitForce;
                player.lateralVelocity += new Vector3(shove.x, 0f, shove.z);
                if (hitUpForce > 0f) player.verticalVelocity = Vector3.up * hitUpForce;
                break;
            }
        }
    }
}
