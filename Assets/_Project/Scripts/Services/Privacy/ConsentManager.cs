using UnityEngine;

namespace CatHotel.Services
{
    /// <summary>
    /// Stocke et expose les choix RGPD de l'utilisateur (PlayerPrefs), un par finalité :
    /// - publicité personnalisée : lu par AdManager avant LevelPlay.Init() ;
    /// - statistiques d'utilisation : lu par GameAnalytics.
    /// </summary>
    public static class ConsentManager
    {
        private const string ChoiceMadeKey = "GDPR_ChoiceMade";
        private const string ConsentKey = "GDPR_Consent";
        private const string AnalyticsChoiceMadeKey = "GDPR_AnalyticsChoiceMade";
        private const string AnalyticsConsentKey = "GDPR_AnalyticsConsent";

        // --- Publicité personnalisée ---
        public static bool HasMadeChoice => PlayerPrefs.GetInt(ChoiceMadeKey, 0) == 1;
        public static bool ConsentGiven => PlayerPrefs.GetInt(ConsentKey, 0) == 1;

        // --- Statistiques d'utilisation ---
        public static bool HasMadeAnalyticsChoice => PlayerPrefs.GetInt(AnalyticsChoiceMadeKey, 0) == 1;
        public static bool AnalyticsConsentGiven => PlayerPrefs.GetInt(AnalyticsConsentKey, 0) == 1;

        public static bool HasMadeAllChoices => HasMadeChoice && HasMadeAnalyticsChoice;

        /// <summary>Fired after any choice is saved or reset.</summary>
        public static event System.Action OnConsentChanged;

        public static void SetConsent(bool accepted)
        {
            PlayerPrefs.SetInt(ChoiceMadeKey, 1);
            PlayerPrefs.SetInt(ConsentKey, accepted ? 1 : 0);
            PlayerPrefs.Save();
            Debug.Log($"[Consent] User choice saved: {(accepted ? "ACCEPTED" : "REFUSED")}");
            OnConsentChanged?.Invoke();
        }

        public static void SetAnalyticsConsent(bool accepted)
        {
            PlayerPrefs.SetInt(AnalyticsChoiceMadeKey, 1);
            PlayerPrefs.SetInt(AnalyticsConsentKey, accepted ? 1 : 0);
            PlayerPrefs.Save();
            Debug.Log($"[Consent] Analytics choice saved: {(accepted ? "ACCEPTED" : "REFUSED")}");
            GameAnalytics.SyncConsent(); // starts, or stops (with flush), immediately
            OnConsentChanged?.Invoke();
        }

        public static void ResetForDebug()
        {
            PlayerPrefs.DeleteKey(ChoiceMadeKey);
            PlayerPrefs.DeleteKey(ConsentKey);
            PlayerPrefs.DeleteKey(AnalyticsChoiceMadeKey);
            PlayerPrefs.DeleteKey(AnalyticsConsentKey);
            PlayerPrefs.Save();
            Debug.Log("[Consent] Choice reset");
            GameAnalytics.SyncConsent();
            OnConsentChanged?.Invoke();
        }
    }
}
