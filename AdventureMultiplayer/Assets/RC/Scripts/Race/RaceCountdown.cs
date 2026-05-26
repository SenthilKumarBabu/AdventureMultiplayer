using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Server-driven pre-race countdown.
    ///
    /// The server ticks a NetworkVariable from countdownSeconds down to 0 (Go!),
    /// then calls RaceManager.StartRace().  Every client receives the value via
    /// NetworkVariable replication and raises the static events below so other
    /// systems (HUD, player input lock) can react without a direct reference here.
    ///
    /// Countdown values:
    ///   > 0  = seconds remaining  (3 … 2 … 1)
    ///     0  = Go!
    ///    -1  = idle / not started yet
    ///
    /// Player input locking is handled by NetworkPlayerInputLocker, which
    /// subscribes to OnRaceStart and re-enables the local player's inputs.
    /// </summary>
    [AddComponentMenu("Adventure Multiplayer/Race Countdown")]
    public class RaceCountdown : NetworkBehaviour
    {
        [SerializeField] private int   countdownSeconds  = 3;
        [SerializeField] private float delayBeforeStart  = 1f;

        /// <summary>Current countdown value replicated to all clients (-1 = idle, 0 = Go).</summary>
        public NetworkVariable<int> CountdownValue { get; private set; } =
            new(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        /// <summary>Raised on every client each tick. value > 0 = seconds left, 0 = Go!</summary>
        public static event System.Action<int> OnCountdownTick;

        /// <summary>Raised on every client when the countdown reaches 0 (Go!).</summary>
        public static event System.Action OnRaceStart;

        public override void OnNetworkSpawn()
        {
            CountdownValue.OnValueChanged += HandleValueChanged;

            if (IsServer)
                RunCountdownAsync().Forget();
        }

        public override void OnNetworkDespawn()
        {
            CountdownValue.OnValueChanged -= HandleValueChanged;
        }

        private void HandleValueChanged(int previous, int current)
        {
            if (current < 0) return;

            OnCountdownTick?.Invoke(current);

            if (current == 0)
            {
                Debug.Log("[RaceCountdown] GO!");
                OnRaceStart?.Invoke();
            }
            else
            {
                Debug.Log($"[RaceCountdown] {current}...");
            }
        }

        private async UniTaskVoid RunCountdownAsync()
        {
            await UniTask.WaitForSeconds(delayBeforeStart, cancellationToken: destroyCancellationToken);

            for (int i = countdownSeconds; i >= 0; i--)
            {
                CountdownValue.Value = i;

                if (i > 0)
                    await UniTask.WaitForSeconds(1f, cancellationToken: destroyCancellationToken);
            }

            if (RaceManager.Instance != null)
                RaceManager.Instance.StartRace();
        }
    }
}
