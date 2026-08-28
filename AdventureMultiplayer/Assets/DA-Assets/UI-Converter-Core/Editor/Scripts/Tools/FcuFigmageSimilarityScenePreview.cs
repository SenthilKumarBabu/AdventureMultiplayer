using DA_Assets.UCC.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using FigmageComponent = DA_Assets.Figmage.Figmage;

namespace DA_Assets.UCC.EditorTools
{
    public static class FcuFigmageSimilarityScenePreview
    {
        const float ExportScale = 2f;
        const string SceneFolderAssetPath = "Assets/DA-Assets/Figma-Converter-for-Unity/Tests/Scenes/FigmageSimilarity";

        static readonly SimilarityFrame[] Frames =
        {
            new SimilarityFrame("FCU Test - Gradient Container 512 Showcase Variants", 16, 190f, 145f, new[] { 270, 273 }),
            new SimilarityFrame("FCU Test - Gradient Container 128 Shadow Stress", 16, 360f, 260f, new[] { 400, 402 }),
            new SimilarityFrame("FCU Test - Gradient Container 80 Layer Blur Stress", 10, 360f, 270f, new[] { 494, 497 })
        };

        [MenuItem("Tools/D.A. Assets/FCU: Create Full Similarity Scenes")]
        public static void CreateFullSimilarityScenes()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string referenceRoot = Path.Combine(projectRoot, "figma_ref");
            string cacheRoot = Path.Combine(projectRoot, "CodexTemp", "Builds", "FigmageSimilarityFull", "NodeJsonCache");

            if (!Directory.Exists(referenceRoot))
                throw new DirectoryNotFoundException(referenceRoot);
            if (!Directory.Exists(cacheRoot))
                throw new DirectoryNotFoundException(cacheRoot);

            EnsureSceneFolder(projectRoot);
            List<SimilaritySceneEntry> entries = LoadEntries(referenceRoot, cacheRoot);

            foreach (SimilarityFrame frame in Frames)
                CreateFrameScene(frame, entries.Where(frame.Contains).ToList(), cacheRoot);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Figmage similarity scenes generated in {SceneFolderAssetPath}.");
        }

