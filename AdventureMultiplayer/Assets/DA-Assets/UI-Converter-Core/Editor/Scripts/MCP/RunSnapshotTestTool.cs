using DA_Assets.UCC.Snapshot;
using DA_Assets.Shared.MCP;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using UnityEngine;

namespace DA_Assets.UCC.MCP
{
    public class RunSnapshotTestTool : FcuMcpToolBase
    {
        public RunSnapshotTestTool(ConverterBase monoBeh, McpToolSO toolSO) : base(monoBeh, toolSO)
        {
        }

        protected override Task<IReadOnlyList<ContentItem>> ExecuteWithContextAsync(ConverterBase monoBeh, Dictionary<string, object> args)
        {

            Transform root = monoBeh.SnapshotSettings.RootFrame;

            if (root == null)
            {
                return ErrorResult(GetTemplate("error_no_root"));
            }


            string baselineName;

            if (args.TryGetValue("baseline", out var baselineObj))
            {
                baselineName = Convert.ToString(baselineObj, CultureInfo.InvariantCulture) ?? string.Empty;
            }
            else
            {
                baselineName = monoBeh.SnapshotSettings.SelectedBaseline;
            }

            if (string.IsNullOrEmpty(baselineName))
            {
                return ErrorResult(GetTemplate("error_no_baseline"));
            }


            string[] available = SnapshotSaver.GetAvailableBaselines();
            bool found = false;

            foreach (string name in available)
            {
                if (string.Equals(name, baselineName, StringComparison.OrdinalIgnoreCase))
                {
                    found = true;
                    baselineName = name;
                    break;
                }
            }

            if (!found)
            {
                string list = available.Length > 0
                    ? string.Join(", ", available)
                    : "none";

                return ErrorResult(FormatTemplate("error_baseline_not_found", baselineName, list));
            }


            string zipPath = SnapshotSaver.GetBaselinePath(baselineName);
            ComparisonReport report = SnapshotComparer.Compare(root, zipPath);

            string formatted = SnapshotReportFormatter.Format(report, root.name, baselineName);

            IReadOnlyList<ContentItem> response = new[]
            {
                new ContentItem
                {
                    Type = "text",
                    Text = formatted
                }
            };

            return Task.FromResult(response);
        }

        private static Task<IReadOnlyList<ContentItem>> ErrorResult(string message)
        {
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
    }
}