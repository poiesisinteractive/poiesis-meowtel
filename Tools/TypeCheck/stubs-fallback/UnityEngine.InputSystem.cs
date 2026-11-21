// STUB of com.unity.inputsystem 1.17 - namespaces UnityEngine.InputSystem(.Controls/.Utilities/.LowLevel/.EnhancedTouch/.UI).
// Compile-only. Covers device polling (Pointer/Mouse/Touchscreen/Keyboard), controls, actions basics, EnhancedTouch basics.
// Note: UnityEngine.InputSystem.TouchPhase really does clash with UnityEngine.TouchPhase when both namespaces are imported
// (same as in Unity). Members marked // GUESS were not verified against the package source.
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.Utilities
{
    public struct ReadOnlyArray<TValue> : IReadOnlyList<TValue>
    {
        public ReadOnlyArray(TValue[] array) { }
        public ReadOnlyArray(TValue[] array, int index, int length) { }
        public TValue[] ToArray() => throw null;
        public int IndexOf(Predicate<TValue> predicate) => throw null;
        public Enumerator GetEnumerator() => throw null;
        IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator() => throw null;
        IEnumerator IEnumerable.GetEnumerator() => throw null;
        public int Count => throw null;
        public TValue this[int index] => throw null;
        public static implicit operator ReadOnlyArray<TValue>(TValue[] array) => throw null;
        public struct Enumerator : IEnumerator<TValue>
        {
            public bool MoveNext() => throw null;
            public void Reset() { }
            public TValue Current => throw null;
            object IEnumerator.Current => throw null;
            public void Dispose() { }
        }
    }

    public struct InternedString : IEquatable<InternedString>, IComparable<InternedString>
    {
        public InternedString(string text) { }
        public int length => throw null;
        public bool IsEmpty() => throw null;
        public string ToLower() => throw null;
        public bool Equals(InternedString other) => throw null;
        public int CompareTo(InternedString other) => throw null;
        public override string ToString() => throw null;
        public static implicit operator string(InternedString str) => throw null;
    }
}

namespace UnityEngine.InputSystem.LowLevel
{
    public struct TouchState // GUESS: fields trimmed
    {
        public int touchId;
        public Vector2 position;
        public Vector2 delta;
        public float pressure;
        public Vector2 radius;
        public byte phaseId;
        public byte tapCount;
        public double startTime;
        public Vector2 startPosition;
        public TouchPhase phase { get => throw null; set { } }
        public bool isPrimaryTouch { get => throw null; set { } }
        public bool isInProgress => throw null;
    }

    public interface IInputStateCallbackReceiver { }
}

namespace UnityEngine.InputSystem
{
    public enum TouchPhase { None = 0, Began = 1, Moved = 2, Ended = 3, Canceled = 4, Stationary = 5 }

    public enum InputDeviceChange
    {
        Added = 0, Removed = 1, Disconnected = 2, Reconnected = 3, Enabled = 4, Disabled = 5, UsageChanged = 6,
        ConfigurationChanged = 7, SoftReset = 8, HardReset = 9
    }

    public enum InputActionPhase { Disabled = 0, Waiting = 1, Started = 2, Performed = 3, Canceled = 4 }
    public enum InputActionType { Value = 0, Button = 1, PassThrough = 2 }

    public abstract class InputControl
    {
        protected InputControl() { }
        public string name => throw null;
        public string displayName => throw null;
        public string shortDisplayName => throw null;
        public string path => throw null;
        public string layout => throw null;
        public string variants => throw null;
        public InputDevice device => throw null;
        public InputControl parent => throw null;
        public ReadOnlyArray<InputControl> children => throw null;
        public ReadOnlyArray<InternedString> usages => throw null;
        public ReadOnlyArray<InternedString> aliases => throw null;
        public bool noisy => throw null;
        public bool synthetic => throw null;
        public float magnitude => throw null;
        public abstract Type valueType { get; }
        public abstract int valueSizeInBytes { get; }
        public InputControl this[string path] => throw null;
        public float EvaluateMagnitude() => throw null;
        public TControl TryGetChildControl<TControl>(string path) where TControl : InputControl => throw null;
        public TControl GetChildControl<TControl>(string path) where TControl : InputControl => throw null;
        public override string ToString() => throw null;
    }

