using System;
using System.Threading.Tasks;
using UnityEngine;

namespace CatHotel.Services
{
    /// <summary>
    /// Central orchestrator for save/load. Cloud-first with local fallback.
    /// Always writes to local cache, then attempts cloud sync.
    /// At boot, loads from cloud if available, otherwise from local cache.
    /// </summary>
    public class CloudSaveManager : MonoBehaviour
    {
        public static CloudSaveManager Instance { get; private set; }

        private const string SettingsKey = "settings";
        private const string ProgressionKey = "progression";
        private const float SyncRetryInterval = 60f;

        /// <summary>Current settings data (in-memory cache).</summary>
        public SettingsSaveData Settings { get; private set; }

        /// <summary>Current progression data (in-memory cache).</summary>
        public ProgressionSaveData Progression { get; set; }

        /// <summary>True if cloud save is reachable and auth is signed in.</summary>
        public bool IsCloudAvailable { get; private set; }

        /// <summary>True if local changes haven't been synced to cloud yet.</summary>
        public bool HasPendingSync { get; private set; }

        /// <summary>True once initial load is complete (cloud or local).</summary>
        public bool IsLoaded { get; private set; }

        /// <summary>
        /// True if a persisted save (local file or cloud key) was found during load.
        /// This is the authoritative signal for "is this a returning player".
        /// </summary>
        public bool HasPersistedSave { get; private set; }

        private bool _settingsDirty;
        private bool _loadCancelled; // set when caller times out → in-flight LoadAllAsync must drop its result
        private bool _progressionDirty;
        private float _syncRetryTimer;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Settings = new SettingsSaveData();
            Progression = new ProgressionSaveData();
        }

        private void Update()
        {
            if (!HasPendingSync) return;

            _syncRetryTimer += Time.unscaledDeltaTime;
            if (_syncRetryTimer < SyncRetryInterval) return;
            _syncRetryTimer = 0f;

            _ = TrySyncPendingAsync();
        }

        // ========== LOAD ==========

        /// <summary>
        /// Load all data. Tries cloud first, falls back to local.
        /// Called by BootManager after auth.
        /// </summary>
        public async Task LoadAllAsync(bool isSignedIn)
        {
            _loadCancelled = false;

            if (isSignedIn)
            {
                try
                {
                    await LoadFromCloudAsync();
                    if (_loadCancelled)
                    {
                        Debug.Log("[CloudSaveManager] Cloud load result discarded (caller timed out)");
                        return;
                    }
                    IsCloudAvailable = true;

                    // Sync any pending local changes from a previous offline session
                    var pending = LocalSaveProvider.LoadPendingSync();
                    if (pending.settingsDirty || pending.progressionDirty)
                    {
                        Debug.Log("[CloudSaveManager] Found pending sync from previous session");
                        await ResolvePendingSyncAsync(pending);
                        if (_loadCancelled) return;
                    }

                    Debug.Log("[CloudSaveManager] Loaded from cloud");
                }
                catch (Exception e)
                {
                    if (_loadCancelled) return;
                    Debug.LogWarning($"[CloudSaveManager] Cloud load failed: {e.Message}");
                    IsCloudAvailable = false;
                    LoadFromLocal();
                }
            }
            else
            {
                IsCloudAvailable = false;
                LoadFromLocal();
            }

            IsLoaded = true;
        }

        /// <summary>
        /// Load all data from local cache only (offline mode).
        /// Also marks any in-flight LoadAllAsync as cancelled so its late result
        /// can't overwrite the just-loaded local state.
        /// </summary>
        public void LoadFromLocal()
        {
            _loadCancelled = true; // tell the in-flight cloud load to discard its result

            var localSettings = LocalSaveProvider.LoadSettings();
            var localProgression = LocalSaveProvider.LoadProgression();

            Settings = localSettings ?? new SettingsSaveData();
            Progression = localProgression ?? new ProgressionSaveData();

            // If either local save file existed, it's a returning player
            if (localProgression != null) HasPersistedSave = true;

            var pending = LocalSaveProvider.LoadPendingSync();
            HasPendingSync = pending.settingsDirty || pending.progressionDirty;
            _settingsDirty = pending.settingsDirty;
            _progressionDirty = pending.progressionDirty;
            IsLoaded = true;

            Debug.Log($"[CloudSaveManager] Loaded from local cache (hasSave={HasPersistedSave})");
        }

