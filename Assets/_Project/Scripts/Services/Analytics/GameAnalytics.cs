using System;
using CatHotel.Core;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Analytics;
#if ENABLE_UNITY_CONSENT
using UnityEngine.UnityConsent;
#endif

namespace CatHotel.Services
{
    /// <summary>
    /// Unity Analytics facade. Collection only runs when the player accepted the analytics
    /// question of the consent popup (ConsentManager) and UGS is initialized; every call is a
    /// no-op otherwise and never throws. Call from the main thread only.
    ///
    /// Unity 6.2+ (ENABLE_UNITY_CONSENT): collection is driven through EndUserConsent
    /// (AnalyticsIntent) — Start/StopDataCollection must never be called in that mode.
    /// Older engines: Start/StopDataCollection after UGS init.
    ///
    /// Every event and parameter below must be declared in the UGS dashboard (Event Manager),
    /// with the same types, otherwise the events are flagged invalid.
    /// </summary>
    public static class GameAnalytics
    {
        public const string CurrencyCoins = "coins";

        private const string EvTutorialStep = "tutorial_step";
        private const string EvTutorialComplete = "tutorial_complete";
        private const string EvTutorialSkip = "tutorial_skip";
        private const string EvPensionComplete = "pension_complete";
        private const string EvCatLeftUnhappy = "cat_left_unhappy";
        private const string EvAdoptionComplete = "adoption_complete";
        private const string EvLevelUp = "level_up";
        private const string EvObjectBought = "object_bought";
        private const string EvAdOfferShown = "ad_offer_shown";
        private const string EvAdRewardGranted = "ad_reward_granted";

        private const string PendingDeletionKey = "Analytics_PendingDeletion";

        private static bool _servicesReady;
        private static bool _consentGranted;
        private static int _warnings;
#if !ENABLE_UNITY_CONSENT
        private static bool _legacyStarted;
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _servicesReady = false;
            _consentGranted = false;
            _warnings = 0;
#if !ENABLE_UNITY_CONSENT
            _legacyStarted = false;
#endif
        }

        // Before any Awake, so before AuthManager initializes UGS.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap() => SyncConsent();

        public static bool IsCollecting => _consentGranted && ServicesReady;

        private static bool ServicesReady
        {
            get
            {
                if (!_servicesReady)
                {
                    try { _servicesReady = UnityServices.State == ServicesInitializationState.Initialized; }
                    catch { /* State throws off the Unity thread */ }
                }
                return _servicesReady;
            }
        }

        /// <summary>Applies the stored analytics choice (ConsentManager) to the SDK. Idempotent.</summary>
        public static void SyncConsent()
        {
            try
            {
                bool chosen = ConsentManager.HasMadeAnalyticsChoice;
                _consentGranted = chosen && ConsentManager.AnalyticsConsentGiven; // cached: PlayerPrefs is main-thread only
#if ENABLE_UNITY_CONSENT
                var wanted = !chosen ? ConsentStatus.Unspecified
                    : _consentGranted ? ConsentStatus.Granted : ConsentStatus.Denied;
                var state = EndUserConsent.GetConsentState();
                if (state.AnalyticsIntent != wanted)
                {
                    // AdsIntent is left untouched. The SDK starts, or stops and flushes, by itself.
                    state.AnalyticsIntent = wanted;
                    EndUserConsent.SetConsentState(state);
                }
#else
                ApplyLegacy();
#endif
                DevLog.Log($"[Analytics] consent sync -> {(chosen ? (_consentGranted ? "granted" : "denied") : "unspecified")}");
            }
            catch (Exception e)
            {
                Warn(nameof(SyncConsent), e);
            }
        }

        /// <summary>Called by AuthManager right after UnityServices.InitializeAsync succeeds.</summary>
        public static void NotifyServicesInitialized()
        {
            _servicesReady = true;
#if !ENABLE_UNITY_CONSENT
            try { ApplyLegacy(); }
            catch (Exception e) { Warn(nameof(NotifyServicesInitialized), e); }
#endif
            DevLog.Log($"[Analytics] UGS ready, collecting={IsCollecting}");

            // A deletion requested while offline / before init is replayed now.
            if (!_consentGranted && PlayerPrefs.GetInt(PendingDeletionKey, 0) == 1)
                RequestDataDeletion();
        }

#if !ENABLE_UNITY_CONSENT
        private static void ApplyLegacy()
        {
            if (!ServicesReady) return; // AnalyticsService.Instance throws before init
            if (_consentGranted && !_legacyStarted)
            {
                AnalyticsService.Instance.StartDataCollection();
                _legacyStarted = true;
            }
            else if (!_consentGranted && _legacyStarted)
            {
                AnalyticsService.Instance.StopDataCollection();
                _legacyStarted = false;
            }
        }
#endif

