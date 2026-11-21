// STUB of the Unity 6.2+ Developer Data framework (engine module, namespace UnityEngine.UnityConsent).
// Only compiled by the game when ENABLE_UNITY_CONSENT is defined (typecheck with HARNESS_EXTRA_DEFINES=ENABLE_UNITY_CONSENT).
// Verified against call sites in com.unity.services.analytics 6.3.0 (ConsentManager.cs): GetConsentState(),
// consentStateChanged (Action<ConsentState>), ConsentState.AnalyticsIntent (read), ConsentStatus.{Unspecified,Denied,Granted}.
// Members marked // GUESS (setters, SetConsentState, AdsIntent) follow the Unity 6.2 docs sample, not verified here.
using System;

namespace UnityEngine.UnityConsent
{
    public enum ConsentStatus { Unspecified, Denied, Granted }

    public struct ConsentState
    {
        public ConsentStatus AdsIntent { get; set; }       // GUESS
        public ConsentStatus AnalyticsIntent { get; set; } // getter verified, setter GUESS
    }

    public static class EndUserConsent
    {
        public static event Action<ConsentState> consentStateChanged;
        public static ConsentState GetConsentState() { consentStateChanged?.Invoke(default); return default; }
        public static void SetConsentState(ConsentState consentState) { } // GUESS
    }
}
