using UnityEngine;

namespace AdventureMultiplayer
{
    /// <summary>
    /// One asset per power-up type. Stores display info and per-level data.
    /// Level data is stored now but only Lv1 is active in gameplay until
    /// the main-menu upgrade system is implemented.
    /// </summary>
    [CreateAssetMenu(menuName = "Rush Champions/Power-Up Definition", fileName = "PowerUp_New")]
    public class PowerUpDefinition : ScriptableObject
    {
        public PowerUpType    type;
        public PowerUpBoxType boxType;
        public string         displayName;
        public Sprite         icon;

        [Tooltip("Index 0 = Lv1, 1 = Lv2, 2 = Lv3")]
        public PowerUpLevelData[] levels = new PowerUpLevelData[3];

        public PowerUpLevelData GetLevel(int level)
        {
            int idx = Mathf.Clamp(level - 1, 0, levels.Length - 1);
            return levels[idx] ?? new PowerUpLevelData();
        }
    }
}
