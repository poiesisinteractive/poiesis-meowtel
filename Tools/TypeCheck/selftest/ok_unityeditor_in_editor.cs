// EXPECT: OK
// CONFIG: Editor
namespace Meowtel.HarnessSelfTest
{
    public static class OkEditor
    {
        public static void F() => UnityEditor.AssetDatabase.Refresh();
    }
}
