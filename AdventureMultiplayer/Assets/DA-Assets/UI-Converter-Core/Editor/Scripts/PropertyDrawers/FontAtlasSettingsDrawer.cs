using UnityEditor;
using UnityEngine;

namespace DA_Assets.UCC
{
    [CustomPropertyDrawer(typeof(FontAtlasSettings))]
    public class FontAtlasSettingsDrawer : PropertyDrawer
    {
        private const float Spacing = 2f;
        private const int RowCount = 6;


        private static readonly GUIContent[] ResolutionLabels =
        {
            new GUIContent("8"),    new GUIContent("16"),   new GUIContent("32"),
            new GUIContent("64"),   new GUIContent("128"),  new GUIContent("256"),
            new GUIContent("512"),  new GUIContent("1024"), new GUIContent("2048"),
            new GUIContent("4096"), new GUIContent("8192")
        };

        private static readonly int[] ResolutionValues =
            { 8, 16, 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192 };

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            float lineH = EditorGUIUtility.singleLineHeight;
            float step  = lineH + Spacing;
            float y     = rect.y;

            var samplingProp  = property.FindPropertyRelative("samplingPointSize");
            var paddingProp   = property.FindPropertyRelative("atlasPadding");
            var renderProp    = property.FindPropertyRelative("renderMode");
            var widthProp     = property.FindPropertyRelative("atlasWidth");
            var heightProp    = property.FindPropertyRelative("atlasHeight");
            var modeProp      = property.FindPropertyRelative("populationMode");

            DrawRow(rect, y, FcuLocKey.label_sampling_point_size.Localize(), FcuLocKey.tooltip_sampling_point_size.Localize(), samplingProp); y += step;
            DrawRow(rect, y, FcuLocKey.label_atlas_padding.Localize(),       FcuLocKey.tooltip_atlas_padding.Localize(),       paddingProp);  y += step;
            DrawRow(rect, y, FcuLocKey.label_render_mode.Localize(),         FcuLocKey.tooltip_render_mode.Localize(),         renderProp);   y += step;
            DrawIntPopupRow(rect, y, FcuLocKey.label_atlas_width.Localize(),  FcuLocKey.tooltip_atlas_resolution.Localize(), widthProp);      y += step;
            DrawIntPopupRow(rect, y, FcuLocKey.label_atlas_height.Localize(), FcuLocKey.tooltip_atlas_resolution.Localize(), heightProp);     y += step;

            if (modeProp != null)
                DrawRow(rect, y, FcuLocKey.label_atlas_population_mode.Localize(), FcuLocKey.tooltip_atlas_population_mode.Localize(), modeProp);
        }

        private void DrawRow(Rect rect, float y, string labelText, string tooltip, SerializedProperty prop)
        {
            if (prop == null)
                return;

            float lineH  = EditorGUIUtility.singleLineHeight;
            float labelW = rect.width * 0.45f;
            float fieldW = rect.width - labelW - 5f;

            var labelRect = new Rect(rect.x,               y, labelW, lineH);
            var fieldRect = new Rect(rect.x + labelW + 5f, y, fieldW, lineH);


            EditorGUI.LabelField(labelRect, new GUIContent(labelText, tooltip));

            using (new EditorGUI.PropertyScope(fieldRect, new GUIContent(string.Empty, tooltip), prop))
            {
                EditorGUI.PropertyField(fieldRect, prop, GUIContent.none);
            }
        }

        private void DrawIntPopupRow(Rect rect, float y, string labelText, string tooltip, SerializedProperty prop)
        {
            if (prop == null)
                return;

            float lineH  = EditorGUIUtility.singleLineHeight;
            float labelW = rect.width * 0.45f;
            float fieldW = rect.width - labelW - 5f;

            var labelRect = new Rect(rect.x,               y, labelW, lineH);
            var fieldRect = new Rect(rect.x + labelW + 5f, y, fieldW, lineH);

            EditorGUI.LabelField(labelRect, new GUIContent(labelText, tooltip));

            using (new EditorGUI.PropertyScope(fieldRect, new GUIContent(string.Empty, tooltip), prop))
            {
                prop.intValue = EditorGUI.IntPopup(fieldRect, prop.intValue, ResolutionLabels, ResolutionValues);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float lineH = EditorGUIUtility.singleLineHeight;
            float step  = lineH + Spacing;
            return step * RowCount - Spacing;
        }
    }
}