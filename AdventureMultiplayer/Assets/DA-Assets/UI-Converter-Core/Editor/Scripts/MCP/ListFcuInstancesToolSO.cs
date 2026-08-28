using DA_Assets.Shared.MCP;
using UnityEngine;

namespace DA_Assets.UCC.MCP
{
    [CreateAssetMenu(fileName = "ListFcuInstancesTool", menuName = "D.A. Assets/FCU/MCP Tools/ListFcuInstances")]
    public class ListFcuInstancesToolSO : McpToolSO
    {
        public override IMcpTool CreateInstance(object context)
        {
            return new ListFcuInstancesTool(this);
        }
    }
}