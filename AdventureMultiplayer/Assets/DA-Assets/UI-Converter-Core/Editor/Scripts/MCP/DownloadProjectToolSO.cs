using DA_Assets.Shared.MCP;
using UnityEngine;

namespace DA_Assets.UCC.MCP
{
    [CreateAssetMenu(fileName = "DownloadProjectTool", menuName = "D.A. Assets/FCU/MCP Tools/DownloadProject")]
    public class DownloadProjectToolSO : McpToolSO
    {
        public override IMcpTool CreateInstance(object context)
        {
            return new DownloadProjectTool(context as ConverterBase, this);
        }
    }
}