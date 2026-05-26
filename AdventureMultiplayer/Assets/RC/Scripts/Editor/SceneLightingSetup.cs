using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace AdventureMultiplayer.Editor
{
    /// <summary>
    /// Editor utility: configures scene lighting to give characters a vibrant,
    /// well-lit look instead of the flat/dull default.
    ///
    /// Usage: Adventure Multiplayer > Setup Scene Lighting
    /// Run this while the target scene is open.
    /// </summary>
    public static class SceneLightingSetup
    {
        [MenuItem("Adventure Multiplayer/Setup Scene Lighting")]
        private static void Setup()
        {
            SetupDirectionalLight();
            SetupAmbient();
            SetupFog();

            // Mark scene dirty so changes are saved.
            UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();

            Debug.Log("[SceneLightingSetup] Lighting configured. Save the scene to keep changes.");
            EditorUtility.DisplayDialog("Scene Lighting Setup",
                "Lighting configured!\n\nRemember to save the scene (Ctrl+S).", "OK");
        }

        // ── Directional Light ─────────────────────────────────────────────────

        private static void SetupDirectionalLight()
        {
            // Find the existing directional light or create one.
            var existing = Object.FindFirstObjectByType<Light>();
            Light dirLight = null;

            if (existing != null && existing.type == LightType.Directional)
            {
                dirLight = existing;
                Debug.Log("[SceneLightingSetup] Using existing Directional Light.");
            }
            else
            {
                var go = new GameObject("Directional Light");
                dirLight = go.AddComponent<Light>();
                dirLight.type = LightType.Directional;
                Debug.Log("[SceneLightingSetup] Created new Directional Light.");
            }

            // Warm sunlight angle — coming from upper-right front for nice character shading.
            dirLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            dirLight.color     = new Color(1.0f, 0.95f, 0.82f); // warm sunlight
            dirLight.intensity = 1.4f;
            dirLight.shadows   = LightShadows.Soft;
            dirLight.shadowStrength = 0.6f;

            Undo.RegisterCreatedObjectUndo(dirLight.gameObject, "Setup Directional Light");
        }

        // ── Ambient ───────────────────────────────────────────────────────────

        private static void SetupAmbient()
        {
            // Gradient ambient: warm sky above, neutral equator, cool bounce below.
            // This gives characters colour separation on top/sides/bottom.
            RenderSettings.ambientMode = AmbientMode.Trilight;

            RenderSettings.ambientSkyColor     = new Color(0.55f, 0.75f, 1.0f);  // sky blue
            RenderSettings.ambientEquatorColor  = new Color(0.6f,  0.65f, 0.6f); // neutral mid
            RenderSettings.ambientGroundColor   = new Color(0.25f, 0.3f,  0.2f); // dark ground bounce

            RenderSettings.ambientIntensity = 1.0f;
            Debug.Log("[SceneLightingSetup] Ambient set to Trilight gradient.");
        }

        // ── Fog ───────────────────────────────────────────────────────────────

        private static void SetupFog()
        {
            // Subtle distance fog to blend the ocean horizon.
            RenderSettings.fog          = true;
            RenderSettings.fogMode      = FogMode.Linear;
            RenderSettings.fogColor     = new Color(0.53f, 0.81f, 0.98f); // sky-blue fog
            RenderSettings.fogStartDistance = 40f;
            RenderSettings.fogEndDistance   = 120f;
            Debug.Log("[SceneLightingSetup] Fog configured.");
        }
    }
}