    public abstract class InputControl<TValue> : InputControl where TValue : struct
    {
        public override Type valueType => throw null;
        public override int valueSizeInBytes => throw null;
        public TValue value => throw null;
        public TValue ReadValue() => throw null;
        public TValue ReadValueFromPreviousFrame() => throw null;
        public TValue ReadDefaultValue() => throw null;
        public TValue ReadUnprocessedValue() => throw null;
    }

    public static class InputControlExtensions // GUESS: subset
    {
        public static bool IsPressed(this InputControl control, float buttonPressPoint = 0) => throw null;
        public static bool IsActuated(this InputControl control, float threshold = 0) => throw null;
        public static object ReadValueAsObject(this InputControl control) => throw null;
    }

    public class InputDevice : InputControl
    {
        public const int InvalidDeviceId = 0;
        public InputDevice() { }
        public int deviceId => throw null;
        public bool added => throw null;
        public bool enabled => throw null;
        public bool canRunInBackground => throw null;
        public bool native => throw null;
        public bool remote => throw null;
        public double lastUpdateTime => throw null;
        public bool wasUpdatedThisFrame => throw null;
        public ReadOnlyArray<InputControl> allControls => throw null;
        public override Type valueType => throw null;
        public override int valueSizeInBytes => throw null;
        public virtual void MakeCurrent() { }
        protected virtual void OnAdded() { }
        protected virtual void OnRemoved() { }
    }

    public class Pointer : InputDevice, IInputStateCallbackReceiver
    {
        public Vector2Control position { get => throw null; protected set { } }
        public DeltaControl delta { get => throw null; protected set { } }
        public Vector2Control radius { get => throw null; protected set { } }
        public AxisControl pressure { get => throw null; protected set { } }
        public ButtonControl press { get => throw null; protected set { } }
        public IntegerControl displayIndex { get => throw null; protected set { } }
        public static Pointer current { get => throw null; internal set { } }
        public override void MakeCurrent() { }
        protected override void OnRemoved() { }
    }

    public class Mouse : Pointer
    {
        public DeltaControl scroll { get => throw null; protected set { } }
        public ButtonControl leftButton { get => throw null; protected set { } }
        public ButtonControl middleButton { get => throw null; protected set { } }
        public ButtonControl rightButton { get => throw null; protected set { } }
        public ButtonControl backButton { get => throw null; protected set { } }
        public ButtonControl forwardButton { get => throw null; protected set { } }
        public IntegerControl clickCount { get => throw null; protected set { } }
        public static new Mouse current { get => throw null; private set { } }
        public override void MakeCurrent() { }
        protected override void OnAdded() { }
        protected override void OnRemoved() { }
        public void WarpCursorPosition(Vector2 position) { }
    }

    public class Pen : Pointer
    {
        public ButtonControl tip { get => throw null; protected set { } }
        public ButtonControl eraser { get => throw null; protected set { } }
        public ButtonControl firstBarrelButton { get => throw null; protected set { } }
        public ButtonControl secondBarrelButton { get => throw null; protected set { } }
        public ButtonControl inRange { get => throw null; protected set { } }
        public Vector2Control tilt { get => throw null; protected set { } }
        public AxisControl twist { get => throw null; protected set { } }
        public static new Pen current { get => throw null; internal set { } }
    }

    public class Touchscreen : Pointer, IInputStateCallbackReceiver
    {
        public TouchControl primaryTouch { get => throw null; protected set { } }
        public ReadOnlyArray<TouchControl> touches { get => throw null; protected set { } }
        public static new Touchscreen current { get => throw null; internal set { } }
        public override void MakeCurrent() { }
        protected override void OnRemoved() { }
    }