        /// <summary>
        /// Asks Unity to delete the data already collected for this player (right to erasure),
        /// called when the player withdraws analytics consent. Only valid once consent is refused
        /// (the SDK throws while it is granted). Persisted and replayed if UGS is not ready yet.
        /// </summary>
        public static void RequestDataDeletion()
        {
            if (_consentGranted) return;
            if (!ServicesReady)
            {
                PlayerPrefs.SetInt(PendingDeletionKey, 1);
                PlayerPrefs.Save();
                return;
            }
            try
            {
                AnalyticsService.Instance.RequestDataDeletion();
                PlayerPrefs.DeleteKey(PendingDeletionKey);
                PlayerPrefs.Save();
                DevLog.Log("[Analytics] data deletion requested");
            }
            catch (Exception e)
            {
                PlayerPrefs.SetInt(PendingDeletionKey, 1);
                PlayerPrefs.Save();
                Warn(nameof(RequestDataDeletion), e);
            }
        }

        // ---------- Events ----------

        public static void TutorialStep(int stepIndex, string stepId)
        {
            if (!Ready(EvTutorialStep)) return;
            Send(EvTutorialStep, new CustomEvent(EvTutorialStep)
            {
                { "step_index", stepIndex },
                { "step_id", Safe(stepId) }
            });
        }

        public static void TutorialComplete(int durationSeconds)
        {
            if (!Ready(EvTutorialComplete)) return;
            Send(EvTutorialComplete, new CustomEvent(EvTutorialComplete)
            {
                { "duration_s", Mathf.Max(0, durationSeconds) }
            });
        }

        public static void TutorialSkip(int stepIndex)
        {
            if (!Ready(EvTutorialSkip)) return;
            Send(EvTutorialSkip, new CustomEvent(EvTutorialSkip)
            {
                { "step_index", stepIndex }
            });
        }

        /// <summary>
        /// coins = payment + tip, without the ×2 ad bonus (measured by ad_reward_granted);
        /// is_boosted = the rewarded-ad revenue boost was active (coins already include it).
        /// </summary>
        public static void PensionComplete(int happiness, int coins, bool isSpecial, int level, bool isBoosted)
        {
            if (!Ready(EvPensionComplete)) return;
            Send(EvPensionComplete, new CustomEvent(EvPensionComplete)
            {
                { "happiness", happiness },
                { "coins", coins },
                { "is_special", isSpecial },
                { "level", level },
                { "is_boosted", isBoosted }
            });
        }

        public static void CatLeftUnhappy(string breed, int level)
        {
            if (!Ready(EvCatLeftUnhappy)) return;
            Send(EvCatLeftUnhappy, new CustomEvent(EvCatLeftUnhappy)
            {
                { "breed", Safe(breed) },
                { "level", level }
            });
        }

        /// <summary>fee = adoption fee credited, without the ×2 ad bonus; is_boosted as in PensionComplete.</summary>
        public static void AdoptionComplete(string breed, int fee, bool isBoosted)
        {
            if (!Ready(EvAdoptionComplete)) return;
            Send(EvAdoptionComplete, new CustomEvent(EvAdoptionComplete)
            {
                { "breed", Safe(breed) },
                { "fee", fee },
                { "is_boosted", isBoosted }
            });
        }

        public static void LevelUp(int level, int minutesPlayed)
        {
            if (!Ready(EvLevelUp)) return;
            Send(EvLevelUp, new CustomEvent(EvLevelUp)
            {
                { "level", level },
                { "minutes_played", Mathf.Max(0, minutesPlayed) }
            });
        }

        public static void ObjectBought(string objectId, string currency, int price)
        {
            if (!Ready(EvObjectBought)) return;
            Send(EvObjectBought, new CustomEvent(EvObjectBought)
            {
                { "object_id", Safe(objectId) },
                { "currency", Safe(currency) },
                { "price", price }
            });
        }

        public static void AdOfferShown(string placement)
        {
            if (!Ready(EvAdOfferShown)) return;
            Send(EvAdOfferShown, new CustomEvent(EvAdOfferShown)
            {
                { "placement", Safe(placement) }
            });
        }

        public static void AdRewardGranted(string placement)
        {
            if (!Ready(EvAdRewardGranted)) return;
            Send(EvAdRewardGranted, new CustomEvent(EvAdRewardGranted)
            {
                { "placement", Safe(placement) }
            });
        }

        // ---------- Helpers ----------

        private static bool Ready(string evt)
        {
            if (IsCollecting) return true;
            DevLog.Log($"[Analytics] {evt} dropped (consent={_consentGranted}, ugs={ServicesReady})");
            return false;
        }

        private static void Send(string evt, CustomEvent e)
        {
            // AnalyticsService.Instance throws if the SDK is not up
            try
            {
                AnalyticsService.Instance.RecordEvent(e);
                DevLog.Log("[Analytics] recorded " + evt);
            }
            catch (Exception ex)
            {
                Warn(evt, ex);
            }
        }

        // CustomEvent.Add(key, null) throws: never send a null string.
        private static string Safe(string s) => string.IsNullOrEmpty(s) ? "unknown" : s;

        private static void Warn(string where, Exception e)
        {
            if (_warnings++ < 5) Debug.LogWarning($"[Analytics] {where} failed: {e.Message}");
        }
    }
}
