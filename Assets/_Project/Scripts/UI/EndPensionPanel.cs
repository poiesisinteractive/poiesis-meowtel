using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using CatHotel.Audio;
using CatHotel.Services;

namespace CatHotel.UI
{
    public struct PensionEndData
    {
        public Sprite CatSprite;
        public string CatName;
        public float AvgHappiness;
        public int BaseCoins;
        public int TipCoins;
        public int TotalCoins;
    }

    /// <summary>
    /// End-of-pension recap panel. Does NOT pause the game.
    /// - Shows cat stats, animates numbers, auto-collects coins instantly
    /// - ByeAction closes immediately at any time
    /// - Auto-closes after 15s of inactivity
    /// - If ad was triggered, closes on return
    /// - Queues multiple pension ends
    /// </summary>
    public class EndPensionPanel : MonoBehaviour
    {
        private GameObject _panelObj;
        private RectTransform _panel;
        private float _panelWidth;
        private float _panelRestX; // target X after slide-in (anchored position)
        private Tween _slideTween;
        private bool _isOpen;
        private bool _initialized;
        private bool _collected;
        private bool _adInProgress;
        private bool _panelReady;

        // UI elements
        private Image _catImage;
        private TMP_Text _catName;
        private TMP_Text _happinessRecap;
        private TMP_Text _baseValue;
        private TMP_Text _tipValue;
        private TMP_Text _totalValue;
        private RectTransform _byeRect;
        private TMP_Text _byeLabel;
        private RectTransform _doubleRect;

        // Coin fly
        private RectTransform _coinTarget;
        private Sprite _coinSprite;
        private Canvas _overlayCanvas;

        // Current session
        private Action<int> _onCollect;
        private PensionEndData _data;
        private int _bonusCoins;
        private Coroutine _animCoroutine;
        private Coroutine _autoCloseCoroutine;

        // Queue for multiple pension ends
        private readonly Queue<(PensionEndData data, Action<int> onCollect)> _pendingQueue = new();

        private const float AutoCloseDuration = 15f;

        // SFX
        [SerializeField] private AudioClip _collectSfx;
        private AudioSource _sfxSource;

        public bool IsOpen => _isOpen;

        private void Start()
        {
            CacheReferences();
            _sfxSource = GetComponent<AudioSource>();
            if (_sfxSource == null)
            {
                _sfxSource = gameObject.AddComponent<AudioSource>();
                _sfxSource.playOnAwake = false;
                _sfxSource.spatialBlend = 0f;
            }
        }

        private void CacheReferences()
        {
            if (_initialized) return;

            _panelObj = FindInactiveByName("EndPensionPanel");
            if (_panelObj == null) return;

            _panel = _panelObj.GetComponent<RectTransform>();

            var panelImg = _panelObj.GetComponent<Image>();
            if (panelImg == null)
            {
                panelImg = _panelObj.AddComponent<Image>();
                panelImg.color = Color.clear;
            }
            panelImg.raycastTarget = true;

            _catImage = FindImage(_panelObj, "CatImage");
            _catName = FindTMP(_panelObj, "CatName");
            _happinessRecap = FindTMP(_panelObj, "HapinessRecapValue");
            _baseValue = FindTMP(_panelObj, "BaseValue");
            _tipValue = FindTMP(_panelObj, "TipValue");
            _totalValue = FindTMP(_panelObj, "TotalValue");
            _byeRect = FindRect(_panelObj, "ByeAction");
            _byeLabel = FindTMP(_panelObj, "ByeLabel");
            _doubleRect = FindRect(_panelObj, "X2GainCollectRewardedAdAction");

            AddJuice(_byeRect);
            AddJuice(_doubleRect);

            var coinTargetObj = GameObject.Find("CatCoinsImage");
            if (coinTargetObj != null)
            {
                _coinTarget = coinTargetObj.GetComponent<RectTransform>();
                var coinImg = coinTargetObj.GetComponent<Image>();
                if (coinImg != null) _coinSprite = coinImg.sprite;
            }

            foreach (var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (c.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    _overlayCanvas = c;
                    break;
                }
            }

            // Store the rest position (where panel should sit after slide-in)
            _panelRestX = _panel.anchoredPosition.x;

            _panelObj.SetActive(false);
            _initialized = true;
        }

