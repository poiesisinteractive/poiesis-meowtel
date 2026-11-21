// STUB of com.unity.ugui 2.0 (Unity 6) - namespace UnityEngine.EventSystems.
// Compile-only: bodies are meaningless. Signatures mirror the real package (from ugui 1.0/2.0 public API).
// Members marked // GUESS were not verified against the package source.
using System;
using System.Collections.Generic;
using UnityEngine.Events;

namespace UnityEngine.EventSystems
{
    public abstract class UIBehaviour : MonoBehaviour
    {
        protected UIBehaviour() { }
        protected virtual void Awake() { }
        protected virtual void OnEnable() { }
        protected virtual void Start() { }
        protected virtual void OnDisable() { }
        protected virtual void OnDestroy() { }
        public virtual bool IsActive() => throw null;
#if UNITY_EDITOR
        protected virtual void OnValidate() { }
        protected virtual void Reset() { }
#endif
        protected virtual void OnRectTransformDimensionsChange() { }
        protected virtual void OnBeforeTransformParentChanged() { }
        protected virtual void OnTransformParentChanged() { }
        protected virtual void OnDidApplyAnimationProperties() { }
        protected virtual void OnCanvasGroupChanged() { }
        protected virtual void OnCanvasHierarchyChanged() { }
        public bool IsDestroyed() => throw null;
    }

    public abstract class AbstractEventData
    {
        protected bool m_Used;
        public virtual void Reset() { }
        public virtual void Use() { }
        public virtual bool used => throw null;
    }

    public class BaseEventData : AbstractEventData
    {
        public BaseEventData(EventSystem eventSystem) { }
        public BaseInputModule currentInputModule => throw null;
        public GameObject selectedObject { get => throw null; set { } }
    }

    public enum MoveDirection { Left = 0, Up = 1, Right = 2, Down = 3, None = 4 }

    public class AxisEventData : BaseEventData
    {
        public AxisEventData(EventSystem eventSystem) : base(eventSystem) { }
        public Vector2 moveVector { get => throw null; set { } }
        public MoveDirection moveDir { get => throw null; set { } }
    }

    public class PointerEventData : BaseEventData
    {
        public enum InputButton { Left = 0, Right = 1, Middle = 2 }
        public enum FramePressState { Pressed = 0, Released = 1, PressedAndReleased = 2, NotChanged = 3 }

        public PointerEventData(EventSystem eventSystem) : base(eventSystem) { }

        public List<GameObject> hovered;
        public GameObject pointerEnter { get => throw null; set { } }
        public GameObject lastPress => throw null;
        public GameObject rawPointerPress { get => throw null; set { } }
        public GameObject pointerDrag { get => throw null; set { } }
        public GameObject pointerClick { get => throw null; set { } }
        public RaycastResult pointerCurrentRaycast { get => throw null; set { } }
        public RaycastResult pointerPressRaycast { get => throw null; set { } }
        public bool eligibleForClick { get => throw null; set { } }
        public int displayIndex { get => throw null; set { } }
        public int pointerId { get => throw null; set { } }
        public Vector2 position { get => throw null; set { } }
        public Vector2 delta { get => throw null; set { } }
        public Vector2 pressPosition { get => throw null; set { } }
        public float clickTime { get => throw null; set { } }
        public int clickCount { get => throw null; set { } }
        public Vector2 scrollDelta { get => throw null; set { } }
        public bool useDragThreshold { get => throw null; set { } }
        public bool dragging { get => throw null; set { } }
        public InputButton button { get => throw null; set { } }
        public float pressure { get => throw null; set { } }
        public float tangentialPressure { get => throw null; set { } }
        public float altitudeAngle { get => throw null; set { } }
        public float azimuthAngle { get => throw null; set { } }
        public float twist { get => throw null; set { } }
        public Vector2 radius { get => throw null; set { } }
        public Vector2 radiusVariance { get => throw null; set { } }
        public bool fullyExited { get => throw null; set { } }
        public bool reentered { get => throw null; set { } }
        public Camera enterEventCamera => throw null;
        public Camera pressEventCamera => throw null;
        public GameObject pointerPress { get => throw null; set { } }
        public bool IsPointerMoving() => throw null;
        public bool IsScrolling() => throw null;
    }

    public struct RaycastResult
    {
        public BaseRaycaster module;
        public float distance;
        public float index;
        public int depth;
        public int sortingGroupID;
        public int sortingGroupOrder;
        public int sortingLayer;
        public int sortingOrder;
        public Vector3 worldPosition;
        public Vector3 worldNormal;
        public Vector2 screenPosition;
        public int displayIndex;
        public GameObject gameObject { get => throw null; set { } }
        public bool isValid => throw null;
        public void Clear() { }
    }

