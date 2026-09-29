using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using CatHotel.Core;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models;
using DeleteOptions = Unity.Services.CloudSave.Models.Data.Player.DeleteOptions;

namespace CatHotel.Services
{
    /// <summary>
    /// Thin wrapper around Unity Cloud Save SDK.
    /// Handles serialization via JsonUtility and Cloud Save key/value API.
    /// </summary>
    public static class CloudSaveProvider
    {
        /// <summary>Save a serializable object under the given key.</summary>
        public static async Task SaveAsync<T>(string key, T data)
        {
            string json = JsonUtility.ToJson(data);
            var saveData = new Dictionary<string, object> { { key, json } };
            await CloudSaveService.Instance.Data.Player.SaveAsync(saveData);
            DevLog.Log($"[CloudSave] Saved key '{key}'");
        }

        /// <summary>Load a serializable object from the given key. Returns null if not found.</summary>
        public static async Task<T> LoadAsync<T>(string key) where T : class
        {
            var keys = new HashSet<string> { key };
            var result = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);

            if (!result.TryGetValue(key, out var item))
            {
                Debug.Log($"[CloudSave] Key '{key}' not found");
                return null;
            }

            string json = item.Value.GetAsString();
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogWarning($"[CloudSave] Key '{key}' has empty value");
                return null;
            }

            var data = JsonUtility.FromJson<T>(json);
            Debug.Log($"[CloudSave] Loaded key '{key}'");
            return data;
        }

        /// <summary>Delete a key from cloud save.</summary>
        public static async Task DeleteAsync(string key)
        {
            await CloudSaveService.Instance.Data.Player.DeleteAsync(key, new DeleteOptions());
            Debug.Log($"[CloudSave] Deleted key '{key}'");
        }
    }
}
