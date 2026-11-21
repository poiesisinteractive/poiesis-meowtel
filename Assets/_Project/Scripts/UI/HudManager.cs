using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using CatHotel.Core;
using CatHotel.Hotel;
using CatHotel.Economy;
using CatHotel.Services;

namespace CatHotel.UI
{
    /// <summary>
    /// Finds named UI objects in the scene and keeps them updated.
    /// Coins counter animates 1-by-1 toward the real value.
    /// </summary>
    public class HudManager : MonoBehaviour
    {
        [SerializeField] private HotelManager _hotel;
        [SerializeField] private EconomyManager _economy;
        [SerializeField] private ReputationManager _reputation;

        private TMP_Text _coinsText;
        private TMP_Text _purrlsText;
        private TMP_Text _capacityText;
        private TMP_Text _comfortText;
        private TMP_Text _floorText;
        private TMP_Text _timerText;
        private RectTransform _timerImage;

        // Level / XP UI
        private TMP_Text _nextLevelObjective;
        private TMP_Text _currentLvlValue;
        private TMP_Text _currentLvlDesc;
        private TMP_Text _nextLvlValue;
        private TMP_Text _nextLvlDesc;
        private RectTransform _pexImage;

        // PexImageValue right offset: 900 = empty, 97 = full
        private const float PexRightEmpty = 900f;
        private const float PexRightFull = 97f;

        // Animated coin counter
        private int _displayedCoins;
        private int _targetCoins;
        private float _coinTickTimer;
        private bool _coinsInitialized;
        private const float CoinTickInterval = 0.04f; // 25 ticks/sec

        // Timer image: sliced, right padding goes from 160 (full) to 5 (empty)
        private const float TimerImageRightFull = 160f;
        private const float TimerImageRightEmpty = 5f;

        // Ad boost UI
        private Button _addBoostButton;
        private TMP_Text _cooldownTimeValue;
        private TMP_Text _remainingAds;
        private TMP_Text _doubleGainsLabel;
        private GameObject _doubleGainLockObj;
        private bool _boostOfferAvailable; // analytics: last known boost availability
        private RectTransform _starRt;
        private GameObject _x2BoostActiveObj;
        private TMP_Text _x2BoostActiveText;

        // Shop — managed by ShopPanel component

        // Dirty-checking: cache previous values to avoid redundant UI updates
        private int _prevCapacityPct = -1;
        private int _prevComfort = -1;
        private int _prevTimerSec = -1;
        private int _prevLevel = -1;
        private int _prevXp = -1;
        private int _prevHappyCats = -1;

        private void Start()
        {
            LocalizedStrings.OnLanguageChanged += OnLanguageChanged;

            // Disable BuildAction for now
            var buildObj = GameObject.Find("BuildAction");
            if (buildObj != null) buildObj.SetActive(false);

            _coinsText = FindText("CoinsCounter");
            _purrlsText = FindText("PurrlsCounter");
            _capacityText = FindText("CapacityPct");
            _comfortText = FindText("ComfortLevel");
            _floorText = FindText("CurrentFloorIndex");
            _timerText = FindText("NexCatTimerSec");

            // Level / XP UI
            _nextLevelObjective = FindText("NextLevelObjectiveCurrentValue");
            _currentLvlValue = FindText("CurrentLvlValue");
            _currentLvlDesc = FindText("CurrentLvlDesc");
            _nextLvlValue = FindText("NextLvlValue");
            _nextLvlDesc = FindText("NextLvlDesc");

            var pexObj = GameObject.Find("PexImageValue");
            if (pexObj != null)
                _pexImage = pexObj.GetComponent<RectTransform>();

            var timerImgObj = GameObject.Find("NextCatTimerImage");
            if (timerImgObj != null)
                _timerImage = timerImgObj.GetComponent<RectTransform>();

            // Ad boost (auto-wire like CollectAllAction)
            var addBoostObj = GameObject.Find("AddBoost");
            if (addBoostObj != null)
            {
                _addBoostButton = addBoostObj.GetComponent<Button>();
                if (_addBoostButton == null)
                    _addBoostButton = addBoostObj.AddComponent<Button>();

                // Ensure targetGraphic is set for touch input on Android
                if (_addBoostButton.targetGraphic == null)
                {
                    var graphic = addBoostObj.GetComponent<Graphic>();
                    if (graphic != null)
                        _addBoostButton.targetGraphic = graphic;
                }

                _addBoostButton.onClick.AddListener(OnAdBoostClicked);

                if (addBoostObj.GetComponent<ButtonJuice>() == null)
                    addBoostObj.AddComponent<ButtonJuice>();

                var starObj = addBoostObj.transform.Find("Star");
                if (starObj != null)
                    _starRt = starObj.GetComponent<RectTransform>();
            }
            // Inactive-aware: these may live under objects that start hidden,
            // so GameObject.Find would miss them.
            var cooldownObj = FindInactiveByName("CooldownTimeValue");
            if (cooldownObj != null) _cooldownTimeValue = cooldownObj.GetComponent<TMP_Text>();

            _remainingAds = FindText("RemainingAds");

            var doubleGainsObj = FindInactiveByName("DoubleGainsLabel");
            if (doubleGainsObj != null)
            {
                _doubleGainsLabel = doubleGainsObj.GetComponent<TMP_Text>();
                if (_doubleGainsLabel != null)
                    _doubleGainsLabel.text = LocalizedStrings.Get("hud.boost.button");
            }

            _doubleGainLockObj = FindInactiveByName("DoubleGainLockObject");

            // Find X2BoostActive even if inactive (GameObject.Find won't find inactive objects)
            _x2BoostActiveObj = FindInactiveByName("X2BoostActive");
            if (_x2BoostActiveObj != null)
            {
                _x2BoostActiveText = _x2BoostActiveObj.GetComponent<TMP_Text>();
                _x2BoostActiveObj.SetActive(false);
            }

            // ShopAction + ShopPanel are now managed by the ShopPanel component

            if (_economy != null)
            {
                _economy.OnCoinsChanged += OnCoinsChanged;
                _economy.OnGemsChanged += g => UpdatePurrls(g);
            }

            // Initial values (no animation)
            if (_economy != null)
            {
                _displayedCoins = _economy.Coins;
                _targetCoins = _economy.Coins;
                RefreshCoinsDisplay();
                UpdatePurrls(_economy.Gems);
            }
        }

