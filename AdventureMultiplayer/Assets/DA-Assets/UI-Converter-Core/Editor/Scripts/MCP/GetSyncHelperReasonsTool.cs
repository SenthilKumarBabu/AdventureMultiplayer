using DA_Assets.UCC.Extensions;
using DA_Assets.UCC.Model;
using DA_Assets.Shared.MCP;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace DA_Assets.UCC.MCP
{
    public class GetSyncHelperReasonsTool : FcuMcpToolBase
    {
        public GetSyncHelperReasonsTool(ConverterBase monoBeh, McpToolSO toolSO) : base(monoBeh, toolSO)
        {
        }

        protected override Task<IReadOnlyList<ContentItem>> ExecuteWithContextAsync(ConverterBase monoBeh, Dictionary<string, object> args)
        {
            SyncHelperReasonsResult result = CreateResult(monoBeh, args);
            string json = JsonUtility.ToJson(result, true);

            return Task.FromResult<IReadOnlyList<ContentItem>>(new[]
            {
                new ContentItem
                {
                    Type = "text",
                    Text = json
                }
            });
        }

        private SyncHelperReasonsResult CreateResult(ConverterBase fcu, Dictionary<string, object> args)
        {
            if (!fcu.IsDebug())
            {
                return SyncHelperReasonsResult.Fail("SyncHelper Reasons are available only when Debug Mode is enabled in the FCU asset.");
            }

            string figmaNodeId = ReadStringArgument(args, "figma_node_id");
            string nameContains = ReadStringArgument(args, "name_contains");
            int? syncHelperInstanceId = ReadNullableIntArgument(args, "sync_helper_instance_id");
            bool includeEmpty = ReadBoolArgument(args, "include_empty");

            List<SyncHelperReasonInfo> items = new List<SyncHelperReasonInfo>();
            foreach (SyncHelper syncHelper in fcu.SyncHelpers.GetAllSyncHelpers())
            {
                if (!Matches(syncHelper, syncHelperInstanceId, figmaNodeId, nameContains))
                    continue;

                SyncHelperReasonInfo info = CreateInfo(syncHelper);
                if (!includeEmpty && info.reasons.Count == 0)
                    continue;

                items.Add(info);
            }

            return SyncHelperReasonsResult.Ok(fcu.GetInstanceID(), items);
        }

        private static bool Matches(SyncHelper syncHelper, int? syncHelperInstanceId, string figmaNodeId, string nameContains)
        {
            if (syncHelper == null)
                return false;

            if (syncHelperInstanceId.HasValue && syncHelper.GetInstanceID() != syncHelperInstanceId.Value)
                return false;

            SyncData data = syncHelper.Data;
            if (!string.IsNullOrWhiteSpace(figmaNodeId) && !string.Equals(data?.Id, figmaNodeId, StringComparison.Ordinal))
                return false;

            if (!string.IsNullOrWhiteSpace(nameContains))
            {
                string gameObjectName = syncHelper.gameObject == null ? string.Empty : syncHelper.gameObject.name;
                string hierarchy = data?.NameHierarchy ?? string.Empty;

                if (gameObjectName.IndexOf(nameContains, StringComparison.OrdinalIgnoreCase) < 0 &&
                    hierarchy.IndexOf(nameContains, StringComparison.OrdinalIgnoreCase) < 0)
                    return false;
            }

            return true;
        }

        private static SyncHelperReasonInfo CreateInfo(SyncHelper syncHelper)
        {
            SyncData data = syncHelper.Data;
            List<FcuTag> tags = data?.Tags ?? new List<FcuTag>();
            List<ReasonEntry> reasons = data?.Reasons ?? new List<ReasonEntry>();

            return new SyncHelperReasonInfo
            {
                sync_helper_instance_id = syncHelper.GetInstanceID(),
                game_object_name = syncHelper.gameObject == null ? string.Empty : syncHelper.gameObject.name,
                hierarchy = data?.NameHierarchy ?? GetTransformPath(syncHelper.transform),
                figma_node_id = data?.Id ?? string.Empty,
                tags = tags.Select(x => x.ToString()).ToList(),
                reasons = reasons
                    .Where(x => x.key != ReasonKey.None)
                    .Select(x => CreateReasonInfo(x, data, tags))
                    .ToList()
            };
        }

        private static ReasonInfo CreateReasonInfo(ReasonEntry entry, SyncData data, List<FcuTag> tags)
        {
            bool hasRelatedTag = entry.relatedTag != FcuTag.None;
            return new ReasonInfo
            {
                key = entry.key.ToString(),
                description = entry.key.GetDescription(data),
                group = hasRelatedTag ? entry.relatedTag.ToString() : entry.key.GetReasonGroupFromPrefix(),
                related_tag = hasRelatedTag ? entry.relatedTag.ToString() : string.Empty,
                is_active = !hasRelatedTag || tags.Contains(entry.relatedTag)
            };
        }

        private static string GetTransformPath(Transform transform)
        {
            if (transform == null)
                return string.Empty;

            List<string> parts = new List<string>();
            Transform current = transform;
            while (current != null)
            {
                parts.Add(current.name);
                current = current.parent;
            }

            parts.Reverse();
            return string.Join("/", parts);
        }

        private static string ReadStringArgument(Dictionary<string, object> args, string key)
        {
            return args != null && args.TryGetValue(key, out object value) && value != null
                ? Convert.ToString(value, CultureInfo.InvariantCulture)
                : string.Empty;
        }

        private static bool ReadBoolArgument(Dictionary<string, object> args, string key)
        {
            return args != null && args.TryGetValue(key, out object value) && value != null && Convert.ToBoolean(value, CultureInfo.InvariantCulture);
        }

        private static int? ReadNullableIntArgument(Dictionary<string, object> args, string key)
        {
            if (args == null || !args.TryGetValue(key, out object value) || value == null)
                return null;

            return Convert.ToInt32(value, CultureInfo.InvariantCulture);
        }

        [Serializable]
        private sealed class SyncHelperReasonsResult
        {
            public bool success;
            public string error;
            public int fcu_instance_id;
            public int sync_helpers_count;
            public List<SyncHelperReasonInfo> sync_helpers;

            public static SyncHelperReasonsResult Ok(int fcuInstanceId, List<SyncHelperReasonInfo> syncHelpers)
            {
                return new SyncHelperReasonsResult
                {
                    success = true,
                    error = string.Empty,
                    fcu_instance_id = fcuInstanceId,
                    sync_helpers_count = syncHelpers.Count,
                    sync_helpers = syncHelpers
                };
            }

            public static SyncHelperReasonsResult Fail(string error)
            {
                return new SyncHelperReasonsResult
                {
                    success = false,
                    error = error,
                    sync_helpers = new List<SyncHelperReasonInfo>()
                };
            }
        }

        [Serializable]
        private sealed class SyncHelperReasonInfo
        {
            public int sync_helper_instance_id;
            public string game_object_name;
            public string hierarchy;
            public string figma_node_id;
            public List<string> tags;
            public List<ReasonInfo> reasons;
        }

        [Serializable]
        private sealed class ReasonInfo
        {
            public string key;
            public string description;
            public string group;
            public string related_tag;
            public bool is_active;
        }
    }
}