    public interface IEventSystemHandler { }
    public interface IPointerMoveHandler : IEventSystemHandler { void OnPointerMove(PointerEventData eventData); }
    public interface IPointerEnterHandler : IEventSystemHandler { void OnPointerEnter(PointerEventData eventData); }
    public interface IPointerExitHandler : IEventSystemHandler { void OnPointerExit(PointerEventData eventData); }
    public interface IPointerDownHandler : IEventSystemHandler { void OnPointerDown(PointerEventData eventData); }
    public interface IPointerUpHandler : IEventSystemHandler { void OnPointerUp(PointerEventData eventData); }
    public interface IPointerClickHandler : IEventSystemHandler { void OnPointerClick(PointerEventData eventData); }
    public interface IInitializePotentialDragHandler : IEventSystemHandler { void OnInitializePotentialDrag(PointerEventData eventData); }
    public interface IBeginDragHandler : IEventSystemHandler { void OnBeginDrag(PointerEventData eventData); }
    public interface IDragHandler : IEventSystemHandler { void OnDrag(PointerEventData eventData); }
    public interface IEndDragHandler : IEventSystemHandler { void OnEndDrag(PointerEventData eventData); }
    public interface IDropHandler : IEventSystemHandler { void OnDrop(PointerEventData eventData); }
    public interface IScrollHandler : IEventSystemHandler { void OnScroll(PointerEventData eventData); }
    public interface IUpdateSelectedHandler : IEventSystemHandler { void OnUpdateSelected(BaseEventData eventData); }
    public interface ISelectHandler : IEventSystemHandler { void OnSelect(BaseEventData eventData); }
    public interface IDeselectHandler : IEventSystemHandler { void OnDeselect(BaseEventData eventData); }
    public interface IMoveHandler : IEventSystemHandler { void OnMove(AxisEventData eventData); }
    public interface ISubmitHandler : IEventSystemHandler { void OnSubmit(BaseEventData eventData); }
    public interface ICancelHandler : IEventSystemHandler { void OnCancel(BaseEventData eventData); }

    public static class ExecuteEvents
    {
        public delegate void EventFunction<T1>(T1 handler, BaseEventData eventData);
        public static T ValidateEventData<T>(BaseEventData data) where T : class => throw null;
        public static EventFunction<IPointerMoveHandler> pointerMoveHandler => throw null;
        public static EventFunction<IPointerEnterHandler> pointerEnterHandler => throw null;
        public static EventFunction<IPointerExitHandler> pointerExitHandler => throw null;
        public static EventFunction<IPointerDownHandler> pointerDownHandler => throw null;
        public static EventFunction<IPointerUpHandler> pointerUpHandler => throw null;
        public static EventFunction<IPointerClickHandler> pointerClickHandler => throw null;
        public static EventFunction<IInitializePotentialDragHandler> initializePotentialDrag => throw null;
        public static EventFunction<IBeginDragHandler> beginDragHandler => throw null;
        public static EventFunction<IDragHandler> dragHandler => throw null;
        public static EventFunction<IEndDragHandler> endDragHandler => throw null;
        public static EventFunction<IDropHandler> dropHandler => throw null;
        public static EventFunction<IScrollHandler> scrollHandler => throw null;
        public static EventFunction<IUpdateSelectedHandler> updateSelectedHandler => throw null;
        public static EventFunction<ISelectHandler> selectHandler => throw null;
        public static EventFunction<IDeselectHandler> deselectHandler => throw null;
        public static EventFunction<IMoveHandler> moveHandler => throw null;
        public static EventFunction<ISubmitHandler> submitHandler => throw null;
        public static EventFunction<ICancelHandler> cancelHandler => throw null;
        public static bool Execute<T>(GameObject target, BaseEventData eventData, EventFunction<T> functor) where T : IEventSystemHandler => throw null;
        public static GameObject ExecuteHierarchy<T>(GameObject root, BaseEventData eventData, EventFunction<T> callbackFunction) where T : IEventSystemHandler => throw null;
        public static bool CanHandleEvent<T>(GameObject go) where T : IEventSystemHandler => throw null;
        public static GameObject GetEventHandler<T>(GameObject root) where T : IEventSystemHandler => throw null;
    }

    public class EventSystem : UIBehaviour
    {
        protected EventSystem() { }
        public static EventSystem current { get => throw null; set { } }
        public bool sendNavigationEvents { get => throw null; set { } }
        public int pixelDragThreshold { get => throw null; set { } }
        public BaseInputModule currentInputModule => throw null;
        public GameObject firstSelectedGameObject { get => throw null; set { } }
        public GameObject currentSelectedGameObject => throw null;
        public bool isFocused => throw null;
        public bool alreadySelecting => throw null;
        public void UpdateModules() { }
        public void SetSelectedGameObject(GameObject selected, BaseEventData pointer) { }
        public void SetSelectedGameObject(GameObject selected) { }
        public void RaycastAll(PointerEventData eventData, List<RaycastResult> raycastResults) { }
        public bool IsPointerOverGameObject() => throw null;
        public bool IsPointerOverGameObject(int pointerId) => throw null;
        protected override void Awake() { }
        protected override void OnEnable() { }
        protected override void OnDisable() { }
        protected virtual void Update() { }
        public override string ToString() => throw null;
    }

