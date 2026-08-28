#if UNITY_EDITOR
using DA_Assets.UCC.Model;
using System;
using UnityEngine;

namespace DA_Assets.UCC
{
    [Serializable]
    public struct FGraphic
    {
        [SerializeField] bool hasFill;
        public bool HasFill { get => hasFill; set => hasFill = value; }

        [SerializeField] bool hasStroke;
        public bool HasStroke { get => hasStroke; set => hasStroke = value; }

        [SerializeField] bool hasSingleColor;
        public bool HasSingleColor { get => hasSingleColor; set => hasSingleColor = value; }

        [SerializeField] bool hasSingleGradient;
        public bool HasSingleGradient { get => hasSingleGradient; set => hasSingleGradient = value; }

        [SerializeField] FFill fill;
        public FFill Fill { get => fill; set => fill = value; }

        [SerializeField] FStroke stroke;
        public FStroke Stroke { get => stroke; set => stroke = value; }

        [SerializeField] Color spriteSingleColor;
        public Color SpriteSingleColor { get => spriteSingleColor; set => spriteSingleColor = value; }

        [SerializeField] Paint spriteSingleLinearGradient;
        public Paint SpriteSingleLinearGradient { get => spriteSingleLinearGradient; set => spriteSingleLinearGradient = value; }

        [SerializeField] bool fillAlpha1;
        public bool FillAlpha1 { get => fillAlpha1; set => fillAlpha1 = value; }
    }
}
#endif