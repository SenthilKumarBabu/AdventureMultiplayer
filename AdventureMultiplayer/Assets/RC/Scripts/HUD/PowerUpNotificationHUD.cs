using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Shows a short on-screen message when the local player uses a power-up (e.g. "SuperCharge
    /// Armed!"), OR when another player's power-up reaches the local player (e.g. "Frozen!",
    /// "Shield Blocked It!"). Power-up feedback otherwise only goes to Debug.Log, which players
    /// never see during a race.
    ///
    /// Setup:
    ///   - Add to the HUD canvas.
    ///   - Assign messageText (a TextMeshProUGUI).
    /// </summary>
    [AddComponentMenu("Rush Champions/Power-Up Notification HUD")]
    public class PowerUpNotificationHUD : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private float punchScale   = 0.3f;
        [SerializeField] private float holdDuration = 1.2f;
        [SerializeField] private float fadeDuration = 0.4f;

        private static readonly Dictionary<PowerUpType, string> Messages = new()
        {
            { PowerUpType.SpeedBoost,   "Speed Boost!" },
            { PowerUpType.Rocket,       "Rocket Fired!" },
            { PowerUpType.Shield,       "Shield Up!" },
            { PowerUpType.StunBolt,     "Stun Bolt Fired!" },
            { PowerUpType.Swap,         "Swapped Places!" },
            { PowerUpType.Freeze,       "Freeze Blast!" },
            { PowerUpType.Banana,       "Banana Dropped!" },
            { PowerUpType.DecoyBox,     "Decoy Box Dropped!" },
            { PowerUpType.Invisible,    "Invisible Activated!" },
            { PowerUpType.SuperCharge,  "SuperCharge Armed — next ability +1s!" },
        };

        // Shown on the RECEIVING end, when another player's power-up actually lands.
        private static readonly Dictionary<PowerUpType, string> AffectedMessages = new()
        {
            { PowerUpType.Rocket,   "Hit by a Rocket!" },
            { PowerUpType.StunBolt, "Stunned!" },
            { PowerUpType.Swap,     "Swapped Backward!" },
            { PowerUpType.Freeze,   "Frozen!" },
            { PowerUpType.Banana,   "Slipped on a Banana!" },
            { PowerUpType.DecoyBox, "Stunned by a Decoy Box!" },
        };

        private PlayerPowerUpInventory _inventory;
        private bool                   _subscribed;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Start()
        {
            if (messageText != null) messageText.alpha = 0f;

            TryBindInventory();
            if (_inventory != null) Subscribe();
            else WaitForInventoryAsync().Forget();
        }

        private void OnEnable() => Subscribe();

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
        }

        private void Subscribe()
        {
            if (_subscribed || _inventory == null) return;
            _inventory.OnPowerUpUsed     += HandlePowerUpUsed;
            _inventory.OnPowerUpAffected += HandlePowerUpAffected;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed || _inventory == null) return;
            _inventory.OnPowerUpUsed     -= HandlePowerUpUsed;
            _inventory.OnPowerUpAffected -= HandlePowerUpAffected;
            _subscribed = false;
        }

        // ── Display ───────────────────────────────────────────────────────────

        private void HandlePowerUpUsed(PowerUpType type)
        {
            ShowMessage(Messages.TryGetValue(type, out var msg) ? msg : $"{type} Used!");
        }

        private void HandlePowerUpAffected(PowerUpType type, PowerUpAffectOutcome outcome)
        {
            string msg = outcome switch
            {
                PowerUpAffectOutcome.ShieldBlocked   => "Shield Blocked It!",
                PowerUpAffectOutcome.InvisibleDodged => "Dodged It (Invisible)!",
                _ => AffectedMessages.TryGetValue(type, out var m) ? m : $"Affected by {type}!",
            };
            ShowMessage(msg);
        }

        private void ShowMessage(string msg)
        {
            if (messageText == null) return;

            messageText.text = msg;

            DOTween.Kill(messageText.transform);
            DOTween.Kill(messageText);

            messageText.transform.localScale = Vector3.one;
            messageText.alpha = 1f;

            messageText.transform
                .DOPunchScale(Vector3.one * punchScale, 0.3f, vibrato: 0)
                .SetEase(Ease.OutBack);

            DOTween.To(() => messageText.alpha, a => messageText.alpha = a, 0f, fadeDuration)
                .SetDelay(holdDuration);
        }
    }
}
