using DA_Assets.Shared.MCP;
using UnityEngine;

namespace DA_Assets.UCC.MCP
{
    [CreateAssetMenu(fileName = "RunSnapshotTestTool", menuName = "D.A. Assets/FCU/MCP Tools/RunSnapshotTest")]
    public class RunSnapshotTestToolSO : McpToolSO
    {
        public override IMcpTool CreateInstance(object context)
        {
            return new RunSnapshotTestTool(context as ConverterBase, this);
        }
    }
}