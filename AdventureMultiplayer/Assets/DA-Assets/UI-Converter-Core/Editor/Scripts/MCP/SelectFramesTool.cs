using DA_Assets.UCC;
using DA_Assets.Shared.MCP;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DA_Assets.UCC.MCP
{
    public class SelectFramesTool : FcuMcpToolBase
    {
        public SelectFramesTool(ConverterBase monoBeh, McpToolSO toolSO) : base(monoBeh, toolSO)
        {
        }

        protected override Task<IReadOnlyList<ContentItem>> ExecuteWithContextAsync(ConverterBase monoBeh, Dictionary<string, object> args)
        {
            var doc = monoBeh.InspectorDrawer.SelectableDocument;

            if (doc == null || doc.Childs == null || doc.Childs.Count == 0)
            {
                return Task.FromResult<IReadOnlyList<ContentItem>>(new[]
                {
                    new ContentItem
                    {
                        Type = "text",
                        Text = GetTemplate("empty")
                    }
                });
            }

            bool selectAll = args != null && args.TryGetValue("select_all", out var selectAllObj) && ToBool(selectAllObj);
            HashSet<string> requestedPages = ExtractStringSet(args, "pages");
            HashSet<string> requestedFrames = ExtractStringSet(args, "frames");

            doc.SetAllSelected(false);

            if (selectAll)
            {
                doc.SetAllSelected(true);

                monoBeh.InspectorDrawer.OnScrollContentUpdated?.Invoke();
                monoBeh.InspectorDrawer.OnFramesChanged?.Invoke();

                return Task.FromResult<IReadOnlyList<ContentItem>>(new[]
                {
                    new ContentItem
                    {
                        Type = "text",
                        Text = GetTemplate("select_all")
                    }
                });
            }

            HashSet<string> foundPages = new HashSet<string>();
            HashSet<string> foundFrames = new HashSet<string>();

            foreach (var page in doc.Childs)
            {
                if (requestedPages.Contains(page.Id))
                {
                    page.SetAllSelected(true);
                    foundPages.Add(page.Id);
                }

                if (page.Childs != null)
                {
                    foreach (var frame in page.Childs)
                    {
                        string key = $"{page.Id}:{frame.Id}";
                        if (requestedFrames.Contains(key))
                        {
                            frame.Selected = true;
                            foundFrames.Add(key);
                        }
                    }
                }
            }

            monoBeh.InspectorDrawer.OnScrollContentUpdated?.Invoke();
            monoBeh.InspectorDrawer.OnFramesChanged?.Invoke();

            List<string> missingPages = requestedPages.Except(foundPages).ToList();
            List<string> missingFrames = requestedFrames.Except(foundFrames).ToList();

            string summary = BuildSummary(foundPages.Count, foundFrames.Count, missingPages, missingFrames);

            return Task.FromResult<IReadOnlyList<ContentItem>>(new[]
            {
                new ContentItem
                {
                    Type = "text",
                    Text = summary
                }
            });
        }

        private static string BuildSummary(int selectedPagesCount, int selectedFramesCount, List<string> missingPages, List<string> missingFrames)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"Selected: {selectedPagesCount} pages, {selectedFramesCount} frames.");

            if (missingPages.Count > 0)
            {
                sb.Append(" Missing pages: ");
                sb.Append(string.Join(", ", missingPages));
                sb.Append(".");
            }

            if (missingFrames.Count > 0)
            {
                sb.Append(" Missing frames: ");
                sb.Append(string.Join(", ", missingFrames));
                sb.Append(".");
            }

            if (missingPages.Count == 0 && missingFrames.Count == 0)
            {
                sb.Append(" All requested items found.");
            }

            return sb.ToString();
        }

        private static HashSet<string> ExtractStringSet(Dictionary<string, object> args, string key)
        {
            HashSet<string> set = new HashSet<string>(StringComparer.Ordinal);

            if (args == null || !args.TryGetValue(key, out var value) || value == null)
                return set;

            if (value is IEnumerable<object> enumerable)
            {
                foreach (var item in enumerable)
                {
                    string str = Convert.ToString(item, CultureInfo.InvariantCulture);
                    if (!string.IsNullOrWhiteSpace(str))
                    {
                        set.Add(str);
                    }
                }
            }
            else
            {
                string str = Convert.ToString(value, CultureInfo.InvariantCulture);
                if (!string.IsNullOrWhiteSpace(str))
                {
                    set.Add(str);
                }
            }

            return set;
        }

        private static bool ToBool(object value)
        {
            if (value == null)
                return false;

            if (value is bool b)
                return b;

            if (bool.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out bool parsed))
                return parsed;

            return false;
        }
    }
}