        private void OnDestroy()
        {
            LocalizedStrings.OnLanguageChanged -= OnLanguageChanged;
        }

        private void OnLanguageChanged()
        {
            // Reset dirty flags to force all labels to refresh
            _prevLevel = -1;
            _prevXp = -1;
            _prevHappyCats = -1;
            _prevTimerSec = -1;
            if (_floorText != null) _floorText.text = "";
        }

        private void Update()
        {
            if (_hotel == null) return;

            AnimateCoinCounter();
            UpdateCapacity();
            UpdateComfort();
            UpdateFloor();
            UpdateTimer();
            UpdateLevel();
            UpdateAdBoostUI();
        }

        private void OnCoinsChanged(int newTotal)
        {
            if (!_coinsInitialized)
            {
                // First event (after Init): snap display instantly, no animation
                _coinsInitialized = true;
                _displayedCoins = newTotal;
                RefreshCoinsDisplay();
            }
            _targetCoins = newTotal;
        }

        private void AnimateCoinCounter()
        {
            if (_displayedCoins == _targetCoins) return;

            _coinTickTimer += Time.deltaTime;
            if (_coinTickTimer < CoinTickInterval) return;
            _coinTickTimer = 0f;

            int diff = _targetCoins - _displayedCoins;
            // Step scales with distance: always finishes in ~0.5s max
            int step = Mathf.Max(1, Mathf.Abs(diff) / 12);
            _displayedCoins += diff > 0 ? step : -step;

            // Clamp to avoid overshooting
            if ((diff > 0 && _displayedCoins > _targetCoins) ||
                (diff < 0 && _displayedCoins < _targetCoins))
                _displayedCoins = _targetCoins;

            RefreshCoinsDisplay();
        }

        private void RefreshCoinsDisplay()
        {
            if (_coinsText != null)
                _coinsText.text = FormatNumber(_displayedCoins);
        }

        private void UpdatePurrls(int gems)
        {
            if (_purrlsText != null)
                _purrlsText.text = FormatNumber(gems);
        }

        private void UpdateCapacity()
        {
            if (_capacityText == null) return;

            int current = _hotel.CatCount;
            int max = CatHotel.Hotel.FloorProgression.Instance != null
                ? CatHotel.Hotel.FloorProgression.Instance.MaxCats
                : (_hotel.Config != null ? _hotel.Config.maxCats : 20);
            int pct = max > 0 ? Mathf.RoundToInt((float)current / max * 100f) : 0;
            if (pct == _prevCapacityPct) return;
            _prevCapacityPct = pct;
            _capacityText.text = $"{pct}%";
        }

