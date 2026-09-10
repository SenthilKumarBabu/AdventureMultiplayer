using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace AdventureMultiplayer.Editor
{
    /// <summary>
    /// One-off builders for the redesigned Race HUD panels. Each panel is authored here in code,
    /// saved as a prefab under Assets/RC/Prefabs/UI/, then swapped into the DeathRun scenes.
    ///
    /// Editor tooling only — the "never AddComponent in scripts" rule is a runtime rule; building
    /// prefabs necessarily uses AddComponent.
    ///
    /// Menu: Adventure Multiplayer / HUD / ...
    /// </summary>
    public static class RaceHudBuilder
    {
        private const string PrefabDir = "Assets/RC/Prefabs/UI";

        private static readonly string[] DeathRunScenes =
        {
            "Assets/RC/Scenes/DeathRunL1.unity",
            "Assets/RC/Scenes/DeathRunL2.unity",
            "Assets/RC/Scenes/DeathRunL3.unity",
        };

        // shared sprites / assets (by GUID)
        private const string GUID_PanelSprite   = "f2cdd529622f49b4b83a636ea3bb6536"; // HudBg — rounded panel (9-slice)
        private const string GUID_PanelOutline  = "db99241f61527d543a47dc08b929ac5b"; // Hud Outline — rounded stroke (9-slice)
        private const string GUID_Crown         = "2b3c4d5e6f708192aabb2c3d4e5f6172"; // hud_crown
        private const string GUID_Circle        = "8192132435465768aabbccddeeff2233"; // hud_count_badge (filled circle)
        private const string GUID_Stopwatch     = "1a2b3c4d5e6f70819aab1c2d3e4f5061"; // hud_stopwatch
        private const string GUID_Padlock       = "5e6f708192132435aabbccddee5f64a5"; // hud_padlock
        private const string GUID_SlotFrame     = "708192132435465700bbccddeeff11c7"; // hud_slot_frame
        private const string GUID_ArrowUp       = "4d5e6f7081921324aabbccdd4e5f6394"; // hud_arrow_up
        private const string GUID_Wing          = "3c4d5e6f70819213aabbcc3d4e5f6283"; // hud_wing
        private const string GUID_Bolt          = "9213243546576879aabbccddeeff3344"; // hud_bolt
        private const string GUID_Font          = "8f586378b4e144a9851e7b34d9b748ee"; // game TMP font

        private static readonly Color NavyBg    = new(0f, 0f, 0f, 0.55f);         // pure black 55%, matches the power-up feed
        private static readonly Color Cyan      = new(1f, 1f, 1f, 0.12f);          // faint neutral edge — no blue
        private static readonly Color Strip     = new(1f, 1f, 1f, 0.06f);
        private static readonly Color TitleCol  = new(0.78f, 0.88f, 1f, 1f);
        private static readonly Color Gold      = new(1f, 0.82f, 0.30f, 1f);
        private static readonly Color RankCol   = new(0.62f, 0.70f, 0.85f, 1f);
        private static readonly Color ScoreCol  = new(0.90f, 0.95f, 1f, 1f);
        private static readonly Color YouHi     = new(0.30f, 0.78f, 1f, 0.16f);

        private static T Load<T>(string guid) where T : Object =>
            AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));

        // ─────────────────────────────────────────────────────────────────────────
        [MenuItem("Adventure Multiplayer/HUD/0 - Build + Install ALL HUD Panels")]
        public static void InstallEverything()
        {
            BuildLeaderboardPrefab();
            BuildPositionBadgePrefab();
            BuildNameplatePrefab();
            BuildRaceTimerPrefab();
            BuildPowerUpTrayPrefab();
            AssetDatabase.SaveAssets();

            InstallLeaderboard();
            InstallPositionBadge();
            InstallNameplate();
            InstallRaceTimer();
            InstallPowerUpTray();
            RestyleActionButtons();
            AddFeedHeader();
            SetupRaceControls();

            Debug.Log("[RaceHudBuilder] ALL HUD panels built + installed.");
        }

        // ─────────────────────────────────────────────────────────────────────────
        [MenuItem("Adventure Multiplayer/HUD/1 - Build Leaderboard Prefab")]
        public static void BuildLeaderboardPrefabMenu() => BuildLeaderboardPrefab();

        public static string BuildLeaderboardPrefab()
        {
            Directory.CreateDirectory(PrefabDir);
            var panelSprite  = Load<Sprite>(GUID_PanelSprite);
            var outlineSprite = Load<Sprite>(GUID_PanelOutline);
            var crownSprite  = Load<Sprite>(GUID_Crown);
            var font         = Load<TMP_FontAsset>(GUID_Font);

            // ── root ────────────────────────────────────────────────────────────
            var root = NewUI("LeaderboardPanel", null, out RectTransform rootRt);
            rootRt.anchorMin = rootRt.anchorMax = new Vector2(1, 1);
            rootRt.pivot = new Vector2(1, 1);
            rootRt.anchoredPosition = new Vector2(-24, -132);
            rootRt.sizeDelta = new Vector2(320, 0);

            var bg = root.AddComponent<Image>();
            bg.sprite = panelSprite; bg.type = Image.Type.Sliced; bg.color = NavyBg; bg.raycastTarget = false;

            var vlg = root.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(0, 0, 0, 0);
            vlg.spacing = 0;
            vlg.childControlWidth = vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

            var csf = root.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // ── header ──────────────────────────────────────────────────────────
            var header = NewUI("Header", rootRt, out RectTransform _);
            var hstrip = header.AddComponent<Image>();
            hstrip.color = Strip; hstrip.raycastTarget = false;
            var hh = header.AddComponent<HorizontalLayoutGroup>();
            hh.padding = new RectOffset(14, 14, 8, 8);
            hh.spacing = 8;
            hh.childAlignment = TextAnchor.MiddleLeft;
            hh.childControlWidth = hh.childControlHeight = true;
            hh.childForceExpandWidth = false; hh.childForceExpandHeight = true;
            SetLayoutElement(header, minH: 44);

            var crown = NewUI("Crown", header.transform as RectTransform, out RectTransform _);
            var ci = crown.AddComponent<Image>();
            ci.sprite = crownSprite; ci.color = Gold; ci.preserveAspect = true; ci.raycastTarget = false;
            SetLayoutElement(crown, prefW: 22, prefH: 22, flexW: 0);

            var title = NewText("Title", header.transform as RectTransform, font, "LIVE LEADERBOARD",
                size: 21, bold: true, col: TitleCol, align: TextAlignmentOptions.MidlineLeft);
            title.characterSpacing = 4;
            SetLayoutElement(title.gameObject, flexW: 1);

            // ── body ────────────────────────────────────────────────────────────
            var body = NewUI("Body", rootRt, out RectTransform _);
            var bh = body.AddComponent<VerticalLayoutGroup>();
            bh.padding = new RectOffset(8, 8, 5, 9);
            bh.spacing = 4;
            bh.childControlWidth = bh.childControlHeight = true;
            bh.childForceExpandWidth = true; bh.childForceExpandHeight = false;
            var bcsf = body.AddComponent<ContentSizeFitter>();
            bcsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // ── row template ────────────────────────────────────────────────────
            var row = NewUI("RowTemplate", body.transform as RectTransform, out RectTransform _);
            var band = row.AddComponent<Image>();
            band.sprite = panelSprite; band.type = Image.Type.Sliced; band.color = YouHi;
            band.raycastTarget = false; band.enabled = false;   // shown only on the local player's row
            var rh = row.AddComponent<HorizontalLayoutGroup>();
            rh.padding = new RectOffset(10, 10, 3, 3);
            rh.spacing = 8;
            rh.childAlignment = TextAnchor.MiddleLeft;
            rh.childControlWidth = rh.childControlHeight = true;
            rh.childForceExpandWidth = false; rh.childForceExpandHeight = true;
            SetLayoutElement(row, minH: 32, prefH: 32);

            var rank = NewText("RankText", row.transform as RectTransform, font, "1",
                size: 21, bold: true, col: RankCol, align: TextAlignmentOptions.Center);
            SetLayoutElement(rank.gameObject, prefW: 22, flexW: 0);

            var name = NewText("NameText", row.transform as RectTransform, font, "Name",
                size: 21, bold: false, col: Color.white, align: TextAlignmentOptions.MidlineLeft);
            name.textWrappingMode = TextWrappingModes.NoWrap;
            name.overflowMode = TextOverflowModes.Ellipsis;
            SetLayoutElement(name.gameObject, flexW: 1);

            var score = NewText("ScoreText", row.transform as RectTransform, font, "0",
                size: 20, bold: true, col: ScoreCol, align: TextAlignmentOptions.MidlineRight);
            SetLayoutElement(score.gameObject, prefW: 58, flexW: 0);

            row.SetActive(false);

            // ── outline overlay (drawn last, ignores layout) ────────────────────
            var outline = NewUI("Outline", rootRt, out RectTransform outRt);
            outRt.anchorMin = Vector2.zero; outRt.anchorMax = Vector2.one;
            outRt.offsetMin = Vector2.zero; outRt.offsetMax = Vector2.zero;
            var oi = outline.AddComponent<Image>();
            oi.sprite = outlineSprite; oi.type = Image.Type.Sliced; oi.color = Cyan; oi.raycastTarget = false;
            SetLayoutElement(outline, ignore: true);

            // ── script ──────────────────────────────────────────────────────────
            var hud = root.AddComponent<LeaderboardHUD>();
            var so = new SerializedObject(hud);
            so.FindProperty("rowsContainer").objectReferenceValue = body.transform;
            so.FindProperty("rowTemplate").objectReferenceValue   = row.transform;
            so.FindProperty("pointsPerCheckpoint").intValue        = 100;
            so.FindProperty("youHighlight").colorValue             = YouHi;
            so.ApplyModifiedPropertiesWithoutUndo();

            string path = $"{PrefabDir}/LeaderboardPanel.prefab";
            var saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            Debug.Log($"[RaceHudBuilder] Saved {path}");
            Selection.activeObject = saved;
            return path;
        }

        [MenuItem("Adventure Multiplayer/HUD/2 - Install Leaderboard Into DeathRun Scenes")]
        public static void InstallLeaderboard() =>
            InstallPrefab($"{PrefabDir}/LeaderboardPanel.prefab", "LeaderboardPanel", BuildLeaderboardPrefab);

        // ─────────────────────────────────────────────────────────────────────────
        [MenuItem("Adventure Multiplayer/HUD/3 - Build Position Badge Prefab")]
        public static void BuildPositionBadgePrefabMenu() => BuildPositionBadgePrefab();

        public static string BuildPositionBadgePrefab()
        {
            Directory.CreateDirectory(PrefabDir);
            var panelSprite   = Load<Sprite>(GUID_PanelSprite);
            var outlineSprite = Load<Sprite>(GUID_PanelOutline);
            var font          = Load<TMP_FontAsset>(GUID_Font);

            // root holds the script and sits top-right; the visual lives on "Badge"
            var root = NewUI("PositionBadge", null, out RectTransform rootRt);
            rootRt.anchorMin = rootRt.anchorMax = new Vector2(1, 1);
            rootRt.pivot = new Vector2(1, 1);
            rootRt.anchoredPosition = new Vector2(-30, -24);
            rootRt.sizeDelta = new Vector2(94, 94);

            var badge = NewUI("Badge", rootRt, out RectTransform badgeRt);
            badgeRt.anchorMin = Vector2.zero; badgeRt.anchorMax = Vector2.one;
            badgeRt.offsetMin = Vector2.zero; badgeRt.offsetMax = Vector2.zero;
            var bg = badge.AddComponent<Image>();
            bg.sprite = panelSprite; bg.type = Image.Type.Sliced; bg.color = NavyBg; bg.raycastTarget = false;
            var vlg = badge.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(4, 4, 8, 8);
            vlg.spacing = -4;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

            var ordinal = NewText("OrdinalText", badgeRt, font, "1<size=55%>st</size>",
                size: 46, bold: true, col: Color.white, align: TextAlignmentOptions.Center);
            SetLayoutElement(ordinal.gameObject, prefH: 40);

            var total = NewText("TotalText", badgeRt, font, "/4",
                size: 22, bold: true, col: new Color(0.55f, 0.80f, 1f, 0.9f), align: TextAlignmentOptions.Center);
            SetLayoutElement(total.gameObject, prefH: 18);

            var outline = NewUI("Outline", badgeRt, out RectTransform outRt);
            outRt.anchorMin = Vector2.zero; outRt.anchorMax = Vector2.one;
            outRt.offsetMin = Vector2.zero; outRt.offsetMax = Vector2.zero;
            var oi = outline.AddComponent<Image>();
            oi.sprite = outlineSprite; oi.type = Image.Type.Sliced; oi.color = Cyan; oi.raycastTarget = false;
            SetLayoutElement(outline, ignore: true);

            var hud = root.AddComponent<RacePositionHUD>();
            var so = new SerializedObject(hud);
            so.FindProperty("badge").objectReferenceValue       = badge;
            so.FindProperty("ordinalText").objectReferenceValue = ordinal;
            so.FindProperty("totalText").objectReferenceValue   = total;
            so.ApplyModifiedPropertiesWithoutUndo();

            string path = $"{PrefabDir}/PositionBadge.prefab";
            var saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            Debug.Log($"[RaceHudBuilder] Saved {path}");
            Selection.activeObject = saved;
            return path;
        }

        [MenuItem("Adventure Multiplayer/HUD/4 - Install Position Badge Into DeathRun Scenes")]
        public static void InstallPositionBadge() =>
            InstallPrefab($"{PrefabDir}/PositionBadge.prefab", "PositionPanel", BuildPositionBadgePrefab);

        // ─────────────────────────────────────────────────────────────────────────
        [MenuItem("Adventure Multiplayer/HUD/5 - Build Nameplate Prefab")]
        public static void BuildNameplatePrefabMenu() => BuildNameplatePrefab();

        public static string BuildNameplatePrefab()
        {
            Directory.CreateDirectory(PrefabDir);
            var panelSprite   = Load<Sprite>(GUID_PanelSprite);
            var outlineSprite = Load<Sprite>(GUID_PanelOutline);
            var circleSprite  = Load<Sprite>(GUID_Circle);
            var font          = Load<TMP_FontAsset>(GUID_Font);

            var root = NewUI("Nameplate", null, out RectTransform rootRt);
            rootRt.anchorMin = rootRt.anchorMax = new Vector2(0, 1);
            rootRt.pivot = new Vector2(0, 1);
            rootRt.anchoredPosition = new Vector2(20, -18);
            rootRt.sizeDelta = new Vector2(296, 62);

            var bg = root.AddComponent<Image>();
            bg.sprite = panelSprite; bg.type = Image.Type.Sliced; bg.color = NavyBg; bg.raycastTarget = false;
            var rh = root.AddComponent<HorizontalLayoutGroup>();
            rh.padding = new RectOffset(9, 12, 8, 8);
            rh.spacing = 10;
            rh.childAlignment = TextAnchor.MiddleLeft;
            rh.childControlWidth = rh.childControlHeight = true;
            rh.childForceExpandWidth = false; rh.childForceExpandHeight = true;

            // ── avatar ──────────────────────────────────────────────────────────
            var avatar = NewUI("Avatar", rootRt, out RectTransform _);
            SetLayoutElement(avatar, prefW: 44, prefH: 44, flexW: 0);
            var ring = NewUI("Ring", avatar.transform as RectTransform, out RectTransform ringRt);
            Stretch(ringRt);
            var ringImg = ring.AddComponent<Image>();
            ringImg.sprite = circleSprite; ringImg.color = Cyan; ringImg.raycastTarget = false;
            var disc = NewUI("Disc", avatar.transform as RectTransform, out RectTransform discRt);
            Stretch(discRt); discRt.offsetMin = new Vector2(3, 3); discRt.offsetMax = new Vector2(-3, -3);
            var discImg = disc.AddComponent<Image>();
            discImg.sprite = circleSprite; discImg.color = new Color(0.31f, 0.83f, 1f); discImg.raycastTarget = false;
            var initial = NewText("Initial", avatar.transform as RectTransform, font, "Ga",
                size: 24, bold: true, col: new Color(0.06f, 0.08f, 0.12f, 1f), align: TextAlignmentOptions.Center);
            Stretch(initial.rectTransform);

            // ── info column ─────────────────────────────────────────────────────
            var info = NewUI("Info", rootRt, out RectTransform _);
            var iv = info.AddComponent<VerticalLayoutGroup>();
            iv.padding = new RectOffset(0, 0, 2, 2);
            iv.spacing = 5;
            iv.childAlignment = TextAnchor.UpperLeft;
            iv.childControlWidth = iv.childControlHeight = true;
            iv.childForceExpandWidth = true; iv.childForceExpandHeight = false;
            SetLayoutElement(info, flexW: 1);

            var name = NewText("NameText", info.transform as RectTransform, font, "Gale",
                size: 24, bold: true, col: Color.white, align: TextAlignmentOptions.MidlineLeft);
            SetLayoutElement(name.gameObject, prefH: 20);

            var hpRow = NewUI("HPRow", info.transform as RectTransform, out RectTransform _);
            var hr = hpRow.AddComponent<HorizontalLayoutGroup>();
            hr.padding = new RectOffset(0, 0, 0, 0);
            hr.spacing = 6;
            hr.childAlignment = TextAnchor.MiddleLeft;
            hr.childControlWidth = hr.childControlHeight = true;
            hr.childForceExpandWidth = false; hr.childForceExpandHeight = true;
            SetLayoutElement(hpRow, prefH: 14);

            // ── health bar (Slider) ─────────────────────────────────────────────
            var bar = NewUI("HPBar", hpRow.transform as RectTransform, out RectTransform barRt);
            SetLayoutElement(bar, flexW: 1, prefH: 12);
            var barBg = bar.AddComponent<Image>();
            barBg.sprite = panelSprite; barBg.type = Image.Type.Sliced;
            barBg.color = new Color(0f, 0f, 0f, 0.55f); barBg.raycastTarget = false;
            var slider = bar.AddComponent<Slider>();
            slider.transition = Selectable.Transition.None;
            slider.interactable = false;
            slider.minValue = 0; slider.maxValue = 1; slider.value = 1;
            slider.direction = Slider.Direction.LeftToRight;

            var fillArea = NewUI("Fill Area", barRt, out RectTransform faRt);
            Stretch(faRt); faRt.offsetMin = new Vector2(2, 2); faRt.offsetMax = new Vector2(-2, -2);
            var fill = NewUI("Fill", faRt, out RectTransform fillRt);
            Stretch(fillRt);
            var fillImg = fill.AddComponent<Image>();
            fillImg.sprite = panelSprite; fillImg.type = Image.Type.Sliced;
            fillImg.color = new Color(0.24f, 0.82f, 0.30f, 0.95f); fillImg.raycastTarget = false;
            slider.fillRect = fillRt;

            var hp = NewText("HPText", hpRow.transform as RectTransform, font, "100/100",
                size: 18, bold: true, col: new Color(0.86f, 0.92f, 1f, 1f), align: TextAlignmentOptions.MidlineRight);
            SetLayoutElement(hp.gameObject, prefW: 52, flexW: 0);

            // ── outline overlay ─────────────────────────────────────────────────
            var outline = NewUI("Outline", rootRt, out RectTransform outRt);
            Stretch(outRt);
            var oi = outline.AddComponent<Image>();
            oi.sprite = outlineSprite; oi.type = Image.Type.Sliced; oi.color = Cyan; oi.raycastTarget = false;
            SetLayoutElement(outline, ignore: true);

            // ── script ──────────────────────────────────────────────────────────
            var hud = root.AddComponent<HPHUD>();
            var so = new SerializedObject(hud);
            so.FindProperty("slider").objectReferenceValue      = slider;
            so.FindProperty("fill").objectReferenceValue        = fillImg;
            so.FindProperty("hpText").objectReferenceValue      = hp;
            so.FindProperty("nameText").objectReferenceValue    = name;
            so.FindProperty("avatarDisc").objectReferenceValue  = discImg;
            so.FindProperty("initialText").objectReferenceValue = initial;
            so.ApplyModifiedPropertiesWithoutUndo();

            string path = $"{PrefabDir}/Nameplate.prefab";
            var saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            Debug.Log($"[RaceHudBuilder] Saved {path}");
            Selection.activeObject = saved;
            return path;
        }

        [MenuItem("Adventure Multiplayer/HUD/6 - Install Nameplate Into DeathRun Scenes")]
        public static void InstallNameplate() =>
            InstallPrefab($"{PrefabDir}/Nameplate.prefab", "HPPanel", BuildNameplatePrefab);

        // ─────────────────────────────────────────────────────────────────────────
        [MenuItem("Adventure Multiplayer/HUD/7 - Build Race Timer Prefab")]
        public static void BuildRaceTimerPrefabMenu() => BuildRaceTimerPrefab();

        public static string BuildRaceTimerPrefab()
        {
            Directory.CreateDirectory(PrefabDir);
            var panelSprite   = Load<Sprite>(GUID_PanelSprite);
            var outlineSprite = Load<Sprite>(GUID_PanelOutline);
            var watchSprite   = Load<Sprite>(GUID_Stopwatch);
            var font          = Load<TMP_FontAsset>(GUID_Font);

            var root = NewUI("RaceTimerBadge", null, out RectTransform rootRt);
            rootRt.anchorMin = rootRt.anchorMax = new Vector2(0.5f, 1f);
            rootRt.pivot = new Vector2(0.5f, 1f);
            rootRt.anchoredPosition = new Vector2(0, -18);
            rootRt.sizeDelta = new Vector2(138, 44);

            var badge = NewUI("Badge", rootRt, out RectTransform badgeRt);
            Stretch(badgeRt);
            var bg = badge.AddComponent<Image>();
            bg.sprite = panelSprite; bg.type = Image.Type.Sliced; bg.color = NavyBg; bg.raycastTarget = false;
            var hh = badge.AddComponent<HorizontalLayoutGroup>();
            hh.padding = new RectOffset(14, 16, 6, 6);
            hh.spacing = 7;
            hh.childAlignment = TextAnchor.MiddleCenter;
            hh.childControlWidth = hh.childControlHeight = true;
            hh.childForceExpandWidth = false; hh.childForceExpandHeight = true;

            var icon = NewUI("Icon", badgeRt, out RectTransform _);
            var ii = icon.AddComponent<Image>();
            ii.sprite = watchSprite; ii.color = new Color(0.75f, 0.90f, 1f, 1f);
            ii.preserveAspect = true; ii.raycastTarget = false;
            SetLayoutElement(icon, prefW: 20, prefH: 20, flexW: 0);

            var time = NewText("TimeText", badgeRt, font, "00:00",
                size: 19, bold: true, col: Color.white, align: TextAlignmentOptions.Center);
            SetLayoutElement(time.gameObject, flexW: 1);

            var outline = NewUI("Outline", badgeRt, out RectTransform outRt);
            Stretch(outRt);
            var oi = outline.AddComponent<Image>();
            oi.sprite = outlineSprite; oi.type = Image.Type.Sliced; oi.color = Cyan; oi.raycastTarget = false;
            SetLayoutElement(outline, ignore: true);

            var hud = root.AddComponent<RaceTimerHUD>();
            var so = new SerializedObject(hud);
            so.FindProperty("badge").objectReferenceValue    = badge;
            so.FindProperty("timeText").objectReferenceValue = time;
            so.ApplyModifiedPropertiesWithoutUndo();

            string path = $"{PrefabDir}/RaceTimerBadge.prefab";
            var saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            Debug.Log($"[RaceHudBuilder] Saved {path}");
            Selection.activeObject = saved;
            return path;
        }

        [MenuItem("Adventure Multiplayer/HUD/8 - Install Race Timer Into DeathRun Scenes")]
        public static void InstallRaceTimer() =>
            InstallPrefab($"{PrefabDir}/RaceTimerBadge.prefab", "RaceTimerBadge", BuildRaceTimerPrefab,
                fallbackParent: "RacingElements");

        // ─────────────────────────────────────────────────────────────────────────
        [MenuItem("Adventure Multiplayer/HUD/9 - Build Power-Up Tray Prefab")]
        public static void BuildPowerUpTrayPrefabMenu() => BuildPowerUpTrayPrefab();

        public static string BuildPowerUpTrayPrefab()
        {
            Directory.CreateDirectory(PrefabDir);
            var panelSprite   = Load<Sprite>(GUID_PanelSprite);
            var outlineSprite = Load<Sprite>(GUID_PanelOutline);
            var frameSprite   = Load<Sprite>(GUID_PanelSprite);
            var lockSprite    = Load<Sprite>(GUID_Padlock);
            var font          = Load<TMP_FontAsset>(GUID_Font);

            var root = NewUI("PowerUpTray", null, out RectTransform rootRt);
            rootRt.anchorMin = rootRt.anchorMax = new Vector2(0.5f, 0f);
            rootRt.pivot = new Vector2(0.5f, 0f);
            rootRt.anchoredPosition = new Vector2(0, 18);
            rootRt.sizeDelta = new Vector2(520, 190);

            var bg = root.AddComponent<Image>();
            bg.sprite = panelSprite; bg.type = Image.Type.Sliced; bg.color = NavyBg; bg.raycastTarget = false;
            var vg = root.AddComponent<VerticalLayoutGroup>();
            vg.padding = new RectOffset(10, 10, 7, 10);
            vg.spacing = 4;
            vg.childAlignment = TextAnchor.UpperCenter;
            vg.childControlWidth = vg.childControlHeight = true;
            vg.childForceExpandWidth = true; vg.childForceExpandHeight = false;

            var label = NewText("Label", rootRt, font, "POWER-UPS",
                size: 24, bold: true, col: new Color(0.55f, 0.78f, 1f, 0.85f), align: TextAlignmentOptions.Center);
            label.characterSpacing = 3;
            SetLayoutElement(label.gameObject, prefH: 14);

            var slotsRow = NewUI("SlotsRow", rootRt, out RectTransform _);
            var sr = slotsRow.AddComponent<HorizontalLayoutGroup>();
            sr.padding = new RectOffset(0, 0, 0, 0);
            sr.spacing = 8;
            sr.childAlignment = TextAnchor.MiddleCenter;
            sr.childControlWidth = sr.childControlHeight = true;
            sr.childForceExpandWidth = false; sr.childForceExpandHeight = false;
            SetLayoutElement(slotsRow, prefH: 150);

            var buttons  = new Button[3];
            var icons    = new Image[3];
            var overlays = new GameObject[3];

            for (int i = 0; i < 3; i++)
            {
                var slot = NewUI($"Slot{i}", slotsRow.transform as RectTransform, out RectTransform slotRt);
                SetLayoutElement(slot, prefW: 148, prefH: 148, flexW: 0);
                var frameImg = slot.AddComponent<Image>();
                frameImg.sprite = frameSprite; frameImg.type = Image.Type.Sliced;
                frameImg.color = new Color(0f, 0f, 0f, 0.4f);
                var btn = slot.AddComponent<Button>();
                btn.transition = Selectable.Transition.ColorTint;
                var nav = btn.navigation; nav.mode = Navigation.Mode.None; btn.navigation = nav;
                btn.targetGraphic = frameImg;

                var icon = NewUI("Icon", slotRt, out RectTransform iconRt);
                Stretch(iconRt); iconRt.offsetMin = new Vector2(9, 9); iconRt.offsetMax = new Vector2(-9, -9);
                var iconImg = icon.AddComponent<Image>();
                iconImg.raycastTarget = false; iconImg.preserveAspect = true;
                var ic = iconImg.color; ic.a = 0f; iconImg.color = ic; // hidden until an item is held

                var overlay = NewUI("EmptyOverlay", slotRt, out RectTransform ovRt);
                Stretch(ovRt); ovRt.offsetMin = new Vector2(16, 16); ovRt.offsetMax = new Vector2(-16, -16);
                var lockImg = overlay.AddComponent<Image>();
                lockImg.sprite = lockSprite; lockImg.raycastTarget = false; lockImg.preserveAspect = true;
                lockImg.color = new Color(0.45f, 0.55f, 0.70f, 0.85f);

                buttons[i] = btn; icons[i] = iconImg; overlays[i] = overlay;
            }

            var outline = NewUI("Outline", rootRt, out RectTransform outRt);
            Stretch(outRt);
            var oi = outline.AddComponent<Image>();
            oi.sprite = outlineSprite; oi.type = Image.Type.Sliced; oi.color = Cyan; oi.raycastTarget = false;
            SetLayoutElement(outline, ignore: true);

            var hud = root.AddComponent<PowerUpSlotHUD>();
            var so = new SerializedObject(hud);
            var slotsProp = so.FindProperty("slots");
            slotsProp.arraySize = 3;
            for (int i = 0; i < 3; i++)
            {
                var el = slotsProp.GetArrayElementAtIndex(i);
                el.FindPropertyRelative("button").objectReferenceValue       = buttons[i];
                el.FindPropertyRelative("icon").objectReferenceValue         = icons[i];
                el.FindPropertyRelative("emptyOverlay").objectReferenceValue = overlays[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            string path = $"{PrefabDir}/PowerUpTray.prefab";
            var saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            Debug.Log($"[RaceHudBuilder] Saved {path}");
            Selection.activeObject = saved;
            return path;
        }

        [MenuItem("Adventure Multiplayer/HUD/10 - Install Power-Up Tray Into DeathRun Scenes")]
        public static void InstallPowerUpTray() =>
            InstallPrefab($"{PrefabDir}/PowerUpTray.prefab", "PowerUpPanel", BuildPowerUpTrayPrefab,
                keepPosition: false, transfer: TransferPowerUpIcons);

        // ─────────────────────────────────────────────────────────────────────────
        [MenuItem("Adventure Multiplayer/HUD/11 - Restyle Action Buttons (Glide / Jump)")]
        public static void RestyleActionButtons()
        {
            var circle = Load<Sprite>(GUID_Circle);
            var arrow  = Load<Sprite>(GUID_ArrowUp);
            var wing   = Load<Sprite>(GUID_Wing);
            var font   = Load<TMP_FontAsset>(GUID_Font);

            // 1. the shared VirtualGamepad prefab — L1 / L3 / Obstacle scenes reference it
            const string prefabPath = "Assets/RC/Prefabs/UI/VirtualGamepad.prefab";
            var contents = PrefabUtility.LoadPrefabContents(prefabPath);
            RestyleActionButton(FindRecursive(contents.transform, "Jump"),    true,  circle, arrow, wing, font);
            RestyleActionButton(FindRecursive(contents.transform, "Ability"), false, circle, arrow, wing, font);
            PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
            PrefabUtility.UnloadPrefabContents(contents);
            Debug.Log("[RaceHudBuilder] Restyled VirtualGamepad.prefab action buttons");

            // 2. DeathRunL2 has an unpacked copy of the rig
            var scene = EditorSceneManager.OpenScene("Assets/RC/Scenes/DeathRunL2.unity", OpenSceneMode.Single);
            bool touched = false;
            foreach (var root in scene.GetRootGameObjects())
            {
                var j = FindRecursive(root.transform, "Jump");
                var a = FindRecursive(root.transform, "Ability");
                if (j != null && j.GetComponent<Button>() != null) { RestyleActionButton(j, true,  circle, arrow, wing, font); touched = true; }
                if (a != null && a.GetComponent<Button>() != null) { RestyleActionButton(a, false, circle, arrow, wing, font); touched = true; }
            }
            if (touched)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("[RaceHudBuilder] Restyled DeathRunL2 action buttons");
            }
        }

        private static void RestyleActionButton(GameObject btn, bool isJump,
            Sprite circle, Sprite arrow, Sprite wing, TMP_FontAsset font)
        {
            if (btn == null) return;
            var btnRt = btn.GetComponent<RectTransform>();

            if (btn.TryGetComponent(out Image img))
            {
                img.sprite = circle;
                img.type = Image.Type.Simple;
                img.preserveAspect = true;
                img.color = isJump ? new Color(1f, 0.55f, 0.14f, 0.95f)
                                   : new Color(0.16f, 0.55f, 1f, 0.95f);
            }

            var iconT = btn.transform.Find("Icon");
            GameObject iconGo = iconT != null ? iconT.gameObject : NewUI("Icon", btnRt, out RectTransform _);
            iconGo.layer = btn.layer;
            var iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = iconRt.anchorMax = new Vector2(0.5f, 0.5f);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.anchoredPosition = new Vector2(0, 7);
            iconRt.sizeDelta = new Vector2(52, 52);
            var iconImg = iconGo.GetComponent<Image>() ?? iconGo.AddComponent<Image>();
            iconImg.sprite = isJump ? arrow : wing;
            iconImg.color = Color.white;
            iconImg.raycastTarget = false;
            iconImg.preserveAspect = true;

            var labelT = btn.transform.Find("Label");
            if (labelT != null)
            {
                var lrt = labelT.GetComponent<RectTransform>();
                lrt.anchorMin = new Vector2(0, 0);
                lrt.anchorMax = new Vector2(1, 0);
                lrt.pivot = new Vector2(0.5f, 0);
                lrt.sizeDelta = new Vector2(0, 22);
                lrt.anchoredPosition = new Vector2(0, 12);
                if (labelT.TryGetComponent(out TextMeshProUGUI tmp))
                {
                    tmp.enableAutoSizing = false;
                    tmp.fontSize = 24;
                    tmp.fontStyle = FontStyles.Bold;
                    tmp.alignment = TextAlignmentOptions.Center;
                    tmp.color = Color.white;
                    if (font != null) tmp.font = font;
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        [MenuItem("Adventure Multiplayer/HUD/13 - Setup Reset + Exit Buttons")]
        public static void SetupRaceControls()
        {
            var panelSprite = Load<Sprite>(GUID_PanelSprite);
            var font         = Load<TMP_FontAsset>(GUID_Font);

            foreach (string scenePath in DeathRunScenes)
            {
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

                var reset = FindInScene(scene, "ResetButton");
                if (reset == null) { Debug.LogWarning($"[RaceHudBuilder] no ResetButton in {scenePath}"); continue; }

                Transform parent = reset.transform.parent;
                StyleControlButton(reset, panelSprite, font, "RESET", new Color(1f, 0.72f, 0.30f, 1f),
                    new Vector2(-206, -22));

                var exit = FindInScene(scene, "ExitButton");
                if (exit == null)
                {
                    exit = Object.Instantiate(reset, parent);
                    exit.name = "ExitButton";
                    var dupHud = exit.GetComponent<ResetButtonHUD>();
                    if (dupHud != null) Object.DestroyImmediate(dupHud);
                }
                StyleControlButton(exit, panelSprite, font, "EXIT", new Color(1f, 0.42f, 0.42f, 1f),
                    new Vector2(-132, -22));

                var popup = BuildExitConfirmPopupInScene(scene, exit, panelSprite, font);

                var hud = reset.GetComponent<ResetButtonHUD>();
                if (hud != null)
                {
                    var so = new SerializedObject(hud);
                    so.FindProperty("resetButton").objectReferenceValue = reset.GetComponent<Button>();
                    so.FindProperty("exitButton").objectReferenceValue  = exit.GetComponent<Button>();
                    so.FindProperty("lobbySceneName").stringValue        = "Lobby";
                    if (popup.root  != null) so.FindProperty("confirmExitPopup").objectReferenceValue      = popup.root;
                    if (popup.yes   != null) so.FindProperty("confirmExitYesButton").objectReferenceValue  = popup.yes;
                    if (popup.no    != null) so.FindProperty("confirmExitNoButton").objectReferenceValue   = popup.no;
                    if (popup.panel != null) so.FindProperty("confirmExitPanel").objectReferenceValue      = popup.panel;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[RaceHudBuilder] Reset + Exit buttons + confirm popup set up in {scenePath}");
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        [MenuItem("Adventure Multiplayer/HUD/14 - Build + Wire Exit Confirm Popup")]
        public static void BuildExitConfirmPopupMenu()
        {
            var panelSprite = Load<Sprite>(GUID_PanelSprite);
            var font         = Load<TMP_FontAsset>(GUID_Font);

            foreach (string scenePath in DeathRunScenes)
            {
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

                var exit = FindInScene(scene, "ExitButton");
                if (exit == null) { Debug.LogWarning($"[RaceHudBuilder] no ExitButton in {scenePath}"); continue; }

                var popup = BuildExitConfirmPopupInScene(scene, exit, panelSprite, font);

                var reset = FindInScene(scene, "ResetButton");
                var hud   = reset != null ? reset.GetComponent<ResetButtonHUD>() : null;
                if (hud != null && popup.root != null)
                {
                    var so = new SerializedObject(hud);
                    so.FindProperty("confirmExitPopup").objectReferenceValue     = popup.root;
                    so.FindProperty("confirmExitYesButton").objectReferenceValue = popup.yes;
                    so.FindProperty("confirmExitNoButton").objectReferenceValue  = popup.no;
                    so.FindProperty("confirmExitPanel").objectReferenceValue     = popup.panel;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[RaceHudBuilder] Exit confirm popup built + wired in {scenePath}");
            }
        }

        /// <summary>
        /// Creates the full-screen "Are you sure you want to exit?" popup (dim overlay + centre
        /// panel + YES / NO buttons) as a child of the Canvas that holds the Exit button. Starts
        /// inactive — ResetButtonHUD shows it when Exit is pressed. Idempotent: an existing
        /// ExitConfirmPopup is removed and rebuilt.
        /// </summary>
        private static (GameObject root, Button yes, Button no, RectTransform panel)
            BuildExitConfirmPopupInScene(UnityEngine.SceneManagement.Scene scene, GameObject exitButton,
                                         Sprite panelSprite, TMP_FontAsset font)
        {
            var canvas = exitButton.GetComponentInParent<Canvas>();
            Transform parent = canvas != null ? canvas.transform : exitButton.transform.root;

            var existing = FindInScene(scene, "ExitConfirmPopup");
            if (existing != null) Object.DestroyImmediate(existing);

            // ── dim full-screen overlay (blocks all clicks behind it) ────────────
            var root = NewUI("ExitConfirmPopup", parent as RectTransform, out RectTransform rootRt);
            Stretch(rootRt);
            var dim = root.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.62f);
            dim.raycastTarget = true; // eat clicks so the race HUD underneath can't be touched
            root.transform.SetAsLastSibling();

            // ── centre panel ────────────────────────────────────────────────────
            var panel = NewUI("Panel", rootRt, out RectTransform panelRt);
            panelRt.anchorMin = panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.anchoredPosition = Vector2.zero;
            panelRt.sizeDelta = new Vector2(520, 240);
            var pbg = panel.AddComponent<Image>();
            pbg.sprite = panelSprite; pbg.type = Image.Type.Sliced;
            pbg.color = new Color(0.05f, 0.06f, 0.09f, 0.98f);
            var pv = panel.AddComponent<VerticalLayoutGroup>();
            pv.padding = new RectOffset(28, 28, 26, 24);
            pv.spacing = 22;
            pv.childAlignment = TextAnchor.MiddleCenter;
            pv.childControlWidth = pv.childControlHeight = true;
            pv.childForceExpandWidth = true; pv.childForceExpandHeight = false;

            var msg = NewText("Message", panelRt, font, "Are you sure you want to exit?",
                size: 30, bold: true, col: Color.white, align: TextAlignmentOptions.Center);
            msg.textWrappingMode = TextWrappingModes.Normal;
            SetLayoutElement(msg.gameObject, prefH: 78, flexW: 1);

            // ── button row ──────────────────────────────────────────────────────
            var rowGo = NewUI("Buttons", panelRt, out RectTransform _);
            var rl = rowGo.AddComponent<HorizontalLayoutGroup>();
            rl.padding = new RectOffset(0, 0, 0, 0);
            rl.spacing = 22;
            rl.childAlignment = TextAnchor.MiddleCenter;
            rl.childControlWidth = rl.childControlHeight = true;
            rl.childForceExpandWidth = true; rl.childForceExpandHeight = true;
            SetLayoutElement(rowGo, prefH: 64);

            var yes = MakeConfirmButton("YesButton", rowGo.transform as RectTransform, font, "YES",
                new Color(1f, 0.42f, 0.42f, 1f));
            var no = MakeConfirmButton("NoButton", rowGo.transform as RectTransform, font, "NO",
                new Color(0.30f, 0.55f, 0.35f, 1f));

            root.SetActive(false);
            return (root, yes, no, panelRt);
        }

        private static Button MakeConfirmButton(string name, RectTransform parent, TMP_FontAsset font,
            string label, Color tint)
        {
            var go = NewUI(name, parent, out RectTransform _);
            SetLayoutElement(go, prefW: 200, prefH: 60, flexW: 1);
            var img = go.AddComponent<Image>();
            img.color = tint;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.ColorTint;
            var nav = btn.navigation; nav.mode = Navigation.Mode.None; btn.navigation = nav;

            var t = NewText("Label", go.transform as RectTransform, font, label,
                size: 26, bold: true, col: Color.white, align: TextAlignmentOptions.Center);
            Stretch(t.rectTransform);
            return btn;
        }

        private static void StyleControlButton(GameObject go, Sprite panel, TMP_FontAsset font,
            string label, Color labelCol, Vector2 anchoredPos)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 1);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(66, 40);

            if (go.TryGetComponent(out Image img))
            {
                img.sprite = panel; img.type = Image.Type.Sliced;
                img.color = NavyBg;
            }

            var labelT = go.transform.Find("Label");
            if (labelT != null && labelT.TryGetComponent(out TextMeshProUGUI tmp))
            {
                tmp.text = label;
                tmp.font = font != null ? font : tmp.font;
                tmp.enableAutoSizing = false;
                tmp.fontSize = 24;
                tmp.fontStyle = FontStyles.Bold;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = labelCol;
                var lrt = labelT.GetComponent<RectTransform>();
                lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
                lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        [MenuItem("Adventure Multiplayer/HUD/12 - Add Feed Header To DeathRun Scenes")]
        public static void AddFeedHeader()
        {
            const float H = 26f; // header strip height
            var boltSprite = Load<Sprite>(GUID_Bolt);
            var font       = Load<TMP_FontAsset>(GUID_Font);

            foreach (string scenePath in DeathRunScenes)
            {
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

                var panel = FindInScene(scene, "PowerUpFeedPanel");
                if (panel == null) { Debug.LogWarning($"[RaceHudBuilder] no PowerUpFeedPanel in {scenePath}"); continue; }
                if (FindRecursive(panel.transform, "FeedHeader") != null)
                {
                    Debug.Log($"[RaceHudBuilder] FeedHeader already present in {scenePath} — skipped");
                    continue;
                }

                var panelRt = panel.GetComponent<RectTransform>();
                panelRt.sizeDelta = new Vector2(panelRt.sizeDelta.x, panelRt.sizeDelta.y + H);

                var vp = FindRecursive(panel.transform, "Viewport");
                if (vp != null)
                {
                    var r = vp.GetComponent<RectTransform>();
                    r.sizeDelta        = new Vector2(r.sizeDelta.x, r.sizeDelta.y - H);
                    r.anchoredPosition = new Vector2(r.anchoredPosition.x, r.anchoredPosition.y - H / 2f);
                }
                var sb = FindRecursive(panel.transform, "Scrollbar");
                if (sb != null)
                {
                    var r = sb.GetComponent<RectTransform>();
                    r.sizeDelta        = new Vector2(r.sizeDelta.x, r.sizeDelta.y - H);
                    r.anchoredPosition = new Vector2(r.anchoredPosition.x, r.anchoredPosition.y - H);
                }

                var header = NewUI("FeedHeader", panelRt, out RectTransform hRt);
                hRt.anchorMin = new Vector2(0, 1);
                hRt.anchorMax = new Vector2(1, 1);
                hRt.pivot = new Vector2(0.5f, 1);
                hRt.anchoredPosition = Vector2.zero;
                hRt.sizeDelta = new Vector2(0, H);
                var strip = header.AddComponent<Image>();
                strip.color = new Color(1f, 1f, 1f, 0.05f);
                strip.raycastTarget = false;
                var hh = header.AddComponent<HorizontalLayoutGroup>();
                hh.padding = new RectOffset(14, 14, 4, 4);
                hh.spacing = 7;
                hh.childAlignment = TextAnchor.MiddleLeft;
                hh.childControlWidth = hh.childControlHeight = true;
                hh.childForceExpandWidth = false; hh.childForceExpandHeight = true;

                var icon = NewUI("HeaderIcon", hRt, out RectTransform _);
                var ii = icon.AddComponent<Image>();
                ii.sprite = boltSprite; ii.color = new Color(0.55f, 0.82f, 1f, 1f);
                ii.preserveAspect = true; ii.raycastTarget = false;
                SetLayoutElement(icon, prefW: 13, prefH: 13, flexW: 0);

                var label = NewText("HeaderLabel", hRt, font, "POWER-UP FEED",
                    size: 10.5f, bold: true, col: new Color(0.60f, 0.80f, 1f, 0.9f),
                    align: TextAlignmentOptions.MidlineLeft);
                label.characterSpacing = 2;
                SetLayoutElement(label.gameObject, flexW: 1);

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[RaceHudBuilder] Added FeedHeader to {scenePath}");
            }
        }

        private static void TransferPowerUpIcons(GameObject oldGo, GameObject newInst)
        {
            var oldHud = oldGo.GetComponent<PowerUpSlotHUD>();
            var newHud = newInst.GetComponent<PowerUpSlotHUD>();
            if (oldHud == null || newHud == null) { Debug.LogWarning("[RaceHudBuilder] PowerUpSlotHUD missing during transfer"); return; }

            var src = new SerializedObject(oldHud);
            var dst = new SerializedObject(newHud);
            dst.CopyFromSerializedProperty(src.FindProperty("powerUpIcons"));
            dst.CopyFromSerializedProperty(src.FindProperty("emptySlotSprite"));
            dst.ApplyModifiedPropertiesWithoutUndo();
        }

        // ─────────────────────────────────────────────────────────────────────────
        /// <summary>
        /// Swaps a prefab into every DeathRun scene. If an object named <paramref name="sceneObjName"/>
        /// exists it is replaced in place (keeping parent / sibling index / anchored position).
        /// Otherwise, when <paramref name="fallbackParent"/> is given, the prefab is added fresh as a
        /// child of that object at the prefab's own default position. Re-running is idempotent.
        /// </summary>
        private static void InstallPrefab(string prefabPath, string sceneObjName,
            System.Func<string> builder, string fallbackParent = null, bool keepPosition = true,
            System.Action<GameObject, GameObject> transfer = null)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) { prefabPath = builder(); prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath); }
            if (prefab == null) { Debug.LogError($"[RaceHudBuilder] could not build/load {prefabPath}"); return; }

            foreach (string scenePath in DeathRunScenes)
            {
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

                // Match the original scene object on the first run, and the prefab's own root
                // name on every re-run (so re-installing is idempotent).
                var old = FindInScene(scene, sceneObjName) ?? FindInScene(scene, prefab.name);
                Transform parent;
                int       siblingIdx  = -1;
                Vector2   anchoredPos  = Vector2.zero;
                bool      hadOld       = old != null;

                GameObject oldForTransfer = null;
                if (hadOld)
                {
                    parent      = old.transform.parent;
                    siblingIdx  = old.transform.GetSiblingIndex();
                    var oldRt   = old.GetComponent<RectTransform>();
                    if (oldRt != null) anchoredPos = oldRt.anchoredPosition;
                    if (transfer != null) oldForTransfer = old;
                }
                else if (fallbackParent != null)
                {
                    var p = FindInScene(scene, fallbackParent);
                    if (p == null) { Debug.LogWarning($"[RaceHudBuilder] no '{fallbackParent}' in {scenePath}"); continue; }
                    parent = p.transform;
                }
                else
                {
                    Debug.LogWarning($"[RaceHudBuilder] no '{sceneObjName}' in {scenePath}"); continue;
                }

                var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                inst.transform.SetParent(parent, false);
                if (siblingIdx >= 0) inst.transform.SetSiblingIndex(siblingIdx);
                if (hadOld && keepPosition)
                {
                    var rt = inst.GetComponent<RectTransform>();
                    if (rt != null) rt.anchoredPosition = anchoredPos;
                }

                if (oldForTransfer != null) transfer(oldForTransfer, inst);
                if (hadOld) Object.DestroyImmediate(old);

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[RaceHudBuilder] Installed {prefab.name} into {scenePath}");
            }
        }

        // ── helpers ─────────────────────────────────────────────────────────────
        private static GameObject NewUI(string name, RectTransform parent, out RectTransform rt)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5; // UI
            rt = go.GetComponent<RectTransform>();
            if (parent != null) rt.SetParent(parent, false);
            rt.localScale = Vector3.one;
            return go;
        }

        private static TextMeshProUGUI NewText(string name, RectTransform parent, TMP_FontAsset font,
            string text, float size, bool bold, Color col, TextAlignmentOptions align)
        {
            var go = NewUI(name, parent, out RectTransform _);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.font = font;
            t.text = text;
            t.fontSize = size;
            t.enableAutoSizing = false;
            t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            t.color = col;
            t.alignment = align;
            t.raycastTarget = false;
            t.richText = true;
            return t;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void SetLayoutElement(GameObject go, float minH = -1, float prefW = -1,
            float prefH = -1, float flexW = -1, bool ignore = false)
        {
            var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            le.ignoreLayout    = ignore;
            le.minHeight       = minH;
            le.preferredWidth  = prefW;
            le.preferredHeight = prefH;
            le.flexibleWidth   = flexW;
        }

        private static GameObject FindInScene(UnityEngine.SceneManagement.Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var found = FindRecursive(root.transform, name);
                if (found != null) return found;
            }
            return null;
        }

        private static GameObject FindRecursive(Transform t, string name)
        {
            if (t.name == name) return t.gameObject;
            foreach (Transform c in t)
            {
                var f = FindRecursive(c, name);
                if (f != null) return f;
            }
            return null;
        }
    }
}
