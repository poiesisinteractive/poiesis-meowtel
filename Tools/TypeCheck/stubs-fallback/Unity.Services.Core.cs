// STUB of com.unity.services.core 1.x (dependency of Authentication 3.6.1 / Cloud Save 3.4 / Analytics 6.3)
// namespaces Unity.Services.Core, Unity.Services.Core.Environments. Compile-only.
// Members marked // GUESS were not verified against the package source.
using System;
using System.Threading.Tasks;

namespace Unity.Services.Core
{
    public enum ServicesInitializationState { Uninitialized = 0, Initializing = 1, Initialized = 2 }

    public static class UnityServices
    {
        public static ServicesInitializationState State => throw null;
        public static string ExternalUserId { get => throw null; set { } }
        public static event Action Initialized { add { } remove { } }
        public static event Action<Exception> InitializeFailed { add { } remove { } }
        public static Task InitializeAsync() => throw null;
        public static Task InitializeAsync(InitializationOptions options) => throw null;
    }

    public class InitializationOptions
    {
        public InitializationOptions() { }
        public bool TryGetOption(string key, out string option) => throw null;
        public bool TryGetOption(string key, out bool option) => throw null;
        public bool TryGetOption(string key, out int option) => throw null;
        public bool TryGetOption(string key, out float option) => throw null;
        public InitializationOptions SetOption(string key, string value) => throw null;
        public InitializationOptions SetOption(string key, bool value) => throw null;
        public InitializationOptions SetOption(string key, int value) => throw null;
        public InitializationOptions SetOption(string key, float value) => throw null;
    }

    public class RequestFailedException : Exception
    {
        public RequestFailedException(int errorCode, string message) { }
        public RequestFailedException(int errorCode, string message, Exception innerException) { }
        public int ErrorCode => throw null;
    }

    public class ServicesInitializationException : Exception
    {
        public ServicesInitializationException() { }
        public ServicesInitializationException(string message) { }
        public ServicesInitializationException(string message, Exception inner) { }
    }

    public static class CommonErrorCodes
    {
        public const int Unknown = 0;
        public const int TransportError = 1;
        public const int Timeout = 2;
        public const int ServiceUnavailable = 3;
        public const int ApiMissing = 4;
        public const int RequestRejected = 5;
        public const int TooManyRequests = 50;
        public const int InvalidToken = 51;
        public const int TokenExpired = 52;
        public const int Forbidden = 53;
        public const int NotFound = 54;
        public const int InvalidRequest = 55;
    }
}

namespace Unity.Services.Core.Environments
{
    public static class EnvironmentsOptionsExtensions
    {
        public static InitializationOptions SetEnvironmentName(this Unity.Services.Core.InitializationOptions self, string environmentName) => throw null;
    }
}