    public enum Key
    {
        None = 0, Space, Enter, Tab, Backquote, Quote, Semicolon, Comma, Period, Slash, Backslash, LeftBracket, RightBracket, Minus, Equals,
        A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z,
        Digit1, Digit2, Digit3, Digit4, Digit5, Digit6, Digit7, Digit8, Digit9, Digit0,
        LeftShift, RightShift, LeftAlt, RightAlt, AltGr = RightAlt, LeftCtrl, RightCtrl, LeftMeta, RightMeta,
        LeftWindows = LeftMeta, RightWindows = RightMeta, LeftApple = LeftMeta, RightApple = RightMeta, LeftCommand = LeftMeta, RightCommand = RightMeta,
        ContextMenu, Escape, LeftArrow, RightArrow, UpArrow, DownArrow, Backspace, PageDown, PageUp, Home, End, Insert, Delete,
        CapsLock, NumLock, PrintScreen, ScrollLock, Pause,
        NumpadEnter, NumpadDivide, NumpadMultiply, NumpadPlus, NumpadMinus, NumpadPeriod, NumpadEquals,
        Numpad0, Numpad1, Numpad2, Numpad3, Numpad4, Numpad5, Numpad6, Numpad7, Numpad8, Numpad9,
        F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,
        OEM1, OEM2, OEM3, OEM4, OEM5, IMESelected
    }

