#if UNITEXT
using System.Collections.Generic;
using DA_Assets.UCC.Drawers.CanvasDrawers;
using LightSide;
using NUnit.Framework;
using LeadingTrim = DA_Assets.UCC.Model.LeadingTrim;
using Style = DA_Assets.UCC.Model.Style;

namespace DA_Assets.UCC.Tests.Editor
{
    public sealed class UniTextDrawerModifierParameterTests
    {
        [Test]
        public void BuildLineHeightParameter_Pixels_ReturnsAbsoluteContentLeadingAbove()
        {
            Style style = new Style
            {
                LineHeightPx = 24f,
                LineHeightUnit = "PIXELS"
            };

            string parameter = UniTextModifierParameterBuilder.BuildLineHeightParameter(style);

            Assert.That(parameter, Is.EqualTo("24,content,1.2,leadingAbove"));
        }

        [Test]
        public void BuildLineHeightParameter_FontSizePercent_ReturnsScaledLeadingAbove()
        {
            Style style = new Style
            {
                LineHeightPercentFontSize = 140f,
                LineHeightUnit = "FONT_SIZE_%"
            };

            string parameter = UniTextModifierParameterBuilder.BuildLineHeightParameter(style);

            Assert.That(parameter, Is.EqualTo(",scaled,1.4,leadingAbove"));
        }

        [Test]
        public void BuildLineHeightParameter_IntrinsicPercent_ReturnsContentPercentLeadingAbove()
        {
            Style style = new Style
            {
#pragma warning disable CS0618
                LineHeightPercent = 140f,
#pragma warning restore CS0618
                LineHeightUnit = "INTRINSIC_%"
            };

            string parameter = UniTextModifierParameterBuilder.BuildLineHeightParameter(style);

            Assert.That(parameter, Is.EqualTo("140%,content,1.2,leadingAbove"));
        }

        [Test]
        public void BuildLineHeightParameter_ZeroLineHeight_ReturnsNull()
        {
            Style style = new Style
            {
                LineHeightPx = 0f,
                LineHeightUnit = "PIXELS"
            };

            string parameter = UniTextModifierParameterBuilder.BuildLineHeightParameter(style);

            Assert.That(parameter, Is.Null);
        }

        [Test]
        public void BuildTextBoxTrimParameter_CapHeight_ReturnsCapHeightBaseline()
        {
            string parameter = UniTextModifierParameterBuilder.BuildTextBoxTrimParameter(LeadingTrim.CAP_HEIGHT);

            Assert.That(parameter, Is.EqualTo("capHeight,baseline"));
        }

        [Test]
        public void BuildTextBoxTrimParameter_None_ReturnsNull()
        {
            string parameter = UniTextModifierParameterBuilder.BuildTextBoxTrimParameter(LeadingTrim.NONE);

            Assert.That(parameter, Is.Null);
        }

        [Test]
        public void BuildParagraphSpacingParameter_PositiveValue_ReturnsAfterSpacing()
        {
            string parameter = UniTextModifierParameterBuilder.BuildParagraphSpacingParameter(12f);

            Assert.That(parameter, Is.EqualTo("12"));
        }

        [Test]
        public void BuildParagraphSpacingParameter_ZeroValue_ReturnsNull()
        {
            string parameter = UniTextModifierParameterBuilder.BuildParagraphSpacingParameter(0f);

            Assert.That(parameter, Is.Null);
        }

        [Test]
        public void BuildListData_UnorderedLine_ReturnsBulletRange()
        {
            List<RangeRule.Data> data = UniTextModifierParameterBuilder.BuildListData(
                "Item",
                new List<string> { "UNORDERED" },
                new List<int> { 0 });

            Assert.That(data, Has.Count.EqualTo(1));
            Assert.That(data[0].range, Is.EqualTo("0..4"));
            Assert.That(data[0].parameter, Is.EqualTo("0"));
        }

        [Test]
        public void BuildListData_OrderedLines_IncrementsNumbers()
        {
            List<RangeRule.Data> data = UniTextModifierParameterBuilder.BuildListData(
                "One\nTwo",
                new List<string> { "ORDERED", "ORDERED" },
                new List<int> { 0, 0 });

            Assert.That(data, Has.Count.EqualTo(2));
            Assert.That(data[0].parameter, Is.EqualTo("0,1"));
            Assert.That(data[1].parameter, Is.EqualTo("0,2"));
        }

        [Test]
        public void BuildListData_OrderedAfterNone_ResetsNumbering()
        {
            List<RangeRule.Data> data = UniTextModifierParameterBuilder.BuildListData(
                "One\nPlain\nTwo",
                new List<string> { "ORDERED", "NONE", "ORDERED" },
                new List<int> { 0, 0, 0 });

            Assert.That(data, Has.Count.EqualTo(2));
            Assert.That(data[0].parameter, Is.EqualTo("0,1"));
            Assert.That(data[1].parameter, Is.EqualTo("0,1"));
        }

        [Test]
        public void BuildListData_NestedOrderedLine_UsesLineIndentation()
        {
            List<RangeRule.Data> data = UniTextModifierParameterBuilder.BuildListData(
                "Parent\nChild",
                new List<string> { "ORDERED", "ORDERED" },
                new List<int> { 0, 1 });

            Assert.That(data, Has.Count.EqualTo(2));
            Assert.That(data[0].parameter, Is.EqualTo("0,1"));
            Assert.That(data[1].parameter, Is.EqualTo("1,1"));
        }

        [Test]
        public void BuildListSpacingData_SkipsNonListLines()
        {
            List<RangeRule.Data> data = UniTextModifierParameterBuilder.BuildListSpacingData(
                "One\nPlain\nTwo",
                new List<string> { "ORDERED", "NONE", "UNORDERED" },
                8f);

            Assert.That(data, Has.Count.EqualTo(2));
            Assert.That(data[0].range, Is.EqualTo("0..3"));
            Assert.That(data[0].parameter, Is.EqualTo("8"));
            Assert.That(data[1].range, Is.EqualTo("10..13"));
            Assert.That(data[1].parameter, Is.EqualTo("8"));
        }

    }
}
#endif
