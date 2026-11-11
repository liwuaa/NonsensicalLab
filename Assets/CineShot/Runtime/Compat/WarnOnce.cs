using System.Collections.Generic;
using UnityEngine;

namespace CineShot.Compat
{
    public static class WarnOnce
    {
        static readonly HashSet<string> s_Runtime = new HashSet<string>();

        public static void Log(string id, string message)
        {
            if (string.IsNullOrEmpty(id))
                id = message;

#if UNITY_EDITOR
            if (UnityEditor.SessionState.GetBool("CineShot.Warn." + id, false))
                return;
            UnityEditor.SessionState.SetBool("CineShot.Warn." + id, true);
#else
            if (!s_Runtime.Add(id))
                return;
#endif
            Debug.LogWarning(message);
        }

        public static void ResetRuntime()
        {
            s_Runtime.Clear();
        }
    }
}
