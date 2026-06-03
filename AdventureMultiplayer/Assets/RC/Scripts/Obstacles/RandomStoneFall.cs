using DG.Tweening;
using UnityEngine;

namespace AdventureMultiplayer
{
    [AddComponentMenu("Adventure Multiplayer/Obstacles/Random Stone Fall")]
    public class RandomStoneFall : MonoBehaviour
    {
        [SerializeField] private Vector3 endLocalPosition;
        [SerializeField] private float   randomRangeX = 3f;
        [SerializeField] private float   randomRangeZ = 2f;
        [SerializeField] private float   duration     = 3.5f;
        [SerializeField] private float   minDelay     = 0f;
        [SerializeField] private float   maxDelay     = 3f;

        private Vector3  m_startLocalPos;
        private Sequence m_sequence;

        private void Start()
        {
            m_startLocalPos = transform.localPosition;

            var anim = GetComponent<Animator>();
            if (anim != null) anim.enabled = false;

            var rao = GetComponent<RandomAnimationOffset>();
            if (rao != null) Destroy(rao);

            DOVirtual.DelayedCall(Random.Range(minDelay, maxDelay), PlayOneCycle);
        }

        private void OnDestroy() => m_sequence?.Kill();

        private void PlayOneCycle()
        {
            transform.localPosition = m_startLocalPos;
            transform.localRotation = Quaternion.identity;

            var endPos = endLocalPosition + new Vector3(
                Random.Range(-randomRangeX, randomRangeX),
                0f,
                Random.Range(-randomRangeZ, randomRangeZ));

            m_sequence?.Kill();
            m_sequence = DOTween.Sequence();
            m_sequence.Join(transform.DOLocalMove(endPos, duration).SetEase(Ease.InQuad));
            m_sequence.Join(transform.DOLocalRotate(
                new Vector3(0f, 0f, -360f), duration, RotateMode.FastBeyond360).SetEase(Ease.Linear));
            m_sequence.OnComplete(PlayOneCycle);
        }
    }
}
