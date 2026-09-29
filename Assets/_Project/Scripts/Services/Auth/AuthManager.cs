using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using Unity.Services.Authentication;
#if UNITY_ANDROID && !UNITY_EDITOR
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif

namespace CatHotel.Services
{
    public class AuthManager : MonoBehaviour
    {
        public static AuthManager Instance { get; private set; }

        [SerializeField] private AuthConfig _config;

        public bool IsSignedIn { get; private set; }
        public bool IsGooglePlayLinked { get; private set; }
        public string PlayerId { get; private set; }
        public string EnvironmentName { get; private set; }

        public event Action OnSignInComplete;
        public event Action<string> OnSignInFailed;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // Retry policy for transient network failures at boot
        private static readonly int[] RetryDelaysMs = { 0, 1000, 3000 };

        public async Task InitializeAsync()
        {
            Exception lastError = null;

            EnvironmentName = AuthConfig.ResolveEnvironmentName(_config);
            Debug.Log($"[Auth] UGS environment: '{EnvironmentName}' ({AuthConfig.BuildKind})");
            if (!AuthConfig.IsDevBuild && EnvironmentName != AuthConfig.ReleaseEnvironment)
                Debug.LogWarning("[Auth] Release build is NOT using the 'production' UGS environment — check AuthConfig.releaseEnvironmentName");

            // Push the stored analytics choice before UnityServices init, so the SDK only starts when already granted.
            GameAnalytics.SyncConsent();

            for (int attempt = 0; attempt < RetryDelaysMs.Length; attempt++)
            {
                if (RetryDelaysMs[attempt] > 0)
                    await Task.Delay(RetryDelaysMs[attempt]);

                try
                {
                    var options = new InitializationOptions();
                    options.SetEnvironmentName(EnvironmentName);
                    await UnityServices.InitializeAsync(options);
                    // Before sign-in: Analytics doesn't need it, and events are buffered even if sign-in fails offline.
                    GameAnalytics.NotifyServicesInitialized();

                    if (!AuthenticationService.Instance.IsSignedIn)
                        await AuthenticationService.Instance.SignInAnonymouslyAsync();

                    PlayerId = AuthenticationService.Instance.PlayerId;
                    IsSignedIn = true;
                    if (attempt > 0)
                        Debug.Log($"[Auth] UGS sign-in OK on retry #{attempt}. PlayerId: {PlayerId}");
                    else
                        Debug.Log($"[Auth] UGS anonymous sign-in OK. PlayerId: {PlayerId}");

                    await TryLinkGooglePlayAsync();

                    OnSignInComplete?.Invoke();
                    return;
                }
                catch (Exception e)
                {
                    lastError = e;
                    Debug.LogWarning($"[Auth] Sign-in attempt {attempt + 1}/{RetryDelaysMs.Length} failed: {e.Message}");
                }
            }

            // All attempts exhausted — game continues offline
            Debug.LogWarning($"[Auth] All sign-in attempts failed. Continuing offline. Last error: {lastError?.Message}");
            OnSignInFailed?.Invoke(lastError?.Message ?? "unknown");
        }

        private async Task TryLinkGooglePlayAsync()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                PlayGamesPlatform.Activate();

                string authCode = await RequestServerAuthCodeAsync();
                if (string.IsNullOrEmpty(authCode))
                {
                    Debug.LogWarning("[Auth] GPGS: no auth code obtained");
                    return;
                }

                try
                {
                    await AuthenticationService.Instance
                        .LinkWithGooglePlayGamesAsync(authCode);
                    IsGooglePlayLinked = true;
                    Debug.Log("[Auth] Google Play linked successfully");
                }
                catch (AuthenticationException e) when (e.ErrorCode == 10002)
                {
                    await AuthenticationService.Instance
                        .SignInWithGooglePlayGamesAsync(authCode);
                    IsGooglePlayLinked = true;
                    PlayerId = AuthenticationService.Instance.PlayerId;
                    Debug.Log("[Auth] Google Play sign-in (already linked)");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Auth] GPGS link failed: {e.Message}");
            }
#else
            await Task.CompletedTask;
            Debug.Log("[Auth] GPGS skipped (not Android)");
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private Task<string> RequestServerAuthCodeAsync()
        {
            var tcs = new TaskCompletionSource<string>();
            PlayGamesPlatform.Instance.RequestServerSideAccess(
                forceRefreshToken: false,
                code => tcs.SetResult(code)
            );
            return tcs.Task;
        }
#endif
    }
}
