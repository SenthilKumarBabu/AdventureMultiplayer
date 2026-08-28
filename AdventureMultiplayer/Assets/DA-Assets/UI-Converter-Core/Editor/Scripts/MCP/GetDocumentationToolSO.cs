using DA_Assets.Shared.MCP;
using UnityEngine;

namespace DA_Assets.UCC.MCP
{
    [CreateAssetMenu(fileName = "GetDocumentationTool", menuName = "D.A. Assets/FCU/MCP Tools/GetDocumentation")]
    public class GetDocumentationToolSO : McpToolSO
    {
        public override IMcpTool CreateInstance(object context)
        {
            return new GetDocumentationTool(context as ConverterBase, this);
        }
    }
}