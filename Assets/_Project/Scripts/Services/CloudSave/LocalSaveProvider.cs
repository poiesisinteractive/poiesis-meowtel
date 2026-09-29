using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CatHotel.Core;
using UnityEngine;

namespace CatHotel.Services
{
    /// <summary>
    /// JSON file-based local save. Acts as cache for cloud data
    /// and as offline fallback when cloud is unavailable.
    ///
    /// Writes are atomic (write "file.tmp", then replace "file", keeping the previous
    /// version as "file.bak"), serialized behind a single lock, and ordered: every request
    /// takes a sequence number on the main thread, and a write older than what is already
    /// on disk for that path is dropped. Loads recover from ".tmp" then ".bak" when the
    /// main file is missing or unreadable.
    /// </summary>
    public static class LocalSaveProvider
    {
        private const string SettingsFile = "save_settings.json";
        private const string ProgressionFile = "save_progression.json";
        private const string PendingSyncFile = "save_pending_sync.json";

        private const string TmpSuffix = ".tmp";
        private const string BakSuffix = ".bak";
        private const string CorruptSuffix = ".corrupt";

        // Same bytes as File.WriteAllText: UTF-8 without BOM.
        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);

        // All disk work (read, write, delete, recovery) happens under this lock.
        private static readonly object IoLock = new object();
        private static long _writeSeq;
        // Highest request committed per path. Only touched under IoLock.
        private static readonly Dictionary<string, long> CommittedSeq = new Dictionary<string, long>();

        private static string SettingsPath =>
            Path.Combine(Application.persistentDataPath, SettingsFile);

        private static string ProgressionPath =>
            Path.Combine(Application.persistentDataPath, ProgressionFile);

        private static string PendingSyncPath =>
            Path.Combine(Application.persistentDataPath, PendingSyncFile);

        // --- Settings ---

        public static void SaveSettings(SettingsSaveData data)
        {
            WriteJson(SettingsPath, data);
        }

        public static Task SaveSettingsAsync(SettingsSaveData data)
        {
            return WriteJsonAsync(SettingsPath, data);
        }

        public static SettingsSaveData LoadSettings()
        {
            return ReadJson<SettingsSaveData>(SettingsPath);
        }

        // --- Progression ---

        public static void SaveProgression(ProgressionSaveData data)
        {
            WriteJson(ProgressionPath, data);
        }

        public static Task SaveProgressionAsync(ProgressionSaveData data)
        {
            return WriteJsonAsync(ProgressionPath, data);
        }

        public static ProgressionSaveData LoadProgression()
        {
            return ReadJson<ProgressionSaveData>(ProgressionPath);
        }

        public static void DeleteProgression()
        {
            DeleteAll(ProgressionPath);
        }

        // --- Pending Sync tracking ---

        public static void SetPendingSync(bool settingsDirty, bool progressionDirty)
        {
            var state = new PendingSyncState
            {
                settingsDirty = settingsDirty,
                progressionDirty = progressionDirty
            };
            // Tiny file rewritten on the main thread after every offline save:
            // no fsync hitch, and no .bak that could bring back a cleared flag.
            WriteJson(PendingSyncPath, state, keepBackup: false, durable: false);
        }

        public static PendingSyncState LoadPendingSync()
        {
            return ReadJson<PendingSyncState>(PendingSyncPath) ?? new PendingSyncState();
        }

        public static void ClearPendingSync()
        {
            DeleteAll(PendingSyncPath);
        }

        // --- Helpers ---

        private static long NextSeq() => Interlocked.Increment(ref _writeSeq);

