using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Editor-only: watch every race bot at once in the GAME view. Splits the screen 2x2 —
    /// your own camera top-left, one chase camera per bot in the other three quarters, each
    /// labelled with the bot's name and route status. Tools/RC/Bot Cams/Show All Bots
    /// (Ctrl+Alt+B) toggles it; it also switches off when Play mode stops.
    /// Nothing is saved into the scene and nothing ships in a build.
    /// </summary>
    [InitializeOnLoad]
    public static class BotSpectatorCams
    {
        private const string k_prefabPath = "Assets/RC/Prefabs/Debug/BotSpectatorCam.prefab";
        private const string k_menu       = "Tools/RC/Bot Cams/Show All Bots";

        // Viewports: [0] = your camera, [1..3] = bots.
        private static readonly Rect[] k_slots =
        {
            new Rect(0f,   0.5f, 0.5f, 0.5f),
            new Rect(0.5f, 0.5f, 0.5f, 0.5f),
            new Rect(0f,   0f,   0.5f, 0.5f),
            new Rect(0.5f, 0f,   0.5f, 0.5f),
        };

        private static readonly List<BotSpectatorCamView> s_cams = new();
        private static readonly List<RaceBotBrain>        s_bots = new();
        private static Camera s_mainCam;
        private static Rect   s_mainRect;
        private static bool   s_active;
        private static double s_nextBotScan;

        static BotSpectatorCams()
        {
            EditorApplication.playModeStateChanged += s =>
            {
                if (s == PlayModeStateChange.ExitingPlayMode) Close();
            };
        }

        [MenuItem(k_menu + " %&b")]
        private static void Toggle()
        {
            if (s_active) { Close(); return; }
            if (!EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Bot Cams", "Enter Play mode first — bots only exist during a race.", "OK");
                return;
            }
            var prefab = AssetDatabase.LoadAssetAtPath<BotSpectatorCamView>(k_prefabPath);
            if (prefab == null)
            {
                EditorUtility.DisplayDialog("Bot Cams", $"Missing {k_prefabPath}.", "OK");
                return;
            }

            s_mainCam = FindMainCamera();
            if (s_mainCam != null)
            {
                s_mainRect = s_mainCam.rect;
                s_mainCam.rect = k_slots[0];
            }

            for (int i = 1; i < k_slots.Length; i++)
            {
                var cam = Object.Instantiate(prefab);
                cam.name = $"BotSpectatorCam_{i}";
                cam.gameObject.hideFlags = HideFlags.DontSave;
                cam.gameObject.SetActive(false);
                s_cams.Add(cam);
            }

            s_active = true;
            s_nextBotScan = 0;
            EditorApplication.update += Update;
            Debug.Log("[BotSpectatorCams] On — Game view split 2x2 (you + bots).");
        }

        [MenuItem(k_menu + " %&b", true)]
        private static bool ToggleValidate()
        {
            Menu.SetChecked(k_menu, s_active);
            return true;
        }

        private static void Close()
        {
            if (!s_active) return;
            s_active = false;
            EditorApplication.update -= Update;
            foreach (var c in s_cams) if (c != null) Object.Destroy(c.gameObject);
            s_cams.Clear();
            s_bots.Clear();
            if (s_mainCam != null) s_mainCam.rect = s_mainRect;
            s_mainCam = null;
            Debug.Log("[BotSpectatorCams] Off.");
        }

        private static Camera FindMainCamera()
        {
            var follow = Object.FindFirstObjectByType<NetworkCameraFollow>();
            if (follow != null && follow.TryGetComponent(out Camera c)) return c;
            return Camera.main;
        }

        private static void Update()
        {
            if (!EditorApplication.isPlaying) { Close(); return; }
            if (EditorApplication.timeSinceStartup < s_nextBotScan) return;
            s_nextBotScan = EditorApplication.timeSinceStartup + 1.0;

            // Bots spawn after the race starts (on the host only) and can despawn — keep looking.
            var found = new List<RaceBotBrain>(Object.FindObjectsByType<RaceBotBrain>(FindObjectsSortMode.None));
            found.RemoveAll(b => b == null || !b.isActiveAndEnabled);
            found.Sort((a, b) => a.BotId.CompareTo(b.BotId));

            bool same = found.Count == s_bots.Count;
            for (int i = 0; same && i < found.Count; i++) same = found[i] == s_bots[i];
            if (same) return;

            s_bots.Clear();
            s_bots.AddRange(found);
            for (int i = 0; i < s_cams.Count; i++)
            {
                var cam = s_cams[i];
                if (cam == null) continue;
                bool has = i < s_bots.Count;
                cam.gameObject.SetActive(has);
                if (has) cam.SetTarget(s_bots[i], k_slots[i + 1]);
            }
            if (s_bots.Count > 0)
                Debug.Log($"[BotSpectatorCams] Watching {Mathf.Min(s_bots.Count, s_cams.Count)} bot(s).");
        }
    }
}
