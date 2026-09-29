// EXPECT: CS0234
// CONFIG: Android
// UnityEditor outside #if UNITY_EDITOR breaks the player build.
namespace Meowtel.HarnessSelfTest
{
    public static class BadEditor
    {
        public static void F() => UnityEditor.AssetDatabase.Refresh();
    }
}
