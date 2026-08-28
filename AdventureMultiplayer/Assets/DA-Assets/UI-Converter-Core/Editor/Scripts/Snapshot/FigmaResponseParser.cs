using System;
using System.Collections.Generic;
using UnityEngine;

#if JSONNET_EXISTS
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#endif

namespace DA_Assets.UCC.Snapshot
{
    public static class FigmaResponseParser
    {
        private const string FigmaResponseEntry = "_figma_response.json";

        public static string FigmaResponseEntryName => FigmaResponseEntry;

        public static string ExtractJsonBody(string logContent)
        {
            if (string.IsNullOrEmpty(logContent))
                return null;

            int firstNewline = logContent.IndexOf('\n');
            if (firstNewline < 0)
                return null;

            int secondNewline = logContent.IndexOf('\n', firstNewline + 1);
            if (secondNewline < 0)
                return null;

            return logContent.Substring(secondNewline + 1).Trim();
        }

        public static Dictionary<string, string> ParseNodes(string jsonBody)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrEmpty(jsonBody))
                return result;

#if JSONNET_EXISTS
            try
            {
                JObject root = JObject.Parse(jsonBody);


                JToken nodesToken = root["nodes"];

                if (nodesToken != null && nodesToken.Type == JTokenType.Object)
                {
                    foreach (var kvp in (JObject)nodesToken)
                    {
                        JToken documentToken = kvp.Value?["document"];

                        if (documentToken != null && documentToken.Type == JTokenType.Object)
                        {
                            CollectNodesRecursive((JObject)documentToken, result);
                        }
                    }
                }
                else
                {

                    JToken documentToken = root["document"];

                    if (documentToken != null && documentToken.Type == JTokenType.Object)
                    {
                        CollectNodesRecursive((JObject)documentToken, result);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"FigmaResponseParser: Failed to parse JSON. {ex.Message}");
            }
#endif

            return result;
        }

        public static Dictionary<string, string> ParseNodesFromLog(string logContent)
        {
            string jsonBody = ExtractJsonBody(logContent);
            return ParseNodes(jsonBody);
        }

#if JSONNET_EXISTS
        private static void CollectNodesRecursive(JObject node, Dictionary<string, string> result)
        {
            if (node == null)
                return;

            string id = node["id"]?.ToString();

            if (!string.IsNullOrEmpty(id))
            {

                JObject cleaned = new JObject();

                foreach (var prop in node.Properties())
                {
                    if (string.Equals(prop.Name, "children", StringComparison.OrdinalIgnoreCase))
                        continue;

                    cleaned.Add(prop.Name, prop.Value.DeepClone());
                }

                result[id] = cleaned.ToString(Formatting.Indented);
            }


            JToken childrenToken = node["children"];

            if (childrenToken != null && childrenToken.Type == JTokenType.Array)
            {
                foreach (JToken child in childrenToken)
                {
                    if (child.Type == JTokenType.Object)
                    {
                        CollectNodesRecursive((JObject)child, result);
                    }
                }
            }
        }
#endif
    }
}