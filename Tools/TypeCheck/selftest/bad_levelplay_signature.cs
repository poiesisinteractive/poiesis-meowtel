// EXPECT: CS1593
// OnAdDisplayFailed is Action<LevelPlayAdInfo, LevelPlayAdError>: a one-parameter lambda must fail.
using Unity.Services.LevelPlay;
namespace Meowtel.HarnessSelfTest
{
    public static class BadLevelPlay
    {
        public static void F(LevelPlayRewardedAd ad) { ad.OnAdDisplayFailed += error => { }; }
    }
}
