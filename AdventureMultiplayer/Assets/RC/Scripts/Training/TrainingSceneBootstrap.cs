using UnityEngine;
using UnityEngine.SceneManagement;
using PLAYERTWO.PlatformerProject;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Patches scenes that have no Level component (e.g. the ML-Agents training scene).
    ///
    /// Problems fixed:
    ///   1. Collectible.OnEnable() / HandleCollectionDisabling() crashes because
    ///      LevelScore.instance is null during scene load.
    ///      Fix: spawn a LevelScore via BeforeSceneLoad. It is moved into the loading
    ///      scene immediately so it is destroyed naturally on scene unload and never
    ///      interferes with gameplay scenes.
    ///   2. CollectiblePhysics leaves m_velocity at zero when Level.instance is null,
    ///      causing a zero-direction Physics.SphereCast every frame.
    ///      Fix: call Restore() in AfterSceneLoad.
    /// </summary>
    public static class TrainingSceneBootstrap
    {
        private static GameObject s_levelScoreGO;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void BeforeSceneLoad()
        {
            // Only inject the stub in the training scene.  Gameplay scenes have a real
            // LevelScore serialized in the scene; creating the stub there causes it to
            // become the singleton (m_instance is set before scene Awakes run), which
            // forces the real LevelScore to Destroy itself and leaves CollectibleDisplay
            // / HUD with a stub whose UnityEvent fields are null → NullReferenceException.
            var sceneName = SceneManager.GetActiveScene().name;
            if (!string.IsNullOrEmpty(sceneName) && sceneName != "TrainingScene")
                return;

            // Spawn LevelScore so it exists before any Collectible.OnEnable fires.
            s_levelScoreGO = new GameObject("__TrainingLevelScore__");
            s_levelScoreGO.AddComponent<TrainingLevelScore>();

            // Move it into the loading scene as soon as it's available so it is
            // destroyed when the scene unloads (not kept in DontDestroyOnLoad).
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;

            if (s_levelScoreGO == null)
                return;

            // If this is a scene with its own Level/LevelScore, destroy our stub
            // immediately so the real LevelScore becomes the singleton.
            if (Level.instance != null)
            {
                Object.Destroy(s_levelScoreGO);
                s_levelScoreGO = null;
                return;
            }

            // Move the stub into the loaded scene so it is cleaned up on scene unload.
            SceneManager.MoveGameObjectToScene(s_levelScoreGO, scene);

            // Fix CollectiblePhysics zero-velocity → IsNormalized assertion.
            foreach (var cp in Object.FindObjectsByType<CollectiblePhysics>(FindObjectsSortMode.None))
                cp.Restore();
        }
    }
}
