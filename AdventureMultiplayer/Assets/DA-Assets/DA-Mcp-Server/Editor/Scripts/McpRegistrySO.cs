using System.Collections.Generic;
using UnityEngine;

namespace DA_Assets.Shared.MCP
{
    [CreateAssetMenu(menuName = "D.A. Assets/MCP/Registry")]
    public sealed class McpRegistrySO : ScriptableObject
    {
        [SerializeField] List<ScriptableObject> servers = new();

        public IReadOnlyList<ScriptableObject> Servers => servers;
    }
}
