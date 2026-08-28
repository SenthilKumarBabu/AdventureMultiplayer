#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

#if TextMeshPro
using TMPro;
#endif

namespace DA_Assets.UCC
{
    [Serializable]
    public struct FontAtlasSettings
    {
        [SerializeField] int samplingPointSize;
        public int SamplingPointSize => samplingPointSize;

        [SerializeField] int atlasPadding;
        public int AtlasPadding => atlasPadding;

        [SerializeField] GlyphRenderMode renderMode;
        public GlyphRenderMode RenderMode => renderMode;

        [SerializeField] int atlasWidth;
        [SerializeField] int atlasHeight;


        public Vector2Int Resolution => new Vector2Int(atlasWidth, atlasHeight);

#if TextMeshPro
        [SerializeField] AtlasPopulationMode populationMode;
        public AtlasPopulationMode PopulationMode => populationMode;

        public FontAtlasSettings(
            int atlasWidth,
            int atlasHeight,
            AtlasPopulationMode populationMode,
            int samplingPointSize = 90,
            int atlasPadding = 5,
            GlyphRenderMode renderMode = GlyphRenderMode.SDFAA)
        {
            this.atlasWidth = atlasWidth;
            this.atlasHeight = atlasHeight;
            this.populationMode = populationMode;
            this.samplingPointSize = samplingPointSize;
            this.atlasPadding = atlasPadding;
            this.renderMode = renderMode;
        }
#else
        public FontAtlasSettings(
            int atlasWidth,
            int atlasHeight,
            int samplingPointSize = 90,
            int atlasPadding = 5,
            GlyphRenderMode renderMode = GlyphRenderMode.SDFAA)
        {
            this.atlasWidth = atlasWidth;
            this.atlasHeight = atlasHeight;
            this.samplingPointSize = samplingPointSize;
            this.atlasPadding = atlasPadding;
            this.renderMode = renderMode;
        }
#endif
    }
}
#endif