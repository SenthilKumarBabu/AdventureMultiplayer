using DA_Assets.Shared.MCP;
using UnityEngine;

namespace DA_Assets.UCC.MCP
{
    [CreateAssetMenu(fileName = "SetFcuToSyncHelpersTool", menuName = "D.A. Assets/FCU/MCP Tools/Set FCU To SyncHelpers")]
    public class SetFcuToSyncHelpersToolSO : McpToolSO
    {
        public override IMcpTool CreateInstance(object context)
        {
            return new SetFcuToSyncHelpersTool(context as ConverterBase, this);
        }
    }
}