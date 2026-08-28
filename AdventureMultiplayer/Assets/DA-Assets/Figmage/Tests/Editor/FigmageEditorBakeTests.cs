using NUnit.Framework;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DA_Assets.Figmage.Tests
{
    public sealed class FigmageEditorBakeTests
    {
        const string TempBakePath = "Assets/__FigmageBakeTest.png";
        const string TempSourceBakePath = "Assets/__FigmageSourceBakeTest.png";

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(TempBakePath);
            AssetDatabase.DeleteAsset(TempSourceBakePath);
        }

        [Test]
        public void BakeToSprite_AssignsSpriteDisablesShaderAndStoresGuid()
        {
            GameObject gameObject = new GameObject("Figmage Bake Test", typeof(RectTransform), typeof(Image), typeof(Figmage));
            try
            {
                RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(24f, 18f);

                Figmage figmage = gameObject.GetComponent<Figmage>();
                figmage.Fills.Clear();
                figmage.Fills.Add(FigmagePaint.Solid(Color.red));

                Sprite firstSprite = FigmageEditor.BakeToSprite(figmage, 1f, TempBakePath);
                string firstGuid = AssetDatabase.AssetPathToGUID(TempBakePath);

                Assert.That(firstSprite, Is.Not.Null);
                Assert.That(figmage.Image.sprite, Is.EqualTo(firstSprite));
                Assert.That(figmage.ShaderEnabled, Is.False);
                Assert.That(figmage.BakedSpriteGuid, Is.EqualTo(firstGuid));

                figmage.ShaderEnabled = true;
                figmage.Fills.Clear();
                figmage.Fills.Add(FigmagePaint.Solid(Color.green));

                Sprite secondSprite = FigmageEditor.BakeToSprite(figmage, 1f, TempBakePath);

                Assert.That(secondSprite, Is.Not.Null);
                Assert.That(figmage.Image.sprite, Is.EqualTo(secondSprite));
                Assert.That(figmage.ShaderEnabled, Is.False);
                Assert.That(figmage.BakedSpriteGuid, Is.EqualTo(firstGuid));
                Assert.That(AssetDatabase.GUIDToAssetPath(figmage.BakedSpriteGuid), Is.EqualTo(TempBakePath));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void BakeToSprite_SourceBoundsKeepsOverflowAnchorsLocal()
        {
            GameObject gameObject = new GameObject("Figmage Source Bake Test", typeof(RectTransform), typeof(Image), typeof(Figmage));
            try
            {
                FigmageNode source = CreateNode(100, 80);
                source.AbsoluteBoundingBox = new FigmageRect(240f, 120f, 100f, 80f);
                source.AbsoluteRenderBounds = new FigmageRect(230f, 112f, 124f, 96f);
                source.Fills.Add(FigmagePaint.Solid(Color.white));

                Figmage figmage = gameObject.GetComponent<Figmage>();
                figmage.ApplyNode(source);

                Sprite sprite = FigmageEditor.BakeToSprite(figmage, 1f, TempSourceBakePath);

                Assert.That(sprite, Is.Not.Null);
                Assert.That(sprite.texture.width, Is.EqualTo(124));
                Assert.That(sprite.texture.height, Is.EqualTo(96));
                Assert.That(figmage.ShaderEnabled, Is.False);
                Assert.That(figmage.OverflowAnchors.Left, Is.EqualTo(0.1f).Within(0.0001f));
                Assert.That(figmage.OverflowAnchors.Right, Is.EqualTo(0.14f).Within(0.0001f));
                Assert.That(figmage.OverflowAnchors.Top, Is.EqualTo(0.1f).Within(0.0001f));
                Assert.That(figmage.OverflowAnchors.Bottom, Is.EqualTo(0.1f).Within(0.0001f));

                VertexHelper vertexHelper = new VertexHelper();
                try
                {
                    figmage.ModifyMesh(vertexHelper);
                    Rect meshRect = GetMeshRect(vertexHelper);
                    Assert.That(meshRect.width, Is.EqualTo(124f).Within(0.001f));
                    Assert.That(meshRect.height, Is.EqualTo(96f).Within(0.001f));
                }
                finally
                {
                    vertexHelper.Dispose();
                }
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        static Rect GetMeshRect(VertexHelper vertexHelper)
        {
            UIVertex vertex = default;
            Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);

            for (int i = 0; i < vertexHelper.currentVertCount; i++)
            {
                vertexHelper.PopulateUIVertex(ref vertex, i);
                min = Vector2.Min(min, vertex.position);
                max = Vector2.Max(max, vertex.position);
            }

            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        static FigmageNode CreateNode(int width, int height)
        {
            return new FigmageNode
            {
                Id = "test",
                Name = "test",
                AbsoluteBoundingBox = new FigmageRect(0f, 0f, width, height),
                AbsoluteRenderBounds = new FigmageRect(0f, 0f, width, height),
                Size = new Vector2(width, height),
                Opacity = 1f,
                CornerRadius = 0f,
                StrokeAlign = FigmageStrokeAlign.Center,
                StrokeWeight = 0f,
                StrokeJoin = FigmageStrokeJoin.Miter,
                StrokeCap = FigmageStrokeCap.None,
                StrokeMiterLimit = 4f,
                DashPattern = new List<float>(),
                IndividualStrokeWeights = FigmageStrokeWeights.Uniform(0f),
                Fills = new List<FigmagePaint>(),
                Strokes = new List<FigmagePaint>(),
                Effects = new List<FigmageEffect>()
            };
        }
    }
}
