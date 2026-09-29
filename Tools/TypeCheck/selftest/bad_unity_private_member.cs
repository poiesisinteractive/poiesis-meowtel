// EXPECT: CS1061
// Private UnityEngine member (Roslyn reports inaccessible instance members as CS1061): the nuget reference DLLs are publicized; UnityRefPatcher must restore accessibility.
namespace Meowtel.HarnessSelfTest
{
    public static class BadPrivate
    {
        public static System.IntPtr F(UnityEngine.Object o) => o.GetCachedPtr();
    }
}
