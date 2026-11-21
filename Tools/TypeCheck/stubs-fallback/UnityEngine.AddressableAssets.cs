// STUB of com.unity.addressables 2.8 + ResourceManager - namespaces UnityEngine.AddressableAssets(.ResourceLocators),
// UnityEngine.ResourceManagement.AsyncOperations, UnityEngine.ResourceManagement.ResourceLocations, UnityEngine.ResourceManagement.ResourceProviders.
// Compile-only. Members marked // GUESS were not verified against the package source.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace UnityEngine.ResourceManagement.AsyncOperations
{
    public enum AsyncOperationStatus { None = 0, Succeeded = 1, Failed = 2 }

    public struct DownloadStatus
    {
        public long TotalBytes;
        public long DownloadedBytes;
        public bool IsDone;
        public float Percent => throw null;
    }

    public struct AsyncOperationHandle<TObject> : IEnumerator, IEquatable<AsyncOperationHandle<TObject>>
    {
        public static implicit operator AsyncOperationHandle(AsyncOperationHandle<TObject> obj) => throw null;
        public event Action<AsyncOperationHandle<TObject>> Completed { add { } remove { } }
        public event Action<AsyncOperationHandle> CompletedTypeless { add { } remove { } }
        public string DebugName => throw null;
        public event Action<AsyncOperationHandle> Destroyed { add { } remove { } }
        public void GetDependencies(List<AsyncOperationHandle> deps) { }
        public bool Equals(AsyncOperationHandle<TObject> other) => throw null;
        public override int GetHashCode() => throw null;
        public bool IsDone => throw null;
        public bool IsValid() => throw null;
        public Exception OperationException => throw null;
        public float PercentComplete => throw null;
        public DownloadStatus GetDownloadStatus() => throw null;
        public void Release() { }
        public TObject Result => throw null;
        public AsyncOperationStatus Status => throw null;
        public Task<TObject> Task => throw null;
        public TObject WaitForCompletion() => throw null;
        object IEnumerator.Current => throw null;
        bool IEnumerator.MoveNext() => throw null;
        void IEnumerator.Reset() { }
    }

    public struct AsyncOperationHandle : IEnumerator
    {
        public AsyncOperationHandle<T> Convert<T>() => throw null;
        public bool Equals(AsyncOperationHandle other) => throw null;
        public event Action<AsyncOperationHandle> Completed { add { } remove { } }
        public event Action<AsyncOperationHandle> Destroyed { add { } remove { } }
        public void GetDependencies(List<AsyncOperationHandle> deps) { }
        public override int GetHashCode() => throw null;
        public bool IsDone => throw null;
        public bool IsValid() => throw null;
        public Exception OperationException => throw null;
        public float PercentComplete => throw null;
        public DownloadStatus GetDownloadStatus() => throw null;
        public void Release() { }
        public object Result => throw null;
        public AsyncOperationStatus Status => throw null;
        public Task<object> Task => throw null;
        public object WaitForCompletion() => throw null;
        object IEnumerator.Current => throw null;
        bool IEnumerator.MoveNext() => throw null;
        void IEnumerator.Reset() { }
    }
}

namespace UnityEngine.ResourceManagement.ResourceLocations
{
    public interface IResourceLocation
    {
        string InternalId { get; }
        string ProviderId { get; }
        IList<IResourceLocation> Dependencies { get; }
        int Hash(Type resultType);
        int DependencyHashCode { get; }
        bool HasDependencies { get; }
        object Data { get; }
        string PrimaryKey { get; }
        Type ResourceType { get; }
    }
}

namespace UnityEngine.ResourceManagement.ResourceProviders
{
    public struct SceneInstance
    {
        public Scene Scene => throw null;
        [Obsolete("Use ActivateAsync instead.")] // GUESS
        public AsyncOperation Activate() => throw null;
        public AsyncOperation ActivateAsync() => throw null;
    }
}

namespace UnityEngine.AddressableAssets.ResourceLocators
{
    public interface IResourceLocator
    {
        string LocatorId { get; }
        IEnumerable<object> Keys { get; }
        IEnumerable<IResourceLocation> AllLocations { get; }
        bool Locate(object key, Type type, out IList<IResourceLocation> locations);
    }
}

