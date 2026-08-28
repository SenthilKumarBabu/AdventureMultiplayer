using UnityEngine;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Visual-only glass/transparent look for the Invisible power-up.
    /// Swaps every renderer's materials to a shared glass material and back.
    /// Purely cosmetic — collision and damage immunity are handled by
    /// NetworkedHealth.IsInvisible and PlayerPowerUpInventory's collision-ignore logic.
    ///
    /// Add to every player prefab; assign glassMaterial in the Inspector.
    /// </summary>
    [AddComponentMenu("Rush Champions/Invisible Effect")]
    public class InvisibleEffect : MonoBehaviour
    {
        [SerializeField] private Material glassMaterial;

        private Renderer[]   _renderers;
        private Material[][] _originalMaterials;
        private bool         _active;

        private void Awake()
        {
            _renderers = GetComponentsInChildren<Renderer>();
            _originalMaterials = new Material[_renderers.Length][];
            for (int i = 0; i < _renderers.Length; i++)
                _originalMaterials[i] = _renderers[i].sharedMaterials;
        }

        /// <summary>Toggle the glass look on/off. Safe to call redundantly.</summary>
        public void SetActive(bool active)
        {
            if (_active == active) return;
            _active = active;

            if (glassMaterial == null)
            {
                Debug.LogWarning($"[InvisibleEffect] {name}: no glass material assigned in the Inspector.");
                return;
            }

            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] == null) continue;

                if (active)
                {
                    var glass = new Material[_originalMaterials[i].Length];
                    for (int m = 0; m < glass.Length; m++)
                        glass[m] = glassMaterial;
                    _renderers[i].materials = glass;
                }
                else
                {
                    _renderers[i].materials = _originalMaterials[i];
                }
            }

            Debug.Log($"[InvisibleEffect] {name} glass look {(active ? "on" : "off")}.");
        }
    }
}
