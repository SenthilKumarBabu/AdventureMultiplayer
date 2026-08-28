using DA_Assets.Shared.MCP;
using UnityEngine;

namespace DA_Assets.UCC.MCP
{
    [CreateAssetMenu(fileName = "SelectFcuInstanceTool", menuName = "D.A. Assets/FCU/MCP Tools/SelectFcuInstance")]
    public class SelectFcuInstanceToolSO : McpToolSO
    {
        public override IMcpTool CreateInstance(object context)
        {
            return new SelectFcuInstanceTool(this);
        }
    }
}