        private void UpdateComfort()
        {
            if (_comfortText == null) return;

            int max = CatHotel.Hotel.FloorProgression.Instance != null
                ? CatHotel.Hotel.FloorProgression.Instance.MaxCats
                : (_hotel.Config != null ? _hotel.Config.maxCats : 20);
            int comfortFloor = _hotel != null && _hotel.GridRenderer != null ? _hotel.GridRenderer.CurrentFloor : 0;
            float comfort = ObjectRegistry.CalculateComfort(_hotel.CatCount, max, comfortFloor);
            int rounded = Mathf.RoundToInt(comfort);
            if (rounded == _prevComfort) return;
            _prevComfort = rounded;
            _comfortText.text = $"{rounded}";
        }

        private void UpdateFloor()
        {
            if (_floorText == null) return;
            int f = _hotel != null && _hotel.GridRenderer != null ? _hotel.GridRenderer.CurrentFloor : 0;
            string txt = f == 0
                ? Core.LocalizedStrings.Get("hud.floor.ground")
                : string.Format(Core.LocalizedStrings.Get("hud.floor"), f);
            if (_floorText.text != txt) _floorText.text = txt;
        }

        private void UpdateTimer()
        {
            if (_hotel.Config == null) return;

            float interval = _hotel.Config.arrivalInterval;
            float remaining = interval - _hotel.ArrivalTimer;
            int sec = Mathf.CeilToInt(remaining);

            // Only update text when the displayed second changes
            if (sec != _prevTimerSec)
            {
                _prevTimerSec = sec;

                if (_timerText != null)
                {
                    if (remaining >= 60f)
                    {
                        int min = sec / 60;
                        int s = sec % 60;
                        _timerText.text = $"{min}min {s:00}s";
                    }
                    else
                    {
                        _timerText.text = $"{sec}s";
                    }
                }
            }

            if (_timerImage != null)
            {
                float t = Mathf.Clamp01(remaining / interval);
                float right = Mathf.Lerp(TimerImageRightEmpty, TimerImageRightFull, t);
                var offset = _timerImage.offsetMax;
                offset.x = -right;
                _timerImage.offsetMax = offset;
            }
        }

        private void UpdateLevel()
        {
            if (_reputation == null) return;

            int level = _reputation.Level;
            int xp = _reputation.Xp;
            int happyCats = _hotel != null ? _hotel.CountHappyCats() : 0;

            // Dirty check (also re-render when the happy-cats count changes)
            if (level == _prevLevel && xp == _prevXp && happyCats == _prevHappyCats) return;
            _prevLevel = level;
            _prevXp = xp;
            _prevHappyCats = happyCats;

            var current = _reputation.CurrentLevel;
            var next = _reputation.NextLevel;

            // Current level
            if (_currentLvlValue != null)
                _currentLvlValue.text = LocalizedStrings.Get("hud.level", level);
            if (_currentLvlDesc != null)
                _currentLvlDesc.text = LocalizedStrings.Get(current.NameKey);

            if (next.HasValue)
            {
                int qualifiedCats = happyCats;

                if (_nextLvlValue != null)
                    _nextLvlValue.text = LocalizedStrings.Get("hud.level", next.Value.Index);
                if (_nextLvlDesc != null)
                    _nextLvlDesc.text = LocalizedStrings.Get(next.Value.NameKey);
                if (_nextLevelObjective != null)
                    _nextLevelObjective.text = LocalizedStrings.Get("hud.level.objective",
                        qualifiedCats, next.Value.HappyCatsRequired, ReputationManager.HappyCatThreshold);

                // XP bar based on accumulated XP progress
                if (_pexImage != null)
                {
                    float progress = _reputation.XpProgress;
                    float right = Mathf.Lerp(PexRightEmpty, PexRightFull, progress);
                    var offset = _pexImage.offsetMax;
                    offset.x = -right;
                    _pexImage.offsetMax = offset;
                }
            }
            else
            {
                if (_nextLvlValue != null)
                    _nextLvlValue.text = LocalizedStrings.Get("hud.level.max");
                if (_nextLvlDesc != null)
                    _nextLvlDesc.text = "";
                if (_nextLevelObjective != null)
                    _nextLevelObjective.text = LocalizedStrings.Get("hud.level.max.reached");
                if (_pexImage != null)
                {
                    var offset = _pexImage.offsetMax;
                    offset.x = -PexRightFull;
                    _pexImage.offsetMax = offset;
                }
            }
        }

