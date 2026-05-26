using UnityEngine;
using UnityEngine.Events;
using PLAYERTWO.PlatformerProject;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Minimal LevelScore stub for the training scene (which has no UI or Level).
    /// Overrides Start() to skip OnScoreLoaded.Invoke(), which crashes when the
    /// component is created programmatically (UnityEvent field is not serialized).
    /// </summary>
    public class TrainingLevelScore : LevelScore
    {
        protected override void Awake()
        {
            // During BeforeSceneLoad no scene is loaded, so FindFirstObjectByType
            // (called inside base.Awake) returns null, which causes base.Awake to
            // call Destroy(gameObject) on us.  Set m_instance directly instead.
            m_instance = this;

            // UnityEvent fields are null when a component is created via AddComponent
            // (no serialization pass).  Initialize them so any accidental access from
            // CollectibleDisplay or HUD doesn't throw a NullReferenceException.
            OnScoreLoaded ??= new UnityEvent();
            OnCollectibleAdded ??= new UnityEvent<Collectible>();
            OnCollectibleSet ??= new UnityEvent<string>();
        }

        protected override void Start() { }
    }
}
