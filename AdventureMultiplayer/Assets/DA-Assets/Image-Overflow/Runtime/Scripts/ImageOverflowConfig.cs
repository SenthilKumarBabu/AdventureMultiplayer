using DA_Assets.Constants;
using DA_Assets.Singleton;
using UnityEngine;

namespace DA_Assets.ImgOverflow
{
    [CreateAssetMenu(menuName = DAConstants.Publisher + "/" + ImageOverflowConfigName)]
    [ResourcePath("")]
    public class ImageOverflowConfig : AssetConfig<ImageOverflowConfig>
    {
        public const string ImageOverflowConfigName = "ImageOverflowConfig";
    }
}