    public class Keyboard : InputDevice, ITextInputReceiver
    {
        public const int KeyCount = 110; // GUESS
        public event Action<char> onTextInput { add { } remove { } }
        public event Action<IMECompositionString> onIMECompositionChange { add { } remove { } }
        public void SetIMEEnabled(bool enabled) { }
        public void SetIMECursorPosition(Vector2 position) { }
        public string keyboardLayout { get => throw null; protected set { } }
        public AnyKeyControl anyKey { get => throw null; protected set { } }
        public KeyControl spaceKey => throw null;
        public KeyControl enterKey => throw null;
        public KeyControl tabKey => throw null;
        public KeyControl backquoteKey => throw null;
        public KeyControl quoteKey => throw null;
        public KeyControl semicolonKey => throw null;
        public KeyControl commaKey => throw null;
        public KeyControl periodKey => throw null;
        public KeyControl slashKey => throw null;
        public KeyControl backslashKey => throw null;
        public KeyControl leftBracketKey => throw null;
        public KeyControl rightBracketKey => throw null;
        public KeyControl minusKey => throw null;
        public KeyControl equalsKey => throw null;
        public KeyControl aKey => throw null;
        public KeyControl bKey => throw null;
        public KeyControl cKey => throw null;
        public KeyControl dKey => throw null;
        public KeyControl eKey => throw null;
        public KeyControl fKey => throw null;
        public KeyControl gKey => throw null;
        public KeyControl hKey => throw null;
        public KeyControl iKey => throw null;
        public KeyControl jKey => throw null;
        public KeyControl kKey => throw null;
        public KeyControl lKey => throw null;
        public KeyControl mKey => throw null;
        public KeyControl nKey => throw null;
        public KeyControl oKey => throw null;
        public KeyControl pKey => throw null;
        public KeyControl qKey => throw null;
        public KeyControl rKey => throw null;
        public KeyControl sKey => throw null;
        public KeyControl tKey => throw null;
        public KeyControl uKey => throw null;
        public KeyControl vKey => throw null;
        public KeyControl wKey => throw null;
        public KeyControl xKey => throw null;
        public KeyControl yKey => throw null;
        public KeyControl zKey => throw null;
        public KeyControl digit1Key => throw null;
        public KeyControl digit2Key => throw null;
        public KeyControl digit3Key => throw null;
        public KeyControl digit4Key => throw null;
        public KeyControl digit5Key => throw null;
        public KeyControl digit6Key => throw null;
        public KeyControl digit7Key => throw null;
        public KeyControl digit8Key => throw null;
        public KeyControl digit9Key => throw null;
        public KeyControl digit0Key => throw null;
        public KeyControl leftShiftKey => throw null;
        public KeyControl rightShiftKey => throw null;
        public KeyControl leftAltKey => throw null;
        public KeyControl rightAltKey => throw null;
        public KeyControl leftCtrlKey => throw null;
        public KeyControl rightCtrlKey => throw null;
        public KeyControl leftMetaKey => throw null;
        public KeyControl rightMetaKey => throw null;
        public KeyControl leftWindowsKey => throw null;
        public KeyControl rightWindowsKey => throw null;
        public KeyControl leftAppleKey => throw null;
        public KeyControl rightAppleKey => throw null;
        public KeyControl leftCommandKey => throw null;
        public KeyControl rightCommandKey => throw null;
        public KeyControl contextMenuKey => throw null;
        public KeyControl escapeKey => throw null;
        public KeyControl leftArrowKey => throw null;
        public KeyControl rightArrowKey => throw null;
        public KeyControl upArrowKey => throw null;
        public KeyControl downArrowKey => throw null;
        public KeyControl backspaceKey => throw null;
        public KeyControl pageDownKey => throw null;
        public KeyControl pageUpKey => throw null;
        public KeyControl homeKey => throw null;
        public KeyControl endKey => throw null;
        public KeyControl insertKey => throw null;
        public KeyControl deleteKey => throw null;
        public KeyControl capsLockKey => throw null;
        public KeyControl scrollLockKey => throw null;
        public KeyControl numLockKey => throw null;
        public KeyControl printScreenKey => throw null;
        public KeyControl pauseKey => throw null;
        public KeyControl numpadEnterKey => throw null;
        public KeyControl numpadDivideKey => throw null;
        public KeyControl numpadMultiplyKey => throw null;
        public KeyControl numpadMinusKey => throw null;
        public KeyControl numpadPlusKey => throw null;
        public KeyControl numpadPeriodKey => throw null;
        public KeyControl numpadEqualsKey => throw null;
        public KeyControl numpad0Key => throw null;
        public KeyControl numpad1Key => throw null;
        public KeyControl numpad2Key => throw null;
        public KeyControl numpad3Key => throw null;
        public KeyControl numpad4Key => throw null;
        public KeyControl numpad5Key => throw null;
        public KeyControl numpad6Key => throw null;
        public KeyControl numpad7Key => throw null;
        public KeyControl numpad8Key => throw null;
        public KeyControl numpad9Key => throw null;
        public KeyControl f1Key => throw null;
        public KeyControl f2Key => throw null;
        public KeyControl f3Key => throw null;
        public KeyControl f4Key => throw null;
        public KeyControl f5Key => throw null;
        public KeyControl f6Key => throw null;
        public KeyControl f7Key => throw null;
        public KeyControl f8Key => throw null;
        public KeyControl f9Key => throw null;
        public KeyControl f10Key => throw null;
        public KeyControl f11Key => throw null;
        public KeyControl f12Key => throw null;
        public KeyControl oem1Key => throw null;
        public KeyControl oem2Key => throw null;
        public KeyControl oem3Key => throw null;
        public KeyControl oem4Key => throw null;
        public KeyControl oem5Key => throw null;
        public ButtonControl shiftKey { get => throw null; protected set { } }
        public ButtonControl ctrlKey { get => throw null; protected set { } }
        public ButtonControl altKey { get => throw null; protected set { } }
        public ButtonControl imeSelected { get => throw null; protected set { } }
        public KeyControl this[Key key] => throw null;
        public ReadOnlyArray<KeyControl> allKeys => throw null;
        public static Keyboard current { get => throw null; private set { } }
        public override void MakeCurrent() { }
        protected override void OnRemoved() { }
        public void OnTextInput(char character) { }
        public KeyControl FindKeyOnCurrentKeyboardLayout(string displayName) => throw null;
        public void OnIMECompositionChanged(IMECompositionString compositionString) { }
    }

