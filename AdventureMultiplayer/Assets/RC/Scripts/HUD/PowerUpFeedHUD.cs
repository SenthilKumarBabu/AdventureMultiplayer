using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Global, PUBG-style power-up feed on the left side of the gameplay HUD. Every client shows
    /// the same rows — attacks (hit/blocked/dodged) via PlayerPowerUpInventory.
    /// NotifyGlobalAttackClientRpc, and self-buff usage via NotifyGlobalSelfUseClientRpc — both
    /// broadcast with no target filter, so spectators of an event see it too, not just the
    /// participants.
    ///
    /// Setup:
    ///   - Add to a Canvas GameObject, left side.
    ///   - Assign container (a RectTransform with a Vertical Layout Group — new rows are
    ///     instantiated into it and reflow automatically).
    ///   - Assign rowTemplate (a TextMeshProUGUI child of container) — kept inactive; each
    ///     event clones it, fills the text, animates in/out, then destroys the clone.
    ///   - backgroundImage (optional; auto-resolved via GetComponent&lt;Image&gt; on this
    ///     GameObject) is kept disabled until the first row shows, so the panel doesn't render
    ///     as an empty rounded box before any power-up has been used.
    ///
    /// Each row clone lives inside its own tiny "slot" GameObject rather than directly under
    /// container. The Vertical Layout Group re-asserts every DIRECT child's X position (for
    /// alignment) on every rebuild — including rebuilds triggered by *other* rows arriving or
    /// expiring — which was snapping an in-flight slide-in back to rest the instant a second
    /// event fired close behind the first. Bots trigger power-ups close together far more often
    /// than a single human does, so this was visible almost only on bot rows. The slot is the
    /// layout-managed child (just reserves the row's height/width); the visible row animates
    /// freely as its un-managed grandchild, immune to any future rebuild.
    /// </summary>
    [AddComponentMenu("Adventure Multiplayer/HUD/Power-Up Feed HUD")]
    public class PowerUpFeedHUD : MonoBehaviour
    {
        public static PowerUpFeedHUD Instance { get; private set; }

        [SerializeField] private RectTransform     container;
        [SerializeField] private TextMeshProUGUI   rowTemplate;
        [SerializeField] private Image             backgroundImage;
        [SerializeField] private float             fadeDuration     = 0.35f;
        [SerializeField] private int               maxRows          = 5;
        [SerializeField] private float             slideDistance    = 300f;
        [SerializeField] private float             entranceDuration = 0.6f;

        private static readonly string[] k_characterNames =
            { "Gale", "Blaze", "Bolt", "Bruno", "Spike" };

        // Per-character colors so each racer's name is visually distinct in the feed —
        // must stay index-aligned with k_characterNames and match LeaderboardHUD's palette.
        private static readonly string[] k_characterColors =
            { "#4FD3FF", "#FF6B35", "#FFEA00", "#4CD964", "#C77DFF" };

        // Past-tense verbs for attack lines ("X Verb → Y").
        private static readonly Dictionary<PowerUpType, string> k_verbs = new()
        {
            { PowerUpType.StunBolt, "Zapped" },
            { PowerUpType.Swap,     "Swapped" },
            { PowerUpType.Freeze,   "Froze" },
            { PowerUpType.Rocket,   "Rocketed" },
            { PowerUpType.Banana,   "Banana'd" },
            { PowerUpType.DecoyBox, "Decoyed" },
        };

        // Self-buffs have no opponent, so they get a plain "X used Y!" line instead.
        private static readonly Dictionary<PowerUpType, string> k_selfUseMessages = new()
        {
            { PowerUpType.SpeedBoost,  "used Speed Boost!" },
            { PowerUpType.Shield,      "used Shield!" },
            { PowerUpType.Invisible,   "used Invisible!" },
            { PowerUpType.SuperCharge, "used SuperCharge!" },
        };

        private readonly Queue<TextMeshProUGUI> m_active = new();

        private void Awake()
        {
            Instance = this;

            if (container == null) container = transform as RectTransform;
            if (rowTemplate == null) rowTemplate = GetComponentInChildren<TextMeshProUGUI>(includeInactive: true);
            if (backgroundImage == null) backgroundImage = GetComponent<Image>();

            if (rowTemplate != null) rowTemplate.gameObject.SetActive(false);

            // No power-up has happened yet — the panel would otherwise show as an empty rounded
            // box (ContentSizeFitter shrinks it to just its padding with zero rows). Hidden until
            // the first row arrives; see ShowRow. Only the background Graphic is disabled, not
            // this GameObject, so the container/layout keeps working underneath.
            if (backgroundImage != null) backgroundImage.enabled = false;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Called on every client via NotifyGlobalAttackClientRpc — for a landed hit,
        /// or an attack that was blocked by Shield / dodged via Invisible (the target still
        /// deserves feed credit for surviving it).</summary>
        public void ShowAttack(ulong attackerRaceId, ulong targetRaceId, PowerUpType type, PowerUpAffectOutcome outcome)
        {
            if (container == null || rowTemplate == null) return;
            if (!k_verbs.TryGetValue(type, out string verb)) return;

            string suffix = outcome switch
            {
                PowerUpAffectOutcome.ShieldBlocked   => " (Blocked!)",
                PowerUpAffectOutcome.InvisibleDodged => " (Dodged!)",
                _ => "",
            };

            string line = $"{ResolveName(attackerRaceId)} {verb} → {ResolveName(targetRaceId)}{suffix}";
            ShowRow(line);
        }

        /// <summary>Called on every client via NotifyGlobalSelfUseClientRpc — a self-buff
        /// (SpeedBoost/Shield/Invisible/SuperCharge) with no opponent to name.</summary>
        public void ShowSelfUse(ulong userRaceId, PowerUpType type)
        {
            if (container == null || rowTemplate == null) return;
            if (!k_selfUseMessages.TryGetValue(type, out string msg)) return;

            ShowRow($"{ResolveName(userRaceId)} {msg}");
        }

        private void ShowRow(string line)
        {
            // See the class doc comment: the slot is the Vertical Layout Group's real child
            // (reserves this row's height/width in the stack); the row itself is an un-managed
            // grandchild so its slide-in can never be interrupted by a later layout rebuild.
            var slotGO   = new GameObject("FeedRowSlot", typeof(RectTransform), typeof(LayoutElement));
            var slotRect = (RectTransform)slotGO.transform;
            slotRect.SetParent(container, false);
            slotRect.SetAsLastSibling();

            // Mirror the template's own RectTransform geometry onto the slot (a fresh
            // RectTransform otherwise defaults to a 100x100 center-pivot rect) so the layout
            // group sizes/spaces the slot exactly as it would have sized the row itself —
            // regardless of whether "Control Child Size" is enabled on the group.
            var templateRect = rowTemplate.rectTransform;
            slotRect.anchorMin = templateRect.anchorMin;
            slotRect.anchorMax = templateRect.anchorMax;
            slotRect.pivot     = templateRect.pivot;
            slotRect.sizeDelta = templateRect.sizeDelta;

            var slotLayout = slotGO.GetComponent<LayoutElement>();
            slotLayout.preferredWidth  = templateRect.rect.width;
            slotLayout.preferredHeight = templateRect.rect.height;

            var row = Instantiate(rowTemplate, slotRect);
            row.gameObject.SetActive(true);
            row.text  = line;
            row.alpha = 0f;
            row.transform.localScale = Vector3.one * 0.9f;

            // Stretch the row to fill its slot (tracks whatever width/height the layout group
            // actually assigns the slot), then offset it left, off-screen, for the slide-in.
            var rowRect = row.rectTransform;
            rowRect.anchorMin = Vector2.zero;
            rowRect.anchorMax = Vector2.one;
            rowRect.offsetMin = Vector2.zero;
            rowRect.offsetMax = Vector2.zero;
            rowRect.anchoredPosition = new Vector2(-slideDistance, 0f);

            // Entrance: one smooth left-to-right slide-in — fade and scale are synced to the
            // same duration/ease as the slide so the row arrives as a single cohesive motion
            // (fast-start, gentle-settle) rather than several short, mismatched tweens.
            // Rows stay on screen indefinitely and are only removed once a 6th (maxRows + 1)
            // entry pushes the oldest one out below.
            DOTween.To(() => row.alpha, a => row.alpha = a, 1f, entranceDuration).SetEase(Ease.OutQuad);
            row.transform.DOScale(1f, entranceDuration).SetEase(Ease.OutQuint);
            rowRect.DOAnchorPosX(0f, entranceDuration).SetEase(Ease.OutQuint);

            // First-ever row: reveal the background now that there's something to show it behind.
            if (backgroundImage != null) backgroundImage.enabled = true;

            m_active.Enqueue(row);
            while (m_active.Count > maxRows)
            {
                var old = m_active.Dequeue();
                if (old == null) continue;

                DOTween.Kill(old.transform);
                DOTween.Kill(old);
                DOTween.To(() => old.alpha, a => old.alpha = a, 0f, fadeDuration)
                    .OnComplete(() =>
                    {
                        // Destroy the slot, not just the row, or its empty height keeps
                        // reserving space in the feed forever.
                        if (old != null) Destroy(old.transform.parent.gameObject);
                    });
            }
        }

        // Bots resolve through the same CharacterPicker selection as humans (they register a
        // character index on spawn — see RaceBotBrain), so they read as ordinary player names
        // here with no "(Bot)" tag, indistinguishable from a real player in the feed.
        private static string ResolveName(ulong raceId)
        {
            int    charIdx  = CharacterPicker.Instance != null ? CharacterPicker.Instance.GetSelection(raceId) : 0;
            string charName = charIdx >= 0 && charIdx < k_characterNames.Length
                ? k_characterNames[charIdx] : "Player";
            string color = charIdx >= 0 && charIdx < k_characterColors.Length
                ? k_characterColors[charIdx] : "#FFFFFF";
            string coloredName = $"<color={color}>{charName}</color>";

            ulong localId = NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : ulong.MaxValue;
            return raceId == localId ? $"{coloredName} (You)" : coloredName;
        }
    }
}
