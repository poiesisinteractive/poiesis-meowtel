using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CatHotel.Services;
using CatHotel.Core;

namespace CatHotel.UI
{
    /// <summary>
    /// Popup RGPD. Auto-wire et localise les elements par nom (recherche recursive) :
    ///   TitleLabel, DescriptionLabel (TMP_Text), AcceptButton, RefuseButton (Button).
    /// Deux questions successives, une par finalite : publicite personnalisee, puis
    /// statistiques d'utilisation.
    /// - Au premier lancement (scene Boot) : seules les questions sans reponse sont posees ;
    ///   le BootManager attend ConsentManager.HasMadeAllChoices avant d'initialiser les pubs.
    /// - En revision (menu pause > Confidentialite, via ShowForReview) : les deux questions sont
    ///   reposees avec le choix actuel, et le nouveau choix s'applique immediatement.
    /// Le prefab vit dans Resources/UI pour pouvoir etre ouvert depuis la scene de jeu.
    /// </summary>
    public class ConsentPanel : MonoBehaviour
    {
        public const string ResourcePath = "UI/ConsentPanel";

        [Tooltip("Racine du panel a masquer apres choix. Si null, utilise ce GameObject.")]
        [SerializeField] private GameObject _panelRoot;

        [SerializeField] private TMP_Text _titleLabel;
        [SerializeField] private TMP_Text _descriptionLabel;
        [SerializeField] private Button _acceptButton;
        [SerializeField] private Button _refuseButton;

        private enum Step { Ads, Analytics }
        private Step _step;
        private float _inputUnlockTime;
        private bool _reviewMode;
        private bool _listening;
        private Action _onReviewClosed;

        public bool IsOpen => _panelRoot != null && _panelRoot.activeSelf;

        private void Awake()
        {
            if (_panelRoot == null) _panelRoot = gameObject;

            if (_titleLabel == null) _titleLabel = FindChild<TMP_Text>("TitleLabel");
            if (_descriptionLabel == null) _descriptionLabel = FindChild<TMP_Text>("DescriptionLabel");
            if (_acceptButton == null) _acceptButton = FindChild<Button>("AcceptButton");
            if (_refuseButton == null) _refuseButton = FindChild<Button>("RefuseButton");

            if (_acceptButton != null) _acceptButton.onClick.AddListener(OnAccept);
            if (_refuseButton != null) _refuseButton.onClick.AddListener(OnRefuse);

            // First-launch flow: only the unanswered questions.
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
            Open();
        }

        /// <summary>Re-asks both questions (from the pause menu), showing the current choices.</summary>
        public void ShowForReview(Action onClosed)
        {
            _reviewMode = true;
            _onReviewClosed = onClosed;
            _step = Step.Ads;
            Open();
        }

        private void Open()
        {
            ApplyLocalizedText();
            if (!_listening)
            {
                // The language may change while the popup is up (BootManager re-applies the saved one).
                LocalizedStrings.OnLanguageChanged += ApplyLocalizedText;
                _listening = true;
            }
            _inputUnlockTime = Time.unscaledTime + 0.3f;
            _panelRoot.SetActive(true);
            _panelRoot.transform.SetAsLastSibling();
        }

        private void OnDestroy()
        {
            if (_listening) LocalizedStrings.OnLanguageChanged -= ApplyLocalizedText;
        }

        private void ApplyLocalizedText()
        {
            bool ads = _step == Step.Ads;
            if (_titleLabel != null)
                _titleLabel.text = LocalizedStrings.Get(ads ? "consent.title" : "consent.analytics.title");
            if (_descriptionLabel != null)
            {
                string body = LocalizedStrings.Get(ads ? "consent.body" : "consent.analytics.body");
                if (_reviewMode)
                {
                    bool given = ads ? ConsentManager.ConsentGiven : ConsentManager.AnalyticsConsentGiven;
                    string state = LocalizedStrings.Get(given ? "consent.state.accepted" : "consent.state.refused");
                    body += "\n\n" + LocalizedStrings.Get("consent.current", state);
                }
                _descriptionLabel.text = body;
            }

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
                AdManager.Instance?.ApplyPrivacySettings(); // applies to the next ad requests
                if (_reviewMode || !ConsentManager.HasMadeAnalyticsChoice)
                {
                    _step = Step.Analytics;
                    _inputUnlockTime = Time.unscaledTime + 0.5f;
                    ApplyLocalizedText();
                    return;
                }
            }
            else
            {
                bool wasGranted = ConsentManager.HasMadeAnalyticsChoice && ConsentManager.AnalyticsConsentGiven;
                ConsentManager.SetAnalyticsConsent(accepted); // starts or stops collection immediately
                // Withdrawal: also ask Unity to delete what was collected (right to erasure)
                if (wasGranted && !accepted)
                    GameAnalytics.RequestDataDeletion();
            }
            Close();
        }

        private void Close()
        {
            _panelRoot.SetActive(false);
            if (_listening)
            {
                LocalizedStrings.OnLanguageChanged -= ApplyLocalizedText;
                _listening = false;
            }
            if (_reviewMode)
            {
                _reviewMode = false;
                var callback = _onReviewClosed;
                _onReviewClosed = null;
                callback?.Invoke();
            }
        }
    }
}