        /// <summary>Synchronous write (used for Load path and critical saves).</summary>
        private static void WriteJson<T>(string path, T data, bool keepBackup = true, bool durable = true)
        {
            long seq = NextSeq();
            try
            {
                // JsonUtility must run on main thread
                string json = JsonUtility.ToJson(data, true);
                lock (IoLock) CommitLocked(path, json, seq, keepBackup, durable);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LocalSave] Write failed ({Path.GetFileName(path)}): {e.Message}");
            }
        }

        /// <summary>Async write — serializes on main thread, I/O on background thread.</summary>
        public static async Task WriteJsonAsync<T>(string path, T data)
        {
            long seq = NextSeq();
            try
            {
                // JsonUtility must run on main thread — snapshot before any await, only offload I/O
                string json = JsonUtility.ToJson(data, false);
#if UNITY_WEBGL && !UNITY_EDITOR
                // No worker threads on WebGL: commit synchronously.
                lock (IoLock) CommitLocked(path, json, seq, true, true);
                await Task.CompletedTask;
#else
                await Task.Run(() => { lock (IoLock) CommitLocked(path, json, seq, true, true); });
#endif
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LocalSave] Async write failed ({Path.GetFileName(path)}): {e.Message}");
            }
        }

        // Caller holds IoLock.
        private static void CommitLocked(string path, string json, long seq, bool keepBackup, bool durable)
        {
            if (CommittedSeq.TryGetValue(path, out var last) && seq < last)
                return; // a newer write or delete is already on disk
            WriteAtomicLocked(path, json, keepBackup, durable);
            CommittedSeq[path] = seq;
        }

        // Caller holds IoLock. Temp file in the same directory, so the rename never crosses devices.
        private static void WriteAtomicLocked(string path, string json, bool keepBackup, bool durable)
        {
            string tmp = path + TmpSuffix;
            string bak = keepBackup ? path + BakSuffix : null;
            try
            {
                byte[] bytes = Utf8NoBom.GetBytes(json);
                using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    fs.Write(bytes, 0, bytes.Length);
                    fs.Flush(durable); // true = flush to disk before the rename
                }

                if (!File.Exists(path))
                {
                    File.Move(tmp, path); // first save
                    return;
                }

                try
                {
                    File.Replace(tmp, path, bak, true);
                }
                catch (Exception e)
                {
                    // File.Replace is not supported everywhere (IL2CPP / some Android filesystems).
                    DevLog.Warn($"[LocalSave] File.Replace failed ({e.GetType().Name}), rename fallback");
                    if (!File.Exists(tmp)) return; // Replace completed despite throwing
                    if (File.Exists(path))         // Replace may have stopped half-way
                    {
                        if (bak != null)
                        {
                            if (File.Exists(bak)) File.Delete(bak);
                            File.Move(path, bak);
                        }
                        else
                        {
                            File.Delete(path);
                        }
                    }
                    File.Move(tmp, path);
                }
            }
            catch
            {
                // Keep the temp file only if it is the only copy left.
                if (File.Exists(path)) TryDelete(tmp);
                throw;
            }
        }

        private enum ReadStatus { Ok, Missing, Corrupt, IoError }

        private static T ReadJson<T>(string path) where T : class
        {
            lock (IoLock)
            {
                var status = TryReadLocked(path, out T data);
                if (status == ReadStatus.Ok) return data;

                if (status == ReadStatus.Corrupt) Quarantine(path);

                // A complete .tmp that survived is never older than the main file.
                foreach (var candidate in new[] { path + TmpSuffix, path + BakSuffix })
                {
                    if (TryReadLocked(candidate, out data) != ReadStatus.Ok) continue;
                    Debug.LogWarning($"[LocalSave] {Path.GetFileName(path)} {status}, recovered from {Path.GetFileName(candidate)}");
                    if (status != ReadStatus.IoError) Promote(candidate, path);
                    return data;
                }

                return null; // missing and no sidecar = fresh install
            }
        }

        // Caller holds IoLock.
        private static ReadStatus TryReadLocked<T>(string path, out T data) where T : class
        {
            data = null;
            if (!File.Exists(path)) return ReadStatus.Missing;

            string json;
            try
            {
                json = File.ReadAllText(path).Trim();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LocalSave] Read failed ({Path.GetFileName(path)}): {e.Message}");
                return ReadStatus.IoError;
            }

            // Empty, truncated or NUL-filled file.
            if (json.Length < 2 || json[0] != '{' || json[json.Length - 1] != '}')
                return ReadStatus.Corrupt;

            try
            {
                data = JsonUtility.FromJson<T>(json);
            }
            catch
            {
                return ReadStatus.Corrupt;
            }
            return data != null ? ReadStatus.Ok : ReadStatus.Corrupt;
        }

        // Caller holds IoLock. Moves a corrupt main file aside so it never rotates into .bak.
        private static void Quarantine(string path)
        {
            try
            {
                string corrupt = path + CorruptSuffix;
                if (File.Exists(corrupt)) File.Delete(corrupt);
                File.Move(path, corrupt);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LocalSave] Quarantine failed ({Path.GetFileName(path)}): {e.Message}");
            }
        }

        // Caller holds IoLock. Restores a recovered sidecar as the main file (.bak is kept).
        private static void Promote(string candidate, string path)
        {
            try
            {
                if (candidate.EndsWith(TmpSuffix, StringComparison.Ordinal))
                {
                    if (File.Exists(path)) File.Delete(path);
                    File.Move(candidate, path);
                }
                else
                {
                    File.Copy(candidate, path, true);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LocalSave] Recovery promote failed ({Path.GetFileName(candidate)}): {e.Message}");
            }
        }

        /// <summary>
        /// Deletes a save file and all its sidecars. The sidecars must go too, otherwise a
        /// New Game or a reset would be undone by .bak recovery at the next load. Bumping the
        /// committed sequence also drops any older async write still queued.
        /// </summary>
        private static void DeleteAll(string path)
        {
            long seq = NextSeq();
            lock (IoLock)
            {
                CommittedSeq[path] = seq;
                foreach (var p in new[] { path, path + TmpSuffix, path + BakSuffix, path + CorruptSuffix })
                {
                    try
                    {
                        if (File.Exists(p)) File.Delete(p);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[LocalSave] Delete failed ({Path.GetFileName(p)}): {e.Message}");
                    }
                }
            }
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch
            {
                // best effort
            }
        }
    }

    [Serializable]
    public class PendingSyncState
    {
        public bool settingsDirty;
        public bool progressionDirty;
    }
}
