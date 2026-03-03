using UnityEngine;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace _Project.Scripts.VisualScripting
{
    public static class VisualLogger
    {
        [Conditional("DEBUG"), Conditional("UNITY_EDITOR")]
        public static void Log(this UnityEngine.Object context, string message, bool showLog = true)
        {
            if (!showLog) return;
            string objectName = (context != null) ? context.name : "Global";
            Debug.Log($"<color=#00FF00>[{objectName}]</color> {message}", context);
        }

        [Conditional("DEBUG"), Conditional("UNITY_EDITOR")]
        public static void LogError(this UnityEngine.Object context, string message)
        {
            string objectName = (context != null) ? context.name : "Global";
            Debug.LogError($"[{objectName}] {message}", context);
        }
    }
}