using System.Collections.Generic;
using DA_Assets.UCC.Extensions;
using DA_Assets.UCC.Model;
using NUnit.Framework;
using UnityEngine;

namespace DA_Assets.UCC.Tests.Editor
{
    public sealed class SolidFillCompositionTests
    {
        [Test]
        public void TryGetSolidCompositeColor_WithMultipleSolidFills_ReturnsTopmostCompositedColor()
        {
            var fills = new List<Paint>
            {
                Solid(new Color(1f, 1f, 1f, 1f), 1f),
                Solid(new Color(0f, 0f, 0f, 1f), 0.5f),
                Solid(new Color(0.8f, 0.8f, 0.8f, 1f), 0.5f)
            };

            bool result = fills.TryGetSolidCompositeColor(out Color color);

            Assert.That(result, Is.True);
            AssertColor(color, new Color(0.65f, 0.65f, 0.65f, 1f));
        }

        [Test]
        public void TryGetSolidCompositeColor_WithOnlyTransparentSolidFills_ReturnsTransparentColor()
        {
            var fills = new List<Paint>
            {
                Solid(new Color(1f, 0f, 0f, 1f), 0f),
                Solid(new Color(0f, 1f, 0f, 1f), 0f)
            };

            bool result = fills.TryGetSolidCompositeColor(out Color color);

            Assert.That(result, Is.True);
            AssertColor(color, new Color(0f, 0f, 0f, 0f));
        }

        [Test]
        public void TryGetSolidCompositeColor_WithVisibleGradient_ReturnsFalse()
        {
            var fills = new List<Paint>
            {
                Solid(Color.white, 1f),
                new Paint
                {
                    Type = PaintType.GRADIENT_LINEAR,
                    Visible = true
                }
            };

            bool result = fills.TryGetSolidCompositeColor(out _);

            Assert.That(result, Is.False);
        }

        private static Paint Solid(Color color, float opacity)
        {
            return new Paint
            {
                Type = PaintType.SOLID,
                Color = color,
                Opacity = opacity,
                Visible = true
            };
        }

        private static void AssertColor(Color actual, Color expected)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(0.0001f));
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(0.0001f));
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(0.0001f));
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(0.0001f));
        }
    }
}