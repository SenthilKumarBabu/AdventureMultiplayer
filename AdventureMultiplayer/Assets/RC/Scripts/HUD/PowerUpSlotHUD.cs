using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Displays the local player's three power-up slots and handles tap-to-use.
    ///
    /// Setup:
    ///   - Add this component to the HUD canvas (or a child panel).
    ///   - Assign 3 SlotRoot GameObjects, each containing an Image (icon) and a Button.
    ///   - Assign icon sprites per PowerUpType in the Inspector array.
    ///   - Set Navigation.None on all buttons (prevents Submit / gamepad re-fire).
    /// </summary>
    [AddComponentMenu("Rush Champions/Power-Up Slot HUD")]
    public class PowerUpSlotHUD : MonoBehaviour
    {
        [System.Serializable]
        public struct SlotUI
        {
            public Button     button;
            public Image      icon;
            public GameObject emptyOverlay;
        }

        [System.Serializable]
        public struct PowerUpIconEntry
        {
            public PowerUpType type;
            public Sprite      icon;
        }

        [SerializeField] private SlotUI[]           slots = new SlotUI[3];
        [SerializeField] private PowerUpIconEntry[] powerUpIcons;
        [SerializeField] private Sprite             emptySlotSprite;

        private PlayerPowerUpInventory _inventory;
        private bool                   _subscribed;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Start()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                int slotIdx = i;
                if (slots[i].button != null)
                {
                    var nav = slots[i].button.navigation;
                    nav.mode = Navigation.Mode.None;
                    slots[i].button.navigation = nav;
                    slots[i].button.onClick.AddListener(() => OnSlotTapped(slotIdx));
                }
            }

            // Try immediately; if the local player's inventory hasn't registered yet
            // (common for non-host clients), poll each frame until it appears.
            TryBindInventory();
            if (_inventory != null)
            {
                Subscribe();
                RefreshAll();
            }
            else
            {
                WaitForInventoryAsync().Forget();
            }
        }

        private void OnEnable()
        {
            // Re-subscribe after the HUD was disabled (e.g. countdown hide/show).
            // No-op if inventory not found yet — WaitForInventoryAsync handles that.
            Subscribe();
            RefreshAll();
        }

        private void OnDisable() => Unsubscribe();

        // ── Inventory binding ─────────────────────────────────────────────────

        private void TryBindInventory()
        {
            if (_inventory != null) return;
            var localObj = NetworkManager.Singleton?.LocalClient?.PlayerObject;
            if (localObj != null) _inventory = localObj.GetComponent<PlayerPowerUpInventory>();
        }

        private async UniTaskVoid WaitForInventoryAsync()
        {
            while (_inventory == null)
            {
                await UniTask.DelayFrame(1, cancellationToken: this.GetCancellationTokenOnDestroy());
                TryBindInventory();
            }

            Subscribe();
            RefreshAll();
        }

        private void Subscribe()
        {
            if (_subscribed || _inventory == null) return;
            _inventory.Slots.OnListChanged += OnSlotsChanged;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed || _inventory == null) return;
            _inventory.Slots.OnListChanged -= OnSlotsChanged;
            _subscribed = false;
        }

        // ── Slot tapped ───────────────────────────────────────────────────────

        private void OnSlotTapped(int slotIndex)
        {
            if (_inventory == null || _inventory.GetSlot(slotIndex) == -1) return;
            _inventory.UseSlotServerRpc(slotIndex);
        }

        // ── NetworkList change ────────────────────────────────────────────────

        private void OnSlotsChanged(NetworkListEvent<int> _) => RefreshAll();

        // ── Refresh UI ────────────────────────────────────────────────────────

        private void RefreshAll()
        {
            for (int i = 0; i < slots.Length; i++)
                RefreshSlot(i);
        }

        private void RefreshSlot(int i)
        {
            if (i >= slots.Length) return;

            int  typeInt = _inventory != null ? _inventory.GetSlot(i) : -1;
            bool hasItem = typeInt >= 0;

            if (slots[i].icon != null)
                slots[i].icon.sprite = hasItem ? GetIcon(typeInt) : emptySlotSprite;

            if (slots[i].emptyOverlay != null)
                slots[i].emptyOverlay.SetActive(!hasItem);

            if (slots[i].button != null)
                slots[i].button.interactable = hasItem;
        }

        private Sprite GetIcon(int typeInt)
        {
            var type = (PowerUpType)typeInt;
            if (powerUpIcons != null)
                foreach (var entry in powerUpIcons)
                    if (entry.type == type) return entry.icon;

            // No mapping for this type — return null (blank icon) rather than
            // emptySlotSprite, so a held item never renders identically to an
            // empty slot. Assign an icon for this type in the Inspector.
            Debug.LogWarning($"[PowerUpSlotHUD] No icon mapped for {type} — add an entry to powerUpIcons.");
            return null;
        }
    }
}
