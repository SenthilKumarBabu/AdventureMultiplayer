using DA_Assets.Shared.MCP;
using UnityEngine;

namespace DA_Assets.UCC.MCP
{
    [CreateAssetMenu(fileName = "BackupActiveSceneTool", menuName = "D.A. Assets/FCU/MCP Tools/Backup Active Scene")]
    public class BackupActiveSceneToolSO : McpToolSO
    {
        public override IMcpTool CreateInstance(object context)
        {
            return new BackupActiveSceneTool(context as ConverterBase, this);
        }
    }
}