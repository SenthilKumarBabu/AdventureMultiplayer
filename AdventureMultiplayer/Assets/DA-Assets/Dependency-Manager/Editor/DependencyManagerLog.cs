using UnityEngine;

namespace DA_Assets.DM
{
    internal static class DependencyManagerLog
    {
        private static bool IsEnabled => DependencyManagerConfig.Instance.Debug;

        public static void Log(string message)
        {
            if (IsEnabled)
            {
                Debug.Log(message);
            }
        }

        public static void Warning(string message)
        {
            if (IsEnabled)
            {
                Debug.LogWarning(message);
            }
        }

        public static void Error(string message)
        {
            if (IsEnabled)
            {
                Debug.LogError(message);
            }
        }
    }
}
