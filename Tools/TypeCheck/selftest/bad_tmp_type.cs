// EXPECT: CS0029
using TMPro;
namespace Meowtel.HarnessSelfTest
{
    public static class BadTmp
    {
        public static void F(TMP_Text t) { t.fontSize = "big"; }
    }
}
