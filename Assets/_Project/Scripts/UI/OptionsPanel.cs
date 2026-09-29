using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using CatHotel.Audio;
using CatHotel.Core;
using CatHotel.Hotel;
using CatHotel.Services;

namespace CatHotel.UI
{
    /// <summary>
    /// Manages the "OptionsPanel" overlay.
    /// Opens when tapping "System" button, slides in from the right.
    /// </summary>
    public class OptionsPanel : MonoBehaviour
    {
        private RectTransform _panel;
        private GameObject _panelObj;
        private float _panelWidth;
        private Tween _slideTween;
        private bool _isOpen;

        // Button hit rects
        private RectTransform _backToGameRect;
        private RectTransform _paramsRect;
        private RectTransform _mainMenuRect;
        private RectTransform _privacyRect;   // created at runtime (no scene edit)
        private TMP_Text _privacyLabel;

        private ParametersPanel _parametersPanel;
        private ConsentPanel _consentPanel;   // instantiated on demand from Resources

        public bool IsOpen => _isOpen;

        private void Start()
        {
            // Find the OptionsPanel (may be inactive)
            _panelObj = FindInactiveByName("OptionsPanel");
            if (_panelObj == null) return;

            _panel = _panelObj.GetComponent<RectTransform>();

            // Activate briefly to measure, then deactivate
            _panelObj.SetActive(true);

            var panelImg = _panelObj.GetComponent<Image>();
            if (panelImg == null)
            {
                panelImg = _panelObj.AddComponent<Image>();
                panelImg.color = Color.clear;
            }
            panelImg.raycastTarget = true;

            Canvas.ForceUpdateCanvases();
            _panelWidth = _panel.rect.width;
            if (_panelWidth <= 0f) _panelWidth = 800f;

            // Position off-screen and deactivate
            var pos = _panel.anchoredPosition;
            pos.x = _panelWidth;
            _panel.anchoredPosition = pos;
            _panelObj.SetActive(false);

            // Find button rects + add tap juice
            _backToGameRect = FindRect(_panelObj, "BackToGameOption");
            _paramsRect = FindRect(_panelObj, "ParamsOption");
            _mainMenuRect = FindRect(_panelObj, "MainMenuOption");
            AddJuice(_backToGameRect);
            AddJuice(_paramsRect);
            AddJuice(_mainMenuRect);
            _privacyRect = CreatePrivacyOption();

            // Get or add ParametersPanel on same object
            _parametersPanel = GetComponent<ParametersPanel>();
            if (_parametersPanel == null)
                _parametersPanel = gameObject.AddComponent<ParametersPanel>();

            // Wire "System" button to open this panel
            var systemObj = GameObject.Find("System");
            if (systemObj != null)
            {
                var btn = systemObj.GetComponent<Button>();
                if (btn == null) btn = systemObj.AddComponent<Button>();
                btn.onClick.AddListener(Open);

                // Ensure graphic for raycast
                var graphic = systemObj.GetComponent<Graphic>();
                if (graphic != null) graphic.raycastTarget = true;

                AddJuice(systemObj.GetComponent<RectTransform>());
            }
        }

        private void Update()
        {
            if (!_isOpen) return;

            var pointer = Pointer.current;
            if (pointer == null || !pointer.press.wasPressedThisFrame) return;

            Vector2 screenPos = pointer.position.ReadValue();

            // BackToGame => close
            if (_backToGameRect != null &&
                RectTransformUtility.RectangleContainsScreenPoint(_backToGameRect, screenPos, null))
            {
                Close();
                return;
            }

            // Params => open ParametersPanel
            if (_paramsRect != null &&
                RectTransformUtility.RectangleContainsScreenPoint(_paramsRect, screenPos, null))
            {
                OpenParameters();
                return;
            }

            // Privacy => re-ask the GDPR choices (the consent popup promises this entry)
            if (_privacyRect != null &&
                RectTransformUtility.RectangleContainsScreenPoint(_privacyRect, screenPos, null))
            {
                OpenPrivacy();
                return;
            }

            // MainMenu => return to Boot scene
            if (_mainMenuRect != null &&
                RectTransformUtility.RectangleContainsScreenPoint(_mainMenuRect, screenPos, null))
            {
                ReturnToMainMenu();
                return;
            }
        }