    public interface ITextInputReceiver
    {
        void OnTextInput(char character);
        void OnIMECompositionChanged(IMECompositionString compositionString);
    }

    public struct IMECompositionString : IEnumerable<char>
    {
        public int Count => throw null;
        public char this[int index] => throw null;
        public override string ToString() => throw null;
        public IEnumerator<char> GetEnumerator() => throw null;
        IEnumerator IEnumerable.GetEnumerator() => throw null;
    }

    public class Gamepad : InputDevice
    {
        public ButtonControl buttonWest { get => throw null; protected set { } }
        public ButtonControl buttonNorth { get => throw null; protected set { } }
        public ButtonControl buttonSouth { get => throw null; protected set { } }
        public ButtonControl buttonEast { get => throw null; protected set { } }
        public ButtonControl leftStickButton { get => throw null; protected set { } }
        public ButtonControl rightStickButton { get => throw null; protected set { } }
        public ButtonControl startButton { get => throw null; protected set { } }
        public ButtonControl selectButton { get => throw null; protected set { } }
        public DpadControl dpad { get => throw null; protected set { } }
        public ButtonControl leftShoulder { get => throw null; protected set { } }
        public ButtonControl rightShoulder { get => throw null; protected set { } }
        public StickControl leftStick { get => throw null; protected set { } }
        public StickControl rightStick { get => throw null; protected set { } }
        public ButtonControl leftTrigger { get => throw null; protected set { } }
        public ButtonControl rightTrigger { get => throw null; protected set { } }
        public ButtonControl aButton => throw null;
        public ButtonControl bButton => throw null;
        public ButtonControl xButton => throw null;
        public ButtonControl yButton => throw null;
        public static Gamepad current { get => throw null; private set { } }
        public static ReadOnlyArray<Gamepad> all => throw null;
        public override void MakeCurrent() { }
        public virtual void SetMotorSpeeds(float lowFrequency, float highFrequency) { } // GUESS
    }

    public class Joystick : InputDevice
    {
        public ButtonControl trigger { get => throw null; protected set { } }
        public StickControl stick { get => throw null; protected set { } }
        public static Joystick current { get => throw null; private set { } }
    }

    public class TrackedDevice : InputDevice { }
    public abstract class Sensor : InputDevice { public float samplingFrequency { get => throw null; set { } } }
    public class Accelerometer : Sensor { public Vector3Control acceleration { get => throw null; protected set { } } public static Accelerometer current { get => throw null; private set { } } }
    public class Gyroscope : Sensor { public Vector3Control angularVelocity { get => throw null; protected set { } } public static Gyroscope current { get => throw null; private set { } } }
    public class GravitySensor : Sensor { public Vector3Control gravity { get => throw null; protected set { } } public static GravitySensor current { get => throw null; private set { } } }
    public class AttitudeSensor : Sensor { public QuaternionControl attitude { get => throw null; protected set { } } public static AttitudeSensor current { get => throw null; private set { } } }
    public class LinearAccelerationSensor : Sensor { public Vector3Control acceleration { get => throw null; protected set { } } public static LinearAccelerationSensor current { get => throw null; private set { } } }

    public static class InputSystem
    {
        public static ReadOnlyArray<InputDevice> devices => throw null;
        public static ReadOnlyArray<InputDevice> disconnectedDevices => throw null;
        public static event Action<InputDevice, InputDeviceChange> onDeviceChange { add { } remove { } }
        public static event Action onBeforeUpdate { add { } remove { } }
        public static event Action onAfterUpdate { add { } remove { } }
        public static event Action<object, InputActionChange> onActionChange { add { } remove { } }
        public static string version => throw null;
        public static float pollingFrequency { get => throw null; set { } }
        public static void Update() { }
        public static void EnableDevice(InputDevice device) { }
        public static void DisableDevice(InputDevice device, bool keepSendingEvents = false) { }
        public static TDevice GetDevice<TDevice>() where TDevice : InputDevice => throw null;
        public static InputDevice GetDeviceById(int deviceId) => throw null;
        public static TDevice AddDevice<TDevice>(string name = null) where TDevice : InputDevice => throw null;
        public static void RemoveDevice(InputDevice device) { }
        public static void ResetDevice(InputDevice device, bool alsoResetDontResetControls = false) { }
    }