        static void CreateFrameScene(SimilarityFrame frame, List<SimilaritySceneEntry> entries, string cacheRoot)
        {
            if (entries.Count == 0)
                throw new InvalidOperationException($"No similarity entries found for {frame.Name}.");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = frame.Name;

            int rows = Mathf.CeilToInt(entries.Count / (float)frame.Columns);
            Vector2 canvasSize = new Vector2(
                frame.Columns * frame.CellWidth + 160f,
                rows * frame.CellHeight + 220f);

            GameObject canvasObject = new GameObject(frame.Name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = canvasSize;

            CreateLabel(canvasRect, frame.Name, new Vector2(0f, canvasSize.y * 0.5f - 55f), new Vector2(canvasSize.x - 120f, 36f), 22, TextAnchor.MiddleLeft);

            float startX = -canvasSize.x * 0.5f + frame.CellWidth * 0.5f + 80f;
            float startY = canvasSize.y * 0.5f - 145f;

            for (int i = 0; i < entries.Count; i++)
            {
                SimilaritySceneEntry entry = entries[i];
                DesignProject project = DAJson.FromJson<DesignProject>(File.ReadAllText(entry.NodeJsonPath));
                Node renderNode = project.Document;
                Rect? canvasBounds = TryResolveSingleChildRenderNode(project.Document, entry.ReferenceWidth, entry.ReferenceHeight, cacheRoot, out Node childNode, out Rect childCanvasBounds)
                    ? childCanvasBounds
                    : ResolveCanvasBounds(project.Document, entry.ReferenceWidth, entry.ReferenceHeight, cacheRoot);
                if (childNode.Id != null)
                    renderNode = childNode;

                FigmageComponent figmage = FcuFigmageComponentFactory.Create(renderNode, canvasRect, canvasBounds);
                figmage.name = $"{entry.NodeId} {project.Document.Name}";

                RectTransform rect = figmage.transform as RectTransform;
                int column = i % frame.Columns;
                int row = i / frame.Columns;
                rect.anchoredPosition = new Vector2(startX + column * frame.CellWidth, startY - row * frame.CellHeight);
                rect.localEulerAngles = Vector3.zero;

                CreateLabel(canvasRect, entry.NodeId, rect.anchoredPosition + new Vector2(0f, -frame.CellHeight * 0.5f + 18f), new Vector2(frame.CellWidth - 20f, 22f), 12, TextAnchor.MiddleCenter);
            }

            string scenePath = $"{SceneFolderAssetPath}/{SanitizeFileName(frame.Name)}.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
        }

        static void EnsureSceneFolder(string projectRoot)
        {
            string physicalPath = Path.Combine(projectRoot, SceneFolderAssetPath);
            Directory.CreateDirectory(physicalPath);
            AssetDatabase.Refresh();
        }

        static List<SimilaritySceneEntry> LoadEntries(string referenceRoot, string cacheRoot)
        {
            List<SimilaritySceneEntry> result = new List<SimilaritySceneEntry>();
            FileInfo[] pngFiles = new DirectoryInfo(referenceRoot).GetFiles("*.png", SearchOption.TopDirectoryOnly);

            foreach (FileInfo pngFile in pngFiles)
            {
                string nodeId = Path.GetFileNameWithoutExtension(pngFile.Name).Replace('_', ':');
                if (IsExcludedReferenceNode(nodeId))
                    continue;

                string nodeJsonPath = Path.Combine(cacheRoot, ToCacheFileName(nodeId));
                if (!File.Exists(nodeJsonPath))
                    continue;

                Texture2D reference = LoadPng(pngFile.FullName);
                try
                {
                    result.Add(new SimilaritySceneEntry(nodeId, pngFile.FullName, nodeJsonPath, reference.width, reference.height));
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(reference);
                }
            }

            result.Sort((a, b) => CompareNodeIds(a.NodeId, b.NodeId));
            return result;
        }

        static Rect? ResolveCanvasBounds(Node node, int referenceWidth, int referenceHeight, string cacheRoot)
        {
            if (TryGetRect(node.AbsoluteRenderBounds, out Rect renderBounds) &&
                MatchesReferenceSize(renderBounds, referenceWidth, referenceHeight))
            {
                return NormalizeCanvasBounds(renderBounds, referenceWidth, referenceHeight);
            }

            if (TryGetRect(node.AbsoluteBoundingBox, out Rect boundingBox) &&
                MatchesReferenceSize(boundingBox, referenceWidth, referenceHeight))
            {
                return NormalizeCanvasBounds(boundingBox, referenceWidth, referenceHeight);
            }

            if (TryResolvePreviousNodeBounds(node.Id, referenceWidth, referenceHeight, cacheRoot, out Rect previousNodeBounds))
                return previousNodeBounds;

            return null;
        }

        static bool TryResolveSingleChildRenderNode(
            Node frameNode,
            int referenceWidth,
            int referenceHeight,
            string cacheRoot,
            out Node childNode,
            out Rect canvasBounds)
        {
            childNode = default;
            canvasBounds = default;

            if (HasVisibleBodyPaint(frameNode) ||
                frameNode.Children == null ||
                frameNode.Children.Count != 1)
            {
                return false;
            }

            if (!TryGetRect(frameNode.AbsoluteRenderBounds, out canvasBounds) &&
                !TryGetRect(frameNode.AbsoluteBoundingBox, out canvasBounds))
            {
                return false;
            }

            if (!MatchesReferenceSize(canvasBounds, referenceWidth, referenceHeight))
                return false;

            canvasBounds = NormalizeCanvasBounds(canvasBounds, referenceWidth, referenceHeight);

            string childPath = Path.Combine(cacheRoot, ToCacheFileName(frameNode.Children[0].Id));
            if (!File.Exists(childPath))
                return false;

            DesignProject childProject = DAJson.FromJson<DesignProject>(File.ReadAllText(childPath));
            if (string.IsNullOrWhiteSpace(childProject.Document.Id))
                return false;

            childNode = childProject.Document;
            return true;
        }

        static bool HasVisibleBodyPaint(Node node)
        {
            if (HasVisiblePaint(node.Fills))
                return true;

            return node.StrokeWeight > 0f && HasVisiblePaint(node.Strokes);
        }

        static bool HasVisiblePaint(List<Paint> paints)
        {
            if (paints == null)
                return false;

            for (int i = 0; i < paints.Count; i++)
            {
                if (paints[i].Visible != false)
                    return true;
            }

            return false;
        }

        static bool TryResolvePreviousNodeBounds(string nodeId, int referenceWidth, int referenceHeight, string cacheRoot, out Rect bounds)
        {
            bounds = default;
            if (!TryGetPreviousNodeId(nodeId, out string previousNodeId))
                return false;

            string previousPath = Path.Combine(cacheRoot, ToCacheFileName(previousNodeId));
            if (!File.Exists(previousPath))
                return false;

            DesignProject previousProject = DAJson.FromJson<DesignProject>(File.ReadAllText(previousPath));
            if (TryGetRect(previousProject.Document.AbsoluteRenderBounds, out bounds) &&
                MatchesReferenceSize(bounds, referenceWidth, referenceHeight))
            {
                bounds = NormalizeCanvasBounds(bounds, referenceWidth, referenceHeight);
                return true;
            }

            if (TryGetRect(previousProject.Document.AbsoluteBoundingBox, out bounds) &&
                MatchesReferenceSize(bounds, referenceWidth, referenceHeight))
            {
                bounds = NormalizeCanvasBounds(bounds, referenceWidth, referenceHeight);
                return true;
            }

            bounds = default;
            return false;
        }

        static bool TryGetPreviousNodeId(string nodeId, out string previousNodeId)
        {
            previousNodeId = null;
            int separatorIndex = nodeId.IndexOf(':');
            if (separatorIndex < 0 || !int.TryParse(nodeId.Substring(separatorIndex + 1), out int numericId))
                return false;

            previousNodeId = $"{nodeId.Substring(0, separatorIndex)}:{numericId - 1}";
            return true;
        }

        static bool MatchesReferenceSize(Rect bounds, int referenceWidth, int referenceHeight)
        {
            const float tolerancePx = 2f;
            return Mathf.Abs(bounds.width * ExportScale - referenceWidth) <= tolerancePx &&
                   Mathf.Abs(bounds.height * ExportScale - referenceHeight) <= tolerancePx;
        }

        static Rect NormalizeCanvasBounds(Rect bounds, int referenceWidth, int referenceHeight)
        {
            bounds.width = referenceWidth / ExportScale;
            bounds.height = referenceHeight / ExportScale;
            return bounds;
        }

        static bool TryGetRect(BoundingBox box, out Rect rect)
        {
            rect = default;
            if (!box.X.HasValue || !box.Y.HasValue || !box.Width.HasValue || !box.Height.HasValue)
                return false;

            rect = new Rect(box.X.Value, box.Y.Value, box.Width.Value, box.Height.Value);
            return rect.width > 0f && rect.height > 0f;
        }

        static void CreateLabel(RectTransform parent, string text, Vector2 anchoredPosition, Vector2 size, int fontSize, TextAnchor alignment)
        {
            GameObject labelObject = new GameObject(text, typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(parent, false);
            RectTransform rect = labelObject.GetComponent<RectTransform>();
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Text label = labelObject.GetComponent<Text>();
            label.text = text;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = Color.white;
            label.raycastTarget = false;
        }

        static Texture2D LoadPng(string path)
        {
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            if (!texture.LoadImage(File.ReadAllBytes(path)))
                throw new InvalidOperationException($"Failed to load PNG: {path}");
            return texture;
        }

        static int CompareNodeIds(string left, string right)
        {
            string[] l = left.Split(':');
            string[] r = right.Split(':');
            int page = int.Parse(l[0]).CompareTo(int.Parse(r[0]));
            if (page != 0)
                return page;
            return int.Parse(l[1]).CompareTo(int.Parse(r[1]));
        }

        static string ToCacheFileName(string nodeId)
        {
            return $"{nodeId.Replace(':', '-')}.json";
        }

        static bool IsExcludedReferenceNode(string nodeId)
        {
            return nodeId.StartsWith("635:", StringComparison.Ordinal);
        }

        static string SanitizeFileName(string value)
        {
            char[] invalid = Path.GetInvalidFileNameChars();
            char[] chars = value.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (Array.IndexOf(invalid, chars[i]) >= 0)
                    chars[i] = '_';
            }

            return new string(chars).Trim();
        }

        readonly struct SimilarityFrame
        {
            readonly HashSet<int> prefixes;

            public SimilarityFrame(string name, int columns, float cellWidth, float cellHeight, int[] prefixes)
            {
                Name = name;
                Columns = columns;
                CellWidth = cellWidth;
                CellHeight = cellHeight;
                this.prefixes = new HashSet<int>(prefixes);
            }

            public string Name { get; }
            public int Columns { get; }
            public float CellWidth { get; }
            public float CellHeight { get; }

            public bool Contains(SimilaritySceneEntry entry)
            {
                int separatorIndex = entry.NodeId.IndexOf(':');
                if (separatorIndex < 0 || !int.TryParse(entry.NodeId.Substring(0, separatorIndex), out int prefix))
                    return false;

                return prefixes.Contains(prefix);
            }
        }

        readonly struct SimilaritySceneEntry
        {
            public SimilaritySceneEntry(string nodeId, string referencePath, string nodeJsonPath, int referenceWidth, int referenceHeight)
            {
                NodeId = nodeId;
                ReferencePath = referencePath;
                NodeJsonPath = nodeJsonPath;
                ReferenceWidth = referenceWidth;
                ReferenceHeight = referenceHeight;
            }

            public string NodeId { get; }
            public string ReferencePath { get; }
            public string NodeJsonPath { get; }
            public int ReferenceWidth { get; }
            public int ReferenceHeight { get; }
        }
    }
}