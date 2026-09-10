using UnityEngine;

namespace ithappy.rc
{
    /// <summary>
    /// Slides this transform back and forth along a world-space axis, centred on
    /// its starting position. It travels <see cref="moveDistance"/> / 2 to each
    /// side, so the total peak-to-peak range equals <see cref="moveDistance"/>
    /// (e.g. moveDistance = 1.4 → oscillates from -0.7 to +0.7 around the rest
    /// position). Motion is a smooth sine so it eases at both turnaround points
    /// and never overshoots the range.
    /// </summary>
    public class OscillatePosition : MonoBehaviour
    {
        [Tooltip("Direction to slide along. Normalised at runtime. World-space " +
                 "unless Local Space is ticked, in which case it's this " +
                 "transform's own axis (e.g. (1,0,0) = local X).")]
        public Vector3 moveAxis = Vector3.up;

        [Tooltip("Slide along localPosition instead of world position — use when " +
                 "this is a child that should slide along its own local axis.")]
        public bool localSpace = false;

        [Tooltip("Full peak-to-peak travel. The object moves half this far to " +
                 "each side of its starting position.")]
        public float moveDistance = 2f;

        [Tooltip("Seconds for one full there-and-back cycle.")]
        public float duration = 2f;

        [Tooltip("Phase offset as a fraction of one cycle (0..1). Set 0.5 on " +
                 "alternating copies so half are sliding left while the other " +
                 "half slide right.")]
        [Range(0f, 1f)]
        public float phaseOffset01 = 0f;

        [Tooltip("Toggle a random phase offset so multiple copies don't move in sync.")]
        public bool useRandomDelay = false;

        [Tooltip("Maximum random phase offset in seconds.")]
        public float maxRandomDelay = 1f;

        private Vector3 startPosition;
        private float phaseOffset;

        void Start()
        {
            startPosition = localSpace ? transform.localPosition : transform.position;
            phaseOffset = phaseOffset01 * Mathf.Max(0.01f, duration);
            if (useRandomDelay) phaseOffset += Random.Range(0f, maxRandomDelay);
        }

        void Update()
        {
            float period = Mathf.Max(0.01f, duration);
            float t = (Time.time + phaseOffset) / period;
            float offset = Mathf.Sin(t * Mathf.PI * 2f) * (moveDistance * 0.5f);
            Vector3 delta = moveAxis.normalized * offset;

            if (localSpace)
                transform.localPosition = startPosition + delta;
            else
                transform.position = startPosition + delta;
        }
    }
}