        private void Update()
        {
            if (!_isOpen) return;

            var pointer = Pointer.current;
            if (pointer == null || !pointer.press.wasPressedThisFrame) return;
            Vector2 screenPos = pointer.position.ReadValue();

            // ByeAction — always available, closes immediately
            if (_byeRect != null &&
                RectTransformUtility.RectangleContainsScreenPoint(_byeRect, screenPos, null))
            {
                UISoundManager.Instance?.PlayTapPositive();
                Close();
                return;
            }

            // Double gains via rewarded ad — available as soon as the panel is
            // in place, no need to wait for the number animation / auto-collect.
            if (_panelReady && !_adInProgress && _doubleRect != null && _doubleRect.gameObject.activeSelf &&
                RectTransformUtility.RectangleContainsScreenPoint(_doubleRect, screenPos, null))
            {
                TryDoubleGains();
            }
        }

        public void Show(PensionEndData data, Action<int> onCollect)
        {
            CacheReferences();
            if (_panel == null) return;

            if (_isOpen)
            {
                _pendingQueue.Enqueue((data, onCollect));
                return;
            }

            ShowInternal(data, onCollect);
        }

        private void ShowInternal(PensionEndData data, Action<int> onCollect)
        {
            _data = data;
            _onCollect = onCollect;
            _collected = false;
            _bonusCoins = 0;
            _adInProgress = false;
            _panelReady = false;

            UISoundManager.Instance?.PlayOpenSection();
            _panelObj.SetActive(true);
            Canvas.ForceUpdateCanvases();
            _panelWidth = _panel.rect.width;
            if (_panelWidth <= 0f) _panelWidth = 800f;

            // Start offscreen to the right
            var pos = _panel.anchoredPosition;
            pos.x = _panelRestX + _panelWidth;
            _panel.anchoredPosition = pos;

            if (_catImage != null && data.CatSprite != null)
                _catImage.sprite = data.CatSprite;
            if (_catName != null)
                _catName.text = data.CatName;

            if (_happinessRecap != null) _happinessRecap.text = "0%";
            if (_baseValue != null) _baseValue.text = "0";
            if (_tipValue != null) _tipValue.text = "0";
            if (_totalValue != null) _totalValue.text = "0";

            if (_byeLabel != null)
                _byeLabel.text = Core.LocalizedStrings.Get("pension.bye", data.CatName);

            if (_doubleRect != null)
            {
                _doubleRect.gameObject.SetActive(true);
                if (_doubleRect.gameObject.activeSelf)
                    GameAnalytics.AdOfferShown(AdManager.PlacementPensionX2);
            }

            _isOpen = true;

            // Slide in to rest position
            _slideTween?.Kill();
            _slideTween = _panel.DOAnchorPosX(_panelRestX, 0.45f)
                .SetEase(Ease.OutCubic)
                .OnComplete(() =>
                {
                    // Snap to exact rest position
                    var p = _panel.anchoredPosition;
                    p.x = _panelRestX;
                    _panel.anchoredPosition = p;

                    // Panel is now interactive: X2 can be chosen immediately,
                    // even while the numbers are still animating.
                    _panelReady = true;

                    if (_animCoroutine != null) StopCoroutine(_animCoroutine);
                    _animCoroutine = StartCoroutine(AnimateNumbersThenCollect());
                });

            // Start auto-close timer from panel open (covers entire display)
            StartAutoCloseTimer();
        }

        private void Close()
        {
            if (!_isOpen) return;
            _isOpen = false;
            UISoundManager.Instance?.PlayCloseSection();
            _slideTween?.Kill();
            if (_animCoroutine != null)
            {
                StopCoroutine(_animCoroutine);
                _animCoroutine = null;
            }
            StopAutoCloseTimer();

            // Ensure coins are collected even on early close
            if (!_collected)
            {
                _collected = true;
                _onCollect?.Invoke(_data.TotalCoins);
                _onCollect = null;
            }

            // Slide out to the right
            _slideTween = _panel.DOAnchorPosX(_panelRestX + _panelWidth, 0.5f)
                .SetEase(Ease.InCubic)
                .OnComplete(() =>
                {
                    _panelObj.SetActive(false);
                    _onCollect = null;
                    ShowNextInQueue();
                });
        }

        private void StartAutoCloseTimer()
        {
            StopAutoCloseTimer();
            _autoCloseCoroutine = StartCoroutine(AutoCloseAfterDelay());
        }

        private void StopAutoCloseTimer()
        {
            if (_autoCloseCoroutine != null)
            {
                StopCoroutine(_autoCloseCoroutine);
                _autoCloseCoroutine = null;
            }
        }

