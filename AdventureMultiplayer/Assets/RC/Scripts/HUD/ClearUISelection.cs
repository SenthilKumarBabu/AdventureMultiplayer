using UnityEngine;
using UnityEngine.EventSystems;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Clears the EventSystem's selected UI object every frame.
    ///
    /// Without this, pressing Jump (or any action mapped to the UI Submit binding)
    /// also activates whichever button the EventSystem last highlighted — causing
    /// accidental double-triggers (e.g. Jump + Reset both fire on one tap).
    ///
    /// Add to any always-active GameObject in the gameplay scene (e.g. RaceHUD).
    /// </summary>
    [AddComponentMenu("Adventure Multiplayer/HUD/Clear UI Selection")]
    public class ClearUISelection : MonoBehaviour
    {
        private void Update()
        {
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
                EventSystem.current.SetSelectedGameObject(null);
        }
    }
}