        public void Open()
        {
            if (_panel == null) return;
            _isOpen = true;
            UISoundManager.Instance?.PlayOpenSection();
            _panelObj.SetActive(true);
            var p = _panel.anchoredPosition;
            p.x = _panelWidth;
            _panel.anchoredPosition = p;
            Time.timeScale = 0f;
            _slideTween?.Kill();
            _slideTween = _panel.DOAnchorPosX(0f, 0.7f)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true);
        }

        public void Close()
        {
            if (_panel == null) return;
            _isOpen = false;
            UISoundManager.Instance?.PlayCloseSection();
            _slideTween?.Kill();
            _slideTween = _panel.DOAnchorPosX(_panelWidth, 0.5f)
                .SetEase(Ease.InCubic)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    _panelObj.SetActive(false);
                    Time.timeScale = 1f;
                });
        }

        /// <summary>Close panel visually without unpausing (used when navigating to sub-panels).</summary>
        private void CloseKeepPaused()
        {
            if (_panel == null) return;
            _isOpen = false;
            _slideTween?.Kill();
            _slideTween = _panel.DOAnchorPosX(_panelWidth, 0.5f)
                .SetEase(Ease.InCubic)
                .SetUpdate(true)
                .OnComplete(() => _panelObj.SetActive(false));
        }

        private void ReturnToMainMenu()
        {
            _isOpen = false;

            // Force a synchronous save before leaving the scene so the player resumes exactly here
            var hotel = FindAnyObjectByType<HotelManager>();
            if (hotel != null && CloudSaveManager.Instance != null)
            {
                CloudSaveManager.Instance.Progression = hotel.CollectProgressionData();
                CloudSaveManager.Instance.SaveProgressionImmediate();
            }

            LoadingScreen.TransitionTo("Boot", () => Time.timeScale = 1f);
        }

        // ---------- Privacy (B-14) ----------

        /// <summary>
        /// Adds a "Privacy" option above "Main menu" by cloning that option. The options sit in a
        /// VerticalLayoutGroup, so the container grows by one slot to keep the same spacing.
        /// </summary>
        private RectTransform CreatePrivacyOption()
        {
            var template = _mainMenuRect;
            var container = template != null ? template.parent as RectTransform : null;
            if (container == null) return null;

            int slots = container.childCount;
            float slotHeight = slots > 0 ? container.rect.height / slots : template.rect.height;

            var go = Instantiate(template.gameObject, container, false);
            go.name = "PrivacyOption";
            go.transform.SetSiblingIndex(template.GetSiblingIndex());
            container.sizeDelta += new Vector2(0f, slotHeight);

            _privacyLabel = go.GetComponentInChildren<TMP_Text>(true);
            if (_privacyLabel != null) _privacyLabel.gameObject.name = "PrivacyLabel";
            ApplyPrivacyLabel();
            LocalizedStrings.OnLanguageChanged += ApplyPrivacyLabel;

            return go.GetComponent<RectTransform>();
        }

        private void ApplyPrivacyLabel()
        {
            if (_privacyLabel != null) _privacyLabel.text = LocalizedStrings.Get("ui.privacy");
        }

        private void OnDestroy()
        {
            LocalizedStrings.OnLanguageChanged -= ApplyPrivacyLabel;
        }

        private void OpenPrivacy()
        {
            var panel = GetConsentPanel();
            if (panel == null) return;
            CloseKeepPaused();
            panel.ShowForReview(Open); // back to this menu (still paused) once answered
        }

        private ConsentPanel GetConsentPanel()
        {
            if (_consentPanel != null) return _consentPanel;

            var prefab = Resources.Load<GameObject>(ConsentPanel.ResourcePath);
            var canvas = _panel != null ? _panel.GetComponentInParent<Canvas>(true) : null;
            if (prefab == null || canvas == null)
            {
                Debug.LogWarning("[Options] Consent panel prefab or canvas not found");
                return null;
            }

            var go = Instantiate(prefab, canvas.rootCanvas.transform, false);
            go.name = "ConsentPanel";
            // The component lives on the Boot scene instance, not in the prefab: add it here.
            // Its Awake auto-wires the labels/buttons and hides the popup (choices already made).
            _consentPanel = go.GetComponent<ConsentPanel>();
            if (_consentPanel == null) _consentPanel = go.AddComponent<ConsentPanel>();
            return _consentPanel;
        }

        private void OpenParameters()
        {
            if (_parametersPanel == null) return;
            CloseKeepPaused();
            _parametersPanel.Open();
            _parametersPanel.OnClosed = Open; // Return to OptionsPanel when ParametersPanel closes (still paused)
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