        private void ShowNextInQueue()
        {
            if (_pendingQueue.Count > 0)
            {
                var (data, cb) = _pendingQueue.Dequeue();
                ShowInternal(data, cb);
            }
        }

        private IEnumerator AnimateNumbersThenCollect()
        {
            yield return StartCoroutine(AnimateValue(
                _happinessRecap, 0f, _data.AvgHappiness, 0.35f, v => $"{v:0}%"));

            yield return new WaitForSeconds(0.08f);

            yield return StartCoroutine(AnimateValue(
                _baseValue, 0f, _data.BaseCoins, 0.3f, v => $"{v:0}"));

            if (_data.TipCoins > 0)
            {
                yield return new WaitForSeconds(0.06f);
                yield return StartCoroutine(AnimateValue(
                    _tipValue, 0f, _data.TipCoins, 0.25f, v => $"+{v:0}"));
            }
            else
            {
                if (_tipValue != null) _tipValue.text = "-";
            }

            yield return new WaitForSeconds(0.08f);

            yield return StartCoroutine(AnimateValue(
                _totalValue, 0f, _data.TotalCoins, 0.35f, v => $"{v:0}"));

            if (_totalValue != null)
            {
                var rt = _totalValue.GetComponent<RectTransform>();
                if (rt != null)
                    rt.DOPunchScale(Vector3.one * 0.3f, 0.3f, 8, 0.5f);
            }

            _animCoroutine = null;

            yield return new WaitForSeconds(0.2f);
            AutoCollect();
        }

        private void AutoCollect()
        {
            if (_collected) return;
            _collected = true;

            _onCollect?.Invoke(_data.TotalCoins);
            _onCollect = null;

            if (_sfxSource != null && _collectSfx != null)
                _sfxSource.PlayOneShot(_collectSfx, ParametersPanel.EffectsVolume);

            int coinCount = Mathf.Clamp(_data.TotalCoins / 10, 3, 8);
            StartCoroutine(CoinFlyFromTotal(coinCount));
        }

        private IEnumerator AutoCloseAfterDelay()
        {
            yield return new WaitForSeconds(AutoCloseDuration);
            _autoCloseCoroutine = null;
            if (_isOpen && !_adInProgress)
                Close();
        }

        private IEnumerator AnimateValue(TMP_Text text, float from, float to, float duration,
            Func<float, string> format)
        {
            if (text == null) yield break;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                float current = Mathf.Lerp(from, to, eased);
                text.text = format(current);
                yield return null;
            }
            text.text = format(to);
        }

        private void TryDoubleGains()
        {
            if (!_isOpen || !_panelReady || _adInProgress) return;

            var ads = AdManager.Instance;
            if (ads == null || !ads.IsAdReady)
            {
                Debug.LogWarning("[Pension] Ad not ready");
                return;
            }

            // Stop the number animation and snap to final values so the ad
            // launches instantly with no visual lag.
            if (_animCoroutine != null)
            {
                StopCoroutine(_animCoroutine);
                _animCoroutine = null;
            }
            SnapNumbersToFinal();

            _adInProgress = true;
            StopAutoCloseTimer(); // don't close while ad is playing

            ads.OnPensionAdCompleted += OnPensionAdSuccess;
            ads.OnPensionAdFailed += OnPensionAdFail;
            ads.ShowPensionAd();
        }

        private void SnapNumbersToFinal()
        {
            if (_happinessRecap != null) _happinessRecap.text = $"{_data.AvgHappiness:0}%";
            if (_baseValue != null) _baseValue.text = $"{_data.BaseCoins:0}";
            if (_tipValue != null)
                _tipValue.text = _data.TipCoins > 0 ? $"+{_data.TipCoins:0}" : "-";
            if (_totalValue != null) _totalValue.text = $"{_data.TotalCoins:0}";
        }

        private void OnPensionAdSuccess()
        {
            UnsubPensionAd();
            _adInProgress = false;

            _bonusCoins = _data.TotalCoins;
            int newTotal = _data.TotalCoins + _bonusCoins;

            if (_totalValue != null)
            {
                _totalValue.text = $"{newTotal}";
                var rt = _totalValue.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.DOKill();
                    rt.localScale = Vector3.one;
                    rt.DOPunchScale(Vector3.one * 0.4f, 0.35f, 8, 0.5f);
                }
            }

            if (_doubleRect != null)
                _doubleRect.gameObject.SetActive(false);

