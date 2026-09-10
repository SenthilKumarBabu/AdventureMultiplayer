using UnityEngine;

namespace ithappy.rc
{
    /// <summary>
    /// Rocks this transform back and forth around its starting rotation, like a
    /// see-saw. It swings <see cref="rotationAngle"/> / 2 to each side, so the
    /// total peak-to-peak sweep equals <see cref="rotationAngle"/> and the rest
    /// pose sits in the middle (e.g. rotationAngle = 60 → tilts 30 up then 30
    /// down through the start pose). Motion is a smooth sine so it eases at both
    /// turnaround points.
    ///
    /// Unlike the original ithappy version this never parks on one side of the
    /// start pose — it always passes through it — which is what makes a row of
    /// planks read as "half up, half down" once <see cref="phaseOffset01"/> is
    /// staggered.
    /// </summary>
    public class OscillateRotation : MonoBehaviour
    {
        [Tooltip("Local axis to rock around. Normalised at runtime.")]
        public Vector3 rotationAxis = Vector3.right;

        [Tooltip("Full peak-to-peak sweep in degrees. The object rocks half " +
                 "this far to each side of its starting rotation.")]
        public float rotationAngle = 60f;

        [Tooltip("Seconds for one full there-and-back cycle.")]
        public float duration = 2f;

        [Tooltip("Phase offset as a fraction of one cycle (0..1). Set 0.5 on " +
                 "alternating copies so half the row rocks up while the other " +
                 "half rocks down.")]
        [Range(0f, 1f)]
        public float phaseOffset01 = 0f;

        [Tooltip("ON (default): a see-saw that passes THROUGH the start pose, " +
                 "swinging rotationAngle/2 to each side. OFF: a one-sided swing " +
                 "that rocks from the start pose out to +rotationAngle and back " +
                 "(the classic pendulum/hammer motion) — pair with phaseOffset01 " +
                 "0.5 on a second copy so the two swing in opposition.")]
        public bool throughCenter = true;

        [Tooltip("Add a small random phase on top so identical copies don't " +
                 "move in perfect lockstep.")]
        public bool useRandomDelay = false;

        [Tooltip("Maximum random phase offset in seconds.")]
        public float maxRandomDelay = 1f;

        private Quaternion startRotation;
        private float phaseSeconds;

        // When a KINEMATIC Rigidbody is present the swing is driven through it
        // (rb.MoveRotation in FixedUpdate) instead of writing transform.localRotation
        // in Update. A script-rocked collider with no Rigidbody is a *static*
        // collider that teleports every frame — PhysX resolves the overlap with a
        // depenetration impulse that can launch players off the plank/platform. A
        // kinematic Rigidbody makes PhysX carry/push them smoothly instead. See
        // CLAUDE.md, "Physics — Rigidbody vs Transform". No Rigidbody → original
        // transform path, unchanged.
        private Rigidbody m_rb;

        void Start()
        {
            m_rb = GetComponent<Rigidbody>();
            startRotation = transform.localRotation;

            float period = Mathf.Max(0.01f, duration);
            phaseSeconds = phaseOffset01 * period;
            if (useRandomDelay)
            {
                phaseSeconds += Random.Range(0f, maxRandomDelay);
            }
        }

        private float CurrentAngle(float time)
        {
            float period = Mathf.Max(0.01f, duration);
            float t = (time + phaseSeconds) / period;
            float phase = t * Mathf.PI * 2f;

            if (throughCenter)
                return Mathf.Sin(phase) * (rotationAngle * 0.5f);

            // One-sided: 0 → +rotationAngle → 0, eased at both turnaround points
            // (a raised cosine). phaseOffset01 0.5 puts a second copy at the far
            // end of its swing when this one is at the near end.
            return (1f - Mathf.Cos(phase)) * 0.5f * rotationAngle;
        }

        void Update()
        {
            if (m_rb != null && m_rb.isKinematic) return; // driven in FixedUpdate instead

            transform.localRotation = startRotation * Quaternion.AngleAxis(CurrentAngle(Time.time), rotationAxis);
        }

        void FixedUpdate()
        {
            if (m_rb == null || !m_rb.isKinematic) return;

            Quaternion targetLocal = startRotation * Quaternion.AngleAxis(CurrentAngle(Time.time), rotationAxis);
            Quaternion targetWorld = transform.parent != null
                ? transform.parent.rotation * targetLocal
                : targetLocal;
            m_rb.MoveRotation(targetWorld);
        }
    }
}
