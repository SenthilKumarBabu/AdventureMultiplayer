using DA_Assets.Shared.MCP;
using UnityEngine;

namespace DA_Assets.UCC.MCP
{
    [CreateAssetMenu(fileName = "ManageFcuSettingsTool", menuName = "D.A. Assets/FCU/MCP Tools/Manage FCU Settings")]
    public class ManageFcuSettingsToolSO : McpToolSO
    {
        public override IMcpTool CreateInstance(object context)
        {
            return new ManageFcuSettingsTool(context as ConverterBase, this);
        }
    }
}