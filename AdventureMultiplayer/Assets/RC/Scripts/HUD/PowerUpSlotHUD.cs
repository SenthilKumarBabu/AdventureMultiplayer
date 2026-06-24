using TMPro;
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
    ///   - Assign icon sprites per PowerUpType in the Inspector array (index = (int)PowerUpType).
    ///   - Set Navigation.None on all buttons (prevents Submit / gamepad re-fire).
    /// </summary>
    [AddComponentMenu("Rush Champions/Power-Up Slot HUD")]
    public class PowerUpSlotHUD : MonoBehaviour
    {
        [System.Serializable]
        public struct SlotUI
        {
            public Button    button;
            public Image     icon;
            public GameObject emptyOverlay;  // optional grey tint shown when slot is empty
        }

        [SerializeField] private SlotUI[] slots = new SlotUI[3];

        // Icon sprites indexed by (int)PowerUpType — assign in Inspector
        [SerializeField] private Sprite[] powerUpIcons;

        [SerializeField] private Sprite emptySlotSprite;

        private PlayerPowerUpInventory _inventory;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Start()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                int slotIdx = i;
                if (slots[i].button != null)
                {
                    var nav = slots[i].button.navigation;
                    nav.mode = UnityEngine.UI.Navigation.Mode.None;
                    slots[i].button.navigation = nav;

                    slots[i].button.onClick.AddListener(() => OnSlotTapped(slotIdx));
                }
            }

            FindLocalInventory();
        }

        private void OnEnable()
        {
            if (_inventory == null) FindLocalInventory();
            if (_inventory != null)
                _inventory.Slots.OnListChanged += OnSlotsChanged;
            RefreshAll();
        }

        private void OnDisable()
        {
            if (_inventory != null)
                _inventory.Slots.OnListChanged -= OnSlotsChanged;
        }

        // ── Find local inventory ──────────────────────────────────────────────

        private void FindLocalInventory()
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient) return;

            ulong localId = NetworkManager.Singleton.LocalClientId;
            if (PlayerPowerUpInventory.All.TryGetValue(localId, out var inv))
            {
                _inventory = inv;
                _inventory.Slots.OnListChanged += OnSlotsChanged;
            }
        }

        // ── Slot tapped ───────────────────────────────────────────────────────

        private void OnSlotTapped(int slotIndex)
        {
            if (_inventory == null) FindLocalInventory();
            if (_inventory == null) return;
            if (_inventory.GetSlot(slotIndex) == -1) return;

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

            int typeInt = _inventory != null ? _inventory.GetSlot(i) : -1;
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
            if (powerUpIcons != null && typeInt >= 0 && typeInt < powerUpIcons.Length)
                return powerUpIcons[typeInt];
            return emptySlotSprite;
        }
    }
}
