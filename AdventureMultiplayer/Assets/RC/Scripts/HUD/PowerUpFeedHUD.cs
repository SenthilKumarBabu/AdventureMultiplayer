using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
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
    /// The feed is one fixed-size panel with a single semi-transparent black background. Rows live
    /// inside a ScrollRect: newest row is appended at the bottom, the view shows the last few, and
    /// the player can drag / use the scrollbar to read older lines. A new row only auto-scrolls to
    /// the bottom if the player was already at the bottom (standard chat behaviour).
    ///
    /// Scene hierarchy this script expects (all auto-resolved by name, no Inspector wiring needed):
    ///   PowerUpFeedPanel  — Image (bg), ScrollRect, this component
    ///   ├── Viewport      — Image + Mask (clips the rows)
    ///   │   └── Content   — VerticalLayoutGroup + ContentSizeFitter  (== container)
    ///   │       └── RowTemplate  — TextMeshProUGUI, inactive; cloned per row
    ///   │           └── Accent   — gold left bar, inactive; shown on the local player's own rows
    ///   ├── Scrollbar     — standard uGUI Scrollbar (Sliding Area / Handle)
    ///   └── FeedArrowToggle — one arrow tab on the right edge, vertically centred. Tapping it
    ///                         collapses / expands the feed; its sprite swaps between
    ///                         arrowExpandedSprite ("‹", tap to collapse) and arrowCollapsedSprite
    ///                         ("›", tap to expand). Made a raycast target at runtime, clicks
    ///                         bubble up to OnPointerClick — no Button component needed.
    ///
    /// Each row clone lives inside its own tiny "slot" GameObject rather than directly under
    /// container. The Vertical Layout Group re-asserts every DIRECT child's X position on every
    /// rebuild — including rebuilds triggered by other rows arriving or expiring — which was
    /// snapping an in-flight slide-in back to rest the instant a second event fired close behind
    /// the first. The slot is the layout-managed child (reserves the row's height/width); the
    /// visible row animates freely as its un-managed grandchild, immune to any future rebuild.
    /// </summary>
    [AddComponentMenu("Adventure Multiplayer/HUD/Power-Up Feed HUD")]
    public class PowerUpFeedHUD : MonoBehaviour, IPointerClickHandler
    {
        public static PowerUpFeedHUD Instance { get; private set; }

        [SerializeField] private RectTransform     container;          // the ScrollRect Content
        [SerializeField] private TextMeshProUGUI   rowTemplate;
        [SerializeField] private Image             backgroundImage;
        [SerializeField] private float             fadeDuration     = 0.35f;
        [SerializeField] private int               maxRows          = 30;   // history buffer, not visible count
        [SerializeField] private float             slideDistance    = 220f;
        [SerializeField] private float             entranceDuration = 0.5f;

        [Header("Scroll")]
        [SerializeField] private ScrollRect        scrollRect;
        [SerializeField] private RectTransform     viewport;
        [SerializeField] private RectTransform     scrollbarRect;

        [Header("Show / Hide")]
        [SerializeField] private RectTransform     arrowToggle;   // the button (always visible)
        [SerializeField] private Image             arrowGlyph;    // the arrow icon inside it (sprite swaps)
        [SerializeField] private Sprite            arrowExpandedSprite;   // "‹" — feed shown, tap to hide
        [SerializeField] private Sprite            arrowCollapsedSprite;  // "›" — feed hidden, tap to show
        [SerializeField] private bool              startCollapsed;
        [SerializeField] private float             slideDuration = 0.35f; // right↔left slide time
        [SerializeField] private float             hiddenPeek    = 8f;    // px of the button kept on-screen when hidden

        [Header("Own-row highlight")]
        [SerializeField] private Color             accentHighlightColor = new(1f, 0.82f, 0.28f, 1f);

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

        private bool          m_collapsed;
        private Image         m_arrowImage;
        private LayoutElement m_contentFloor;   // keeps Content >= viewport tall so rows sit at the bottom
        private RectTransform m_panelRect;
        private float         m_shownX;         // panel anchoredPosition.x when the feed is visible
        private float         m_hiddenX;        // ...when slid off to the left, only the button peeking
        private bool?         m_shown;          // null until the first ApplyFeedState

        private void Awake()
        {
            Instance = this;

            if (container == null)   container   = transform.Find("Viewport/Content") as RectTransform;
            if (container == null)   container   = transform as RectTransform;
            if (rowTemplate == null) rowTemplate = GetComponentInChildren<TextMeshProUGUI>(includeInactive: true);
            if (backgroundImage == null) backgroundImage = GetComponent<Image>();
            if (scrollRect == null)  scrollRect  = GetComponent<ScrollRect>();
            if (viewport == null)      viewport      = transform.Find("Viewport") as RectTransform;
            if (scrollbarRect == null) scrollbarRect = transform.Find("Scrollbar") as RectTransform;
            if (arrowToggle == null)   arrowToggle   = transform.Find("FeedArrowToggle") as RectTransform;
            if (arrowGlyph == null && arrowToggle != null)
            {
                Transform g = arrowToggle.Find("ArrowGlyph");
                if (g != null) arrowGlyph = g.GetComponent<Image>();
            }

            if (rowTemplate != null) rowTemplate.gameObject.SetActive(false);
            if (container != null) m_contentFloor = container.GetComponent<LayoutElement>();

            if (arrowToggle != null && arrowToggle.TryGetComponent(out Image toggleBg))
                toggleBg.raycastTarget = true;
            m_arrowImage = arrowGlyph;
            SetActiveSafe(arrowToggle, true); // the button is always visible

            // The feed slides out to the left until only the button peeks back in at the screen
            // edge. Compute that resting X from the button's own geometry so it lands flush left
            // instead of hanging in the middle.
            m_panelRect = transform as RectTransform;
            m_shownX    = m_panelRect != null ? m_panelRect.anchoredPosition.x : 20f;
            float panelW = m_panelRect != null ? m_panelRect.rect.width : 460f;
            float btnW   = arrowToggle != null ? arrowToggle.rect.width      : 48f;
            float btnX   = arrowToggle != null ? arrowToggle.anchoredPosition.x : 0f;
            float btnPiv = arrowToggle != null ? arrowToggle.pivot.x         : 0.5f;
            m_hiddenX = hiddenPeek - panelW - btnX + btnW * btnPiv;

            Debug.Log($"[PowerUpFeed] Awake — container={container != null}, viewport={viewport != null}, " +
                      $"scrollbar={scrollbarRect != null}, arrowToggle={arrowToggle != null}, glyph={arrowGlyph != null}, " +
                      $"bg={backgroundImage != null}, shownX={m_shownX}, hiddenX={m_hiddenX}");

            m_collapsed = startCollapsed;
            ApplyFeedState(animate: false);
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
            ShowRow(line, IsLocal(attackerRaceId) || IsLocal(targetRaceId));
        }

        /// <summary>Called on every client via NotifyGlobalSelfUseClientRpc — a self-buff
        /// (SpeedBoost/Shield/Invisible/SuperCharge) with no opponent to name.</summary>
        public void ShowSelfUse(ulong userRaceId, PowerUpType type)
        {
            if (container == null || rowTemplate == null) return;
            if (!k_selfUseMessages.TryGetValue(type, out string msg)) return;

            ShowRow($"{ResolveName(userRaceId)} {msg}", IsLocal(userRaceId));
        }

        /// <summary>Arrow-tab tap. Clicks anywhere else on the panel are ignored — the background
        /// Image isn't a raycast target and row text has its raycast target cleared in ShowRow,
        /// so the arrow tab is the only child that forwards a click here.</summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            GameObject hit = eventData.pointerCurrentRaycast.gameObject;
            if (hit == null || arrowToggle == null) return;
            if (hit == arrowToggle.gameObject) ToggleCollapsed();
        }

        /// <summary>Public so other HUD code (or a debug key) can drive it too.</summary>
        public void ToggleCollapsed() => SetCollapsed(!m_collapsed);

        public void SetCollapsed(bool collapsed)
        {
            if (m_collapsed == collapsed) return;
            m_collapsed = collapsed;
            ApplyFeedState(animate: true);
        }

        // Feed is visible only when not collapsed AND at least one log exists. Otherwise it sits
        // slid off to the left with just the button peeking in at the screen edge. The chrome
        // (background, scroll view, scrollbar) rides along during the slide and switches off once
        // the feed is fully out — nothing empty is ever left on screen.
        private void ApplyFeedState(bool animate)
        {
            bool shown = !m_collapsed && m_active.Count > 0;

            if (m_arrowImage != null)
            {
                Sprite s = shown ? arrowExpandedSprite : arrowCollapsedSprite;
                if (s != null) m_arrowImage.sprite = s;
            }

            if (m_shown == shown) return;   // already in the right place
            m_shown = shown;

            if (m_panelRect == null) return;
            float targetX = shown ? m_shownX : m_hiddenX;
            DOTween.Kill(m_panelRect);

            if (shown) SetChromeActive(true); // reveal before sliding in

            if (animate)
            {
                m_panelRect.DOAnchorPosX(targetX, slideDuration)
                           .SetEase(shown ? Ease.OutQuart : Ease.InQuart)
                           .SetLink(gameObject)
                           .OnComplete(() => { if (!shown) SetChromeActive(false); });
            }
            else
            {
                Vector2 p = m_panelRect.anchoredPosition;
                p.x = targetX;
                m_panelRect.anchoredPosition = p;
                SetChromeActive(shown);
            }
        }

        private void SetChromeActive(bool on)
        {
            if (backgroundImage != null) backgroundImage.enabled = on;
            SetActiveSafe(viewport,      on);
            SetActiveSafe(scrollbarRect, on);
        }

        private static void SetActiveSafe(Component c, bool active)
        {
            if (c != null && c.gameObject.activeSelf != active) c.gameObject.SetActive(active);
        }

        private void ShowRow(string line, bool highlight)
        {
            bool stick = ShouldStickToBottom();

            // The slot is the Vertical Layout Group's real child (reserves this row's height in
            // the stack); the row itself is an un-managed grandchild so its slide-in can never be
            // interrupted by a later layout rebuild.
            var slotGO   = new GameObject("FeedRowSlot", typeof(RectTransform), typeof(LayoutElement));
            var slotRect = (RectTransform)slotGO.transform;
            slotRect.SetParent(container, false);
            slotRect.SetAsLastSibling(); // newest at the bottom of the stack

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
            row.raycastTarget = false; // rows never eat clicks/drags — keeps scroll + arrow working
            row.text  = line;
            row.alpha = 0f;
            row.transform.localScale = Vector3.one * 0.9f;

            var accent = row.transform.Find("Accent");
            if (accent != null)
            {
                accent.gameObject.SetActive(highlight);
                if (highlight && accent.TryGetComponent(out Image accentImg))
                    accentImg.color = accentHighlightColor;
            }

            var rowRect = row.rectTransform;
            rowRect.anchorMin = Vector2.zero;
            rowRect.anchorMax = Vector2.one;
            rowRect.offsetMin = Vector2.zero;
            rowRect.offsetMax = Vector2.zero;
            rowRect.anchoredPosition = new Vector2(-slideDistance, 0f);

            // Entrance: one smooth left-to-right slide-in, fade + scale synced to the same
            // duration/ease so the row arrives as a single cohesive motion.
            DOTween.To(() => row.alpha, a => row.alpha = a, 1f, entranceDuration).SetEase(Ease.OutQuad);
            row.transform.DOScale(1f, entranceDuration).SetEase(Ease.OutQuint);
            rowRect.DOAnchorPosX(0f, entranceDuration).SetEase(Ease.OutQuint);

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

            ApplyFeedState(animate: true);   // first row slides the feed in from the left
            if (stick) StickToBottomDeferred().Forget();
        }

        private bool ShouldStickToBottom()
        {
            if (scrollRect == null || viewport == null || container == null) return true;
            if (container.rect.height <= viewport.rect.height + 1f) return true; // not overflowing yet
            return scrollRect.verticalNormalizedPosition <= 0.05f;              // already at the bottom
        }

        private async UniTaskVoid StickToBottomDeferred()
        {
            // Wait one frame so ContentSizeFitter / VerticalLayoutGroup have rebuilt the new
            // content height, then pin to the bottom.
            await UniTask.NextFrame(cancellationToken: destroyCancellationToken);
            if (scrollRect == null) return;
            if (m_contentFloor != null && viewport != null)
                m_contentFloor.minHeight = viewport.rect.height;
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
            scrollRect.StopMovement();
        }

        private static bool IsLocal(ulong raceId) =>
            NetworkManager.Singleton != null && raceId == NetworkManager.Singleton.LocalClientId;

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

            return IsLocal(raceId) ? $"{coloredName} (You)" : coloredName;
        }
    }
}