namespace UnityEngine.AddressableAssets
{
    public static class Addressables
    {
        public enum MergeMode { None = 0, UseFirst = 0, Union = 1, Intersection = 2 }
        public static string Version => throw null;
        public static string RuntimePath => throw null;
        public static IEnumerable<IResourceLocator> ResourceLocators => throw null;
        public static AsyncOperationHandle<IResourceLocator> InitializeAsync() => throw null;
        public static AsyncOperationHandle<IResourceLocator> InitializeAsync(bool autoReleaseHandle) => throw null;
        public static AsyncOperationHandle<IResourceLocator> LoadContentCatalogAsync(string catalogPath, string providerSuffix = null) => throw null;
        public static AsyncOperationHandle<IResourceLocator> LoadContentCatalogAsync(string catalogPath, bool autoReleaseHandle, string providerSuffix = null) => throw null;
        public static AsyncOperationHandle<TObject> LoadAssetAsync<TObject>(IResourceLocation location) => throw null;
        public static AsyncOperationHandle<TObject> LoadAssetAsync<TObject>(object key) => throw null;
        public static AsyncOperationHandle<IList<IResourceLocation>> LoadResourceLocationsAsync(IEnumerable keys, MergeMode mode, Type type = null) => throw null;
        public static AsyncOperationHandle<IList<IResourceLocation>> LoadResourceLocationsAsync(object key, Type type = null) => throw null;
        public static AsyncOperationHandle<IList<TObject>> LoadAssetsAsync<TObject>(IList<IResourceLocation> locations, Action<TObject> callback) => throw null;
        public static AsyncOperationHandle<IList<TObject>> LoadAssetsAsync<TObject>(IList<IResourceLocation> locations, Action<TObject> callback, bool releaseDependenciesOnFailure) => throw null;
        public static AsyncOperationHandle<IList<TObject>> LoadAssetsAsync<TObject>(IEnumerable keys, Action<TObject> callback, MergeMode mode) => throw null;
        public static AsyncOperationHandle<IList<TObject>> LoadAssetsAsync<TObject>(IEnumerable keys, Action<TObject> callback, MergeMode mode, bool releaseDependenciesOnFailure) => throw null;
        public static AsyncOperationHandle<IList<TObject>> LoadAssetsAsync<TObject>(object key, Action<TObject> callback) => throw null;
        public static AsyncOperationHandle<IList<TObject>> LoadAssetsAsync<TObject>(object key, Action<TObject> callback, bool releaseDependenciesOnFailure) => throw null;
        public static void Release<TObject>(TObject obj) { }
        public static void Release<TObject>(AsyncOperationHandle<TObject> handle) { }
        public static void Release(AsyncOperationHandle handle) { }
        public static bool ReleaseInstance(GameObject instance) => throw null;
        public static bool ReleaseInstance(AsyncOperationHandle handle) => throw null;
        public static bool ReleaseInstance(AsyncOperationHandle<GameObject> handle) => throw null;
        public static AsyncOperationHandle<long> GetDownloadSizeAsync(object key) => throw null;
        public static AsyncOperationHandle<long> GetDownloadSizeAsync(IEnumerable keys) => throw null;
        public static AsyncOperationHandle DownloadDependenciesAsync(object key, bool autoReleaseHandle = false) => throw null;
        public static AsyncOperationHandle DownloadDependenciesAsync(IList<IResourceLocation> locations, bool autoReleaseHandle = false) => throw null;
        public static AsyncOperationHandle DownloadDependenciesAsync(IEnumerable keys, MergeMode mode, bool autoReleaseHandle = false) => throw null;
        public static void ClearDependencyCacheAsync(object key) { }
        public static AsyncOperationHandle<bool> ClearDependencyCacheAsync(object key, bool autoReleaseHandle) => throw null;
        public static AsyncOperationHandle<GameObject> InstantiateAsync(IResourceLocation location, Transform parent = null, bool instantiateInWorldSpace = false, bool trackHandle = true) => throw null;
        public static AsyncOperationHandle<GameObject> InstantiateAsync(IResourceLocation location, Vector3 position, Quaternion rotation, Transform parent = null, bool trackHandle = true) => throw null;
        public static AsyncOperationHandle<GameObject> InstantiateAsync(object key, Transform parent = null, bool instantiateInWorldSpace = false, bool trackHandle = true) => throw null;
        public static AsyncOperationHandle<GameObject> InstantiateAsync(object key, Vector3 position, Quaternion rotation, Transform parent = null, bool trackHandle = true) => throw null;
        public static AsyncOperationHandle<SceneInstance> LoadSceneAsync(object key, LoadSceneMode loadMode = LoadSceneMode.Single, bool activateOnLoad = true, int priority = 100) => throw null;
        public static AsyncOperationHandle<SceneInstance> LoadSceneAsync(IResourceLocation location, LoadSceneMode loadMode = LoadSceneMode.Single, bool activateOnLoad = true, int priority = 100) => throw null;
        public static AsyncOperationHandle<SceneInstance> UnloadSceneAsync(SceneInstance scene, bool autoReleaseHandle = true) => throw null;
        public static AsyncOperationHandle<SceneInstance> UnloadSceneAsync(AsyncOperationHandle handle, bool autoReleaseHandle = true) => throw null;
        public static AsyncOperationHandle<SceneInstance> UnloadSceneAsync(AsyncOperationHandle<SceneInstance> handle, bool autoReleaseHandle = true) => throw null;
        public static AsyncOperationHandle<List<string>> CheckForCatalogUpdates(bool autoReleaseHandle = true) => throw null;
        public static AsyncOperationHandle<List<IResourceLocator>> UpdateCatalogs(IEnumerable<string> catalogs = null, bool autoReleaseHandle = true) => throw null;
    }

