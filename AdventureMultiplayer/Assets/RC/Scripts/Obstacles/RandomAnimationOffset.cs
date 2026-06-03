using UnityEngine;

namespace AdventureMultiplayer
{
    [AddComponentMenu("Adventure Multiplayer/Obstacles/Random Animation Offset")]
    [RequireComponent(typeof(Animator))]
    public class RandomAnimationOffset : MonoBehaviour
    {
        [SerializeField] private float minDelay = 0f;
        [SerializeField] private float maxDelay = 3f;

        private void Start()
        {
            var anim = GetComponent<Animator>();
            // Start at a random normalised time so repeated instances are out of phase
            anim.Play(0, 0, Random.Range(0f, 1f));
            // Also apply a random playback speed-phase via update offset
            anim.Update(Random.Range(minDelay, maxDelay));
        }
    }
}
