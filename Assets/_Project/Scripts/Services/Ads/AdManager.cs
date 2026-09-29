using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Services.LevelPlay;

namespace CatHotel.Services
{
    /// <summary>
    /// LevelPlay rewarded ads. One API for every placement: ShowRewarded(placement, onComplete),
    /// whose callback runs exactly once per accepted request (true = reward granted).
    /// Initialization is idempotent and retried with backoff when it fails.
    /// </summary>
    public class AdManager : MonoBehaviour
    {
        public static AdManager Instance { get; private set; }

        // LevelPlay placement names (also the analytics 'placement' parameter)
        public const string PlacementBoostX2 = "BoostX2";
        public const string PlacementPensionX2 = "X2Pension";
        public const string PlacementAdoptionX2 = "X2Adoption";

        [SerializeField] private AdConfig _config;

        private enum InitState { NotStarted, InProgress, Ready }

        private InitState _initState = InitState.NotStarted;
        private bool _handlersRegistered;
        private int _initAttempts;
        private Coroutine _initRetryCoroutine;
        private static readonly float[] InitRetryDelays = { 10f, 30f, 60f, 120f, 300f };

        private LevelPlayRewardedAd _rewardedAd;
        private bool _adLoaded;
        private Coroutine _loadRetryCoroutine;
        private const float LoadRetryDelaySeconds = 30f;

        // In-flight rewarded request (at most one)
        private sealed class RewardedRequest
        {
            public string Placement;
            public Action<bool> Callback;
            public bool Rewarded;
        }
        private RewardedRequest _request;
        private Coroutine _requestWatchdog;
        // LevelPlay does not guarantee the order of OnAdRewarded and OnAdClosed:
        // after a close without reward, wait this long for a late reward before failing.
        private const float LateRewardGraceSeconds = 3f;
        // Safety net if the SDK never reports back (no close, no reward, no failure).
        private const float RequestTimeoutSeconds = 180f;

        // Daily tracking
        private int _adsWatchedToday;
        private string _lastResetDate;
        private float _dayCheckTimer;
        private const string PrefKey = "AdDailyCount";
        private const string PrefDateKey = "AdDailyDate";

        private bool SdkReady => _initState == InitState.Ready;

        public bool IsAdReady => SdkReady && _adLoaded;
        public bool HasReachedDailyCap => _config != null && _adsWatchedToday >= _config.dailyCap;
        /// <summary>True when a rewarded ad can be shown right now (loaded, under the daily cap, none in progress).</summary>
        public bool CanShowRewarded => IsAdReady && !HasReachedDailyCap && _request == null;
        public bool IsShowingAd => _request != null;
        public int AdsWatchedToday => _adsWatchedToday;
        public int DailyCap => _config != null ? _config.dailyCap : 10;

        /// <summary>Raised whenever CanShowRewarded may have changed.</summary>
        public event Action OnAdAvailabilityChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadDailyCount();
        }

        // ---------- Initialization ----------

        /// <summary>Idempotent: safe to call from Boot and again from the game scene.</summary>
        public void InitializeAds()
        {
            if (_config == null)
            {
                Debug.LogError("[Ads] AdConfig is missing!");
                return;
            }
            if (_initState != InitState.NotStarted) return;

            if (_config.stubAdsForBeta)
            {
                Debug.Log("[Ads] STUB MODE — LevelPlay bypassed, ads will be simulated");
                _initState = InitState.Ready;
                _adLoaded = true;
                RaiseAvailabilityChanged();
                return;
            }

            ApplyPrivacySettings();

            if (_config.testMode)
            {
                LevelPlay.SetMetaData("is_test_suite", "enable");
                Debug.Log("[Ads] Test mode enabled");
            }

            if (!_handlersRegistered)
            {
                LevelPlay.OnInitSuccess += OnSdkInitSuccess;
                LevelPlay.OnInitFailed += OnSdkInitFailed;
                _handlersRegistered = true;
            }

            StartInit();
        }

        /// <summary>
        /// Pushes the player's ads consent to LevelPlay. Called before Init, and again when the
        /// player changes the choice in the settings (applies to the next ad requests).
        /// </summary>
        public void ApplyPrivacySettings()
        {
            bool consent = ConsentManager.ConsentGiven;
            var networkConsents = new Dictionary<string, bool>
            {
                { "UnityAds", consent },
                { "IronSource", consent }
            };
            LevelPlayPrivacySettings.SetGDPRConsents(networkConsents);
            LevelPlayPrivacySettings.SetCOPPA(false); // Meowtel n'est pas destine aux enfants
            Debug.Log($"[Ads] GDPR consent applied: {(consent ? "accepted" : "refused")}");
        }

        private void StartInit()
        {
            _initState = InitState.InProgress;
            _initAttempts++;
            Debug.Log($"[Ads] Initializing LevelPlay (attempt {_initAttempts})");
            LevelPlay.Init(_config.appKey);
        }

