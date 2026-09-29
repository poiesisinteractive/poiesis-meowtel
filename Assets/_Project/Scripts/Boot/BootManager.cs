using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using CatHotel.Audio;
using CatHotel.Core;
using CatHotel.Services;
using CatHotel.UI;

namespace CatHotel.Boot
{
    /// <summary>
    /// Entry point of the game. Initializes services (Auth, Ads) then hands off
    /// to the main menu. Services use DontDestroyOnLoad so they persist across scenes.
    /// </summary>
    public class BootManager : MonoBehaviour
    {
        // Total auth budget: AuthManager retries 3x with 0/1/3s backoff → ~4s of waits + actual calls.
        // 10s leaves headroom on slow networks before falling back to offline mode.
        [SerializeField] private float _authTimeout = 10f;

        public bool IsReady { get; private set; }

        private IEnumerator Start()
        {
            // Detect and apply language (from saved pref or system language)
            CatHotel.Core.LocalizedStrings.InitFromSystem();

            // Reduce async loading impact on main thread → smoother animations during load
            Application.backgroundLoadingPriority = ThreadPriority.Low;

            // Create persistent loading screen (once, survives scene changes)
            if (LoadingScreen.Instance == null)
            {
                var lsGo = new GameObject("[LoadingScreen]");
                lsGo.AddComponent<LoadingScreen>();
            }

            // Create persistent sound managers (once, survive scene changes)
            if (UISoundManager.Instance == null)
            {
                var sfxGo = new GameObject("[UISoundManager]");
                sfxGo.AddComponent<UISoundManager>();
            }
            if (CatSoundManager.Instance == null)
            {
                var catSfxGo = new GameObject("[CatSoundManager]");
                catSfxGo.AddComponent<CatSoundManager>();
            }

            // Create persistent cloud save manager (once, survives scene changes)
            if (CloudSaveManager.Instance == null)
            {
                var csGo = new GameObject("[CloudSaveManager]");
                csGo.AddComponent<CloudSaveManager>();
            }

            // Create persistent battery saver controller (subscribes to ParametersPanel)
            if (BatterySaverController.Instance == null)
            {
                var bsGo = new GameObject("[BatterySaverController]");
                bsGo.AddComponent<BatterySaverController>();
            }

            // Initialize Addressables (loads catalog, required before any breed loading)
            yield return Addressables.InitializeAsync();

            // Initialize Auth (non-blocking, with timeout)
            if (AuthManager.Instance != null)
            {
                var authTask = AuthManager.Instance.InitializeAsync();
                float authDeadline = Time.realtimeSinceStartup + _authTimeout;
                while (!authTask.IsCompleted && Time.realtimeSinceStartup < authDeadline)
                {
                    yield return null;
                }
                if (!authTask.IsCompleted)
                    Debug.LogWarning("[Boot] Auth timeout — continuing offline");
            }

            // Load cloud save data (after auth, before anything else needs it)
            if (CloudSaveManager.Instance != null)
            {
                bool isSignedIn = AuthManager.Instance != null && AuthManager.Instance.IsSignedIn;
                var loadTask = CloudSaveManager.Instance.LoadAllAsync(isSignedIn);
                float loadDeadline = Time.realtimeSinceStartup + 8f;
                while (!loadTask.IsCompleted && Time.realtimeSinceStartup < loadDeadline)
                {
                    yield return null;
                }
                if (!loadTask.IsCompleted)
                {
                    Debug.LogWarning("[Boot] Cloud save load timeout — using local cache");
                    CloudSaveManager.Instance.LoadFromLocal();
                }

                // Migrate old SaveManager data if needed
                MigrateOldSave();

                // Re-apply language from cloud save (overrides InitFromSystem if cloud has data)
                var settings = CloudSaveManager.Instance.Settings;
                if (!string.IsNullOrEmpty(settings.language))
                    CatHotel.Core.LocalizedStrings.SetLanguage(settings.language);

                // Verification log: surface exactly what was loaded so prod logs are diagnosable.
                LogSaveState(CloudSaveManager.Instance);
            }

            // Wait for GDPR consent choice (popup in Boot scene) before initializing ads
            if (!ConsentManager.HasMadeChoice)
            {
                Debug.Log("[Boot] Waiting for user GDPR consent choice...");
                while (!ConsentManager.HasMadeChoice)
                {
                    yield return null;
                }
                Debug.Log($"[Boot] Consent received: {(ConsentManager.ConsentGiven ? "accepted" : "refused")}");
            }

            // Initialize Ads (non-blocking)
            var adManager = AdManager.Instance ?? FindAnyObjectByType<AdManager>();
            if (adManager != null)
                adManager.InitializeAds();

            IsReady = true;
            Debug.Log("[Boot] Services initialized. Main menu ready.");
        }