    public abstract class BaseInputModule : UIBehaviour
    {
        protected List<RaycastResult> m_RaycastResultCache;
        public BaseInput input => throw null;
        public BaseInput inputOverride { get => throw null; set { } }
        protected EventSystem eventSystem => throw null;
        protected override void OnEnable() { }
        protected override void OnDisable() { }
        public abstract void Process();
        public virtual bool IsPointerOverGameObject(int pointerId) => throw null;
        public virtual bool ShouldActivateModule() => throw null;
        public virtual void DeactivateModule() { }
        public virtual void ActivateModule() { }
        public virtual void UpdateModule() { }
        public virtual bool IsModuleSupported() => throw null;
        public virtual int ConvertUIToolkitPointerId(PointerEventData sourcePointerData) => throw null;
    }

    public class BaseInput : UIBehaviour
    {
        public virtual string compositionString => throw null;
        public virtual IMECompositionMode imeCompositionMode { get => throw null; set { } }
        public virtual Vector2 compositionCursorPos { get => throw null; set { } }
        public virtual bool mousePresent => throw null;
        public virtual bool GetMouseButtonDown(int button) => throw null;
        public virtual bool GetMouseButtonUp(int button) => throw null;
        public virtual bool GetMouseButton(int button) => throw null;
        public virtual Vector2 mousePosition => throw null;
        public virtual Vector2 mouseScrollDelta => throw null;
        public virtual bool touchSupported => throw null;
        public virtual int touchCount => throw null;
        public virtual Touch GetTouch(int index) => throw null;
        public virtual float GetAxisRaw(string axisName) => throw null;
        public virtual bool GetButtonDown(string buttonName) => throw null;
    }

    public abstract class BaseRaycaster : UIBehaviour
    {
        public abstract void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList);
        public abstract Camera eventCamera { get; }
        [Obsolete("Please use sortOrderPriority and renderOrderPriority", false)]
        public virtual int priority => throw null;
        public virtual int sortOrderPriority => throw null;
        public virtual int renderOrderPriority => throw null;
        public BaseRaycaster rootRaycaster => throw null;
        public override string ToString() => throw null;
        protected override void OnEnable() { }
        protected override void OnDisable() { }
        protected override void OnCanvasHierarchyChanged() { }
        protected override void OnTransformParentChanged() { }
    }

    [RequireComponent(typeof(Camera))]
    public class PhysicsRaycaster : BaseRaycaster
    {
        protected PhysicsRaycaster() { }
        public override Camera eventCamera => throw null;
        public virtual int depth => throw null;
        public int finalEventMask => throw null;
        public LayerMask eventMask { get => throw null; set { } }
        public int maxRayIntersections { get => throw null; set { } }
        public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList) { }
    }

    [RequireComponent(typeof(Camera))]
    public class Physics2DRaycaster : PhysicsRaycaster
    {
        protected Physics2DRaycaster() { }
        public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList) { }
    }

    public enum EventTriggerType
    {
        PointerEnter = 0, PointerExit = 1, PointerDown = 2, PointerUp = 3, PointerClick = 4, Drag = 5, Drop = 6, Scroll = 7,
        UpdateSelected = 8, Select = 9, Deselect = 10, Move = 11, InitializePotentialDrag = 12, BeginDrag = 13, EndDrag = 14,
        Submit = 15, Cancel = 16
    }

    public class EventTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler,
        IPointerClickHandler, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler,
        IScrollHandler, IUpdateSelectedHandler, ISelectHandler, IDeselectHandler, IMoveHandler, ISubmitHandler, ICancelHandler
    {
        [Serializable] public class TriggerEvent : UnityEvent<BaseEventData> { }
        [Serializable] public class Entry { public EventTriggerType eventID; public TriggerEvent callback; }
        protected EventTrigger() { }
        public List<Entry> triggers { get => throw null; set { } }
        public virtual void OnPointerEnter(PointerEventData eventData) { }
        public virtual void OnPointerExit(PointerEventData eventData) { }
        public virtual void OnDrag(PointerEventData eventData) { }
        public virtual void OnDrop(PointerEventData eventData) { }
        public virtual void OnPointerDown(PointerEventData eventData) { }
        public virtual void OnPointerUp(PointerEventData eventData) { }
        public virtual void OnPointerClick(PointerEventData eventData) { }
        public virtual void OnSelect(BaseEventData eventData) { }
        public virtual void OnDeselect(BaseEventData eventData) { }
        public virtual void OnScroll(PointerEventData eventData) { }
        public virtual void OnMove(AxisEventData eventData) { }
        public virtual void OnUpdateSelected(BaseEventData eventData) { }
        public virtual void OnInitializePotentialDrag(PointerEventData eventData) { }
        public virtual void OnBeginDrag(PointerEventData eventData) { }
        public virtual void OnEndDrag(PointerEventData eventData) { }
        public virtual void OnSubmit(BaseEventData eventData) { }
        public virtual void OnCancel(BaseEventData eventData) { }
    }
}
