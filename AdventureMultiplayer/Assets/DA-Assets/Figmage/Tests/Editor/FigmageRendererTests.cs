using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DA_Assets.Figmage.Tests
{
    public sealed class FigmageRendererTests
    {
        [Test]
        public void BakePngBytes_SolidFill_ReturnsExpectedPixels()
        {
            FigmageNode node = CreateNode(32, 24);
            node.Fills.Add(FigmagePaint.Solid(Color.red));

            byte[] png = FigmageBakeUtility.BakePngBytes(node);

            Assert.That(png, Is.Not.Null);
            Assert.That(png.Length, Is.GreaterThan(0));

            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            Assert.That(texture.LoadImage(png), Is.True);
            Assert.That(texture.width, Is.EqualTo(32));
            Assert.That(texture.height, Is.EqualTo(24));

            Color center = texture.GetPixel(texture.width / 2, texture.height / 2);
            Assert.That(center.r, Is.GreaterThan(0.95f));
            Assert.That(center.g, Is.LessThan(0.05f));
            Assert.That(center.b, Is.LessThan(0.05f));
            Object.DestroyImmediate(texture);
        }

        [Test]
        public void LinearGradient_CopiesFigmaHandlesAndStopsIndependently()
        {
            List<Vector2> handles = new List<Vector2>
            {
                new Vector2(0f, 0.5f),
                new Vector2(1f, 0.5f),
                new Vector2(0f, 1f)
            };
            List<FigmageGradientStop> stops = new List<FigmageGradientStop>
            {
                new FigmageGradientStop(0f, Color.blue),
                new FigmageGradientStop(0.5f, Color.green),
                new FigmageGradientStop(1f, Color.red)
            };

            FigmagePaint paint = FigmagePaint.LinearGradient(handles, stops);
            handles.Clear();
            stops.Clear();

            Assert.That(paint.Type, Is.EqualTo(FigmagePaintType.GradientLinear));
            Assert.That(paint.GradientHandlePositions, Has.Count.EqualTo(3));
            Assert.That(paint.GradientStops, Has.Count.EqualTo(3));
            Assert.That(paint.GradientStops[1].Position, Is.EqualTo(0.5f));
        }

        [Test]
        public void BakePngBytes_LinearGradient_ContainsBothEndColors()
        {
            FigmageNode node = CreateNode(64, 16);
            node.Fills.Add(FigmagePaint.LinearGradient(
                new List<Vector2>
                {
                    new Vector2(0f, 0.5f),
                    new Vector2(1f, 0.5f),
                    new Vector2(0f, 1f)
                },
                new List<FigmageGradientStop>
                {
                    new FigmageGradientStop(0f, Color.blue),
                    new FigmageGradientStop(1f, Color.red)
                }));

            byte[] png = FigmageBakeUtility.BakePngBytes(node);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            Assert.That(texture.LoadImage(png), Is.True);

            Color left = texture.GetPixel(2, texture.height / 2);
            Color right = texture.GetPixel(texture.width - 3, texture.height / 2);
            Assert.That(left.b, Is.GreaterThan(left.r));
            Assert.That(right.r, Is.GreaterThan(right.b));
            Object.DestroyImmediate(texture);
        }

        [Test]
        public void BakePngBytes_LinearGradient_UsesStopsBeyondLegacyShaderLimit()
        {
            FigmageNode node = CreateNode(64, 16);
            node.Fills.Add(FigmagePaint.LinearGradient(
                new List<Vector2>
                {
                    new Vector2(0f, 0.5f),
                    new Vector2(1f, 0.5f),
                    new Vector2(0f, 1f)
                },
                new List<FigmageGradientStop>
                {
                    new FigmageGradientStop(0f, Color.red),
                    new FigmageGradientStop(0.16f, Color.red),
                    new FigmageGradientStop(0.32f, Color.red),
                    new FigmageGradientStop(0.48f, Color.red),
                    new FigmageGradientStop(0.64f, Color.red),
                    new FigmageGradientStop(0.82f, Color.red),
                    new FigmageGradientStop(1f, Color.green)
                }));

            Texture2D texture = LoadTexture(FigmageBakeUtility.BakePngBytes(node));
            Color right = texture.GetPixel(texture.width - 3, texture.height / 2);

            Assert.That(right.g, Is.GreaterThan(right.r));
            Object.DestroyImmediate(texture);
        }

        [Test]
        public void BakePngBytes_CompositesFillLayersBeyondLegacyShaderLimit()
        {
            FigmageNode node = CreateNode(32, 32);
            node.Fills.Add(FigmagePaint.Solid(Color.white));
            node.Fills.Add(FigmagePaint.Solid(Color.red));
            node.Fills.Add(FigmagePaint.Solid(Color.green));
            node.Fills.Add(FigmagePaint.Solid(Color.blue));

            Texture2D texture = LoadTexture(FigmageBakeUtility.BakePngBytes(node));
            Color center = texture.GetPixel(texture.width / 2, texture.height / 2);

            Assert.That(center.b, Is.GreaterThan(0.9f));
            Assert.That(center.r, Is.LessThan(0.1f));
            Assert.That(center.g, Is.LessThan(0.1f));
            Object.DestroyImmediate(texture);
        }

        [Test]
        public void BakePngBytes_CompositesStrokeLayersBeyondLegacyShaderLimit()
        {
            FigmageNode node = CreateNode(40, 40);
            node.StrokeWeight = 8f;
            node.StrokeAlign = FigmageStrokeAlign.Inside;
            node.Strokes.Add(FigmagePaint.Solid(Color.blue));
            node.Strokes.Add(FigmagePaint.Solid(Color.green));
            node.Strokes.Add(FigmagePaint.Solid(Color.red));

            Texture2D texture = LoadTexture(FigmageBakeUtility.BakePngBytes(node));
            Color topStroke = texture.GetPixel(texture.width / 2, texture.height - 3);

            Assert.That(topStroke.r, Is.GreaterThan(0.9f));
            Assert.That(topStroke.g, Is.LessThan(0.1f));
            Assert.That(topStroke.b, Is.LessThan(0.1f));
            Object.DestroyImmediate(texture);
        }

        [Test]
        public void BakePngBytes_StrokeGeometry_RendersOnlyProvidedStrokeArea()
        {
            FigmageNode node = CreateNode(80, 40);
            node.Fills.Add(FigmagePaint.Solid(Color.white));
            node.StrokeWeight = 6f;
            node.StrokeAlign = FigmageStrokeAlign.Inside;
            node.Strokes.Add(FigmagePaint.Solid(Color.blue));
            node.StrokeGeometry = new List<FigmageGeometry>
            {
                new FigmageGeometry { Path = "M 0 34 L 80 34 L 80 40 L 0 40 Z" }
            };

            Texture2D texture = LoadTexture(FigmageBakeUtility.BakePngBytes(node));
            Color top = texture.GetPixel(texture.width / 2, texture.height - 3);
            Color bottom = texture.GetPixel(texture.width / 2, 3);

            Assert.That(top.b, Is.LessThan(0.1f));
            Assert.That(top.r, Is.GreaterThan(0.9f));
            Assert.That(bottom.b, Is.GreaterThan(0.9f));
            Object.DestroyImmediate(texture);
        }

        [Test]
        public void BakePngBytes_StrokeGeometry_RendersAllProvidedPaths()
        {
            FigmageNode node = CreateNode(80, 40);
            node.Fills.Add(FigmagePaint.Solid(Color.white));
            node.StrokeWeight = 6f;
            node.StrokeAlign = FigmageStrokeAlign.Inside;
            node.Strokes.Add(FigmagePaint.Solid(Color.blue));
            node.StrokeGeometry = new List<FigmageGeometry>
            {
                new FigmageGeometry { Path = "M 0 34 L 18 34 L 18 40 L 0 40 Z" },
                new FigmageGeometry { Path = "M 62 34 L 80 34 L 80 40 L 62 40 Z" }
            };

            Texture2D texture = LoadTexture(FigmageBakeUtility.BakePngBytes(node));
            Color left = texture.GetPixel(8, 3);
            Color center = texture.GetPixel(texture.width / 2, 3);
            Color right = texture.GetPixel(texture.width - 8, 3);

            Assert.That(left.b, Is.GreaterThan(0.9f));
            Assert.That(center.b, Is.LessThan(0.1f));
            Assert.That(right.b, Is.GreaterThan(0.9f));
            Object.DestroyImmediate(texture);
        }

        [Test]
        public void BakePngBytes_StrokeGeometry_ClipsInsideStrokeToRoundedShape()
        {
            FigmageNode node = CreateNode(80, 40);
            node.CornerRadius = 16f;
            node.Fills.Add(FigmagePaint.Solid(Color.white));
            node.StrokeWeight = 6f;
            node.StrokeAlign = FigmageStrokeAlign.Inside;
            node.Strokes.Add(FigmagePaint.Solid(Color.blue));
            node.StrokeGeometry = new List<FigmageGeometry>
            {
                new FigmageGeometry { Path = "M 0 34 L 80 34 L 80 40 L 0 40 Z" }
            };

            Texture2D texture = LoadTexture(FigmageBakeUtility.BakePngBytes(node));
            Color bottomCenter = texture.GetPixel(texture.width / 2, 3);
            Color bottomLeftCorner = texture.GetPixel(2, 2);
            Color bottomRightCorner = texture.GetPixel(texture.width - 3, 2);

            Assert.That(bottomCenter.b, Is.GreaterThan(0.9f));
            Assert.That(bottomCenter.r, Is.LessThan(0.1f));
            Assert.That(bottomLeftCorner.a, Is.LessThan(0.1f));
            Assert.That(bottomRightCorner.a, Is.LessThan(0.1f));
            Object.DestroyImmediate(texture);
        }

        [Test]
        public void BakePngBytes_OutsideStroke_ExpandsSpriteCanvas()
        {
            FigmageNode node = CreateNode(32, 24);
            node.StrokeWeight = 6f;
            node.StrokeAlign = FigmageStrokeAlign.Outside;
            node.Strokes.Add(FigmagePaint.Solid(Color.white));

            byte[] png = FigmageBakeUtility.BakePngBytes(node);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            Assert.That(texture.LoadImage(png), Is.True);
            Assert.That(texture.width, Is.EqualTo(44));
            Assert.That(texture.height, Is.EqualTo(36));
            Object.DestroyImmediate(texture);
        }

        [Test]
        public void BakePngBytes_UsesRequestedScale()
        {
            FigmageNode node = CreateNode(32, 24);
            node.Fills.Add(FigmagePaint.Solid(Color.white));

            byte[] png = FigmageBakeUtility.BakePngBytes(node, 4f);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            Assert.That(texture.LoadImage(png), Is.True);
            Assert.That(texture.width, Is.EqualTo(128));
            Assert.That(texture.height, Is.EqualTo(96));
            Object.DestroyImmediate(texture);
        }

        [Test]
        public void BakePngBytes_RectangleCornerRadiuses_MapToFigmaCornerOrder()
        {
            FigmageNode node = CreateNode(32, 32);
            node.CornerRadiuses = new List<float> { 14f, 0f, 0f, 0f };
            node.Fills.Add(FigmagePaint.Solid(Color.white));

            byte[] png = FigmageBakeUtility.BakePngBytes(node);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            Assert.That(texture.LoadImage(png), Is.True);

            Color topLeft = texture.GetPixel(1, texture.height - 2);
            Color topRight = texture.GetPixel(texture.width - 2, texture.height - 2);
            Color bottomLeft = texture.GetPixel(1, 1);
            Assert.That(topLeft.a, Is.LessThan(0.1f));
            Assert.That(topRight.a, Is.GreaterThan(0.9f));
            Assert.That(bottomLeft.a, Is.GreaterThan(0.9f));
            Object.DestroyImmediate(texture);
        }

        [Test]
        public void BakePngBytes_CornerSmoothing_ChangesRoundedRectShape()
        {
            FigmageNode circular = CreateNode(40, 40);
            circular.CornerRadius = 18f;
            circular.CornerSmoothing = 0f;
            circular.Fills.Add(FigmagePaint.Solid(Color.white));

            FigmageNode smoothed = CreateNode(40, 40);
            smoothed.CornerRadius = 18f;
            smoothed.CornerSmoothing = 0.6f;
            smoothed.Fills.Add(FigmagePaint.Solid(Color.white));

            byte[] circularPng = FigmageBakeUtility.BakePngBytes(circular);
            byte[] smoothedPng = FigmageBakeUtility.BakePngBytes(smoothed);

            Assert.That(smoothedPng, Is.Not.EqualTo(circularPng));
        }

        [Test]
        public void BakePngBytes_CornerSmoothing_UsesFigmaBezierPathBudget()
        {
            Type rendererType = typeof(FigmageBakeUtility).Assembly.GetType("DA_Assets.Figmage.FigmageRenderer");
            MethodInfo method = rendererType.GetMethod("BuildSmoothedRoundedRectPath", BindingFlags.Static | BindingFlags.NonPublic);

            string path = (string)method.Invoke(null, new object[] { 100f, 100f, new Vector4(24f, 24f, 24f, 24f), 0.6f });
            string[] tokens = path.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            float firstX = float.Parse(tokens[1], CultureInfo.InvariantCulture);

            Assert.That(tokens[0], Is.EqualTo("M"));
            Assert.That(firstX, Is.EqualTo(61.6f).Within(0.001f));
            Assert.That(firstX, Is.LessThan(76f));
        }

        [Test]
        public void FigmageComponent_ToNode_CopiesIndividualCornerRadiuses()
        {
            GameObject gameObject = new GameObject("Figmage Corner Test", typeof(RectTransform), typeof(Image), typeof(Figmage));
            try
            {
                Figmage figmage = gameObject.GetComponent<Figmage>();
                figmage.CornerRadiuses.AddRange(new[] { 4f, 8f, 12f, 16f });

                FigmageNode node = figmage.ToNode();

                Assert.That(node.CornerRadiuses, Is.EqualTo(new[] { 4f, 8f, 12f, 16f }));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void FigmageNode_DefaultCornerSmoothing_IsCircular()
        {
            FigmageNode node = CreateNode(32, 32);

            Assert.That(node.CornerSmoothing, Is.EqualTo(0f));
        }

        [Test]
        public void FigmageComponent_ToNode_CopiesCornerSmoothing()
        {
            GameObject gameObject = new GameObject("Figmage Corner Smoothing Test", typeof(RectTransform), typeof(Image), typeof(Figmage));
            try
            {
                Figmage figmage = gameObject.GetComponent<Figmage>();
                FieldInfo smoothingField = typeof(Figmage).GetField("cornerSmoothing", BindingFlags.Instance | BindingFlags.NonPublic);
                smoothingField.SetValue(figmage, 0.6f);

                FigmageNode node = figmage.ToNode();

                Assert.That(node.CornerSmoothing, Is.EqualTo(0.6f).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void FigmageComponent_ToNode_CopiesStrokeSettings()
        {
            GameObject gameObject = new GameObject("Figmage Stroke Settings Test", typeof(RectTransform), typeof(Image), typeof(Figmage));
            try
            {
                Figmage figmage = gameObject.GetComponent<Figmage>();
                FieldInfo strokeJoinField = typeof(Figmage).GetField("strokeJoin", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo strokeCapField = typeof(Figmage).GetField("strokeCap", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo strokeMiterField = typeof(Figmage).GetField("strokeMiterLimit", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo strokeWeightsField = typeof(Figmage).GetField("individualStrokeWeights", BindingFlags.Instance | BindingFlags.NonPublic);
                strokeJoinField.SetValue(figmage, FigmageStrokeJoin.Round);
                strokeCapField.SetValue(figmage, FigmageStrokeCap.Square);
                strokeMiterField.SetValue(figmage, 3f);
                strokeWeightsField.SetValue(figmage, new FigmageStrokeWeights(1f, 2f, 3f, 4f));
                figmage.DashPattern.AddRange(new[] { 8f, 4f });

                FigmageNode node = figmage.ToNode();

                Assert.That(node.StrokeJoin, Is.EqualTo(FigmageStrokeJoin.Round));
                Assert.That(node.StrokeCap, Is.EqualTo(FigmageStrokeCap.Square));
                Assert.That(node.StrokeMiterLimit, Is.EqualTo(3f));
                Assert.That(node.DashPattern, Is.EqualTo(new[] { 8f, 4f }));
                Assert.That(node.IndividualStrokeWeights.Top, Is.EqualTo(1f));
                Assert.That(node.IndividualStrokeWeights.Bottom, Is.EqualTo(2f));
                Assert.That(node.IndividualStrokeWeights.Left, Is.EqualTo(3f));
                Assert.That(node.IndividualStrokeWeights.Right, Is.EqualTo(4f));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void FigmageComponent_ToNode_RestoresSourceRelativeTransformAfterSerialization()
        {
            GameObject gameObject = new GameObject("Figmage Source Transform Test", typeof(RectTransform), typeof(Image), typeof(Figmage));
            try
            {
                FigmageNode source = CreateNode(32, 20);
                source.Rotation = 0.314159274f;
                source.RelativeTransform = new List<List<float>>
                {
                    new List<float> { 0.95105654f, -0.309017f, 53f },
                    new List<float> { 0.309017f, 0.95105654f, 48f }
                };
                source.Fills.Add(FigmagePaint.Solid(Color.white));

                Figmage figmage = gameObject.GetComponent<Figmage>();
                figmage.ApplyNode(source);

                FieldInfo sourceNodeField = typeof(Figmage).GetField("sourceNode", BindingFlags.Instance | BindingFlags.NonPublic);
                FigmageNode serializedSource = (FigmageNode)sourceNodeField.GetValue(figmage);
                serializedSource.RelativeTransform = null;

                FigmageNode restored = figmage.ToNode();

                Assert.That(restored.RelativeTransform, Is.Not.Null);
                Assert.That(restored.RelativeTransform[0], Is.EqualTo(source.RelativeTransform[0]).Within(0.0001f));
                Assert.That(restored.RelativeTransform[1], Is.EqualTo(source.RelativeTransform[1]).Within(0.0001f));
                Assert.That(restored.Rotation, Is.EqualTo(source.Rotation).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void FigmageComponent_ToNode_UsesCurrentInspectorFieldsAfterApplyNode()
        {
            GameObject gameObject = new GameObject("Figmage Inspector Field Test", typeof(RectTransform), typeof(Image), typeof(Figmage));
            try
            {
                FigmageNode source = CreateNode(32, 20);
                source.Opacity = 0.25f;
                source.CornerRadius = 2f;
                source.FillGeometry = new List<FigmageGeometry>
                {
                    new FigmageGeometry { Path = "M 0 0 L 32 0 L 32 20 L 0 20 Z" }
                };
                source.Fills.Add(FigmagePaint.Solid(Color.white));

                Figmage figmage = gameObject.GetComponent<Figmage>();
                figmage.ApplyNode(source);
                figmage.Fills.Clear();
                figmage.Fills.Add(FigmagePaint.Solid(Color.red));

                FieldInfo opacityField = typeof(Figmage).GetField("opacity", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo cornerRadiusField = typeof(Figmage).GetField("cornerRadius", BindingFlags.Instance | BindingFlags.NonPublic);
                opacityField.SetValue(figmage, 0.75f);
                cornerRadiusField.SetValue(figmage, 9f);

                FigmageNode node = figmage.ToNode();

                Assert.That(node.Opacity, Is.EqualTo(0.75f).Within(0.0001f));
                Assert.That(node.CornerRadius, Is.EqualTo(9f).Within(0.0001f));
                Assert.That(node.Fills[0].Color, Is.EqualTo(Color.red));
                Assert.That(node.Id, Is.EqualTo(source.Id));
                Assert.That(node.Size, Is.EqualTo(source.Size));
                Assert.That(node.AbsoluteBoundingBox.Width, Is.EqualTo(source.AbsoluteBoundingBox.Width).Within(0.0001f));
                Assert.That(node.FillGeometry, Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void FigmageComponent_DoesNotSerializeManualSize()
        {
            FieldInfo sizeField = typeof(Figmage).GetField("size", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.That(sizeField, Is.Null);
        }

        [Test]
        public void FigmageComponent_ExecutesInEditModeForScenePreview()
        {
            Assert.That(typeof(Figmage).GetCustomAttribute<ExecuteAlways>(), Is.Not.Null);
        }

        [Test]
        public void FigmageComponent_OnEnable_AssignsPreviewSpriteInEditMode()
        {
            GameObject gameObject = new GameObject("Figmage Enable Test", typeof(RectTransform), typeof(Image), typeof(Figmage));
            try
            {
                RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(32f, 20f);

                Figmage figmage = gameObject.GetComponent<Figmage>();
                figmage.enabled = false;
                figmage.Image.sprite = null;
                figmage.enabled = true;

                Assert.That(figmage.Image.sprite, Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void FigmageComponent_RefreshImmediate_AssignsPreviewSpriteFromRectTransform()
        {
            GameObject gameObject = new GameObject("Figmage Preview Test", typeof(RectTransform), typeof(Image), typeof(Figmage));
            try
            {
                RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(40f, 28f);

                Figmage figmage = gameObject.GetComponent<Figmage>();
                figmage.Fills.Clear();
                figmage.Fills.Add(FigmagePaint.Solid(Color.green));
                figmage.RefreshImmediate();

                Assert.That(figmage.Image.sprite, Is.Not.Null);
                Assert.That(figmage.Image.sprite.texture.width, Is.EqualTo(40));
                Assert.That(figmage.Image.sprite.texture.height, Is.EqualTo(28));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void FigmageComponent_RefreshImmediate_ClearsExistingImageMaterial()
        {
            GameObject gameObject = new GameObject("Figmage Material Test", typeof(RectTransform), typeof(Image), typeof(Figmage));
            Material customMaterial = null;
            try
            {
                RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(24f, 24f);

                Image image = gameObject.GetComponent<Image>();
                customMaterial = new Material(Shader.Find("UI/Default"));
                image.material = customMaterial;

                Figmage figmage = gameObject.GetComponent<Figmage>();
                figmage.Fills.Clear();
                figmage.Fills.Add(FigmagePaint.Solid(Color.cyan));
                figmage.RefreshImmediate();

                Assert.That(image.sprite, Is.Not.Null);
                Assert.That(image.material, Is.EqualTo(image.defaultMaterial));
            }
            finally
            {
                if (customMaterial != null)
                    Object.DestroyImmediate(customMaterial);
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void FigmageComponent_RefreshImmediate_RecalculatesOverflowFromCurrentEffects()
        {
            GameObject gameObject = new GameObject("Figmage Overflow Recalculate Test", typeof(RectTransform), typeof(Image), typeof(Figmage));
            try
            {
                RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(100f, 80f);

                Figmage figmage = gameObject.GetComponent<Figmage>();
                figmage.Fills.Clear();
                figmage.Fills.Add(FigmagePaint.Solid(Color.white));
                figmage.Effects.Add(new FigmageEffect
                {
                    Type = FigmageEffectType.DropShadow,
                    Visible = true,
                    Color = Color.black,
                    Opacity = 1f,
                    Offset = new Vector2(0f, -20f),
                    Radius = 4f
                });

                figmage.RefreshImmediate();
                Assert.That(HasAnyOverflow(figmage.OverflowAnchors), Is.True);

                figmage.Effects.Clear();
                figmage.RefreshImmediate();

                Assert.That(figmage.OverflowAnchors.Left, Is.EqualTo(0f).Within(0.0001f));
                Assert.That(figmage.OverflowAnchors.Right, Is.EqualTo(0f).Within(0.0001f));
                Assert.That(figmage.OverflowAnchors.Top, Is.EqualTo(0f).Within(0.0001f));
                Assert.That(figmage.OverflowAnchors.Bottom, Is.EqualTo(0f).Within(0.0001f));
                Assert.That(figmage.Image.sprite.texture.width, Is.EqualTo(100));
                Assert.That(figmage.Image.sprite.texture.height, Is.EqualTo(80));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void FigmageComponent_RefreshImmediate_RestoresSourceLocalRenderBoundsAfterSerialization()
        {
            GameObject gameObject = new GameObject("Figmage Source Bounds Reload Test", typeof(RectTransform), typeof(Image), typeof(Figmage));
            try
            {
                FigmageNode source = CreateNode(100, 80);
                source.AbsoluteBoundingBox = new FigmageRect(240f, 120f, 100f, 80f);
                source.AbsoluteRenderBounds = new FigmageRect(230f, 112f, 124f, 96f);
                source.Fills.Add(FigmagePaint.Solid(Color.white));

                Figmage figmage = gameObject.GetComponent<Figmage>();
                figmage.ApplyNode(source);

                FieldInfo hasOverrideField = typeof(Figmage).GetField("hasRenderBoundsOverride", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo overrideField = typeof(Figmage).GetField("renderBoundsOverride", BindingFlags.Instance | BindingFlags.NonPublic);
                hasOverrideField.SetValue(figmage, false);
                overrideField.SetValue(figmage, default(Rect));

                figmage.RefreshImmediate();

                Assert.That(figmage.Image.sprite, Is.Not.Null);
                Assert.That(figmage.Image.sprite.texture.width, Is.EqualTo(124));
                Assert.That(figmage.Image.sprite.texture.height, Is.EqualTo(96));
                Assert.That(figmage.OverflowAnchors.Left, Is.EqualTo(0.1f).Within(0.0001f));
                Assert.That(figmage.OverflowAnchors.Right, Is.EqualTo(0.14f).Within(0.0001f));
                Assert.That(figmage.OverflowAnchors.Top, Is.EqualTo(0.1f).Within(0.0001f));
                Assert.That(figmage.OverflowAnchors.Bottom, Is.EqualTo(0.1f).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        static bool HasAnyOverflow(FigmageOverflowAnchors anchors)
        {
            return anchors.Left > 0f ||
                   anchors.Right > 0f ||
                   anchors.Top > 0f ||
                   anchors.Bottom > 0f;
        }

        static Texture2D LoadTexture(byte[] png)
        {
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            Assert.That(texture.LoadImage(png), Is.True);
            return texture;
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
