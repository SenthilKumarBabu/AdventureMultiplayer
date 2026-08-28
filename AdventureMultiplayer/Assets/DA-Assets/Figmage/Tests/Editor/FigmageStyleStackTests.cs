using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace DA_Assets.Figmage.Tests
{
    public sealed class FigmageStyleStackTests
    {
        [Test]
        public void ClonePaint_CopiesNestedGradientData()
        {
            FigmagePaint source = FigmagePaint.LinearGradient(
                new List<Vector2>
                {
                    new Vector2(0f, 0f),
                    new Vector2(1f, 0f),
                    new Vector2(0f, 1f)
                },
                new List<FigmageGradientStop>
                {
                    new FigmageGradientStop(0f, Color.blue),
                    new FigmageGradientStop(1f, Color.red)
                });

            FigmagePaint clone = FigmageStyleStack.ClonePaint(source);
            source.GradientHandlePositions[0] = Vector2.one;
            source.GradientStops[0] = new FigmageGradientStop(0f, Color.green);

            Assert.That(clone, Is.Not.SameAs(source));
            Assert.That(clone.GradientHandlePositions[0], Is.EqualTo(Vector2.zero));
            Assert.That(clone.GradientStops[0].Color, Is.EqualTo(Color.blue));
        }

        [Test]
        public void Move_ClampsDestinationIndex()
        {
            List<FigmageEffect> effects = new List<FigmageEffect>
            {
                new FigmageEffect { Type = FigmageEffectType.DropShadow, Radius = 1f },
                new FigmageEffect { Type = FigmageEffectType.LayerBlur, Radius = 2f },
                new FigmageEffect { Type = FigmageEffectType.InnerShadow, Radius = 3f }
            };

            FigmageStyleStack.Move(effects, 0, 99);

            Assert.That(effects[2].Radius, Is.EqualTo(1f));
            Assert.That(effects[0].Radius, Is.EqualTo(2f));
        }

        [Test]
        public void RemoveAt_IgnoresInvalidIndex()
        {
            List<FigmagePaint> paints = new List<FigmagePaint>
            {
                FigmagePaint.Solid(Color.white)
            };

            FigmageStyleStack.RemoveAt(paints, -1);
            FigmageStyleStack.RemoveAt(paints, 4);

            Assert.That(paints, Has.Count.EqualTo(1));
        }
    }
}