        /// <summary>
        /// Loads cloud + local settings/progression and keeps the one with the most recent
        /// lastSaveTime. If the local copy wins, it's pushed back to the cloud immediately.
        /// </summary>
        private async Task LoadFromCloudAsync()
        {
            var cloudSettings = await CloudSaveProvider.LoadAsync<SettingsSaveData>(SettingsKey);
            if (_loadCancelled) return; // caller timed out; don't touch state

            var cloudProgression = await CloudSaveProvider.LoadAsync<ProgressionSaveData>(ProgressionKey);
            if (_loadCancelled) return;

            var localSettings = LocalSaveProvider.LoadSettings();
            var localProgression = LocalSaveProvider.LoadProgression();

            // ----- Progression: pick the most recent of cloud vs local -----
            var (winnerProg, sourceProg) = PickMostRecentProgression(cloudProgression, localProgression);
            Progression = winnerProg ?? new ProgressionSaveData();

            if (winnerProg != null) HasPersistedSave = true;

            // If LOCAL was newer, push it back to cloud now (don't wait for next save).
            // This is also how a local save reaches a new, empty UGS environment.
            bool progressionPushFailed = false;
            if (sourceProg == "local" && localProgression != null)
            {
                try
                {
                    await CloudSaveProvider.SaveAsync(ProgressionKey, Progression);
                    Debug.Log("[CloudSaveManager] Local progression was newer — pushed to cloud");
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[CloudSaveManager] Failed to push newer local to cloud: {e.Message}");
                    // Will retry via TrySyncPendingAsync below
                    progressionPushFailed = true;
                    LocalSaveProvider.SetPendingSync(_settingsDirty, true);
                    _progressionDirty = true;
                    HasPendingSync = true;
                }
            }
            if (_loadCancelled) return;

            // ----- Settings: same logic but only when both are present (no timestamp on settings) -----
            // Settings has no timestamp; assume cloud > local (last device that changed settings synced)
            // unless pending sync flag says local hasn't been pushed yet.
            var pendingNow = LocalSaveProvider.LoadPendingSync();
            if (pendingNow.settingsDirty && localSettings != null)
            {
                Settings = localSettings;
                try
                {
                    await CloudSaveProvider.SaveAsync(SettingsKey, Settings);
                    Debug.Log("[CloudSaveManager] Local settings were dirty — pushed to cloud");
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[CloudSaveManager] Failed to push dirty settings: {e.Message}");
                }
            }
            else
            {
                Settings = cloudSettings ?? localSettings ?? new SettingsSaveData();
            }

            if (_loadCancelled) return; // caller timed out: never overwrite the local cache with this snapshot

            // Update local cache with the winning data so next offline boot is correct
            LocalSaveProvider.SaveSettings(Settings);
            LocalSaveProvider.SaveProgression(Progression);
            if (progressionPushFailed)
            {
                // Keep the retry: the newer local progression is not on the cloud yet
                LocalSaveProvider.SetPendingSync(false, true);
                _settingsDirty = false;
                _progressionDirty = true;
                HasPendingSync = true;
            }
            else
            {
                LocalSaveProvider.ClearPendingSync();
                _settingsDirty = false;
                _progressionDirty = false;
                HasPendingSync = false;
            }

            Debug.Log($"[CloudSaveManager] Load complete — progression source: {sourceProg ?? "none"}");
        }

