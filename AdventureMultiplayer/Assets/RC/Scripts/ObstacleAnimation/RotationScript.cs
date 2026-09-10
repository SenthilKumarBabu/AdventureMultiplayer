using UnityEngine;

namespace ithappy.rc
{
    public class RotationScript : MonoBehaviour
    {
        public enum RotationAxis
        {
            X,
            Y,
            Z
        }

        public RotationAxis rotationAxis = RotationAxis.Y;
        public float rotationSpeed = 50.0f;

        [Tooltip("When set, neighbouring instances spin OPPOSITE ways — checkerboard by world " +
                 "position. Used by the moving_platform disc field so ~half spin normal and " +
                 "~half reverse. Default off: every other object using this script is unaffected.")]
        public bool alternateByPosition = false;

        // When a KINEMATIC Rigidbody is present the rotation is driven through it
        // (rb.MoveRotation in FixedUpdate) instead of writing transform.rotation in
        // Update. A script-spun collider with no Rigidbody is a *static* collider
        // that teleports every frame — PhysX then resolves the sudden overlap with a
        // depenetration impulse and launches whatever is touching it (players get
        // flung skyward off spinning beams/dumbbells and can never pass). A kinematic
        // Rigidbody makes PhysX treat it as an intentionally moving body and apply a
        // bounded, physically-plausible push instead. See CLAUDE.md, "Physics —
        // Rigidbody vs Transform". Objects with no Rigidbody (power-up pickups, purely
        // decorative spinners) keep the original transform path, unchanged.
        private Rigidbody m_rb;
        private float m_dir = 1f;

        void Awake()
        {
            m_rb = GetComponent<Rigidbody>();

            m_dir = 1f;
            if (alternateByPosition)
            {
                // Column parity in world X (disc field columns are ~3.5 m apart) → adjacent
                // columns spin opposite ways. A staggered grid puts every disc on the same
                // checkerboard colour, so parity must key off ONE axis, not the sum.
                if ((Mathf.RoundToInt(transform.position.x / 3.5f) & 1) != 0) m_dir = -1f;
            }
        }

        private Vector3 Axis()
        {
            switch (rotationAxis)
            {
                case RotationAxis.X: return Vector3.right;
                case RotationAxis.Z: return Vector3.forward;
                default:             return Vector3.up;
            }
        }

        void Update()
        {
            if (m_rb != null && m_rb.isKinematic) return; // driven in FixedUpdate instead

            transform.Rotate(Axis(), rotationSpeed * m_dir * Time.deltaTime);
        }

        void FixedUpdate()
        {
            if (m_rb == null || !m_rb.isKinematic) return;

            Quaternion delta = Quaternion.AngleAxis(rotationSpeed * m_dir * Time.fixedDeltaTime, Axis());
            m_rb.MoveRotation(m_rb.rotation * delta);
        }
    }
}
