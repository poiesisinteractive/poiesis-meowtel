// STUB of com.unity.services.levelplay 9.4.1 - namespace Unity.Services.LevelPlay. Compile-only.
// Based on the call sites in Services/Ads/AdManager.cs plus the LevelPlay 9.x unified API.
// Members marked // GUESS were not verified against the package source.
using System;
using System.Collections.Generic;

namespace Unity.Services.LevelPlay
{
    public static class LevelPlay
    {
        public static event Action<LevelPlayConfiguration> OnInitSuccess { add { } remove { } }   // AdManager.cs:87
        public static event Action<LevelPlayInitError> OnInitFailed { add { } remove { } }        // AdManager.cs:88
        public static event Action<LevelPlayImpressionData> OnImpressionDataReady { add { } remove { } } // GUESS
        public static void Init(string appKey, string userId = null, LevelPlayAdFormat[] adFormats = null) { } // AdManager.cs:91 // GUESS: optional params
        public static void SetMetaData(string key, string value) { }                              // AdManager.cs:83
        public static void SetMetaData(string key, params string[] values) { }                    // GUESS
        public static void LaunchTestSuite() { }                                                   // AdManager.cs:103
        public static void ValidateIntegration() { }                                               // GUESS
        public static void SetConsent(bool consent) { }                                            // GUESS: may be superseded by LevelPlayPrivacySettings
        public static void SetDynamicUserId(string dynamicUserId) { }                              // GUESS
        public static void SetPauseGame(bool pause) { }                                            // GUESS
        public static void SetAdaptersDebug(bool enabled) { }                                      // GUESS
        public static void SetNetworkData(string networkKey, string networkData) { }               // GUESS
        public static void SetSegment(LevelPlaySegment segment) { }                                // GUESS
    }

    public static class LevelPlayPrivacySettings
    {
        public static void SetGDPRConsents(Dictionary<string, bool> consents) { } // AdManager.cs:77 // GUESS: exact parameter type
        public static void SetGDPRConsent(bool consent) { }                       // GUESS
        public static void SetCCPA(bool doNotSell) { }                            // GUESS
        public static void SetCOPPA(bool isChildDirected) { }                     // AdManager.cs:78
    }

    public enum LevelPlayAdFormat { BANNER = 0, INTERSTITIAL = 1, REWARDED = 2, NATIVE_AD = 3 } // GUESS

    public class LevelPlaySegment // GUESS
    {
        public string SegmentName { get => throw null; set { } }
        public int Level { get => throw null; set { } }
        public int IsPaying { get => throw null; set { } }
        public double IapTotal { get => throw null; set { } }
        public long UserCreationDate { get => throw null; set { } }
        public void SetCustom(string key, string value) { }
    }

    public class LevelPlayConfiguration
    {
        public bool IsAdQualityEnabled => throw null; // GUESS
    }

    public class LevelPlayInitError
    {
        public int ErrorCode => throw null;       // GUESS
        public string ErrorMessage => throw null; // AdManager.cs:120
    }

    public class LevelPlayAdError
    {
        public string AdUnitId => throw null;     // GUESS
        public string AdId => throw null;         // GUESS
        public int ErrorCode => throw null;       // GUESS
        public string ErrorMessage => throw null; // AdManager.cs:141,167
    }

    public class LevelPlayAdInfo // GUESS: property list
    {
        public string AdId => throw null;
        public string AdUnitId => throw null;
        public string AdUnitName => throw null;
        public string AdFormat => throw null;
        public string PlacementName => throw null;
        public string AuctionId => throw null;
        public string Country => throw null;
        public string Ab => throw null;
        public string SegmentName => throw null;
        public string AdNetwork => throw null;
        public string InstanceName => throw null;
        public string InstanceId => throw null;
        public double? Revenue => throw null;
        public string Precision => throw null;
        public string EncryptedCPM => throw null;
        public string CreativeId => throw null;
    }

    public class LevelPlayReward
    {
        public string Name => throw null; // AdManager.cs:149
        public int Amount => throw null;  // AdManager.cs:149 // GUESS: int (only interpolated in the game)
    }

    public class LevelPlayImpressionData // GUESS: property list
    {
        public string AuctionId => throw null;
        public string AdUnit => throw null;
        public string AdUnitName => throw null;
        public string AdUnitId => throw null;
        public string AdFormat => throw null;
        public string Country => throw null;
        public string Ab => throw null;
        public string SegmentName => throw null;
        public string Placement => throw null;
        public string AdNetwork => throw null;
        public string InstanceName => throw null;
        public string InstanceId => throw null;
        public double? Revenue => throw null;
        public string Precision => throw null;
        public string EncryptedCPM => throw null;
        public string CreativeId => throw null;
    }

    public sealed class LevelPlayRewardedAd : IDisposable
    {
        public LevelPlayRewardedAd(string adUnitId) { }                                  // AdManager.cs:125
        public event Action<LevelPlayAdInfo> OnAdLoaded { add { } remove { } }           // AdManager.cs:127
        public event Action<LevelPlayAdError> OnAdLoadFailed { add { } remove { } }      // AdManager.cs:139
        public event Action<LevelPlayAdInfo> OnAdDisplayed { add { } remove { } }        // GUESS: same shape as OnAdLoaded
        public event Action<LevelPlayAdInfo, LevelPlayAdError> OnAdDisplayFailed { add { } remove { } } // AdManager.cs:165
        public event Action<LevelPlayAdInfo, LevelPlayReward> OnAdRewarded { add { } remove { } }       // AdManager.cs:147
        public event Action<LevelPlayAdInfo> OnAdClosed { add { } remove { } }           // AdManager.cs:182
        public event Action<LevelPlayAdInfo> OnAdClicked { add { } remove { } }          // GUESS
        public event Action<LevelPlayAdInfo> OnAdInfoChanged { add { } remove { } }      // GUESS
        public void LoadAd() { }                                                         // AdManager.cs:196
        public void ShowAd(string placementName = null) { }                              // AdManager.cs:246 // GUESS: optional param
        public bool IsAdReady() => throw null;                                           // GUESS
        public void DestroyAd() { }                                                      // AdManager.cs:347
        public string GetAdUnitId() => throw null;                                       // GUESS
        public string GetAdId() => throw null;                                           // GUESS
        public static bool IsPlacementCapped(string placementName) => throw null;        // GUESS
        public void Dispose() { }                                                        // GUESS: IDisposable
    }

    public sealed class LevelPlayInterstitialAd : IDisposable // GUESS: mirrors LevelPlayRewardedAd without the reward event
    {
        public LevelPlayInterstitialAd(string adUnitId) { }
        public event Action<LevelPlayAdInfo> OnAdLoaded { add { } remove { } }
        public event Action<LevelPlayAdError> OnAdLoadFailed { add { } remove { } }
        public event Action<LevelPlayAdInfo> OnAdDisplayed { add { } remove { } }
        public event Action<LevelPlayAdInfo, LevelPlayAdError> OnAdDisplayFailed { add { } remove { } }
        public event Action<LevelPlayAdInfo> OnAdClosed { add { } remove { } }
        public event Action<LevelPlayAdInfo> OnAdClicked { add { } remove { } }
        public event Action<LevelPlayAdInfo> OnAdInfoChanged { add { } remove { } }
        public void LoadAd() { }
        public void ShowAd(string placementName = null) { }
        public bool IsAdReady() => throw null;
        public void DestroyAd() { }
        public string GetAdUnitId() => throw null;
        public static bool IsPlacementCapped(string placementName) => throw null;
        public void Dispose() { }
    }
}
