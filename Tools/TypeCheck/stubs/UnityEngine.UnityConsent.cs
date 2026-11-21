// STUB of the Unity 6 Developer Data framework (engine module, namespace UnityEngine.UnityConsent).
// Used by the game under ENABLE_UNITY_CONSENT, which the harness defines by default (as Unity 6000.3 does).
// Verified against call sites in com.unity.services.analytics 6.3.0 (ConsentManager.cs) and the Unity 6000.3
// scripting reference (EndUserConsent.SetConsentState, ConsentState struct with public fields AdsIntent / AnalyticsIntent).
using System;

namespace UnityEngine.UnityConsent
{
    public enum ConsentStatus { Unspecified, Denied, Granted }

    public struct ConsentState
    {
        public ConsentStatus AdsIntent;
        public ConsentStatus AnalyticsIntent;
    }

    public static class EndUserConsent
    {
        public static event Action<ConsentState> consentStateChanged;
        public static ConsentState GetConsentState() { consentStateChanged?.Invoke(default); return default; }
        public static void SetConsentState(ConsentState consentState) { }
    }
}
