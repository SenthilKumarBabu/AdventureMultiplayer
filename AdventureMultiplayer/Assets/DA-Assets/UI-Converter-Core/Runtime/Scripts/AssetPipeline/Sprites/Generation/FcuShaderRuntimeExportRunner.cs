#if UNITY_EDITOR
using DA_Assets.UCC.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace DA_Assets.UCC
{
    public sealed class FcuShaderRuntimeExportRunner : MonoBehaviour
    {
        const string ManifestFileName = "export_manifest.json";
        const string CacheFolderName = "NodeJsonCache";
        const string OutputArg = "--fcu-shader-output";
        const string ScaleArg = "--fcu-shader-scale";
        const string OutputEnv = "FCU_SHADER_OUTPUT";
        const string ScaleEnv = "FCU_SHADER_SCALE";

        [SerializeField, Min(0.01f)] float fallbackScale = 4f;

        async void Start()
        {
            try
            {
                RunExport();
                await System.Threading.Tasks.Task.Yield();
                Application.Quit(0);
            }
            catch (Exception ex)
            {
                Debug.LogError($"FCU shader runtime export failed.\n{ex}");
                Application.Quit(1);
            }
        }

        void RunExport()
        {
            string outputRoot = ResolveOutputRoot();
            float scale = ResolveScale();
            string manifestPath = Path.Combine(outputRoot, ManifestFileName);
            string cacheRoot = Path.Combine(outputRoot, CacheFolderName);

            if (!File.Exists(manifestPath))
                throw new FileNotFoundException("Manifest not found.", manifestPath);
            if (!Directory.Exists(cacheRoot))
                throw new DirectoryNotFoundException(cacheRoot);

            RuntimeExportManifest manifest = DAJson.FromJson<RuntimeExportManifest>(File.ReadAllText(manifestPath));
            if (manifest?.Entries == null)
                throw new InvalidOperationException("Manifest has no entries.");

            manifest.ExportScale = scale;
            manifest.GeneratedAtUtc = DateTime.UtcNow.ToString("O");

            int rendered = 0;
            int failed = 0;

            for (int i = 0; i < manifest.Entries.Count; i++)
            {
                RuntimeExportManifestEntry entry = manifest.Entries[i];
                string nodeCachePath = Path.Combine(cacheRoot, ToCacheFileName(entry.NodeId));
                NormalizeEntryNaming(entry, cacheRoot, scale);
                string outputPath = Path.Combine(outputRoot, entry.OutputFileName);

                try
                {
                    if (!File.Exists(nodeCachePath))
                        throw new FileNotFoundException("Node cache file not found.", nodeCachePath);

                    DesignProject nodeProject = DAJson.FromJson<DesignProject>(File.ReadAllText(nodeCachePath));
                    if (string.IsNullOrWhiteSpace(nodeProject.Document.Id))
                        throw new InvalidOperationException($"Node cache has no document: {entry.NodeId}");

                    Rect? canvasBounds = TryResolveCanvasBounds(entry, nodeProject.Document, cacheRoot, scale);
                    FcuFigmageSpriteBaker.BakePng(nodeProject.Document, scale, outputPath, canvasBounds);
                    entry.RenderedPath = outputPath;
                    entry.Error = null;
                    rendered++;
                }
                catch (Exception ex)
                {
                    entry.RenderedPath = outputPath;
                    entry.Error = ex.Message;
                    failed++;
                    Debug.LogError($"{entry.NodeName}: render failed. {ex.Message}");
                }
            }

            File.WriteAllText(manifestPath, DAJson.ToJson(manifest));
            Debug.Log($"FCU shader runtime export done. rendered={rendered} failed={failed} output={outputRoot}");
        }

        static string ResolveOutputRoot()
        {
            string output = ResolveArg(OutputArg);
            if (string.IsNullOrWhiteSpace(output))
                output = Environment.GetEnvironmentVariable(OutputEnv);
            if (string.IsNullOrWhiteSpace(output))
                output = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Output"));

            Directory.CreateDirectory(output);
            return output;
        }

        float ResolveScale()
        {
            string raw = ResolveArg(ScaleArg);
            if (string.IsNullOrWhiteSpace(raw))
                raw = Environment.GetEnvironmentVariable(ScaleEnv);

            if (!float.TryParse(raw, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float scale))
                scale = fallbackScale;

            return Mathf.Max(0.01f, Mathf.Round(scale * 100f) / 100f);
        }

        static string ResolveArg(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                    return args[i + 1];
            }

            return null;
        }

        static string ToCacheFileName(string nodeId)
        {
            return $"{nodeId.Replace(':', '-')}.json";
        }

        static void NormalizeEntryNaming(RuntimeExportManifestEntry entry, string cacheRoot, float scale)
        {
            if (entry == null)
                return;

            if (string.IsNullOrWhiteSpace(entry.FrameName) ||
                entry.FrameName.IndexOf("Layer Blur Stress", StringComparison.OrdinalIgnoreCase) < 0)
                return;

            string parentName = TryResolveParentFrameName(entry.NodeId, cacheRoot);
            if (string.IsNullOrWhiteSpace(parentName))
                return;

            string normalizedName = NormalizeLayerBlurName(parentName);
            if (string.IsNullOrWhiteSpace(normalizedName))
                return;

            if (TryLoadParentFrameProject(entry.NodeId, cacheRoot, out DesignProject parentProject))
            {
                if (TryGetRect(parentProject.Document.AbsoluteRenderBounds, out Rect parentRenderBounds) ||
                    TryGetRect(parentProject.Document.AbsoluteBoundingBox, out parentRenderBounds))
                {
                    entry.Width = Mathf.Max(1, Mathf.CeilToInt(parentRenderBounds.width * scale));
                    entry.Height = Mathf.Max(1, Mathf.CeilToInt(parentRenderBounds.height * scale));
                }
            }

            entry.NodeName = normalizedName;
            entry.OutputFileName = SanitizeFileName(normalizedName) + ".png";
        }

        static string TryResolveParentFrameName(string nodeId, string cacheRoot)
        {
            if (string.IsNullOrWhiteSpace(nodeId))
                return null;

            int separatorIndex = nodeId.IndexOf(':');
            if (separatorIndex < 0 ||
                !int.TryParse(nodeId.Substring(separatorIndex + 1), out int numericId))
                return null;

            string parentNodeId = $"{nodeId.Substring(0, separatorIndex)}:{numericId - 1}";
            string parentCachePath = Path.Combine(cacheRoot, ToCacheFileName(parentNodeId));
            if (!File.Exists(parentCachePath))
                return null;

            DesignProject parentProject = DAJson.FromJson<DesignProject>(File.ReadAllText(parentCachePath));
            return parentProject.Document.Name;
        }

        static string NormalizeLayerBlurName(string name)
        {
            string normalized = name.Trim();
            normalized = Regex.Replace(normalized, @"\bOutside Bounds\b", "O", RegexOptions.IgnoreCase);
            normalized = Regex.Replace(normalized, @"\s+\d{2}$", string.Empty);
            normalized = Regex.Replace(normalized, @"\s+", " ").Trim();
            return normalized;
        }

        static string SanitizeFileName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "unnamed";

            char[] invalid = Path.GetInvalidFileNameChars();
            char[] chars = value.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (Array.IndexOf(invalid, chars[i]) >= 0)
                    chars[i] = '_';
            }

            return new string(chars).Trim();
        }

        static Rect? TryResolveCanvasBounds(RuntimeExportManifestEntry entry, Node node, string cacheRoot, float scale)
        {
            if (entry.Width <= 0 || entry.Height <= 0 || scale <= 0f)
                return null;

            if (TryGetLayerBlurParentBounds(entry, cacheRoot, scale, out Rect layerBlurParentBounds))
                return layerBlurParentBounds;

            if (TryGetRect(node.AbsoluteRenderBounds, out Rect nodeRenderBounds) &&
                BoundsMatchManifest(nodeRenderBounds, entry, scale))
                return nodeRenderBounds;

            if (TryGetRect(node.AbsoluteBoundingBox, out Rect nodeBounds) &&
                BoundsMatchManifest(nodeBounds, entry, scale))
                return nodeBounds;

            if (TryGetShadowStressRenderBounds(entry, cacheRoot, scale, out Rect shadowStressBounds))
                return shadowStressBounds;

            return null;
        }

        static bool TryGetLayerBlurParentBounds(RuntimeExportManifestEntry entry, string cacheRoot, float scale, out Rect bounds)
        {
            bounds = default;

            if (string.IsNullOrWhiteSpace(entry.FrameName) ||
                entry.FrameName.IndexOf("Layer Blur Stress", StringComparison.OrdinalIgnoreCase) < 0)
                return false;

            if (!TryLoadParentFrameProject(entry.NodeId, cacheRoot, out DesignProject parentProject))
                return false;

            if (TryGetRect(parentProject.Document.AbsoluteRenderBounds, out bounds) &&
                BoundsMatchManifest(bounds, entry, scale))
                return true;

            if (TryGetRect(parentProject.Document.AbsoluteBoundingBox, out bounds) &&
                BoundsMatchManifest(bounds, entry, scale))
                return true;

            return false;
        }

        static bool TryGetShadowStressRenderBounds(RuntimeExportManifestEntry entry, string cacheRoot, float scale, out Rect bounds)
        {
            bounds = default;

            if (string.IsNullOrWhiteSpace(entry.FrameName) ||
                entry.FrameName.IndexOf("Shadow Stress", StringComparison.OrdinalIgnoreCase) < 0 ||
                string.IsNullOrWhiteSpace(entry.NodeId))
                return false;

            int separatorIndex = entry.NodeId.IndexOf(':');
            if (separatorIndex < 0 ||
                !int.TryParse(entry.NodeId.Substring(separatorIndex + 1), out int numericId))
                return false;

            string renderBoundsNodeId = $"{entry.NodeId.Substring(0, separatorIndex)}:{numericId - 1}";
            string nodeCachePath = Path.Combine(cacheRoot, ToCacheFileName(renderBoundsNodeId));
            if (!File.Exists(nodeCachePath))
                return false;

            DesignProject renderBoundsProject = DAJson.FromJson<DesignProject>(File.ReadAllText(nodeCachePath));
            if (TryGetRect(renderBoundsProject.Document.AbsoluteRenderBounds, out bounds) &&
                BoundsMatchManifest(bounds, entry, scale))
                return true;

            if (TryGetRect(renderBoundsProject.Document.AbsoluteBoundingBox, out bounds) &&
                BoundsMatchManifest(bounds, entry, scale))
                return true;

            return false;
        }

        static bool TryLoadParentFrameProject(string nodeId, string cacheRoot, out DesignProject parentProject)
        {
            parentProject = default;

            if (string.IsNullOrWhiteSpace(nodeId))
                return false;

            int separatorIndex = nodeId.IndexOf(':');
            if (separatorIndex < 0 ||
                !int.TryParse(nodeId.Substring(separatorIndex + 1), out int numericId))
                return false;

            string parentNodeId = $"{nodeId.Substring(0, separatorIndex)}:{numericId - 1}";
            string parentCachePath = Path.Combine(cacheRoot, ToCacheFileName(parentNodeId));
            if (!File.Exists(parentCachePath))
                return false;

            parentProject = DAJson.FromJson<DesignProject>(File.ReadAllText(parentCachePath));
            return !string.IsNullOrWhiteSpace(parentProject.Document.Id);
        }

        static bool BoundsMatchManifest(Rect bounds, RuntimeExportManifestEntry entry, float scale)
        {
            const float tolerancePx = 2f;
            return Mathf.Abs(bounds.width * scale - entry.Width) <= tolerancePx &&
                   Mathf.Abs(bounds.height * scale - entry.Height) <= tolerancePx;
        }

        static bool TryGetRect(BoundingBox box, out Rect rect)
        {
            rect = default;

            if (!box.X.HasValue || !box.Y.HasValue || !box.Width.HasValue || !box.Height.HasValue)
                return false;

            if (box.Width.Value <= 0f || box.Height.Value <= 0f)
                return false;

            rect = new Rect(box.X.Value, box.Y.Value, box.Width.Value, box.Height.Value);
            return true;
        }

        [Serializable]
        sealed class RuntimeExportManifest
        {
            public string FileKey { get; set; }
            public float ExportScale { get; set; }
            public string GeneratedAtUtc { get; set; }
            public List<RuntimeExportManifestEntry> Entries { get; set; }
        }

        [Serializable]
        sealed class RuntimeExportManifestEntry
        {
            public string FrameName { get; set; }
            public string NodeId { get; set; }
            public string NodeName { get; set; }
            public string OutputFileName { get; set; }
            public string RenderedPath { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public string Error { get; set; }
        }
    }
}
#endif