            var economy = GetComponent<CatHotel.Economy.EconomyManager>();
            if (economy != null)
                economy.AddCoins(_bonusCoins);

            if (_sfxSource != null && _collectSfx != null)
                _sfxSource.PlayOneShot(_collectSfx, ParametersPanel.EffectsVolume);

            int flyCoinCount = Mathf.Clamp(_bonusCoins / 10, 2, 6);
            StartCoroutine(CoinFlyFromTotal(flyCoinCount));

            Debug.Log($"[Pension] x2 bonus: +{_bonusCoins} coins (total shown: {newTotal})");

            // Close immediately after ad return
            Close();
        }

        private void OnPensionAdFail()
        {
            UnsubPensionAd();
            _adInProgress = false;
            Debug.LogWarning("[Pension] Ad failed");

            // Close on ad failure too
            Close();
        }

        private void UnsubPensionAd()
        {
            var ads = AdManager.Instance;
            if (ads == null) return;
            ads.OnPensionAdCompleted -= OnPensionAdSuccess;
            ads.OnPensionAdFailed -= OnPensionAdFail;
        }

        private IEnumerator CoinFlyFromTotal(int count)
        {
            RectTransform srcRect = _totalValue != null
                ? _totalValue.GetComponent<RectTransform>()
                : _panel;

            if (_overlayCanvas == null || _coinTarget == null || srcRect == null)
                yield break;

            Vector3[] srcCorners = new Vector3[4];
            srcRect.GetWorldCorners(srcCorners);
            Vector3 srcCenter = (srcCorners[0] + srcCorners[2]) / 2f;

            Vector3[] dstCorners = new Vector3[4];
            _coinTarget.GetWorldCorners(dstCorners);
            Vector3 dstCenter = (dstCorners[0] + dstCorners[2]) / 2f;

            var canvasRt = _overlayCanvas.GetComponent<RectTransform>();

            for (int i = 0; i < count; i++)
            {
                var coinGo = new GameObject($"FlyingCoin_{i}");
                coinGo.transform.SetParent(_overlayCanvas.transform, false);

                var coinImg = coinGo.AddComponent<Image>();
                if (_coinSprite != null)
                    coinImg.sprite = _coinSprite;

                var coinRt = coinGo.GetComponent<RectTransform>();
                coinRt.sizeDelta = new Vector2(32f, 32f);

                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRt,
                    RectTransformUtility.WorldToScreenPoint(null, srcCenter),
                    null, out Vector2 srcLocal);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRt,
                    RectTransformUtility.WorldToScreenPoint(null, dstCenter),
                    null, out Vector2 dstLocal);

                srcLocal += new Vector2(
                    UnityEngine.Random.Range(-30f, 30f),
                    UnityEngine.Random.Range(-20f, 20f));
                coinRt.anchoredPosition = srcLocal;

                float delay = i * 0.08f;
                float flyDuration = 0.4f;

                var seq = DOTween.Sequence();
                seq.AppendInterval(delay);
                seq.Append(coinRt.DOScale(Vector3.one * 1.3f, 0.08f).SetEase(Ease.OutBack));
                seq.Append(coinRt.DOAnchorPos(dstLocal, flyDuration).SetEase(Ease.InQuad));
                seq.Join(coinRt.DOScale(Vector3.one * 0.4f, flyDuration).SetEase(Ease.InQuad));
                seq.OnComplete(() =>
                {
                    Destroy(coinGo);
                    if (_coinTarget != null)
                    {
                        _coinTarget.DOKill();
                        _coinTarget.localScale = Vector3.one;
                        _coinTarget.DOPunchScale(Vector3.one * 0.2f, 0.15f, 6, 0.5f);
                    }
                });
            }

            yield return null;
        }

        private static void AddJuice(RectTransform rt)
        {
            if (rt == null) return;
            if (rt.GetComponent<ButtonJuice>() == null)
                rt.gameObject.AddComponent<ButtonJuice>();
        }

        private static RectTransform FindRect(GameObject root, string childName)
        {
            var t = FindInChildren(root.transform, childName);
            return t != null ? t.GetComponent<RectTransform>() : null;
        }

        private static TMP_Text FindTMP(GameObject root, string childName)
        {
            var t = FindInChildren(root.transform, childName);
            return t != null ? t.GetComponent<TMP_Text>() : null;
        }

        private static Image FindImage(GameObject root, string childName)
        {
            var t = FindInChildren(root.transform, childName);
            return t != null ? t.GetComponent<Image>() : null;
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
    }
}
