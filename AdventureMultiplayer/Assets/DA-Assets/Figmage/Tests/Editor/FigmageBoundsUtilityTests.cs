using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace DA_Assets.Figmage.Tests
{
    public sealed class FigmageBoundsUtilityTests
    {
        [Test]
        public void CalculateRenderBounds_OutsideStroke_ExpandsBeyondContent()
        {
            FigmageNode node = CreateNode(100f, 80f);
            node.StrokeWeight = 12f;
            node.StrokeAlign = FigmageStrokeAlign.Outside;
            node.Strokes.Add(FigmagePaint.Solid(Color.white));

            Rect bounds = FigmageBoundsUtility.CalculateRenderBounds(node);

            Assert.That(bounds.xMin, Is.EqualTo(-12f));
            Assert.That(bounds.yMin, Is.EqualTo(-12f));
            Assert.That(bounds.width, Is.EqualTo(124f));
            Assert.That(bounds.height, Is.EqualTo(104f));
        }

        [Test]
        public void CalculateRenderBounds_DropShadow_UsesOffsetBlurAndSpread()
        {
            FigmageNode node = CreateNode(100f, 80f);
            node.Effects.Add(new FigmageEffect
            {
                Type = FigmageEffectType.DropShadow,
                Visible = true,
                Radius = 10f,
                Spread = 4f,
                Offset = new Vector2(6f, -8f)
            });

            Rect bounds = FigmageBoundsUtility.CalculateRenderBounds(node);

            Assert.That(bounds.xMin, Is.EqualTo(-18f));
            Assert.That(bounds.yMin, Is.EqualTo(-32f));
            Assert.That(bounds.xMax, Is.EqualTo(130f));
            Assert.That(bounds.yMax, Is.EqualTo(96f));
        }

        [Test]
        public void CalculateRenderBounds_ExplicitRenderBoundsWithDropShadow_DoesNotExpandAgain()
        {
            FigmageNode node = CreateNode(390f, 884f);
            node.AbsoluteBoundingBox = new FigmageRect(422f, 0f, 390f, 884f);
            node.AbsoluteRenderBounds = new FigmageRect(384f, -13f, 466f, 960f);
            node.Effects.Add(new FigmageEffect
            {
                Type = FigmageEffectType.DropShadow,
                Visible = true,
                Radius = 50f,
                Spread = -12f,
                Offset = new Vector2(0f, 25f)
            });

            Rect bounds = FigmageBoundsUtility.CalculateRenderBounds(node);

            Assert.That(bounds.xMin, Is.EqualTo(384f));
            Assert.That(bounds.yMin, Is.EqualTo(-13f));
            Assert.That(bounds.width, Is.EqualTo(466f));
            Assert.That(bounds.height, Is.EqualTo(960f));
        }

        static FigmageNode CreateNode(float width, float height)
        {
            return new FigmageNode
            {
                Id = "bounds",
                Name = "bounds",
                AbsoluteBoundingBox = new FigmageRect(0f, 0f, width, height),
                Size = new Vector2(width, height),
                Opacity = 1f,
                Fills = new List<FigmagePaint>(),
                Strokes = new List<FigmagePaint>(),
                Effects = new List<FigmageEffect>()
            };
        }
    }
}
