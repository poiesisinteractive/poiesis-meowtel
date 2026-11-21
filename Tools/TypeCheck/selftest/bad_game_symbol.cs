// EXPECT: CS0117
// Unknown member on a real game type must fail (game sources are compiled in place).
namespace Meowtel.HarnessSelfTest
{
    public static class BadGame
    {
        public static void F() { CatHotel.Services.ConsentManager.DoesNotExist(); }
    }
}
