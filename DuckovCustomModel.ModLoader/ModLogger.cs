using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DuckovCustomModel
{
    internal static class ModLogger
    {
        public const string ModTag = $"[{Constant.ModName}]";

        public static void Log(object message)
        {
            Debug.Log($"{ModTag} {message}");
        }

        public static void Log(object message, Object context)
        {
            Debug.Log($"{ModTag} {message}", context);
        }

        public static void LogFormat(string format, params object[] args)
        {
            Debug.LogFormat($"{ModTag} {format}", args);
        }

        public static void LogFormat(Object context, string format, params object[] args)
        {
            Debug.LogFormat(context, $"{ModTag} {format}", args);
        }

        public static void LogFormat(
            LogType logType,
            LogOption logOptions,
            Object context,
            string format,
            params object[] args)
        {
            Debug.LogFormat(logType, logOptions, context, $"{ModTag} {format}", args);
        }

        public static void LogError(object message)
        {
            Debug.LogError($"{ModTag} {message}");
        }

        public static void LogError(object message, Object context)
        {
            Debug.LogError($"{ModTag} {message}", context);
        }

        public static void LogErrorFormat(string format, params object[] args)
        {
            Debug.LogErrorFormat($"{ModTag} {format}", args);
        }

        public static void LogErrorFormat(Object context, string format, params object[] args)
        {
            Debug.LogErrorFormat(context, $"{ModTag} {format}", args);
        }

        public static void LogException(Exception exception)
        {
            Debug.LogException(exception);
        }

        public static void LogException(Exception exception, Object context)
        {
            Debug.LogException(exception, context);
        }

        public static void LogWarning(object message)
        {
            Debug.LogWarning($"{ModTag} {message}");
        }

        public static void LogWarning(object message, Object context)
        {
            Debug.LogWarning($"{ModTag} {message}", context);
        }

        public static void LogWarningFormat(string format, params object[] args)
        {
            Debug.LogWarningFormat($"{ModTag} {format}", args);
        }

        public static void LogWarningFormat(Object context, string format, params object[] args)
        {
            Debug.LogWarningFormat(context, $"{ModTag} {format}", args);
        }
    }
}
