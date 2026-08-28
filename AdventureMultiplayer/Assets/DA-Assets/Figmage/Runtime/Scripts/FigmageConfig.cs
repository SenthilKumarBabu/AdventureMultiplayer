using DA_Assets.Constants;
using DA_Assets.Singleton;
using UnityEngine;

namespace DA_Assets.Figmage
{
    [ResourcePath("Figmage")]
    [CreateAssetMenu(menuName = DAConstants.Publisher + "/" + FigmageConfigName)]
    public sealed class FigmageConfig : AssetConfig<FigmageConfig>
    {
        public const string FigmageConfigName = "FigmageConfig";
    }
}
