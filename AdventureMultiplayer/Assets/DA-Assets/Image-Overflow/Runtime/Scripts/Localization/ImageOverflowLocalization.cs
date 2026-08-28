namespace DA_Assets.ImgOverflow
{
    public enum ImageOverflowLocKey
    {
        tooltip_edit_mode_button,
        label_edit_area_anchors,
        undo_adjust_area_anchors,
        label_distance_in_pixels
    }

    public static class ImageOverflowLocExtensions
    {
        public static string Localize(this ImageOverflowLocKey key, params object[] args) =>
            ImageOverflowConfig.Instance.Localizator.GetLocalizedText(key, null, args);
    }
}
