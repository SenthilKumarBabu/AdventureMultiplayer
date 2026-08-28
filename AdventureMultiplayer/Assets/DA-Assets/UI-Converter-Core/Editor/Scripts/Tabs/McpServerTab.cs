using DA_Assets.DAI;
using DA_Assets.Shared.MCP;
using System;
using UnityEngine.UIElements;

namespace DA_Assets.UCC
{
    internal class McpServerTab : MonoBehaviourLinkerEditor<FcuSettingsWindow, ConverterBase>, IDisposable
    {
        public VisualElement Draw()
        {
            var root = new VisualElement();
            McpServerConfig config = monoBeh.Config.McpServerConfig as McpServerConfig;

            if (config == null)
            {
                root.Add(new HelpBox("MCP server config is not assigned.", HelpBoxMessageType.Warning));
                return root;
            }

            root.Add(new McpAssetTab(config, uitk));
            return root;
        }

        public VisualElement DrawFooter()
        {
            var footer = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row }
            };
            UIHelpers.SetDefaultPadding(footer);
            footer.style.paddingTop = DAI_UitkConstants.SpacingXXS * 2;
            return footer;
        }

        public void Dispose()
        {
        }
    }
}