        private void OnSdkInitSuccess(LevelPlayConfiguration config)
        {
            if (_initState == InitState.Ready) return;
            Debug.Log("[Ads] LevelPlay SDK initialized");
            _initState = InitState.Ready;
            if (_initRetryCoroutine != null)
            {
                StopCoroutine(_initRetryCoroutine);
                _initRetryCoroutine = null;
            }
            CreateRewardedAd();

            if (_config.launchTestSuiteOnStart)
            {
                Debug.Log("[Ads] Launching LevelPlay Test Suite...");
                LevelPlay.LaunchTestSuite();
            }
        }

        private void OnSdkInitFailed(LevelPlayInitError error)
        {
            if (_initState == InitState.Ready) return;
            _initState = InitState.NotStarted;

            int index = _initAttempts - 1;
            if (index >= InitRetryDelays.Length)
            {
                Debug.LogWarning($"[Ads] LevelPlay init failed ({error.ErrorMessage}) — giving up for this session");
                return;
            }
            float delay = InitRetryDelays[index];
            Debug.LogWarning($"[Ads] LevelPlay init failed ({error.ErrorMessage}) — retry in {delay}s");
            if (_initRetryCoroutine != null) StopCoroutine(_initRetryCoroutine);
            _initRetryCoroutine = StartCoroutine(RetryInitAfter(delay));
        }

        private IEnumerator RetryInitAfter(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            _initRetryCoroutine = null;
            if (_initState != InitState.NotStarted) yield break;
            ApplyPrivacySettings();
            StartInit();
        }

        public void LaunchTestSuite()
        {
            if (!SdkReady || _config.stubAdsForBeta)
            {
                Debug.LogWarning("[Ads] Cannot launch Test Suite - SDK not ready");
                return;
            }
            Debug.Log("[Ads] Launching LevelPlay Test Suite manually");
            LevelPlay.LaunchTestSuite();
        }

        // ---------- Loading ----------

        private void CreateRewardedAd()
        {
            _rewardedAd = new LevelPlayRewardedAd(_config.rewardedAdUnitId);

            _rewardedAd.OnAdLoaded += info =>
            {
                Debug.Log("[Ads] Rewarded ad loaded");
                _adLoaded = true;
                if (_loadRetryCoroutine != null)
                {
                    StopCoroutine(_loadRetryCoroutine);
                    _loadRetryCoroutine = null;
                }
                RaiseAvailabilityChanged();
            };

            _rewardedAd.OnAdLoadFailed += error =>
            {
                Debug.LogWarning($"[Ads] Rewarded ad load failed: {error.ErrorMessage} — scheduling retry");
                _adLoaded = false;
                RaiseAvailabilityChanged();
                ScheduleReload();
            };

            _rewardedAd.OnAdRewarded += (info, reward) =>
            {
                Debug.Log($"[Ads] Reward received ({_request?.Placement ?? "no request"}): {reward.Name} x{reward.Amount}");
                OnRewardReceived();
            };

            _rewardedAd.OnAdDisplayFailed += (info, error) =>
            {
                Debug.LogWarning($"[Ads] Rewarded ad display failed ({_request?.Placement}): {error.ErrorMessage}");
                _adLoaded = false;
                CompleteRequest(false);
                LoadRewardedAd();
            };

            _rewardedAd.OnAdClosed += info =>
            {
                Debug.Log("[Ads] Rewarded ad closed — preloading next");
                OnAdClosedByUser();
                LoadRewardedAd();
            };

            LoadRewardedAd();
        }

        private void LoadRewardedAd()
        {
            if (!SdkReady || _rewardedAd == null) return;
            if (HasReachedDailyCap) return; // nothing to show today; reloaded at the next day rollover

            _rewardedAd.LoadAd();
        }

        /// <summary>
        /// After a load failure (e.g. LevelPlay pacing window blocking fill),
        /// retry periodically until the ad loads. Without this the ad would
        /// never reload after the first paced failure → button stays dead.
        /// </summary>
        private void ScheduleReload()
        {
            if (_loadRetryCoroutine != null) return; // already retrying
            if (!SdkReady || _rewardedAd == null) return;
            if (HasReachedDailyCap) return;

            _loadRetryCoroutine = StartCoroutine(RetryLoadLoop());
        }

        private IEnumerator RetryLoadLoop()
        {
            while (!_adLoaded && SdkReady && _rewardedAd != null && !HasReachedDailyCap)
            {
                yield return new WaitForSecondsRealtime(LoadRetryDelaySeconds);
                if (_adLoaded) break;
                Debug.Log("[Ads] Retrying rewarded ad load...");
                _rewardedAd.LoadAd();
            }
            _loadRetryCoroutine = null;
        }

        // ---------- Showing ----------

        /// <summary>
        /// Shows a rewarded ad for a placement. Returns false (without calling onComplete) when
        /// no ad can be shown right now — check CanShowRewarded first. When it returns true,
        /// onComplete is called exactly once: true if the reward was granted, false otherwise
        /// (display failure, ad closed before the end).
        /// </summary>
        public bool ShowRewarded(string placement, Action<bool> onComplete)
        {
            if (!CanShowRewarded)
            {
                Debug.LogWarning($"[Ads] Cannot show ad for {placement} — not ready, daily cap reached or ad in progress");
                return false;
            }

            _request = new RewardedRequest { Placement = placement, Callback = onComplete };
            _adLoaded = false;
            RaiseAvailabilityChanged();
            _requestWatchdog = StartCoroutine(RequestTimeout(_request));

            if (_config.stubAdsForBeta)
            {
                Debug.Log($"[Ads] STUB — simulating ad for {placement}...");
                StartCoroutine(StubAdCoroutine());
                return true;
            }

            Debug.Log($"[Ads] Showing rewarded ad for {placement}...");
            _rewardedAd.ShowAd(placement);
            return true;
        }

