using DA_Assets.Shared.MCP;
using UnityEngine;

namespace DA_Assets.UCC.MCP
{
    [CreateAssetMenu(fileName = "FcuFrameListResource", menuName = "D.A. Assets/FCU/MCP Resources/Frame List")]
    public class FcuFrameListResourceSO : McpResourceSO
    {
        public override IMcpResource CreateInstance(object context)
        {
            return new FcuFrameListResource(context as ConverterBase, this);
        }
    }
}