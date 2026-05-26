/*
Custom-Primitive-Colliders
Torus (Donut) shaped collider — follows the same pattern as FanCylinderCollider.
Generates a torus mesh and assigns it to a MeshCollider at runtime and in the editor.
*/
using UnityEngine;

namespace CustomPrimitiveColliders
{
    [AddComponentMenu("CustomPrimitiveColliders/3D/Torus Collider"), RequireComponent(typeof(MeshCollider))]
    public class TorusCollider : BaseCustomCollider
    {
        [Tooltip("Distance from the centre of the torus to the centre of the tube.")]
        [SerializeField] private float m_ringRadius = 1f;
        [Tooltip("Radius of the tube cross-section.")]
        [SerializeField] private float m_tubeRadius = 0.3f;
        [Tooltip("Segments around the ring (more = smoother donut).")]
        [SerializeField] private int m_ringSegments = 24;
        [Tooltip("Segments around the tube cross-section.")]
        [SerializeField] private int m_tubeSegments = 12;
        [Tooltip("Angle in degrees where the arc starts.")]
        [SerializeField] private float m_startAngle = 0f;
        [Tooltip("Total arc angle in degrees (360 = full torus, less = partial arc).")]
        [SerializeField, Range(1f, 360f)] private float m_totalAngle = 360f;
        [SerializeField] private Vector3 m_center   = Vector3.zero;
        [SerializeField] private Vector3 m_rotation = Vector3.zero;

        private Mesh m_generatedMesh;

        private void Awake() => ReCreate();

#if UNITY_EDITOR
        private void Reset()      => ReCreate();
        private void OnValidate() => ReCreate();
#endif

        public void ReCreate()
        {
            var mesh = CreateTorusMesh();

            if (m_generatedMesh != null)
            {
                if (Application.isPlaying) Destroy(m_generatedMesh);
                else DestroyImmediate(m_generatedMesh, true);
            }

            m_generatedMesh = mesh;
            meshCollider.sharedMesh = mesh;
        }

        private Mesh CreateTorusMesh()
        {
            int rs = Mathf.Max(3, m_ringSegments);
            int ts = Mathf.Max(3, m_tubeSegments);
            float rr = Mathf.Max(0.01f, m_ringRadius);
            float tr = Mathf.Max(0.01f, m_tubeRadius);

            int vertCount = (rs + 1) * (ts + 1);
            var vertices  = new Vector3[vertCount];
            var normals   = new Vector3[vertCount];
            var uvs       = new Vector2[vertCount];

            float startRad = m_startAngle * Mathf.Deg2Rad;
            float totalRad = Mathf.Clamp(m_totalAngle, 1f, 360f) * Mathf.Deg2Rad;

            for (int r = 0; r <= rs; r++)
            {
                float ringAngle = startRad + (float)r / rs * totalRad;
                float cosRing   = Mathf.Cos(ringAngle);
                float sinRing   = Mathf.Sin(ringAngle);

                for (int t = 0; t <= ts; t++)
                {
                    float tubeAngle = (float)t / ts * Mathf.PI * 2f;
                    float cosTube   = Mathf.Cos(tubeAngle);
                    float sinTube   = Mathf.Sin(tubeAngle);

                    int idx = r * (ts + 1) + t;

                    vertices[idx] = new Vector3(
                        (rr + tr * cosTube) * cosRing,
                        tr * sinTube,
                        (rr + tr * cosTube) * sinRing
                    );
                    normals[idx] = new Vector3(cosTube * cosRing, sinTube, cosTube * sinRing);
                    uvs[idx]     = new Vector2((float)r / rs, (float)t / ts);
                }
            }

            int triCount  = rs * ts * 6;
            var triangles = new int[triCount];
            int tri = 0;

            for (int r = 0; r < rs; r++)
            {
                for (int t = 0; t < ts; t++)
                {
                    int a = r * (ts + 1) + t;
                    int b = a + ts + 1;
                    int c = a + 1;
                    int d = b + 1;

                    triangles[tri++] = a; triangles[tri++] = b; triangles[tri++] = c;
                    triangles[tri++] = c; triangles[tri++] = b; triangles[tri++] = d;
                }
            }

            // Apply rotation then centre offset
            var rot = Quaternion.Euler(m_rotation);
            for (int i = 0; i < vertices.Length; i++)
                vertices[i] = rot * vertices[i] + m_center;

            var mesh = new Mesh();
#if UNITY_EDITOR
            mesh.name = $"Torus_ring{rr}_tube{tr}_start{m_startAngle}_angle{m_totalAngle}";
#endif
            mesh.vertices  = vertices;
            mesh.normals   = normals;
            mesh.uv        = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();

            return mesh;
        }
    }
}
