using DA_Assets.DAI;
using UnityEngine.UIElements;

namespace DA_Assets.UCC
{
    internal class MainTab : MonoBehaviourLinkerEditor<FcuSettingsWindow, ConverterBase>
    {
        public VisualElement Draw()
        {
            if (scriptableObject.Inspector.Header.MonoBeh == null)
            {
                scriptableObject.Close();
                return null;
            }
            else
            {
                var root = scriptableObject.Inspector.DrawGUI();
                root.RemoveFromHierarchy();
                return root;
            }
        }
    }
}