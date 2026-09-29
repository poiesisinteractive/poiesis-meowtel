using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CatHotel.Services;
using CatHotel.Core;

namespace CatHotel.UI
{
    /// <summary>
    /// Popup RGPD affichee au premier lancement, dans la scene Boot.
    /// Auto-wire et localise les elements par nom (recherche recursive) :
    ///   TitleLabel, DescriptionLabel (TMP_Text), AcceptButton, RefuseButton (Button).
    /// Deux questions successives, une par finalite : publicite personnalisee, puis
    /// statistiques d'utilisation. Seules les questions sans reponse sont posees.
    /// Le BootManager attend ConsentManager.HasMadeAllChoices avant d'initialiser les pubs.
    /// </summary>
    public class ConsentPanel : MonoBehaviour
    {
        [Tooltip("Racine du panel a masquer apres choix. Si null, utilise ce GameObject.")]
        [SerializeField] private GameObject _panelRoot;

        [SerializeField] private TMP_Text _titleLabel;
        [SerializeField] private TMP_Text _descriptionLabel;
        [SerializeField] private Button _acceptButton;
        [SerializeField] private Button _refuseButton;

        private enum Step { Ads, Analytics }
        private Step _step;
        private float _inputUnlockTime;

        private void Awake()
        {
            if (_panelRoot == null) _panelRoot = gameObject;

            if (_titleLabel == null) _titleLabel = FindChild<TMP_Text>("TitleLabel");
            if (_descriptionLabel == null) _descriptionLabel = FindChild<TMP_Text>("DescriptionLabel");
            if (_acceptButton == null) _acceptButton = FindChild<Button>("AcceptButton");
            if (_refuseButton == null) _refuseButton = FindChild<Button>("RefuseButton");

            // Already chosen: skip popup entirely
            bool needAds = !ConsentManager.HasMadeChoice;
            bool needAnalytics = !ConsentManager.HasMadeAnalyticsChoice;
            if (!needAds && !needAnalytics)
            {
                _panelRoot.SetActive(false);
                return;
            }
            // Players who already answered the ads question (<= 0.35) only get the analytics one.
            _step = needAds ? Step.Ads : Step.Analytics;

            // Awake() runs before BootManager.Start() resolves the language.
            // InitFromSystem() is idempotent → ensures the popup is in the device language.
            LocalizedStrings.InitFromSystem();
            ApplyLocalizedText();
            // BootManager re-applies the saved language after the cloud load, possibly while the popup is up.
            LocalizedStrings.OnLanguageChanged += ApplyLocalizedText;

            _panelRoot.SetActive(true);
            if (_acceptButton != null) _acceptButton.onClick.AddListener(OnAccept);
            if (_refuseButton != null) _refuseButton.onClick.AddListener(OnRefuse);
        }

        private void OnDestroy()
        {
            LocalizedStrings.OnLanguageChanged -= ApplyLocalizedText;
        }

        private void ApplyLocalizedText()
        {
            bool ads = _step == Step.Ads;
            if (_titleLabel != null)
                _titleLabel.text = LocalizedStrings.Get(ads ? "consent.title" : "consent.analytics.title");
            if (_descriptionLabel != null)
                _descriptionLabel.text = LocalizedStrings.Get(ads ? "consent.body" : "consent.analytics.body");

            SetButtonLabel(_acceptButton, LocalizedStrings.Get("consent.accept"));
            SetButtonLabel(_refuseButton, LocalizedStrings.Get("consent.refuse"));
        }

        private static void SetButtonLabel(Button button, string text)
        {
            if (button == null) return;
            var label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null) label.text = text;
        }

        private T FindChild<T>(string childName) where T : Component
        {
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
            {
                if (t.name == childName)
                {
                    var comp = t.GetComponent<T>();
                    if (comp != null) return comp;
                }
            }
            return null;
        }

        private void OnAccept() => Choose(true);

        private void OnRefuse() => Choose(false);

        private void Choose(bool accepted)
        {
            // A double tap on the first question must not also answer the second one.
            if (Time.unscaledTime < _inputUnlockTime) return;

            if (_step == Step.Ads)
            {
                ConsentManager.SetConsent(accepted);
                if (!ConsentManager.HasMadeAnalyticsChoice)
                {
                    _step = Step.Analytics;
                    _inputUnlockTime = Time.unscaledTime + 0.5f;
                    ApplyLocalizedText();
                    return;
                }
            }
            else
            {
                ConsentManager.SetAnalyticsConsent(accepted);
            }
            _panelRoot.SetActive(false);
        }
    }
}