        private void OnAdBoostClicked()
        {
            var ads = AdManager.Instance;
            var boost = RevenueBoostManager.Instance;
            if (ads == null) { Debug.LogWarning("[HUD] AddBoost click ignored: AdManager.Instance is null"); return; }
            if (!ads.CanShowRewardedFor(AdManager.PlacementBoostX2)) { Debug.LogWarning("[HUD] AddBoost click ignored: no ad available"); return; }
            if (boost != null && boost.IsBoosted) { Debug.LogWarning("[HUD] AddBoost click ignored: boost already active"); return; }
            if (IsTutorialActive) { Debug.LogWarning("[HUD] AddBoost click ignored: tutorial in progress"); return; }

            bool shown = ads.ShowRewarded(AdManager.PlacementBoostX2, rewarded =>
            {
                if (rewarded) RevenueBoostManager.Instance?.ActivateBoost();
            });
            if (shown && _starRt != null)
            {
                _starRt.DOKill();
                _starRt.localScale = Vector3.one;
                _starRt.DOPunchScale(Vector3.one * 0.3f, 0.35f, 6, 0.5f);
                _starRt.DOLocalRotate(new Vector3(0, 0, 360f), 0.5f, RotateMode.FastBeyond360)
                    .SetEase(Ease.OutQuad);
            }
        }

        private static bool IsTutorialActive =>
            CatHotel.Tutorial.TutorialManager.Instance != null && CatHotel.Tutorial.TutorialManager.Instance.IsActive;

        private void UpdateAdBoostUI()
        {
            var boost = RevenueBoostManager.Instance;
            var ads = AdManager.Instance;

            // Cooldown: show remaining boost time or empty
            if (_cooldownTimeValue != null)
            {
                if (boost != null && boost.IsBoosted)
                {
                    int sec = Mathf.CeilToInt(boost.BoostTimeRemaining);
                    _cooldownTimeValue.text = $"{sec}s";
                }
                else
                {
                    _cooldownTimeValue.text = "";
                }
            }

            // Remaining ads today
            if (_remainingAds != null && ads != null)
            {
                int remaining = ads.DailyCap - ads.AdsWatchedToday;
                _remainingAds.text = $"{remaining}";
            }

            // X2BoostActive display
            if (_x2BoostActiveObj != null)
            {
                bool boosted = boost != null && boost.IsBoosted;
                _x2BoostActiveObj.SetActive(boosted);
                if (boosted && _x2BoostActiveText != null)
                {
                    int sec = Mathf.CeilToInt(boost.BoostTimeRemaining);
                    _x2BoostActiveText.text = LocalizedStrings.Get("hud.boost.active", sec);
                }
            }

            // Boost availability: can the player trigger a new boost right now?
            bool canWatch = ads != null && ads.CanShowRewardedFor(AdManager.PlacementBoostX2)
                && (boost == null || !boost.IsBoosted)
                && !IsTutorialActive; // no ads during the tutorial

            // Analytics: an 'offer' is each time the (permanent) boost button becomes usable
            if (canWatch && !_boostOfferAvailable)
                GameAnalytics.AdOfferShown(AdManager.PlacementBoostX2);
            _boostOfferAvailable = canWatch;

            // Enable/disable button (blocked during active boost / no ad / cap)
            if (_addBoostButton != null)
                _addBoostButton.interactable = canWatch;

            // Lock overlay: visible when boost NOT available, hidden when available
            if (_doubleGainLockObj != null && _doubleGainLockObj.activeSelf != !canWatch)
                _doubleGainLockObj.SetActive(!canWatch);
        }

        private static TMP_Text FindText(string name)
        {
            var go = GameObject.Find(name);
            if (go == null)
            {
                Debug.LogWarning($"[HUD] UI object '{name}' not found in scene.");
                return null;
            }
            var tmp = go.GetComponent<TMP_Text>();
            if (tmp == null)
                Debug.LogWarning($"[HUD] '{name}' has no TMP_Text component.");
            return tmp;
        }

        private static string FormatNumber(int value)
        {
            if (value >= 1_000_000) return $"{value / 1_000_000f:F1}M";
            if (value >= 1_000) return $"{value / 1_000f:F1}K";
            return value.ToString();
        }

        private static GameObject FindInactiveByName(string name)
        {
            var go = GameObject.Find(name);
            if (go != null) return go;

            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;
                foreach (var root in scene.GetRootGameObjects())
                {
                    var found = FindInChildren(root.transform, name);
                    if (found != null) return found.gameObject;
                }
            }
            return null;
        }

        private static Transform FindInChildren(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name) return child;
                var found = FindInChildren(child, name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
