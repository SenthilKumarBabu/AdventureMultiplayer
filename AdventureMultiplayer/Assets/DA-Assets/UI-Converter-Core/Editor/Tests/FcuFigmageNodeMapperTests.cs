using System.Collections.Generic;
using System.Reflection;
using DA_Assets.UCC.Model;
using NUnit.Framework;
using UnityEngine;

namespace DA_Assets.UCC.Tests.Editor
{
    public sealed class FcuFigmageNodeMapperTests
    {
        [Test]
        public void ToFigmageNode_CopiesStrokeGeometryAndDirectionalStrokeSettings()
        {
            Node source = new Node
            {
                Id = "9:299",
                Name = "Background+HorizontalBorder+Shadow",
                AbsoluteBoundingBox = new BoundingBox { X = 0f, Y = 0f, Width = 280f, Height = 55f },
                AbsoluteRenderBounds = new BoundingBox { X = 0f, Y = 0f, Width = 280f, Height = 61f },
                Size = new Vector2(280f, 55f),
                StrokeWeight = 6f,
                StrokeAlign = StrokeAlign.INSIDE,
                StrokeJoin = "ROUND",
                StrokeCap = StrokeCap.SQUARE,
                StrokeDashes = new List<float> { 8f, 4f },
                IndividualStrokeWeights = new IndividualStrokeWeights
                {
                    Top = 0f,
                    Right = 0f,
                    Bottom = 6f,
                    Left = 0f
                },
                Strokes = new List<Paint> { new Paint { Type = PaintType.SOLID, Color = Color.blue, Visible = true } },
                StrokeGeometry = new List<FillGeometry>
                {
                    new FillGeometry { Path = "M 0 49 L 280 49 L 280 55 L 0 55 Z" }
                }
            };

            object mapped = InvokeToFigmageNode(source);
            object weights = mapped.GetType().GetField("IndividualStrokeWeights").GetValue(mapped);

            Assert.That(GetFloat(weights, "Top"), Is.EqualTo(0f));
            Assert.That(GetFloat(weights, "Right"), Is.EqualTo(0f));
            Assert.That(GetFloat(weights, "Bottom"), Is.EqualTo(6f));
            Assert.That(GetFloat(weights, "Left"), Is.EqualTo(0f));
            Assert.That(mapped.GetType().GetField("StrokeJoin").GetValue(mapped).ToString(), Is.EqualTo("Round"));
            Assert.That(mapped.GetType().GetField("StrokeCap").GetValue(mapped).ToString(), Is.EqualTo("Square"));
            List<float> dashPattern = (List<float>)mapped.GetType().GetField("DashPattern").GetValue(mapped);
            Assert.That(dashPattern, Has.Count.EqualTo(2));
            Assert.That(dashPattern[0], Is.EqualTo(8f));
            Assert.That(dashPattern[1], Is.EqualTo(4f));

            object strokeGeometry = mapped.GetType().GetField("StrokeGeometry").GetValue(mapped);
            object firstGeometry = ((System.Collections.IList)strokeGeometry)[0];
            Assert.That(firstGeometry.GetType().GetField("Path").GetValue(firstGeometry), Is.EqualTo("M 0 49 L 280 49 L 280 55 L 0 55 Z"));
        }

        static object InvokeToFigmageNode(Node source)
        {
            System.Type mapperType = typeof(Node).Assembly.GetType("DA_Assets.UCC.FcuFigmageNodeMapper", true);
            MethodInfo method = mapperType.GetMethod("ToFigmageNode", BindingFlags.Public | BindingFlags.Static);
            return method.Invoke(null, new object[] { source });
        }

        static float GetFloat(object target, string fieldName)
        {
            return (float)target.GetType().GetField(fieldName).GetValue(target);
        }
    }
}