        /// <summary>
        /// Compare cloud vs local progression by lastSaveTime. Returns the most recent
        /// and a label ("cloud" / "local" / null) for logging.
        /// </summary>
        private static (ProgressionSaveData winner, string source) PickMostRecentProgression(
            ProgressionSaveData cloud, ProgressionSaveData local)
        {
            if (cloud == null && local == null) return (null, null);
            if (cloud == null) return (local, "local");
            if (local == null) return (cloud, "cloud");

            DateTime cloudTime = ParseSaveTime(cloud.lastSaveTime);
            DateTime localTime = ParseSaveTime(local.lastSaveTime);

            // Tie-break: prefer cloud (canonical source) when timestamps are equal/missing
            return localTime > cloudTime ? (local, "local") : (cloud, "cloud");
        }

        private static DateTime ParseSaveTime(string iso)
        {
            if (string.IsNullOrEmpty(iso)) return DateTime.MinValue;
            return DateTime.TryParse(iso, null,
                System.Globalization.DateTimeStyles.RoundtripKind, out var dt)
                ? dt : DateTime.MinValue;
        }

        /// <summary>
        /// Called after offline play when cloud comes back. Decides whether the queued
        /// local change should overwrite cloud, by comparing lastSaveTime.
        /// </summary>
        private async Task ResolvePendingSyncAsync(PendingSyncState pending)
        {
            var localSettings = LocalSaveProvider.LoadSettings();
            var localProgression = LocalSaveProvider.LoadProgression();

            // Settings: just push (no timestamp on settings)
            if (pending.settingsDirty && localSettings != null)
            {
                Settings = localSettings;
                try { await CloudSaveProvider.SaveAsync(SettingsKey, Settings); }
                catch (Exception e) { Debug.LogWarning($"[CloudSaveManager] Settings sync failed: {e.Message}"); }
                if (_loadCancelled) return;
            }

            // Progression: only push if local is actually newer than what's on the cloud
            if (pending.progressionDirty && localProgression != null)
            {
                ProgressionSaveData cloudProgression = null;
                try { cloudProgression = await CloudSaveProvider.LoadAsync<ProgressionSaveData>(ProgressionKey); }
                catch { /* network blip — fallback to "trust local" */ }
                if (_loadCancelled) return;

                bool localWins = cloudProgression == null
                    || ParseSaveTime(localProgression.lastSaveTime)
                       >= ParseSaveTime(cloudProgression.lastSaveTime);

                if (localWins)
                {
                    Progression = localProgression;
                    try
                    {
                        await CloudSaveProvider.SaveAsync(ProgressionKey, Progression);
                        Debug.Log("[CloudSaveManager] Pending sync: local was newer, pushed to cloud");
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[CloudSaveManager] Pending sync push failed: {e.Message}");
                        return; // Keep flag, retry later
                    }
                }
                else
                {
                    // Cloud is newer (other device wrote since we went offline) — accept it,
                    // discard our local pending changes
                    Progression = cloudProgression;
                    LocalSaveProvider.SaveProgression(Progression);
                    Debug.Log("[CloudSaveManager] Pending sync: cloud was newer, discarded stale local");
                }
            }

            if (_loadCancelled) return;
            LocalSaveProvider.ClearPendingSync();
            HasPendingSync = false;
            _settingsDirty = false;
            _progressionDirty = false;
        }

        // ========== SAVE ==========

        /// <summary>
        /// Save settings. Async local I/O + cloud attempt.
        /// Non-blocking for the main thread.
        /// </summary>
        public async void SaveSettings()
        {
            // Async local write (serialization on main thread, I/O on background)
            await LocalSaveProvider.SaveSettingsAsync(Settings);

            if (IsCloudAvailable)
            {
                try
                {
                    await CloudSaveProvider.SaveAsync(SettingsKey, Settings);
                    _settingsDirty = false;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[CloudSaveManager] Cloud save settings failed: {e.Message}");
                    MarkPendingSync(settingsDirty: true);
                }
            }
            else
            {
                MarkPendingSync(settingsDirty: true);
            }
        }

