using UnityEngine;

namespace Pholus.Editor.Core
{
    /// <summary>
    /// Centralized logging for Pholus with toggleable log levels.
    /// All log messages are prefixed with [Pholus] for easy filtering.
    /// </summary>
    public static class PholusLogger
    {
        private const string Prefix = "[Pholus]";

        /// <summary>
        /// Logs a debug message if debug logging is enabled.
        /// </summary>
        public static void Log(string message)
        {
            if (PholusSettings.Instance.LogDebugMessages)
            {
                Debug.Log($"{Prefix} {message}");
            }
        }

        /// <summary>
        /// Logs a warning message if warning logging is enabled.
        /// </summary>
        public static void LogWarning(string message)
        {
            if (PholusSettings.Instance.LogWarningMessages)
            {
                Debug.LogWarning($"{Prefix} {message}");
            }
        }

        /// <summary>
        /// Logs an error message if error logging is enabled.
        /// </summary>
        public static void LogError(string message)
        {
            if (PholusSettings.Instance.LogErrorMessages)
            {
                Debug.LogError($"{Prefix} {message}");
            }
        }

        /// <summary>
        /// Logs a formatted debug message if debug logging is enabled.
        /// </summary>
        public static void LogFormat(string format, params object[] args)
        {
            if (PholusSettings.Instance.LogDebugMessages)
            {
                Debug.Log($"{Prefix} {string.Format(format, args)}");
            }
        }

        /// <summary>
        /// Logs a formatted warning message if warning logging is enabled.
        /// </summary>
        public static void LogWarningFormat(string format, params object[] args)
        {
            if (PholusSettings.Instance.LogWarningMessages)
            {
                Debug.LogWarning($"{Prefix} {string.Format(format, args)}");
            }
        }

        /// <summary>
        /// Logs a formatted error message if error logging is enabled.
        /// </summary>
        public static void LogErrorFormat(string format, params object[] args)
        {
            if (PholusSettings.Instance.LogErrorMessages)
            {
                Debug.LogError($"{Prefix} {string.Format(format, args)}");
            }
        }
    }
}
