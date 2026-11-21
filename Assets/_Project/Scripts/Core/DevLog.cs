using UnityEngine;

namespace CatHotel.Core
{
    /// <summary>
    /// Debug logging that costs nothing in release builds.
    /// Log/Warn calls, including the evaluation of their arguments, are compiled out of
    /// release players; use them for anything that runs often (per frame, per service use).
    /// Never put side effects inside their arguments.
    /// </summary>
    public static class DevLog
    {
        [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void Log(string message) => Debug.Log(message);

        [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void Warn(string message) => Debug.LogWarning(message);

        /// <summary>
        /// Always written, even in release (bypasses the release filter).
        /// Reserved for rare diagnostic lines without identifiers.
        /// </summary>
        public static void Always(string message) =>
            Debug.unityLogger.logHandler.LogFormat(LogType.Log, null, "{0}", message);

        /// <summary>
        /// Release players keep warnings, errors and exceptions only. Runs before any scene loads.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        [UnityEngine.Scripting.Preserve]
        private static void ApplyReleaseLogFilter()
        {
            if (!Debug.isDebugBuild)
                Debug.unityLogger.filterLogType = LogType.Warning;
        }
    }
}
