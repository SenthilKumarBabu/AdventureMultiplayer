namespace DA_Assets.DM
{
    public enum DependencyManagerLocKey
    {

        label_dependency_manager_window,
        label_dependencies_count,
        label_refresh,
        label_auto_detect,
        label_disabled_manually,
        label_auto_managed,
    }

    public static class DependencyManagerLocExtensions
    {
        public static string Localize(this DependencyManagerLocKey key, params object[] args) =>
            DependencyManagerConfig.Instance.Localizator.GetLocalizedText(key, null, args);
    }
}
