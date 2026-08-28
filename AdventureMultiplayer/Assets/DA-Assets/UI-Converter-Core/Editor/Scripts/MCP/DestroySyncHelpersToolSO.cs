using DA_Assets.Shared.MCP;
using UnityEngine;

namespace DA_Assets.UCC.MCP
{
    [CreateAssetMenu(fileName = "DestroySyncHelpersTool", menuName = "D.A. Assets/FCU/MCP Tools/Destroy SyncHelpers")]
    public class DestroySyncHelpersToolSO : McpToolSO
    {
        public override IMcpTool CreateInstance(object context)
        {
            return new DestroySyncHelpersTool(context as ConverterBase, this);
        }
    }
}