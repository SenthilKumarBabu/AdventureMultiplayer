using PLAYERTWO.PlatformerProject;
using UnityEngine;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Temporary diagnostic — logs when a player is inside or near the torus tube.
    /// Attach to obstacle_13_002 alongside TorusCollider. No physics changes.
    /// Remove once the stuck issue is diagnosed.
    /// </summary>
    public class TorusDiagnostic : MonoBehaviour
    {
        [SerializeField] private float ringRadius = 2.9f;
        [SerializeField] private float tubeRadius = 0.66f;

        private Player _player;
        private bool   _wasInside;

        private void Start()
        {
            Debug.Log($"[TorusDiag] START on {gameObject.name} — ringR={ringRadius} tubeR={tubeRadius}");
        }

        private int _frameCount;

        private void Update()
        {
            if (_player == null)
            {
                foreach (var p in FindObjectsByType<Player>(FindObjectsSortMode.None))
                {
                    var no = p.GetComponent<Unity.Netcode.NetworkObject>();
                    if (no == null || no.IsOwner) { _player = p; break; }
                }
                // Log every 120 frames if still null
                if (_player == null)
                {
                    if (++_frameCount % 120 == 0)
                        Debug.LogWarning($"[TorusDiag] Player still NULL after {_frameCount} frames");
                    return;
                }
                Debug.Log($"[TorusDiag] Player FOUND: {_player.name}");
            }

            Vector3 local = transform.InverseTransformPoint(_player.transform.position);
            float ringDist  = new Vector2(local.x, local.z).magnitude;
            float tubeD     = Mathf.Sqrt(Mathf.Pow(ringDist - ringRadius, 2) + local.y * local.y);
            bool  inTube    = tubeD < tubeRadius;
            bool  inHole    = ringDist < (ringRadius - tubeRadius) && Mathf.Abs(local.y) < tubeRadius + 0.5f;

            // Raw log every 60 frames for calibration
            if (++_frameCount % 60 == 0)
                Debug.Log($"[TorusDiag] RAW | playerWorldPos={_player.transform.position:F2} | ringDist={ringDist:F2} tubeD={tubeD:F2} localY={local.y:F2} | inTube={inTube} inHole={inHole}");

            bool inside = inTube || inHole;

            if (inside && !_wasInside)
                Debug.LogWarning($"[TorusDiag] ENTERED ({(inTube ? "TUBE" : "HOLE")}) | worldPos={_player.transform.position:F2} | ringDist={ringDist:F2} tubeD={tubeD:F2} localY={local.y:F2} | vel={_player.velocity:F2} state={_player.states.current?.GetType().Name}");

            if (inside)
                Debug.Log($"[TorusDiag] INSIDE {(inTube ? "TUBE" : "HOLE")} | worldPos={_player.transform.position:F2} | ringDist={ringDist:F2} tubeD={tubeD:F2} localY={local.y:F2} | vel={_player.velocity} state={_player.states.current?.GetType().Name}");

            if (!inside && _wasInside)
                Debug.Log($"[TorusDiag] EXITED");

            _wasInside = inside;
        }
    }
}