        /// <summary>
        /// Print a single-line summary of the save state after boot load.
        /// Useful for diagnosing prod issues from logcat — tells you which source
        /// won (cloud vs local), the lastSaveTime, and pending sync state.
        /// </summary>
        private static void LogSaveState(Services.CloudSaveManager mgr)
        {
            if (mgr == null) return;
            var p = mgr.Progression;
            string lastSave = string.IsNullOrEmpty(p?.lastSaveTime) ? "never" : p.lastSaveTime;
            string source = mgr.IsCloudAvailable ? "cloud" : "local-only";
            DevLog.Always(
                $"[Boot] Save state — source={source}, hasSave={mgr.HasPersistedSave}, " +
                $"pendingSync={mgr.HasPendingSync}, lastSaveTime={lastSave}, " +
                $"coins={p?.coins}, rep={p?.reputationLevel}, " +
                $"cats={p?.cats?.Count ?? 0}, objects={p?.placedObjects?.Count ?? 0}");
        }

        /// <summary>
        /// Migrate data from old SaveManager (cathotel_save.json) and PlayerPrefs
        /// into the new CloudSaveManager system. Runs once on first launch after update.
        /// </summary>
        private void MigrateOldSave()
        {
            if (CloudSaveManager.Instance == null) return;

            // Skip if we already have a persisted cloud/local save
            if (CloudSaveManager.Instance.HasPersistedSave) return;

            var progression = CloudSaveManager.Instance.Progression;

            // Migrate from old SaveManager
#pragma warning disable CS0612, CS0618 // Intentional use of obsolete SaveManager for migration
            var oldData = SaveManager.Load();
#pragma warning restore CS0612, CS0618
            if (oldData != null)
            {
                progression.reputationLevel = oldData.reputationLevel;
                progression.reputationXp = oldData.reputationXp;

                // Migrate cats
                if (oldData.cats != null)
                {
                    progression.cats = new List<CatCloudSaveData>();
                    foreach (var oldCat in oldData.cats)
                    {
                        progression.cats.Add(new CatCloudSaveData
                        {
                            breedName = oldCat.breedName,
                            catName = oldCat.catName,
                            mode = oldCat.mode,
                            isSpecial = oldCat.isSpecial,
                            needs = oldCat.needs,
                            happiness = oldCat.happiness,
                            pensionDuration = oldCat.pensionDuration,
                            pensionTimeRemaining = oldCat.pensionTimeRemaining
                        });
                    }
                }

                CloudSaveManager.Instance.SaveProgression();
                Debug.Log("[Boot] Migrated old save data to cloud save system");
            }

            // Migrate settings from PlayerPrefs (only if we don't have cloud settings yet)
            var settings = CloudSaveManager.Instance.Settings;
            if (string.IsNullOrEmpty(settings.language))
            {
                settings.language = PlayerPrefs.GetString("Param_Language", "");
                settings.musicVolume = PlayerPrefs.GetFloat("Param_MusicVolume", 1f);
                settings.effectsVolume = PlayerPrefs.GetFloat("Param_EffectsVolume", 1f);
                settings.pushNotifications = PlayerPrefs.GetInt("Param_PushNotif", 1) == 1;
                settings.batterySaving = PlayerPrefs.GetInt("Param_BatterySaving", 0) == 1;
                settings.soundEnabled = PlayerPrefs.GetInt("Param_SoundEnabled", 1) == 1;
                CloudSaveManager.Instance.SaveSettings();
                Debug.Log("[Boot] Migrated PlayerPrefs settings to cloud save system");
            }
        }

    }
}