        private void OnRewardReceived()
        {
            if (_request == null)
            {
                Debug.LogWarning("[Ads] Reward received with no pending request — ignored");
                return;
            }
            if (_request.Rewarded) return;
            _request.Rewarded = true;

            _adsWatchedToday++;
            SaveDailyCount();
            GameAnalytics.AdRewardGranted(_request.Placement);
            CompleteRequest(true);
        }

        private void OnAdClosedByUser()
        {
            if (_request == null || _request.Rewarded) return;
            // Closed before the reward callback: give a late reward a chance to arrive.
            var pending = _request;
            StopWatchdog();
            _requestWatchdog = StartCoroutine(FailAfterGrace(pending));
        }

        private IEnumerator FailAfterGrace(RewardedRequest pending)
        {
            yield return new WaitForSecondsRealtime(LateRewardGraceSeconds);
            _requestWatchdog = null;
            if (_request == pending && !pending.Rewarded)
            {
                Debug.Log($"[Ads] Ad for {pending.Placement} closed without reward");
                CompleteRequest(false);
            }
        }

        private IEnumerator RequestTimeout(RewardedRequest pending)
        {
            yield return new WaitForSecondsRealtime(RequestTimeoutSeconds);
            _requestWatchdog = null;
            if (_request == pending)
            {
                Debug.LogWarning($"[Ads] No callback for {pending.Placement} after {RequestTimeoutSeconds}s — giving up");
                CompleteRequest(false);
                LoadRewardedAd();
            }
        }

        /// <summary>Ends the in-flight request and runs its callback exactly once.</summary>
        private void CompleteRequest(bool success)
        {
            var request = _request;
            if (request == null) return;
            _request = null;
            StopWatchdog();

            try
            {
                request.Callback?.Invoke(success);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            RaiseAvailabilityChanged();
        }

        private void StopWatchdog()
        {
            if (_requestWatchdog != null)
            {
                StopCoroutine(_requestWatchdog);
                _requestWatchdog = null;
            }
        }

        private IEnumerator StubAdCoroutine()
        {
            yield return new WaitForSecondsRealtime(_config.stubAdDuration);
            Debug.Log($"[Ads] STUB reward granted: {_request?.Placement}");
            OnRewardReceived();

            // Simulate next ad available
            _adLoaded = true;
            RaiseAvailabilityChanged();
        }

        private void RaiseAvailabilityChanged()
        {
            OnAdAvailabilityChanged?.Invoke();
        }

        // ---------- Daily cap ----------

        private static string Today => DateTime.UtcNow.ToString("yyyy-MM-dd");

        private void LoadDailyCount()
        {
            _lastResetDate = PlayerPrefs.GetString(PrefDateKey, "");
            if (_lastResetDate != Today)
            {
                _adsWatchedToday = 0;
                _lastResetDate = Today;
                SaveDailyCount();
            }
            else
            {
                _adsWatchedToday = PlayerPrefs.GetInt(PrefKey, 0);
            }
        }

        private void SaveDailyCount()
        {
            PlayerPrefs.SetInt(PrefKey, _adsWatchedToday);
            PlayerPrefs.SetString(PrefDateKey, _lastResetDate);
            PlayerPrefs.Save();
            RaiseAvailabilityChanged();
        }

        /// <summary>Resets the daily count when the UTC day changes while the game is running.</summary>
        private void CheckDayRollover()
        {
            if (_lastResetDate == Today) return;
            bool wasCapped = HasReachedDailyCap;
            _adsWatchedToday = 0;
            _lastResetDate = Today;
            SaveDailyCount();
            if (wasCapped && !_adLoaded) LoadRewardedAd();
        }

        private void Update()
        {
            _dayCheckTimer += Time.unscaledDeltaTime;
            if (_dayCheckTimer >= 30f)
            {
                _dayCheckTimer = 0f;
                CheckDayRollover();
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null) return;

            if (kb.rKey.wasPressedThisFrame)
            {
                _adsWatchedToday = 0;
                SaveDailyCount();
                Debug.Log("[Ads] Daily ad counter reset to 0");
                if (!_adLoaded) LoadRewardedAd();
            }

            if (kb.tKey.wasPressedThisFrame && SdkReady)
            {
                LaunchTestSuite();
            }
#endif
        }

        private void OnApplicationPause(bool paused)
        {
            if (!paused) CheckDayRollover();
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            _rewardedAd?.DestroyAd();
            if (_handlersRegistered)
            {
                LevelPlay.OnInitSuccess -= OnSdkInitSuccess;
                LevelPlay.OnInitFailed -= OnSdkInitFailed;
            }
        }
    }
}
