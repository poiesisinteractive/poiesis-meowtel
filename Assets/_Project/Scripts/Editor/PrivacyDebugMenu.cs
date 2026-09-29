using UnityEditor;
using CatHotel.Services;

namespace CatHotel.Editor
{
    /// <summary>
    /// Debug menu for the GDPR choices (ads + analytics), to test the consent popup
    /// and analytics start/stop without a device. Additive: touches no scene.
    /// </summary>
    public static class PrivacyDebugMenu
    {
        [MenuItem("Cat Hotel/Debug/Privacy/Reset choices")]
        private static void ResetChoices() => ConsentManager.ResetForDebug();

        [MenuItem("Cat Hotel/Debug/Privacy/Analytics consent: grant")]
        private static void GrantAnalytics() => ConsentManager.SetAnalyticsConsent(true);

        [MenuItem("Cat Hotel/Debug/Privacy/Analytics consent: revoke")]
        private static void RevokeAnalytics() => ConsentManager.SetAnalyticsConsent(false);
    }
}
