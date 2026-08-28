#if UNITY_EDITOR
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;
using DA_Assets.UCC.Attributes;
using DA_Assets.Tools;

#if JSONNET_EXISTS
using Newtonsoft.Json;
#endif

namespace DA_Assets.UCC.Model
{
    /// <summary>
    /// <see href="https://developers.figma.com/docs/rest-api/file-node-types/" />
    /// </summary>
    public struct Node : IHaveId, IVisible
    {
        [IgnoreDataMember] public SyncData Data { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("id")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-node-types/#:~:text=id" />
        /// </summary>
        public string Id { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("name")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-node-types/#:~:text=name" />
        /// </summary>
        public string Name { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("type")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-node-types/#:~:text=type" />
        /// </summary>
        public NodeType Type { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("rotation")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-node-types/#:~:text=rotation" />
        /// </summary>
        public float? Rotation { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("blendMode")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-node-types/#:~:text=blendMode" />
        /// </summary>
        public string BlendMode { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("children")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-node-types/#:~:text=children" />
        /// </summary>
        public List<Node> Children { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("absoluteBoundingBox")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-node-types/#:~:text=absoluteBoundingBox" />
        /// </summary>
        public BoundingBox AbsoluteBoundingBox { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("absoluteRenderBounds")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-node-types/#:~:text=absoluteRenderBounds" />
        /// </summary>
        public BoundingBox AbsoluteRenderBounds { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("constraints")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-node-types/#:~:text=constraints" />
        /// </summary>
        public Constraints Constraints { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("relativeTransform")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-node-types/#:~:text=relativeTransform" />
        /// </summary>
        public List<List<float?>> RelativeTransform { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("size")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-node-types/#:~:text=size" />
        /// </summary>
        public Vector2 Size { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("clipsContent")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-node-types/#:~:text=clipsContent" />
        /// </summary>
        public bool? ClipsContent { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("fills")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-node-types/#:~:text=fills" />
        /// </summary>
        public List<Paint> Fills { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("strokes")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-node-types/#:~:text=strokes" />
        /// </summary>
        public List<Paint> Strokes { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("cornerRadius")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-node-types/#:~:text=cornerRadius" />
        /// </summary>
        public float? CornerRadius { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("cornerSmoothing")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/plugins/api/properties/nodes-cornersmoothing/" />
        /// </summary>
        public float CornerSmoothing { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("strokeWeight")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-node-types/#:~:text=strokeWeight" />
        /// </summary>
        public float StrokeWeight { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("individualStrokeWeights")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-node-types/#:~:text=individualStrokeWeights" />
        /// </summary>
        public IndividualStrokeWeights IndividualStrokeWeights { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("strokeAlign")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-node-types/#:~:text=strokeAlign" />
        /// </summary>
        public StrokeAlign StrokeAlign { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("layoutGrids")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-node-types/#:~:text=layoutGrids" />
        /// </summary>
        public List<LayoutGrid> LayoutGrids { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("effects")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-node-types/#:~:text=effects" />
        /// </summary>
        public List<Effect> Effects { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("overflowDirection")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-node-types/#:~:text=overflowDirection" />
        /// </summary>
        public OverflowDirection OverflowDirection { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("fillGeometry")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-node-types/#:~:text=fillGeometry" />
        /// </summary>
        public List<FillGeometry> FillGeometry { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("strokeGeometry")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-node-types/#:~:text=strokeGeometry" />
        /// </summary>
        public List<FillGeometry> StrokeGeometry { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("strokeCap")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-node-types/#:~:text=strokeCap" />
        /// </summary>
        public StrokeCap StrokeCap { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("strokeJoin")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-node-types/#:~:text=strokeJoin" />
        /// </summary>
        public string StrokeJoin { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("strokeMiterAngle")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-node-types/#:~:text=strokeMiterAngle" />
        /// </summary>
        public float? StrokeMiterAngle { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("opacity")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-node-types/#:~:text=opacity" />
        /// </summary>
        public float? Opacity { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("preserveRatio")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-node-types/#:~:text=preserveRatio" />
        /// </summary>
        public bool? PreserveRatio { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("layoutAlign")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-node-types/#:~:text=layoutAlign" />
        /// </summary>
        public LayoutAlign LayoutAlign { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("layoutGrow")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-node-types/#:~:text=layoutGrow" />
        /// </summary>
        public float? LayoutGrow { get; set; }

        // TEXT node properties
        // https://developers.figma.com/docs/rest-api/file-node-types/#text-props

#if JSONNET_EXISTS
        [JsonProperty("characters")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-node-types/#text-props#:~:text=characters" />
        /// </summary>
        public string Characters { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("style")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-node-types/#text-props#:~:text=style" />
        /// </summary>
        public Style Style { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("styleOverrideTable")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-node-types/#text-props#:~:text=styleOverrideTable" />
        /// </summary>
        public Dictionary<string, Style> StyleOverrideTable { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("characterStyleOverrides")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-node-types/#text-props#:~:text=characterStyleOverrides" />
        /// </summary>
        public List<int> CharacterStyleOverrides { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("lineTypes")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-node-types/#text-props#:~:text=lineTypes" />
        /// </summary>
        public List<string> LineTypes { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("lineIndentations")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-node-types/#text-props#:~:text=lineIndentations" />
        /// </summary>
        public List<int> LineIndentations { get; set; }

        // FRAME / AUTO-LAYOUT properties
        // https://developers.figma.com/docs/rest-api/file-node-types/#frame-props

#if JSONNET_EXISTS
        [JsonProperty("layoutMode")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-node-types/#frame-props#:~:text=layoutMode" />
        /// </summary>
        public LayoutMode LayoutMode { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("itemSpacing")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-node-types/#frame-props#:~:text=itemSpacing" />
        /// </summary>
        public float? ItemSpacing { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("itemReverseZIndex")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-node-types/#frame-props#:~:text=itemReverseZIndex" />
        /// </summary>
        public bool? ItemReverseZIndex { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("counterAxisSpacing")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-node-types/#frame-props#:~:text=counterAxisSpacing" />
        /// </summary>
        public float? CounterAxisSpacing { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("visible")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-node-types/#:~:text=visible" />
        /// </summary>
        public bool? Visible { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("primaryAxisSizingMode")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-node-types/#frame-props#:~:text=primaryAxisSizingMode" />
        /// </summary>
        public PrimaryAxisSizingMode PrimaryAxisSizingMode { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("counterAxisSizingMode")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-node-types/#frame-props#:~:text=counterAxisSizingMode" />
        /// </summary>
        public CounterAxisSizingMode CounterAxisSizingMode { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("counterAxisAlignContent")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-node-types/#frame-props#:~:text=counterAxisAlignContent" />
        /// </summary>
        public CounterAxisAlignContent CounterAxisAlignContent { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("primaryAxisAlignItems")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-node-types/#frame-props#:~:text=primaryAxisAlignItems" />
        /// </summary>
        public PrimaryAxisAlignItem PrimaryAxisAlignItems { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("counterAxisAlignItems")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-node-types/#frame-props#:~:text=counterAxisAlignItems" />
        /// </summary>
        public CounterAxisAlignItem CounterAxisAlignItems { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("layoutWrap")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-node-types/#frame-props#:~:text=layoutWrap" />
        /// </summary>
        public LayoutWrap LayoutWrap { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("isMask")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-node-types/#:~:text=isMask" />
        /// </summary>
        public bool? IsMask { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("paddingLeft")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-node-types/#frame-props#:~:text=paddingLeft" />
        /// </summary>
        public float? PaddingLeft { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("paddingRight")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-node-types/#frame-props#:~:text=paddingRight" />
        /// </summary>
        public float? PaddingRight { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("paddingTop")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-node-types/#frame-props#:~:text=paddingTop" />
        /// </summary>
        public float? PaddingTop { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("paddingBottom")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-node-types/#frame-props#:~:text=paddingBottom" />
        /// </summary>
        public float? PaddingBottom { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("horizontalPadding")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-node-types/#frame-props#:~:text=horizontalPadding" />
        /// </summary>
        public float? HorizontalPadding { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("verticalPadding")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-node-types/#frame-props#:~:text=verticalPadding" />
        /// </summary>
        public float? VerticalPadding { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("rectangleCornerRadii")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-node-types/#:~:text=rectangleCornerRadii" />
        /// </summary>
        public List<float> CornerRadiuses { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("arcData")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-node-types/#:~:text=arcData" />
        /// </summary>
        public ArcData ArcData { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("strokeDashes")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-node-types/#:~:text=strokeDashes" />
        /// </summary>
        public List<float> StrokeDashes { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("layoutPositioning")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-node-types/#frame-props#:~:text=layoutPositioning" />
        /// </summary>
        public LayoutPositioning LayoutPositioning { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("layoutSizingHorizontal")]
#endif
        /// <summary>
        /// Determines how an element is sized horizontally inside an Auto Layout parent.
        /// Values: "FIXED" | "HUG" | "FILL"
        /// <see href="https://developers.figma.com/docs/rest-api/file-node-types/#frame-props" />
        /// </summary>
        public string LayoutSizingHorizontal { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("layoutSizingVertical")]
#endif
        /// <summary>
        /// Determines how an element is sized vertically inside an Auto Layout parent.
        /// Values: "FIXED" | "HUG" | "FILL"
        /// <see href="https://developers.figma.com/docs/rest-api/file-node-types/#frame-props" />
        /// </summary>
        public string LayoutSizingVertical { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("componentId")]
#endif
        /// <summary>
        /// For INSTANCE nodes: the ID of the COMPONENT node this instance references.
        /// </summary>
        public string ComponentId { get; set; }
    }

    public struct DesignComponent
    {
#if JSONNET_EXISTS
        [JsonProperty("key")]
#endif
        public string Key { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("name")]
#endif
        public string Name { get; set; }
    }

    public struct DesignProject
    {
#if JSONNET_EXISTS
        [JsonProperty("document")]
#endif
        public Node Document { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("name")]
#endif
        public string Name { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("components")]
#endif
        public Dictionary<string, DesignComponent> Components { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("nodes")]
#endif
        public Dictionary<string, DesignProject> Nodes { get; set; }
    }

    /// <summary>
    /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#layoutconstraint-type" />
    /// </summary>
    public struct Constraints
    {
#if JSONNET_EXISTS
        [JsonProperty("vertical")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#layoutconstraint-type#:~:text=vertical" />
        /// </summary>
        public string Vertical { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("horizontal")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#layoutconstraint-type#:~:text=horizontal" />
        /// </summary>
        public string Horizontal { get; set; }
    }

    public enum PaintType
    {
        NONE,
        [PaintPriority(1)] SOLID,
        [PaintPriority(2)] GRADIENT_LINEAR,
        [PaintPriority(4)] GRADIENT_RADIAL,
        [PaintPriority(3)] GRADIENT_ANGULAR,
        [PaintPriority(5)] GRADIENT_DIAMOND,
        IMAGE,
        EMOJI,
        VIDEO
    }

    /// <summary>
    /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#paint-type" />
    /// </summary>
    public struct Paint : IVisible
    {
#if JSONNET_EXISTS
        [JsonProperty("blendMode")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#paint-type#:~:text=blendMode" />
        /// </summary>
        public string BlendMode { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("type")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#paint-type#:~:text=type" />
        /// </summary>
        public PaintType Type { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("color")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#paint-type#:~:text=color" />
        /// </summary>
        public Color Color { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("visible")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#paint-type#:~:text=visible" />
        /// </summary>
        public bool? Visible { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("scaleMode")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#paint-type#:~:text=scaleMode" />
        /// </summary>
        public string ScaleMode { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("scalingFactor")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#paint-type#:~:text=scalingFactor" />
        /// </summary>
        public string ScalingFactor { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("imageRef")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#paint-type#:~:text=imageRef" />
        /// </summary>
        public string ImageRef { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("gifRef")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#paint-type#:~:text=gifRef" />
        /// </summary>
        public string GifRef { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("imageTransform")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#paint-type#:~:text=imageTransform" />
        /// </summary>
        public List<List<float>> ImageTransform { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("gradientHandlePositions")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#paint-type#:~:text=gradientHandlePositions" />
        /// </summary>
        public List<Vector2> GradientHandlePositions { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("gradientStops")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#paint-type#:~:text=gradientStops" />
        /// </summary>
        public List<GradientStop> GradientStops { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("opacity")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#paint-type#:~:text=opacity" />
        /// </summary>
        public float? Opacity { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("filters")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#paint-type#:~:text=filters" />
        /// </summary>
        public Filters Filters { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("rotation")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#paint-type#:~:text=rotation" />
        /// </summary>
        public float? Rotation { get; set; }
    }

    /// <summary>
    /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#effect-type" />
    /// </summary>
    public struct Effect : IVisible
    {
#if JSONNET_EXISTS
        [JsonProperty("type")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#effect-type#:~:text=type" />
        /// </summary>
        public EffectType Type { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("visible")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#effect-type#:~:text=visible" />
        /// </summary>
        public bool? Visible { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("color")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#effect-type#:~:text=color" />
        /// </summary>
        public Color Color { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("opacity")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#effect-type#:~:text=opacity" />
        /// </summary>
        public float? Opacity { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("blendMode")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#effect-type#:~:text=blendMode" />
        /// </summary>
        public string BlendMode { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("offset")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#effect-type#:~:text=offset" />
        /// </summary>
        public Vector2 Offset { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("radius")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#effect-type#:~:text=radius" />
        /// </summary>
        public float Radius { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("showShadowBehindNode")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#effect-type#:~:text=showShadowBehindNode" />
        /// </summary>
        public bool? ShowShadowBehindNode { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("spread")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#effect-type#:~:text=spread" />
        /// </summary>
        public float? Spread { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("blurType")]
#endif
        public string BlurType { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("startRadius")]
#endif
        public float? StartRadius { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("startOffset")]
#endif
        public Vector2 StartOffset { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("endOffset")]
#endif
        public Vector2 EndOffset { get; set; }
    }

    /// <summary>
    /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#typestyle-type" />
    /// </summary>
    public struct Style
    {
#if JSONNET_EXISTS
        [JsonProperty("fontFamily")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#TEXT#:~:text=fontFamily" />
        /// </summary>
        public string FontFamily { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("fontPostScriptName")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#TEXT#:~:text=fontPostScriptName" />
        /// </summary>
        public string FontPostScriptName { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("fontStyle")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#TEXT#:~:text=fontStyle" />
        /// </summary>
        public string FontStyle { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("fontWeight")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#TEXT#:~:text=fontWeight" />
        /// </summary>
        public int FontWeight { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("italic")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#TEXT#:~:text=italic" />
        /// </summary>
        public bool? Italic { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("fontSize")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#TEXT#:~:text=fontSize" />
        /// </summary>
        public float FontSize { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("textAlignHorizontal")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#TEXT#:~:text=textAlignHorizontal" />
        /// </summary>
        public TextAlignHorizontal TextAlignHorizontal { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("textAlignVertical")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#TEXT#:~:text=textAlignVertical" />
        /// </summary>
        public TextAlignVertical TextAlignVertical { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("textCase")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#TEXT#:~:text=textCase" />
        /// </summary>
        public TextCase TextCase { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("textDecoration")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#TEXT#:~:text=textDecoration" />
        /// </summary>
        public TextDecoration TextDecoration { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("textAutoResize")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#TEXT#:~:text=textAutoResize" />
        /// </summary>
        public TextAutoResize TextAutoResize { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("textTruncation")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#TEXT#:~:text=textTruncation" />
        /// </summary>
        public TextTruncation TextTruncation { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("maxLines")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#TEXT#:~:text=maxLines" />
        /// </summary>
        public int? MaxLines { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("letterSpacing")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#TEXT#:~:text=letterSpacing" />
        /// </summary>
        public float LetterSpacing { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("lineHeightPx")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#TEXT#:~:text=lineHeightPx" />
        /// </summary>
        public float LineHeightPx { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("lineHeightPercent")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#TEXT#:~:text=lineHeightPercent" />
        /// </summary>
        [System.Obsolete("Deprecated by Figma. Use lineHeightPercentFontSize or lineHeightUnit instead.")]
        public float? LineHeightPercent { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("lineHeightPercentFontSize")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#TEXT#:~:text=lineHeightPercentFontSize" />
        /// </summary>
        public float? LineHeightPercentFontSize { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("lineHeightUnit")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#TEXT#:~:text=lineHeightUnit" />
        /// </summary>
        public string LineHeightUnit { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("leadingTrim")]
#endif
        /// <summary>
        /// Figma Plugin API leadingTrim value when included in imported JSON.
        /// </summary>
        public LeadingTrim? LeadingTrim { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("paragraphSpacing")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#TEXT#:~:text=paragraphSpacing" />
        /// </summary>
        public float ParagraphSpacing { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("paragraphIndent")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#TEXT#:~:text=paragraphIndent" />
        /// </summary>
        public float ParagraphIndent { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("listSpacing")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#TEXT#:~:text=listSpacing" />
        /// </summary>
        public float ListSpacing { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("fills")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#TEXT#:~:text=fills" />
        /// </summary>
        public List<Paint> Fills { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("hyperlink")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#TEXT#:~:text=hyperlink" />
        /// </summary>
        public Hyperlink Hyperlink { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("openTypeFlags")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#TEXT#:~:text=openTypeFlags" />
        /// </summary>
        public Dictionary<string, int> OpenTypeFlags { get; set; }
    }

    public struct Filters
    {
#if JSONNET_EXISTS
        [JsonProperty("exposure")]
#endif
        public float? Exposure { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("contrast")]
#endif
        public float? Contrast { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("saturation")]
#endif
        public float? Saturation { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("temperature")]
#endif
        public float? Temperature { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("tint")]
#endif
        public float? Tint { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("highlights")]
#endif
        public float? Highlights { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("shadows")]
#endif
        public float? Shadows { get; set; }
    }

    /// <summary>
    /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#colorstop-type" />
    /// </summary>
    public struct GradientStop
    {
#if JSONNET_EXISTS
        [JsonProperty("color")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#colorstop-type#:~:text=color" />
        /// </summary>
        public Color Color { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("position")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#colorstop-type#:~:text=position" />
        /// </summary>
        public float Position { get; set; }
    }

    public struct FillGeometry
    {
#if JSONNET_EXISTS
        [JsonProperty("path")]
#endif
        public string Path { get; set; }
    }

    /// <summary>
    /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#arcdata-type" />
    /// </summary>
    public struct ArcData
    {
#if JSONNET_EXISTS
        [JsonProperty("startingAngle")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#arcdata-type#:~:text=startingAngle" />
        /// </summary>
        public float StartingAngle { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("endingAngle")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#arcdata-type#:~:text=endingAngle" />
        /// </summary>
        public float EndingAngle { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("innerRadius")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#arcdata-type#:~:text=innerRadius" />
        /// </summary>
        public float InnerRadius { get; set; }
    }

    /// <summary>
    /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#rectangle-type" />
    /// </summary>
    public struct BoundingBox
    {
#if JSONNET_EXISTS
        [JsonProperty("x")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#rectangle-type#:~:text=x" />
        /// </summary>
        public float? X { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("y")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#rectangle-type#:~:text=y" />
        /// </summary>
        public float? Y { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("width")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#rectangle-type#:~:text=width" />
        /// </summary>
        public float? Width { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("height")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#rectangle-type#:~:text=height" />
        /// </summary>
        public float? Height { get; set; }
    }

    /// <summary>
    /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#hyperlink-type" />
    /// </summary>
    public struct Hyperlink
    {
#if JSONNET_EXISTS
        [JsonProperty("type")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#hyperlink-type#:~:text=type" />
        /// </summary>
        public HyperlinkType Type { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("url")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#hyperlink-type#:~:text=url" />
        /// </summary>
        public string Url { get; set; }
    }

    public struct Styles
    {
#if JSONNET_EXISTS
        [JsonProperty("stroke")]
#endif
        public string Stroke { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("fill")]
#endif
        public string Fill { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("fills")]
#endif
        public string Fills { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("text")]
#endif
        public string Text { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("effect")]
#endif
        public string Effect { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("strokes")]
#endif
        public string Strokes { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("grid")]
#endif
        public string Grid { get; set; }
    }

    /// <summary>
    /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#constraint-type" />
    /// </summary>
    public struct Constraint
    {
#if JSONNET_EXISTS
        [JsonProperty("type")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#constraint-type#:~:text=type" />
        /// </summary>
        public ConstraintType Type { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("value")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#constraint-type#:~:text=value" />
        /// </summary>
        public float Value { get; set; }
    }

    /// <summary>
    /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#exportsetting-type" />
    /// </summary>
    public struct ExportSetting
    {
#if JSONNET_EXISTS
        [JsonProperty("suffix")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#exportsetting-type#:~:text=suffix" />
        /// </summary>
        public string Suffix { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("format")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#exportsetting-type#:~:text=format" />
        /// </summary>
        public string Format { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("constraint")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#exportsetting-type#:~:text=constraint" />
        /// </summary>
        public Constraint Constraint { get; set; }
    }

    /// <summary>
    /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#layoutgrid-type" />
    /// </summary>
    public struct LayoutGrid
    {
#if JSONNET_EXISTS
        [JsonProperty("pattern")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#layoutgrid-type#:~:text=pattern" />
        /// </summary>
        public string Pattern { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("sectionSize")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#layoutgrid-type#:~:text=sectionSize" />
        /// </summary>
        public float SectionSize { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("visible")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#layoutgrid-type#:~:text=visible" />
        /// </summary>
        public bool Visible { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("color")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#layoutgrid-type#:~:text=color" />
        /// </summary>
        public Color Color { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("alignment")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#layoutgrid-type#:~:text=alignment" />
        /// </summary>
        public string Alignment { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("gutterSize")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#layoutgrid-type#:~:text=gutterSize" />
        /// </summary>
        public float GutterSize { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("offset")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#layoutgrid-type#:~:text=offset" />
        /// </summary>
        public float Offset { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("count")]
#endif
        /// <summary>
        /// <see href="https://developers.figma.com/docs/rest-api/file-property-types/#layoutgrid-type#:~:text=count" />
        /// </summary>
        public int Count { get; set; }
    }

    public struct IndividualStrokeWeights
    {
#if JSONNET_EXISTS
        [JsonProperty("top")]
#endif
        public float Top { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("right")]
#endif
        public float Right { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("bottom")]
#endif
        public float Bottom { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("left")]
#endif
        public float Left { get; set; }
    }

    public struct FlowStartingPoint
    {
#if JSONNET_EXISTS
        [JsonProperty("nodeId")]
#endif
        public string NodeId { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("name")]
#endif
        public string Name { get; set; }
    }

    public struct PrototypeDevice
    {
#if JSONNET_EXISTS
        [JsonProperty("type")]
#endif
        public PrototypeDeviceType Type { get; set; }

#if JSONNET_EXISTS
        [JsonProperty("rotation")]
#endif
        public string Rotation { get; set; }
    }

    public enum LayoutPositioning
    {
        AUTO,
        ABSOLUTE
    }

    public enum PrototypeDeviceType
    {
        NONE,
        PRESET,
        CUSTOM,
        PRESENTATION
    }

    public enum ConstraintType
    {
        NONE,
        SCALE,
        WIDTH,
        HEIGHT
    }

    public enum NodeType
    {
        NONE,
        DOCUMENT,
        CANVAS,
        FRAME,
        GROUP,
        SECTION,
        VECTOR,
        BOOLEAN_OPERATION,
        STAR,
        LINE,
        ELLIPSE,
        REGULAR_POLYGON,
        RECTANGLE,
        TABLE,
        TABLE_CELL,
        TEXT,
        SLICE,
        COMPONENT,
        COMPONENT_SET,
        INSTANCE,
        STICKY,
        SHAPE_WITH_TEXT,
        CONNECTOR,
        WASHI_TAPE
    }

    public enum PrimaryAxisAlignItem
    {
        NONE,
        MIN,
        CENTER,
        MAX,
        SPACE_BETWEEN
    }

    public enum HyperlinkType
    {
        NONE,
        URL,
        NODE
    }

    public enum CounterAxisAlignItem
    {
        NONE,
        MIN,
        CENTER,
        MAX,
        BASELINE,
        SPACE_BETWEEN
    }

    public enum StrokeAlign
    {
        NONE,
        INSIDE,
        OUTSIDE,
        CENTER
    }

    public enum OverflowDirection
    {
        NONE,
        HORIZONTAL_SCROLLING,
        VERTICAL_SCROLLING,
        HORIZONTAL_AND_VERTICAL_SCROLLING
    }

    public enum StrokeCap
    {
        NONE,
        ROUND,
        SQUARE,
        LINE_ARROW,
        TRIANGLE_ARROW
    }

    public enum LayoutMode
    {
        NONE,
        HORIZONTAL,
        VERTICAL
    }

    public enum LayoutWrap
    {
        NONE,
        NO_WRAP,
        WRAP
    }

    public enum LayoutAlign
    {
        NONE,
        INHERIT,
        STRETCH
    }

    public enum TextAutoResize
    {
        NONE,
        HEIGHT,
        WIDTH_AND_HEIGHT,
        TRUNCATE
    }

    public enum TextDecoration
    {
        NONE,
        UNDERLINE,
        STRIKETHROUGH
    }

    public enum TextCase
    {
        NONE,
        ORIGINAL,
        UPPER,
        LOWER,
        TITLE,
        SMALL_CAPS,
        SMALL_CAPS_FORCED
    }

    public enum TextAlignHorizontal
    {
        NONE,
        LEFT,
        RIGHT,
        CENTER,
        JUSTIFIED
    }

    public enum TextAlignVertical
    {
        NONE,
        TOP,
        CENTER,
        BOTTOM
    }

    public enum CounterAxisAlignContent
    {
        NONE,
        AUTO,
        SPACE_BETWEEN
    }

    public enum TextTruncation
    {
        NONE,
        DISABLED,
        ENDING
    }

    public enum LeadingTrim
    {
        NONE,
        CAP_HEIGHT
    }

    public enum PrimaryAxisSizingMode
    {
        NONE,
        FIXED,
        AUTO
    }

    public enum CounterAxisSizingMode
    {
        NONE,
        FIXED,
        AUTO
    }

    public enum EffectType
    {
        NONE,
        INNER_SHADOW,
        DROP_SHADOW,
        LAYER_BLUR,
        BACKGROUND_BLUR
    }

    public interface IVisible
    {
        bool? Visible { get; set; }
    }
}
#endif