    [Serializable]
    public class AssetReference : IKeyEvaluator
    {
        public AssetReference() { }
        public AssetReference(string guid) { }
        public AsyncOperationHandle OperationHandle { get => throw null; internal set { } }
        public virtual object RuntimeKey => throw null;
        public virtual string AssetGUID => throw null;
        public virtual string SubObjectName { get => throw null; set { } }
        public bool IsDone => throw null;
        public bool IsValid() => throw null;
        public virtual bool RuntimeKeyIsValid() => throw null;
        public virtual Object Asset => throw null;
        public virtual AsyncOperationHandle<TObject> LoadAssetAsync<TObject>() => throw null;
        public virtual AsyncOperationHandle<SceneInstance> LoadSceneAsync(LoadSceneMode loadMode = LoadSceneMode.Single, bool activateOnLoad = true, int priority = 100) => throw null;
        public virtual AsyncOperationHandle<SceneInstance> UnLoadScene() => throw null;
        public virtual AsyncOperationHandle<GameObject> InstantiateAsync(Vector3 position, Quaternion rotation, Transform parent = null) => throw null;
        public virtual AsyncOperationHandle<GameObject> InstantiateAsync(Transform parent = null, bool instantiateInWorldSpace = false) => throw null;
        public virtual void ReleaseAsset() { }
        public virtual void ReleaseInstance(GameObject obj) { }
        public virtual bool ValidateAsset(Object obj) => throw null;
        public virtual bool ValidateAsset(string path) => throw null;
        public override string ToString() => throw null;
    }

    [Serializable]
    public class AssetReferenceT<TObject> : AssetReference where TObject : Object
    {
        public AssetReferenceT(string guid) : base(guid) { }
        public virtual AsyncOperationHandle<TObject> LoadAssetAsync() => throw null;
        public new TObject Asset => throw null;
    }

    [Serializable] public class AssetReferenceGameObject : AssetReferenceT<GameObject> { public AssetReferenceGameObject(string guid) : base(guid) { } }
    [Serializable] public class AssetReferenceTexture2D : AssetReferenceT<Texture2D> { public AssetReferenceTexture2D(string guid) : base(guid) { } }
    [Serializable] public class AssetReferenceSprite : AssetReferenceT<Sprite> { public AssetReferenceSprite(string guid) : base(guid) { } }

    public interface IKeyEvaluator
    {
        object RuntimeKey { get; }
        bool RuntimeKeyIsValid();
    }
}
