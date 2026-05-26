using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Character selection panel shown in the Lobby.
    /// Wire characterButtons in order matching NetworkGameManager.characterPrefabs (0=Gale … 4=Spike).
    /// </summary>
    [AddComponentMenu("Adventure Multiplayer/Character Select UI")]
    public class CharacterSelectUI : MonoBehaviour
    {
        [SerializeField] private Button[]          characterButtons;
        [SerializeField] private Color             selectedColor   = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color             defaultColor    = new Color(0.3f, 0.3f, 0.3f);
        [SerializeField] private TextMeshProUGUI   selectedLabel;
        [SerializeField] private TextMeshProUGUI   abilityLabel;

        private static readonly string[] CharacterNames   = { "Gale", "Blaze", "Bolt", "Bruno", "Spike" };
        private static readonly string[] CharacterAbility = { "Glider", "Dash", "Sprinter", "Roller", "Air Dive" };

        private int m_SelectedIndex;

        private void Awake()
        {
            for (int i = 0; i < characterButtons.Length; i++)
            {
                var idx = i;
                characterButtons[i].onClick.AddListener(() => OnCharacterSelected(idx));
            }
            Highlight(0);
        }

        private void OnCharacterSelected(int index)
        {
            m_SelectedIndex = index;
            Highlight(index);

            if (CharacterPicker.Instance != null)
                CharacterPicker.Instance.SelectCharacter(index);
        }

        private void Highlight(int index)
        {
            for (int i = 0; i < characterButtons.Length; i++)
            {
                var img = characterButtons[i].GetComponent<Image>();
                if (img != null)
                    img.color = (i == index) ? selectedColor : defaultColor;
            }

            if (selectedLabel != null && index < CharacterNames.Length)
                selectedLabel.text = CharacterNames[index];

            if (abilityLabel != null && index < CharacterAbility.Length)
                abilityLabel.text = CharacterAbility[index];
        }
    }
}
