using DA_Assets.UCC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DA_Assets.UCC.MCP
{
    internal static class FcuMcpReadContent
    {
        public static string GetInstancesJson(string instanceTypeName)
        {
            return JsonUtility.ToJson(new ListFcuInstancesResult
            {
                selected_instance_id = FcuMcpContext.GetSelectedId(instanceTypeName),
                instances = new List<FcuMcpInstanceInfo>(FcuMcpContext.GetInstances(instanceTypeName))
            }, true);
        }

        public static string GetFrameListText(ConverterBase fcu, string emptyText, string instruction, string hierarchyPrefix, string agentDataPrefix)
        {
            SelectableNode document = fcu.InspectorDrawer.SelectableDocument;
            if (document == null || document.Childs == null || document.Childs.Count == 0)
                return emptyText;

            List<PageInfo> pages = document.Childs
                .Select(p => new PageInfo(p, p.Childs ?? new List<SelectableNode>()))
                .ToList();

            string tree = BuildTree(pages);
            string table = BuildTable(pages, out List<string> selectedPages, out List<string> selectedFrames);
            string selectedInfo = $"Selected: pages={(selectedPages.Any() ? string.Join(", ", selectedPages) : "none")}; frames={(selectedFrames.Any() ? string.Join(", ", selectedFrames) : "none")}";

            return $"{instruction}\n\n{hierarchyPrefix}\n{tree}\n\n{agentDataPrefix}\n{table}\n{selectedInfo}";
        }

        private static string BuildTree(IEnumerable<PageInfo> pages)
        {
            StringBuilder sb = new StringBuilder();

            foreach (PageInfo page in pages)
            {
                sb.AppendLine($"- {page.Page.Name}");

                foreach (SelectableNode frame in page.Frames)
                    sb.AppendLine($"  - {frame.Name}");

                if (page.Frames.Count == 0)
                    sb.AppendLine("  - (no frames)");
            }

            return sb.ToString().TrimEnd();
        }

        private static string BuildTable(IEnumerable<PageInfo> pages, out List<string> selectedPages, out List<string> selectedFrames)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("type | name | pageId | frameId");

            selectedPages = new List<string>();
            selectedFrames = new List<string>();

            foreach (PageInfo page in pages)
            {
                sb.AppendLine($"page | {page.Page.Name} | {page.Page.Id} |");

                if (page.Page.Selected && !string.IsNullOrWhiteSpace(page.Page.Id))
                    selectedPages.Add(page.Page.Id);

                foreach (SelectableNode frame in page.Frames)
                {
                    string frameKey = $"{page.Page.Id}:{frame.Id}";
                    sb.AppendLine($"frame | {frame.Name} | {page.Page.Id} | {frame.Id}");

                    if (frame.Selected && !string.IsNullOrWhiteSpace(frame.Id))
                        selectedFrames.Add(frameKey);
                }
            }

            return sb.ToString().TrimEnd();
        }

        [Serializable]
        private sealed class ListFcuInstancesResult
        {
            public string selected_instance_id;
            public List<FcuMcpInstanceInfo> instances;
        }

        private readonly struct PageInfo
        {
            public PageInfo(SelectableNode page, List<SelectableNode> frames)
            {
                Page = page;
                Frames = frames;
            }

            public SelectableNode Page { get; }
            public List<SelectableNode> Frames { get; }
        }
    }
}