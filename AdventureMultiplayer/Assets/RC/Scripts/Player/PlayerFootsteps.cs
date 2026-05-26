using System.Collections.Generic;
using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
    [AddComponentMenu("PLAYER TWO/Platformer Project/Player/Player Footsteps Fixed")]
    public class PlayerFootstepsFixed : MonoBehaviour
    {
        [System.Serializable]
        public class Surface
        {
            public string tag;
            public AudioClip[] footsteps;
            public AudioClip[] landings;
        }

        public Surface[] surfaces;
        public AudioClip[] defaultFootsteps;
        public AudioClip[] defaultLandings;

        [Header("General Settings")]
        public float stepOffset = 1.25f;
        public float footstepVolume = 0.5f;

        protected Vector3 m_lastLateralPosition;

        protected Dictionary<string, AudioClip[]> m_footsteps = new();
        protected Dictionary<string, AudioClip[]> m_landings = new();

        protected Player m_player;
        protected AudioSource m_audio;

        protected virtual void PlayRandomClip(AudioClip[] clips)
        {
            if (clips.Length > 0)
            {
                var index = Random.Range(0, clips.Length);
                var clip = clips[index];
                m_audio.PlayOneShot(clip, footstepVolume);
                Debug.Log($"[PlayerFootstepsFixed] Playing clip: {clip?.name} via {m_audio.gameObject.name}", m_audio.gameObject);
            }
        }

        protected virtual bool TryPlayFootstep(Dictionary<string, AudioClip[]> dict, Collider other)
        {
            foreach (var key in dict.Keys)
            {
                if (other.CompareTag(key))
                {
                    PlayRandomClip(dict[key]);
                    return true;
                }
            }

            return false;
        }

        protected virtual void Landing()
        {
            // Suppress sound if the player was only off the ground for one or two frames
            // (slope detection oscillation). A real landing from a jump takes much longer.
            if (Time.time - m_player.lastGroundTime < 0.1f)
                return;

            if (!m_player.onWater)
            {
                if (!TryPlayFootstep(m_landings, m_player.groundHit.collider))
                    PlayRandomClip(defaultLandings);
            }
        }

        protected virtual void Start()
        {
            m_player = GetComponent<Player>();
            m_player.entityEvents.OnGroundEnter.AddListener(Landing);

            if (!TryGetComponent(out m_audio))
            {
                m_audio = gameObject.AddComponent<AudioSource>();
            }

            foreach (var surface in surfaces)
            {
                m_footsteps.Add(surface.tag, surface.footsteps);
                m_landings.Add(surface.tag, surface.landings);
            }

            m_lastLateralPosition = transform.position;
            Debug.Log($"[PlayerFootstepsFixed] Start — using AudioSource: {m_audio.name}, clip: {m_audio.clip}");
        }

        protected virtual void Update()
        {
            if (!m_player.isGrounded || !m_player.states.IsCurrentOfType(typeof(WalkPlayerState)))
                return;

            var position = transform.position;

            var dx = position.x - m_lastLateralPosition.x;
            var dz = position.z - m_lastLateralPosition.z;
            var distance = Mathf.Sqrt(dx * dx + dz * dz);

            if (distance < stepOffset)
                return;

            Debug.Log($"[PlayerFootstepsFixed] Step — XZ dist={distance:F2}, pos={position}, state={m_player.states.current?.GetType().Name}");

            if (!TryPlayFootstep(m_footsteps, m_player.groundHit.collider))
                PlayRandomClip(defaultFootsteps);

            m_lastLateralPosition = position;
        }
    }
}
