// STUB of the UnityEditor API subset used by RUNTIME scripts inside `#if UNITY_EDITOR` blocks.
// Only compiled in the Editor configuration: in player configurations UnityEditor does not exist, exactly like in a
// real player build, so any UnityEditor use outside `#if UNITY_EDITOR` is reported as an error.
// Editor-folder scripts (Assets/_Project/Scripts/Editor/**) are NOT compiled by the harness.
#if UNITY_EDITOR
using System;
using UnityEngine;

namespace UnityEditor
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class MenuItemAttribute : Attribute
    {
        public MenuItemAttribute(string itemName) { }
        public MenuItemAttribute(string itemName, bool isValidateFunction) { }
        public MenuItemAttribute(string itemName, bool isValidateFunction, int priority) { }
    }

    public enum PlayModeStateChange { EnteredEditMode = 0, ExitingEditMode = 1, EnteredPlayMode = 2, ExitingPlayMode = 3 }

    public static class AssetDatabase
    {
        public static T LoadAssetAtPath<T>(string assetPath) where T : UnityEngine.Object => throw null;
        public static UnityEngine.Object LoadAssetAtPath(string assetPath, Type type) => throw null;
        public static UnityEngine.Object[] LoadAllAssetsAtPath(string assetPath) => throw null;
        public static string[] FindAssets(string filter) => throw null;
        public static string[] FindAssets(string filter, string[] searchInFolders) => throw null;
        public static string GUIDToAssetPath(string guid) => throw null;
        public static string AssetPathToGUID(string path) => throw null;
        public static string GetAssetPath(UnityEngine.Object assetObject) => throw null;
        public static void Refresh() { }
        public static void SaveAssets() { }
    }

    public static class EditorApplication
    {
        public static bool isPlaying { get => throw null; set { } }
        public static bool isPaused { get => throw null; set { } }
        public static bool isCompiling => throw null;
        public static event Action<PlayModeStateChange> playModeStateChanged { add { } remove { } }
        public delegate void CallbackFunction();
        public static CallbackFunction update;
        public static CallbackFunction delayCall;
    }

    public static class EditorUtility
    {
        public static void SetDirty(UnityEngine.Object target) { }
        public static bool DisplayDialog(string title, string message, string ok, string cancel = "") => throw null;
    }

    public static class EditorPrefs
    {
        public static bool GetBool(string key, bool defaultValue = false) => throw null;
        public static void SetBool(string key, bool value) { }
        public static int GetInt(string key, int defaultValue = 0) => throw null;
        public static void SetInt(string key, int value) { }
        public static string GetString(string key, string defaultValue = "") => throw null;
        public static void SetString(string key, string value) { }
    }
}
#endif
