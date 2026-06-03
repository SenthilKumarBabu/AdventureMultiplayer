using System.Collections.Generic;
using PLAYERTWO.PlatformerProject;
using UnityEngine;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Conveyor-belt / treadmill surface. Continuously pushes any player standing on
    /// the trigger in a direction defined in local space (so rotating the GameObject
    /// rotates the push direction automatically).
    ///
    /// Requires a trigger Collider on this GameObject (or a child) to detect players.
    /// Direction is in local space: Vector3.forward = obstacle's forward, etc.
    /// </summary>
    [AddComponentMenu("Adventure Multiplayer/Obstacles/Treadmill Obstacle")]
    public class TreadmillObstacle : MonoBehaviour
    {
        [SerializeField] private Vector3 localDirection = Vector3.back;
        [SerializeField] private float   force          = 5f;  // m/s added directly each frame on top of player velocity

        private readonly HashSet<Player> m_players = new();

        private void OnTriggerEnter(Collider other)
        {
            var player = other.GetComponentInParent<Player>();
            if (player == null) return;
            m_players.Add(player);
            Debug.Log($"[Treadmill] {name} — player entered: {other.gameObject.name}");
        }

        private void OnTriggerStay(Collider other)
        {
            var player = other.GetComponentInParent<Player>();
            if (player != null) m_players.Add(player);
        }

        private void OnTriggerExit(Collider other)
        {
            var player = other.GetComponentInParent<Player>();
            if (player == null) return;
            m_players.Remove(player);
            Debug.Log($"[Treadmill] {name} — player exited: {other.gameObject.name}");
        }

        // Update instead of FixedUpdate — PLAYER TWO moves the character in its own Update (order 0)
        // via controller.Move(velocity * Time.deltaTime). By running at order 200 we stack the
        // belt displacement on top of whatever PLAYER TWO already moved the character.
        private void Update()
        {
            if (m_players.Count == 0) return;
            var worldDir = transform.TransformDirection(localDirection.normalized);
            var delta    = force * Time.deltaTime * worldDir;
            foreach (var player in m_players)
            {
                if (player == null || !player.isGrounded) continue;
                var posBefore = player.transform.position;
                player.transform.position += delta;
                Debug.Log($"[Treadmill] {name} | grounded={player.isGrounded} | delta={delta} | pos {posBefore} → {player.transform.position}");
            }
            m_players.RemoveWhere(p => p == null);
        }
    }
}
