#if UNITY_EDITOR
using DA_Assets.UCC.Model;

namespace DA_Assets.UCC
{
    /// <summary>
    /// Shadow instances of every Settings class, created with default constructors.
    /// Used for per-field reset: compare current value to the corresponding default and restore on demand.
    /// Never serialize or mutate these instances.
    /// </summary>
    public static class FcuDefaults
    {
        public static readonly MainSettings MainSettings = new MainSettings();
        public static readonly ButtonSettings ButtonSettings = new ButtonSettings();
        public static readonly UnityButtonSettings UnityButtonSettings = new UnityButtonSettings();
        public static readonly TextFontsSettings TextFontsSettings = new TextFontsSettings();
        public static readonly TextMeshSettings TextMeshSettings = new TextMeshSettings();
        public static readonly UnityTextSettings UnityTextSettings = new UnityTextSettings();
        public static readonly UitkTextSettings UitkTextSettings = new UitkTextSettings();
        public static readonly ImageSpritesSettings ImageSpritesSettings = new ImageSpritesSettings();
        public static readonly BaseImageSettings BaseImageSettings = new BaseImageSettings();
        public static readonly ShadowSettings ShadowSettings = new ShadowSettings();
        public static readonly LocalizationSettings LocalizationSettings = new LocalizationSettings();
        public static readonly PrefabSettings PrefabSettings = new PrefabSettings();
        public static readonly ScriptGeneratorSettings ScriptGeneratorSettings = new ScriptGeneratorSettings();
        public static readonly UITK_Settings UITK_Settings = new UITK_Settings();
    }
}
#endif