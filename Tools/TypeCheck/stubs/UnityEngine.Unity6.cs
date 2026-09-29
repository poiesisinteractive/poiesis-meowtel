// Unity 6-only TYPES of UnityEngine (CoreModule) that do not exist in the 2021.3 reference DLLs.
// New MEMBERS on existing UnityEngine types (e.g. MonoBehaviour.destroyCancellationToken, Rigidbody2D.linearVelocity)
// cannot be added from source: they are injected into patched copies of the reference DLLs by tools/UnityRefPatcher.
// Compile-only. Members marked // GUESS were not verified against the Unity 6 scripting reference.
using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace UnityEngine
{
    [AsyncMethodBuilder(typeof(Awaitable.AwaitableAsyncMethodBuilder))]
    public class Awaitable
    {
        public bool IsCompleted => throw null;
        public void Cancel() { }
        public Awaiter GetAwaiter() => throw null;
        public static Awaitable NextFrameAsync(CancellationToken cancellationToken = default) => throw null;
        public static Awaitable WaitForSecondsAsync(float seconds, CancellationToken cancellationToken = default) => throw null;
        public static Awaitable FixedUpdateAsync(CancellationToken cancellationToken = default) => throw null;
        public static Awaitable EndOfFrameAsync(CancellationToken cancellationToken = default) => throw null;
        public static Awaitable MainThreadAsync() => throw null;
        public static Awaitable BackgroundThreadAsync() => throw null;
        public static Awaitable FromAsyncOperation(AsyncOperation op, CancellationToken cancellationToken = default) => throw null;

        public struct Awaiter : INotifyCompletion
        {
            public bool IsCompleted => throw null;
            public void OnCompleted(Action continuation) { }
            public void GetResult() { }
        }

        public struct AwaitableAsyncMethodBuilder
        {
            public static AwaitableAsyncMethodBuilder Create() => throw null;
            public Awaitable Task => throw null;
            public void SetResult() { }
            public void SetException(Exception e) { }
            public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine { }
            public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
                where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine { }
            public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
                where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine { }
            public void SetStateMachine(IAsyncStateMachine stateMachine) { }
        }
    }

    [AsyncMethodBuilder(typeof(Awaitable<>.AwaitableAsyncMethodBuilder))]
    public class Awaitable<T>
    {
        public bool IsCompleted => throw null; // GUESS
        public void Cancel() { }
        public Awaiter GetAwaiter() => throw null;

        public struct Awaiter : INotifyCompletion
        {
            public bool IsCompleted => throw null;
            public void OnCompleted(Action continuation) { }
            public T GetResult() => throw null;
        }

        public struct AwaitableAsyncMethodBuilder
        {
            public static AwaitableAsyncMethodBuilder Create() => throw null;
            public Awaitable<T> Task => throw null;
            public void SetResult(T result) { }
            public void SetException(Exception e) { }
            public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine { }
            public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
                where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine { }
            public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
                where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine { }
            public void SetStateMachine(IAsyncStateMachine stateMachine) { }
        }
    }

    public class AwaitableCompletionSource
    {
        public Awaitable Awaitable => throw null;
        public void SetResult() { }
        public void SetCanceled() { }
        public void SetException(Exception exception) { }
        public bool TrySetResult() => throw null;
        public bool TrySetCanceled() => throw null;
        public bool TrySetException(Exception exception) => throw null;
        public void Reset() { }
    }

    public class AwaitableCompletionSource<T>
    {
        public Awaitable<T> Awaitable => throw null;
        public void SetResult(in T value) { }
        public void SetCanceled() { }
        public void SetException(Exception exception) { }
        public bool TrySetResult(in T value) => throw null;
        public bool TrySetCanceled() => throw null;
        public bool TrySetException(Exception exception) => throw null;
        public void Reset() { }
    }
}
