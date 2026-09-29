// STUB of com.unity.services.cloudsave 3.4.0 - namespaces Unity.Services.CloudSave, .Models, .Models.Data.Player. Compile-only.
// Call sites: CloudSaveProvider.cs uses Data.Player.SaveAsync(Dictionary<string,object>), LoadAsync(HashSet<string>),
// DeleteAsync(string, DeleteOptions), Item.Value.GetAsString().
// Members marked // GUESS were not verified against the package source.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.CloudSave.Internal.Http;
using Unity.Services.CloudSave.Models;
using Unity.Services.CloudSave.Models.Data.Player;
using Unity.Services.Core;

namespace Unity.Services.CloudSave
{
    public static class CloudSaveService
    {
        public static ICloudSaveService Instance => throw null;
    }

    public interface ICloudSaveService
    {
        ICloudSaveDataService Data { get; } // GUESS: interface name
    }

    public interface ICloudSaveDataService // GUESS: interface name
    {
        IPlayerDataService Player { get; }
    }

    public interface IPlayerDataService
    {
        Task<List<ItemKey>> ListAllKeysAsync(ListAllKeysOptions options = null);
        Task<Dictionary<string, Item>> LoadAsync(ISet<string> keys, LoadOptions options = null);
        Task<Dictionary<string, Item>> LoadAllAsync(LoadAllOptions options = null);
        Task<Dictionary<string, string>> SaveAsync(IDictionary<string, object> data);
        Task<Dictionary<string, string>> SaveAsync(IDictionary<string, object> data, SaveOptions options);
        Task<Dictionary<string, string>> SaveAsync(IDictionary<string, SaveItem> data);
        Task<Dictionary<string, string>> SaveAsync(IDictionary<string, SaveItem> data, SaveOptions options);
        Task DeleteAsync(string key, DeleteOptions options = null);
        Task DeleteAllAsync(DeleteAllOptions options = null);
    }

    public enum CloudSaveExceptionReason // GUESS: members
    {
        Unknown = 0, NoInternetConnection = 1, ProjectIdMissing = 2, PlayerIdMissing = 3, AccessTokenMissing = 4, InvalidArgument = 5,
        Unauthorized = 6, KeyLimitExceeded = 7, NotFound = 8, TooManyRequests = 9, ServiceUnavailable = 10
    }

    public class CloudSaveException : RequestFailedException
    {
        public CloudSaveException(CloudSaveExceptionReason reason, int errorCode, string message, Exception innerException) : base(errorCode, message, innerException) { }
        public CloudSaveExceptionReason Reason => throw null;
    }

    public class CloudSaveValidationException : CloudSaveException
    {
        public CloudSaveValidationException(CloudSaveExceptionReason reason, int errorCode, string message, Exception innerException) : base(reason, errorCode, message, innerException) { }
    }

    public class CloudSaveRateLimitedException : CloudSaveException
    {
        public CloudSaveRateLimitedException(CloudSaveExceptionReason reason, int errorCode, string message, float retryAfter, Exception innerException) : base(reason, errorCode, message, innerException) { }
        public float RetryAfter => throw null;
    }

    public class CloudSaveConflictException : CloudSaveException
    {
        public CloudSaveConflictException(CloudSaveExceptionReason reason, int errorCode, string message, Exception innerException) : base(reason, errorCode, message, innerException) { }
    }
}

namespace Unity.Services.CloudSave.Internal.Http // GUESS: namespace of IDeserializable
{
    public class DeserializationSettings { }

    public interface IDeserializable
    {
        T GetAs<T>(DeserializationSettings deserializationSettings = null);
        string GetAsString();
    }
}

namespace Unity.Services.CloudSave.Models
{
    public class Item
    {
        public Item(string key, object value, string writeLock = null, DateTime? modified = null, DateTime? created = null) { } // GUESS
        public string Key => throw null;
        public IDeserializable Value => throw null;
        public string WriteLock => throw null;
        public DateTime? Modified => throw null;
        public DateTime? Created => throw null;
    }

    public class ItemKey
    {
        public string Key => throw null;
        public string WriteLock => throw null;
        public DateTime? Modified => throw null;
    }

    public class SaveItem
    {
        public SaveItem(object value, string writeLock) { }
        public object Value => throw null;
        public string WriteLock => throw null;
    }
}

namespace Unity.Services.CloudSave.Models.Data.Player
{
    public class SaveOptions { public SaveOptions() { } } // GUESS: real ctor takes an access-class options object
    public class LoadOptions { public LoadOptions() { } } // GUESS
    public class LoadAllOptions { public LoadAllOptions() { } } // GUESS
    public class ListAllKeysOptions { public ListAllKeysOptions() { } } // GUESS
    public class DeleteAllOptions { public DeleteAllOptions() { } } // GUESS
    public class DeleteOptions
    {
        public DeleteOptions() { }
        public string WriteLock { get => throw null; set { } }
    }
}
