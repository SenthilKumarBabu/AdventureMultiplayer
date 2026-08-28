#if UNITY_EDITOR
using DA_Assets.UCC.Model;

namespace DA_Assets.UCC
{
    public static class FigmaEndpoints
    {
        public const string DefaultApiBaseUrl = "https://api.figma.com";
        public const string DefaultWebBaseUrl = "https://www.figma.com";
        public const string DefaultGovApiBaseUrl = "https://api.figma-gov.com";
        public const string DefaultGovWebBaseUrl = "https://www.figma-gov.com";

        public static string GetApiBaseUrl(FigmaEnvironment env)
        {
            switch (env)
            {
                case FigmaEnvironment.FigmaGov: return DefaultGovApiBaseUrl;
                default: return DefaultApiBaseUrl;
            }
        }

        public static string GetWebBaseUrl(FigmaEnvironment env)
        {
            switch (env)
            {
                case FigmaEnvironment.FigmaGov: return DefaultGovWebBaseUrl;
                default: return DefaultWebBaseUrl;
            }
        }

    }
}
#endif