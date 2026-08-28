using DA_Assets.Shared.MCP;
using UnityEngine;

namespace DA_Assets.UCC.MCP
{
    [CreateAssetMenu(fileName = "StepperTool", menuName = "D.A. Assets/FCU/MCP Tools/Stepper")]
    public class StepperToolSO : McpToolSO
    {
        public override IMcpTool CreateInstance(object context)
        {
            return new StepperTool(context as ConverterBase, this);
        }
    }
}