    public enum InputActionChange
    {
        ActionEnabled = 0, ActionDisabled = 1, ActionMapEnabled = 2, ActionMapDisabled = 3, ActionStarted = 4, ActionPerformed = 5,
        ActionCanceled = 6, BoundControlsAboutToChange = 7, BoundControlsChanged = 8
    }

    [Serializable]
    public sealed class InputAction : ICloneable, IDisposable
    {
        public InputAction(string name = null, InputActionType type = InputActionType.Value, string binding = null, string interactions = null, string processors = null, string expectedControlType = null) { }
        public string name => throw null;
        public InputActionType type => throw null;
        public Guid id => throw null;
        public string expectedControlType { get => throw null; set { } }
        public string processors => throw null;
        public string interactions => throw null;
        public InputActionMap actionMap => throw null;
        public ReadOnlyArray<InputControl> controls => throw null;
        public InputActionPhase phase => throw null;
        public bool inProgress => throw null;
        public bool enabled => throw null;
        public bool triggered => throw null;
        public InputControl activeControl => throw null;
        public Type activeValueType => throw null;
        public bool wantsInitialStateCheck { get => throw null; set { } }
        public event Action<CallbackContext> started { add { } remove { } }
        public event Action<CallbackContext> canceled { add { } remove { } }
        public event Action<CallbackContext> performed { add { } remove { } }
        public void Enable() { }
        public void Disable() { }
        public InputAction Clone() => throw null;
        object ICloneable.Clone() => throw null;
        public TValue ReadValue<TValue>() where TValue : struct => throw null;
        public object ReadValueAsObject() => throw null;
        public float GetControlMagnitude() => throw null;
        public void Reset() { }
        public bool IsPressed() => throw null;
        public bool IsInProgress() => throw null;
        public bool WasPressedThisFrame() => throw null;
        public bool WasReleasedThisFrame() => throw null;
        public bool WasPerformedThisFrame() => throw null;
        public bool WasCompletedThisFrame() => throw null;
        public bool WasPressedThisDynamicUpdate() => throw null; // GUESS
        public float GetTimeoutCompletionPercentage() => throw null;
        public void Dispose() { }
        public override string ToString() => throw null;

        public struct CallbackContext
        {
            public InputActionPhase phase => throw null;
            public bool started => throw null;
            public bool performed => throw null;
            public bool canceled => throw null;
            public InputAction action => throw null;
            public InputControl control => throw null;
            public double time => throw null;
            public double startTime => throw null;
            public double duration => throw null;
            public Type valueType => throw null;
            public int valueSizeInBytes => throw null;
            public TValue ReadValue<TValue>() where TValue : struct => throw null;
            public bool ReadValueAsButton() => throw null;
            public object ReadValueAsObject() => throw null;
            public override string ToString() => throw null;
        }
    }

    [Serializable]
    public sealed class InputActionMap : ICloneable, IDisposable, IEnumerable<InputAction>
    {
        public InputActionMap(string name = null) { }
        public string name => throw null;
        public InputActionAsset asset => throw null;
        public Guid id => throw null;
        public bool enabled => throw null;
        public ReadOnlyArray<InputAction> actions => throw null;
        public InputAction this[string actionNameOrId] => throw null;
        public event Action<InputAction.CallbackContext> actionTriggered { add { } remove { } }
        public InputAction FindAction(string actionNameOrId, bool throwIfNotFound = false) => throw null;
        public void Enable() { }
        public void Disable() { }
        public InputActionMap Clone() => throw null;
        object ICloneable.Clone() => throw null;
        public void Dispose() { }
        public IEnumerator<InputAction> GetEnumerator() => throw null;
        IEnumerator IEnumerable.GetEnumerator() => throw null;
    }

