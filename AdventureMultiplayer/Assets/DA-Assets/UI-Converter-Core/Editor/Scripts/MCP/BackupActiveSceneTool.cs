using DA_Assets.Shared.MCP;
using DA_Assets.Tools;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DA_Assets.UCC.MCP
{
    public class BackupActiveSceneTool : FcuMcpToolBase
    {
        public BackupActiveSceneTool(ConverterBase monoBeh, McpToolSO toolSO) : base(monoBeh, toolSO)
        {
        }

        protected override bool RequiresFcuContext => false;

        protected override Task<IReadOnlyList<ContentItem>> ExecuteWithContextAsync(ConverterBase monoBeh, Dictionary<string, object> args)
        {
            bool success = SceneBackuper.TryBackupActiveScene();
            string message = success
                ? FormatTemplate("success", GetLatestBackupPath())
                : GetTemplate("error");

            IReadOnlyList<ContentItem> response = new[]
            {
                new ContentItem
                {
                    Type = "text",
                    Text = message
                }
            };

            return Task.FromResult(response);
        }

        private static string GetLatestBackupPath()
        {
            string backupsPath = SceneBackuper.GetBackupsPath();
            if (!Directory.Exists(backupsPath))
            {
                return string.Empty;
            }

            string latestBackup = Directory.GetFiles(backupsPath, "*.unity")
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault();

            return latestBackup ?? string.Empty;
        }
    }
}