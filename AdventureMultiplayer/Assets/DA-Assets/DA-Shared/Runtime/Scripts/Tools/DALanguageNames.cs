using System.Collections.Generic;
using DA_Assets.Singleton;

namespace DA_Assets.Tools
{
    public static class DALanguageNames
    {
        // Human-readable display names for each DALanguage value.
        private static readonly Dictionary<DALanguage, string> _names = new Dictionary<DALanguage, string>
        {
            { DALanguage.en, "English" },
            { DALanguage.ja, "日本語" },
            { DALanguage.ko, "한국어" },
            { DALanguage.zh, "中文" },
            { DALanguage.es, "Español" },
            { DALanguage.de, "Deutsch" },
            { DALanguage.fr, "Français" },
            { DALanguage.id, "Bahasa Indonesia" },
            { DALanguage.hi, "हिन्दी" },
        };

        /// <summary>
        /// Returns the human-readable display name for the given <see cref="DALanguage"/>.
        /// Falls back to the enum's ToString() if no mapping is found.
        /// </summary>
        public static string GetDisplayName(this DALanguage language)
        {
            return _names.TryGetValue(language, out string name) ? name : language.ToString();
        }

        /// <summary>
        /// Returns an ordered list of all display names matching the order of <see cref="DALanguage"/> enum values.
        /// </summary>
        public static List<string> GetAllDisplayNames()
        {
            var result = new List<string>();
            foreach (DALanguage lang in System.Enum.GetValues(typeof(DALanguage)))
            {
                result.Add(lang.GetDisplayName());
            }
            return result;
        }
    }
}