    public class InputActionAsset : ScriptableObject, IEnumerable<InputAction>
    {
        public const string Extension = "inputactions";
        public bool enabled => throw null;
        public ReadOnlyArray<InputActionMap> actionMaps => throw null;
        public InputAction this[string actionNameOrId] => throw null;
        public string ToJson() => throw null;
        public void LoadFromJson(string json) { }
        public static InputActionAsset FromJson(string json) => throw null;
        public InputAction FindAction(string actionNameOrId, bool throwIfNotFound = false) => throw null;
        public InputActionMap FindActionMap(string nameOrId, bool throwIfNotFound = false) => throw null;
        public void Enable() { }
        public void Disable() { }
        public IEnumerator<InputAction> GetEnumerator() => throw null;
        IEnumerator IEnumerable.GetEnumerator() => throw null;
    }

    public class InputActionReference : ScriptableObject
    {
        public InputActionAsset asset => throw null;
        public InputAction action => throw null;
        public void Set(InputAction action) { }
        public void Set(InputActionAsset asset, string mapName, string actionName) { }
        public static InputActionReference Create(InputAction action) => throw null;
        public override string ToString() => throw null;
        public static implicit operator InputAction(InputActionReference reference) => throw null;
    }

    [Serializable]
    public struct InputActionProperty : IEquatable<InputActionProperty>
    {
        public InputActionProperty(InputAction action) { }
        public InputActionProperty(InputActionReference reference) { }
        public InputAction action => throw null;
        public InputActionReference reference => throw null;
        public bool Equals(InputActionProperty other) => throw null;
    }
}

namespace UnityEngine.InputSystem.Controls
{
    public class AxisControl : InputControl<float>
    {
        public enum Clamp { None = 0, BeforeNormalize = 1, AfterNormalize = 2, ToConstantBeforeNormalize = 3 }
        public Clamp clamp;
        public float clampMin;
        public float clampMax;
        public float clampConstant;
        public bool invert;
        public bool normalize;
        public float normalizeMin;
        public float normalizeMax;
        public float normalizeZero;
        public bool scale;
        public float scaleFactor;
        public AxisControl() { }
    }

    public class ButtonControl : AxisControl
    {
        public float pressPoint;
        public static float s_GlobalDefaultButtonPressPoint;
        public float pressPointOrDefault => throw null;
        public ButtonControl() { }
        public bool IsValueConsideredPressed(float value) => throw null;
        public bool isPressed => throw null;
        public bool wasPressedThisFrame => throw null;
        public bool wasReleasedThisFrame => throw null;
    }

    public class KeyControl : ButtonControl
    {
        public Key keyCode { get => throw null; set { } }
        public int scanCode => throw null;
    }

    public class AnyKeyControl : ButtonControl { }
    public class TouchPressControl : ButtonControl { }
    public class DiscreteButtonControl : ButtonControl { }

    public class IntegerControl : InputControl<int> { }
    public class DoubleControl : InputControl<double> { }

    public class TouchPhaseControl : InputControl<TouchPhase> { }

    public class Vector2Control : InputControl<Vector2>
    {
        public AxisControl x { get => throw null; set { } }
        public AxisControl y { get => throw null; set { } }
    }

    public class DeltaControl : Vector2Control
    {
        public AxisControl up { get => throw null; set { } }
        public AxisControl down { get => throw null; set { } }
        public AxisControl left { get => throw null; set { } }
        public AxisControl right { get => throw null; set { } }
    }

    public class StickControl : Vector2Control
    {
        public ButtonControl up { get => throw null; set { } }
        public ButtonControl down { get => throw null; set { } }
        public ButtonControl left { get => throw null; set { } }
        public ButtonControl right { get => throw null; set { } }
    }