        /// <summary>
        /// Save progression. Async local I/O + cloud attempt.
        /// Non-blocking for the main thread.
        /// </summary>
        public async void SaveProgression()
        {
            Progression.lastSaveTime = DateTime.UtcNow.ToString("o");
            HasPersistedSave = true;

            // Async local write (serialization on main thread, I/O on background)
            await LocalSaveProvider.SaveProgressionAsync(Progression);

            if (IsCloudAvailable)
            {
                try
                {
                    await CloudSaveProvider.SaveAsync(ProgressionKey, Progression);
                    _progressionDirty = false;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[CloudSaveManager] Cloud save progression failed: {e.Message}");
                    MarkPendingSync(progressionDirty: true);
                }
            }
            else
            {
                MarkPendingSync(progressionDirty: true);
            }
        }

        /// <summary>
        /// Synchronous save for OnApplicationPause/Quit where we MUST finish
        /// before the OS kills the process.
        /// </summary>
        public void SaveProgressionImmediate()
        {
            Progression.lastSaveTime = DateTime.UtcNow.ToString("o");
            HasPersistedSave = true;

            // 1. Synchronous local write — guaranteed to be on disk before this method returns
            LocalSaveProvider.SaveProgression(Progression);

            // 2. Mark as needing cloud sync NOW (in case cloud push below doesn't complete)
            _progressionDirty = true;
            HasPendingSync = true;
            LocalSaveProvider.SetPendingSync(_settingsDirty, true);

            // 3. Fire-and-forget cloud push. If it completes before the OS suspends us, great —
            //    the pending sync flag will be cleared on next boot's load. If it doesn't, the
            //    flag stays set and ResolvePendingSyncAsync handles it later.
            if (IsCloudAvailable)
                _ = TryPushProgressionToCloudAsync();
        }

        private async Task TryPushProgressionToCloudAsync()
        {
            try
            {
                await CloudSaveProvider.SaveAsync(ProgressionKey, Progression);
                _progressionDirty = false;
                if (!_settingsDirty) HasPendingSync = false;
                LocalSaveProvider.SetPendingSync(_settingsDirty, _progressionDirty);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[CloudSaveManager] Immediate cloud push failed: {e.Message}");
                // Pending flag already set — will retry on next online boot
            }
        }

        /// <summary>
        /// Reset all progression data (new game). Keeps settings intact.
        /// Clears local + cloud.
        /// </summary>
        public async void ResetAllData()
        {
            Progression = new ProgressionSaveData();
            LocalSaveProvider.DeleteProgression();
            LocalSaveProvider.ClearPendingSync();
            HasPendingSync = false;
            _progressionDirty = false;
            // No persisted save anymore — HotelManager will initialize with GameConfig defaults.
            // First real SaveProgression (auto-save, event) will re-create the file.
            HasPersistedSave = false;

            if (IsCloudAvailable)
            {
                try
                {
                    await CloudSaveProvider.DeleteAsync(ProgressionKey);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[CloudSaveManager] Cloud reset failed: {e.Message}");
                }
            }

            Debug.Log("[CloudSaveManager] Progression reset (new game)");
        }

        private void MarkPendingSync(bool settingsDirty = false, bool progressionDirty = false)
        {
            if (settingsDirty) _settingsDirty = true;
            if (progressionDirty) _progressionDirty = true;
            HasPendingSync = _settingsDirty || _progressionDirty;
            LocalSaveProvider.SetPendingSync(_settingsDirty, _progressionDirty);
        }

        /// <summary>Retry syncing pending local changes to cloud.</summary>
        private async Task TrySyncPendingAsync()
        {
            if (!HasPendingSync) return;

            try
            {
                if (_settingsDirty)
                {
                    await CloudSaveProvider.SaveAsync(SettingsKey, Settings);
                    _settingsDirty = false;
                }

                if (_progressionDirty)
                {
                    await CloudSaveProvider.SaveAsync(ProgressionKey, Progression);
                    _progressionDirty = false;
                }

                HasPendingSync = false;
                IsCloudAvailable = true;
                LocalSaveProvider.ClearPendingSync();
                Debug.Log("[CloudSaveManager] Pending sync completed");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[CloudSaveManager] Sync retry failed: {e.Message}");
            }
        }
    }
}
