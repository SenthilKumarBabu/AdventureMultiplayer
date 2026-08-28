using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DA_Assets.UCC.Extensions;
using DA_Assets.UCC.Model;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace DA_Assets.UCC.Tests.Editor
{
    public sealed class ImageTypeSetterTests
    {
        private readonly List<GameObject> createdObjects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject item in createdObjects)
            {
                Object.DestroyImmediate(item);
            }

            createdObjects.Clear();
        }

        [Test]
        public async Task SetImageTypes_AutoSlice9StrokeOnlyRoundedRectangle_UsesDrawable()
        {
            TestConverter converter = CreateConverter();
            Node node = CreateAutoSlice9StrokeOnlyRoundedRectangle();

            await converter.ImageTypeSetter.SetImageTypes(new List<Node> { node }, CancellationToken.None);

            Assert.That(node.Data.FcuImageType, Is.EqualTo(FcuImageType.Drawable));
        }

        [Test]
        public async Task SetTags_AutoSlice9StrokeOnlyRoundedRectangle_DoesNotForceImage()
        {
            TestConverter converter = CreateConverter();
            Node page = CreatePageWithChild(CreateRawStrokeOnlyRoundedRectangle());

            await converter.TagSetter.SetTags(page, CancellationToken.None);
            Node taggedNode = page.Children[0];
            await converter.ImageTypeSetter.SetImageTypes(new List<Node> { taggedNode }, CancellationToken.None);

            Assert.That(taggedNode.ContainsTag(FcuTag.AutoSlice9), Is.True);
            Assert.That(taggedNode.Data.ForceImage, Is.False);
            Assert.That(taggedNode.Data.FcuImageType, Is.EqualTo(FcuImageType.Drawable));
        }

        [Test]
        public void SetColor_DrawableStrokeOnly_AddsDAOutline()
        {
            TestConverter converter = CreateConverter();
            GameObject target = new GameObject("StrokeOnlyTarget");
            createdObjects.Add(target);
            Image image = target.AddComponent<Image>();
            Node node = CreateAutoSlice9StrokeOnlyRoundedRectangle();
            node.FillGeometry = new List<FillGeometry>
            {
                new FillGeometry { Path = "M 0 0 L 31 0 L 31 31 L 0 31 Z" }
            };
            node.Data.GameObject = target;
            node.Data.FcuImageType = FcuImageType.Drawable;

            converter.CanvasDrawer.ImageDrawer.UnityImageDrawer.SetColor(node, image);

            Assert.That(image.color.a, Is.EqualTo(0f));
            Assert.That(target.GetComponent("DAOutlineEffect"), Is.Not.Null);
        }

        [Test]
        public void Draw_DrawableAutoSlice9StrokeOnly_KeepsUnityImageSimple()
        {
            TestConverter converter = CreateConverter();
            GameObject target = new GameObject("DrawableAutoSlice9Target");
            createdObjects.Add(target);
            Node node = CreateAutoSlice9StrokeOnlyRoundedRectangle();
            node.FillGeometry = new List<FillGeometry>
            {
                new FillGeometry { Path = "M 0 0 L 31 0 L 31 31 L 0 31 Z" }
            };
            node.Data.GameObject = target;
            node.Data.FcuImageType = FcuImageType.Drawable;

            converter.CanvasDrawer.ImageDrawer.Draw(node);

            Image image = target.GetComponent<Image>();
            Assert.That(image, Is.Not.Null);
            Assert.That(image.type, Is.EqualTo(Image.Type.Simple));
            Assert.That(target.GetComponent("DAOutlineEffect"), Is.Not.Null);
        }

        private TestConverter CreateConverter()
        {
            GameObject gameObject = new GameObject(nameof(TestConverter));
            createdObjects.Add(gameObject);

            TestConverter converter = gameObject.AddComponent<TestConverter>();
            converter.InitServices();
            converter.Settings.MainSettings.UIFramework = UIFramework.UGUI;
            converter.Settings.ImageSpritesSettings.ImageComponent = ImageComponent.UnityImage;
            converter.Settings.ImageSpritesSettings.ImageFormat = ImageFormat.PNG;

            return converter;
        }

        private static Node CreateAutoSlice9StrokeOnlyRoundedRectangle()
        {
            Paint stroke = new Paint
            {
                Type = PaintType.SOLID,
                Color = Color.red,
                Opacity = 1f,
                Visible = true
            };

            FStroke fStroke = new FStroke
            {
                Align = StrokeAlign.OUTSIDE,
                Weight = 8f,
                HasSolid = true,
                SolidPaint = stroke,
                SingleColor = Color.red
            };

            return new Node
            {
                Id = "9:318",
                Name = "Border",
                Type = NodeType.RECTANGLE,
                Size = new Vector2(31f, 31f),
                CornerRadius = 24.8f,
                StrokeWeight = 8f,
                StrokeAlign = StrokeAlign.OUTSIDE,
                Fills = new List<Paint>(),
                Strokes = new List<Paint> { stroke },
                Effects = new List<Effect>(),
                Children = new List<Node>(),
                Data = new SyncData
                {
                    Id = "9:318",
                    Tags = new List<FcuTag> { FcuTag.Image, FcuTag.AutoSlice9 },
                    Graphic = new FGraphic
                    {
                        HasStroke = true,
                        Stroke = fStroke,
                        HasSingleColor = true,
                        SpriteSingleColor = Color.red
                    }
                }
            };
        }

        private static Node CreateRawStrokeOnlyRoundedRectangle()
        {
            Paint stroke = new Paint
            {
                Type = PaintType.SOLID,
                Color = Color.red,
                Opacity = 1f,
                Visible = true
            };

            return new Node
            {
                Id = "9:318",
                Name = "Border",
                Type = NodeType.RECTANGLE,
                Size = new Vector2(31f, 31f),
                CornerRadius = 24.8f,
                StrokeWeight = 8f,
                StrokeAlign = StrokeAlign.OUTSIDE,
                AbsoluteBoundingBox = new BoundingBox
                {
                    X = 0f,
                    Y = 0f,
                    Width = 31f,
                    Height = 31f
                },
                AbsoluteRenderBounds = new BoundingBox
                {
                    X = 0f,
                    Y = 0f,
                    Width = 31f,
                    Height = 31f
                },
                Fills = new List<Paint>(),
                Strokes = new List<Paint> { stroke },
                Effects = new List<Effect>(),
                Children = new List<Node>(),
                Visible = true
            };
        }

        private static Node CreatePageWithChild(Node child)
        {
            return new Node
            {
                Id = "0:1",
                Name = "Page",
                Type = NodeType.CANVAS,
                Children = new List<Node> { child },
                Visible = true,
                Data = new SyncData
                {
                    Id = "0:1",
                    Tags = new List<FcuTag> { FcuTag.Page },
                    Hierarchy = new List<FcuHierarchy>()
                }
            };
        }
    }

    public sealed class TestConverter : ConverterBase
    {
        public override IConvConfig Config => FcuConfig.Instance;
    }
}