    public class DpadControl : Vector2Control
    {
        public ButtonControl up { get => throw null; set { } }
        public ButtonControl down { get => throw null; set { } }
        public ButtonControl left { get => throw null; set { } }
        public ButtonControl right { get => throw null; set { } }
    }

    public class Vector3Control : InputControl<Vector3>
    {
        public AxisControl x { get => throw null; set { } }
        public AxisControl y { get => throw null; set { } }
        public AxisControl z { get => throw null; set { } }
    }

    public class QuaternionControl : InputControl<Quaternion> { }

    public class TouchControl : InputControl<TouchState>
    {
        public TouchPressControl press { get => throw null; set { } }
        public IntegerControl displayIndex { get => throw null; set { } }
        public IntegerControl touchId { get => throw null; set { } }
        public Vector2Control position { get => throw null; set { } }
        public DeltaControl delta { get => throw null; set { } }
        public AxisControl pressure { get => throw null; set { } }
        public Vector2Control radius { get => throw null; set { } }
        public TouchPhaseControl phase { get => throw null; set { } }
        public ButtonControl indirectTouch { get => throw null; set { } }
        public ButtonControl tap { get => throw null; set { } }
        public IntegerControl tapCount { get => throw null; set { } }
        public DoubleControl startTime { get => throw null; set { } }
        public Vector2Control startPosition { get => throw null; set { } }
        public bool isInProgress => throw null;
    }
}

namespace UnityEngine.InputSystem.EnhancedTouch
{
    public static class EnhancedTouchSupport
    {
        public static bool enabled => throw null;
        public static void Enable() { }
        public static void Disable() { }
    }

    public struct Touch : IEquatable<Touch>
    {
        public bool valid => throw null;
        public Finger finger => throw null;
        public TouchPhase phase => throw null;
        public bool began => throw null;
        public bool inProgress => throw null;
        public bool ended => throw null;
        public int touchId => throw null;
        public float pressure => throw null;
        public Vector2 radius => throw null;
        public double startTime => throw null;
        public double time => throw null;
        public Touchscreen screen => throw null;
        public Vector2 screenPosition => throw null;
        public Vector2 startScreenPosition => throw null;
        public Vector2 delta => throw null;
        public int tapCount => throw null;
        public bool isTap => throw null;
        public int displayIndex => throw null;
        public bool isInProgress => throw null;
        public static ReadOnlyArray<Touch> activeTouches => throw null;
        public static ReadOnlyArray<Finger> fingers => throw null;
        public static ReadOnlyArray<Finger> activeFingers => throw null;
        public static IEnumerable<Touchscreen> screens => throw null;
        public static event Action<Finger> onFingerDown { add { } remove { } }
        public static event Action<Finger> onFingerUp { add { } remove { } }
        public static event Action<Finger> onFingerMove { add { } remove { } }
        public bool Equals(Touch other) => throw null;
    }

    public class Finger
    {
        public Touchscreen screen => throw null;
        public int index => throw null;
        public bool isActive => throw null;
        public Vector2 screenPosition => throw null;
        public Touch lastTouch => throw null;
        public Touch currentTouch => throw null;
    }
}

namespace UnityEngine.InputSystem.UI
{
    public class InputSystemUIInputModule : UnityEngine.EventSystems.BaseInputModule
    {
        public override void Process() { }
        public InputActionAsset actionsAsset { get => throw null; set { } }
        public InputActionReference point { get => throw null; set { } }
        public InputActionReference leftClick { get => throw null; set { } }
        public InputActionReference scrollWheel { get => throw null; set { } }
        public InputActionReference submit { get => throw null; set { } }
        public InputActionReference cancel { get => throw null; set { } }
        public InputActionReference move { get => throw null; set { } }
        public void AssignDefaultActions() { }
        public void UnassignActions() { }
    }
}
