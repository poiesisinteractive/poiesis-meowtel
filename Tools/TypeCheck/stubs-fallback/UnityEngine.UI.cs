// STUB of com.unity.ugui 2.0 (Unity 6) - namespace UnityEngine.UI (runtime assembly "UnityEngine.UI").
// Compile-only. Signatures mirror the real ugui public API; the legacy InputField and Dropdown are NOT stubbed.
// Members marked // GUESS were not verified against the package source.
using System;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace UnityEngine.UI
{
    public enum CanvasUpdate { Prelayout = 0, Layout = 1, PostLayout = 2, PreRender = 3, LatePreRender = 4, MaxUpdateValue = 5 }

    public interface ICanvasElement
    {
        void Rebuild(CanvasUpdate executing);
        Transform transform { get; }
        void LayoutComplete();
        void GraphicUpdateComplete();
        bool IsDestroyed();
    }

    public interface IClippable
    {
        GameObject gameObject { get; }
        void RecalculateClipping();
        RectTransform rectTransform { get; }
        void Cull(Rect clipRect, bool validRect);
        void SetClipRect(Rect value, bool validRect);
        void SetClipSoftness(Vector2 clipSoftness);
    }
    public interface IClipper { void PerformClipping(); }
    public interface IMaskable { void RecalculateMasking(); }
    public interface IMaterialModifier { Material GetModifiedMaterial(Material baseMaterial); }
    public interface IMeshModifier
    {
        [Obsolete("use IMeshModifier.ModifyMesh (VertexHelper verts) instead", false)]
        void ModifyMesh(Mesh mesh);
        void ModifyMesh(VertexHelper verts);
    }

    public interface ILayoutElement
    {
        void CalculateLayoutInputHorizontal();
        void CalculateLayoutInputVertical();
        float minWidth { get; }
        float preferredWidth { get; }
        float flexibleWidth { get; }
        float minHeight { get; }
        float preferredHeight { get; }
        float flexibleHeight { get; }
        int layoutPriority { get; }
    }
    public interface ILayoutController { void SetLayoutHorizontal(); void SetLayoutVertical(); }
    public interface ILayoutGroup : ILayoutController { }
    public interface ILayoutSelfController : ILayoutController { }
    public interface ILayoutIgnorer { bool ignoreLayout { get; } }

    public class VertexHelper : IDisposable
    {
        public VertexHelper() { }
        public VertexHelper(Mesh m) { }
        public void Clear() { }
        public int currentVertCount => throw null;
        public int currentIndexCount => throw null;
        public void PopulateUIVertex(ref UIVertex vertex, int i) { }
        public void SetUIVertex(UIVertex vertex, int i) { }
        public void FillMesh(Mesh mesh) { }
        public void Dispose() { }
        public void AddVert(Vector3 position, Color32 color, Vector4 uv0, Vector4 uv1, Vector4 uv2, Vector4 uv3, Vector3 normal, Vector4 tangent) { }
        public void AddVert(Vector3 position, Color32 color, Vector4 uv0, Vector4 uv1, Vector3 normal, Vector4 tangent) { }
        public void AddVert(Vector3 position, Color32 color, Vector4 uv0) { }
        public void AddVert(UIVertex v) { }
        public void AddTriangle(int idx0, int idx1, int idx2) { }
        public void AddUIVertexQuad(UIVertex[] verts) { }
        public void AddUIVertexStream(List<UIVertex> verts, List<int> indices) { }
        public void AddUIVertexTriangleStream(List<UIVertex> verts) { }
        public void GetUIVertexStream(List<UIVertex> stream) { }
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [ExecuteAlways]
    public abstract class Graphic : UIBehaviour, ICanvasElement
    {
        protected static Material s_DefaultUI;
        protected static Texture2D s_WhiteTexture;
        protected static Mesh s_Mesh;
        protected bool useLegacyMeshGeneration { get => throw null; set { } }
        protected Graphic() { }
        public static Material defaultGraphicMaterial => throw null;
        public virtual Color color { get => throw null; set { } }
        public virtual bool raycastTarget { get => throw null; set { } }
        public Vector4 raycastPadding { get => throw null; set { } }
        public int depth => throw null;
        public RectTransform rectTransform => throw null;
        public Canvas canvas => throw null;
        public CanvasRenderer canvasRenderer => throw null;
        public virtual Material defaultMaterial => throw null;
        public virtual Material material { get => throw null; set { } }
        public virtual Material materialForRendering => throw null;
        public virtual Texture mainTexture => throw null;
        public virtual void SetAllDirty() { }
        public virtual void SetLayoutDirty() { }
        public virtual void SetVerticesDirty() { }
        public virtual void SetMaterialDirty() { }
        public void SetRaycastDirty() { }
        protected override void OnRectTransformDimensionsChange() { }
        protected override void OnBeforeTransformParentChanged() { }
        protected override void OnTransformParentChanged() { }
        protected override void OnEnable() { }
        protected override void OnDisable() { }
        protected override void OnDestroy() { }
        protected override void OnCanvasHierarchyChanged() { }
        public virtual void OnCullingChanged() { }
        public virtual void Rebuild(CanvasUpdate update) { }
        public virtual void LayoutComplete() { }
        public virtual void GraphicUpdateComplete() { }
        protected virtual void UpdateMaterial() { }
        protected virtual void UpdateGeometry() { }
        protected virtual void OnPopulateMesh(VertexHelper vh) { }
        [Obsolete("Use OnPopulateMesh(VertexHelper vh) instead.", false)]
        protected virtual void OnPopulateMesh(Mesh m) { }
        protected override void OnDidApplyAnimationProperties() { }
        public virtual void SetNativeSize() { }
        public virtual bool Raycast(Vector2 sp, Camera eventCamera) => throw null;
        public Vector2 PixelAdjustPoint(Vector2 point) => throw null;
        public Rect GetPixelAdjustedRect() => throw null;
        public virtual void CrossFadeColor(Color targetColor, float duration, bool ignoreTimeScale, bool useAlpha) { }
        public virtual void CrossFadeColor(Color targetColor, float duration, bool ignoreTimeScale, bool useAlpha, bool useRGB) { }
        public virtual void CrossFadeAlpha(float alpha, float duration, bool ignoreTimeScale) { }
        public void RegisterDirtyLayoutCallback(UnityAction action) { }
        public void UnregisterDirtyLayoutCallback(UnityAction action) { }
        public void RegisterDirtyVerticesCallback(UnityAction action) { }
        public void UnregisterDirtyVerticesCallback(UnityAction action) { }
        public void RegisterDirtyMaterialCallback(UnityAction action) { }
        public void UnregisterDirtyMaterialCallback(UnityAction action) { }
#if UNITY_EDITOR
        protected override void OnValidate() { }
        protected override void Reset() { }
#endif
    }

    public abstract class MaskableGraphic : Graphic, IClippable, IMaskable, IMaterialModifier
    {
        [Serializable] public class CullStateChangedEvent : UnityEvent<bool> { }
        protected bool m_ShouldRecalculateStencil;
        protected Material m_MaskMaterial;
        protected int m_StencilValue;
        protected MaskableGraphic() { }
        public CullStateChangedEvent onCullStateChanged { get => throw null; set { } }
        public bool maskable { get => throw null; set { } }
        public bool isMaskingGraphic { get => throw null; set { } }
        public virtual Material GetModifiedMaterial(Material baseMaterial) => throw null;
        public virtual void Cull(Rect clipRect, bool validRect) { }
        public virtual void SetClipRect(Rect clipRect, bool validRect) { }
        public virtual void SetClipSoftness(Vector2 clipSoftness) { }
        protected override void OnEnable() { }
        protected override void OnDisable() { }
        protected override void OnTransformParentChanged() { }
        [Obsolete("Not used anymore.", true)]
        public virtual void ParentMaskStateChanged() { }
        protected override void OnCanvasHierarchyChanged() { }
        public virtual void RecalculateClipping() { }
        public virtual void RecalculateMasking() { }
    }

    [RequireComponent(typeof(CanvasRenderer))]
    public class Image : MaskableGraphic, ISerializationCallbackReceiver, ILayoutElement, ICanvasRaycastFilter
    {
        public enum Type { Simple = 0, Sliced = 1, Tiled = 2, Filled = 3 }
        public enum FillMethod { Horizontal = 0, Vertical = 1, Radial90 = 2, Radial180 = 3, Radial360 = 4 }
        public enum OriginHorizontal { Left = 0, Right = 1 }
        public enum OriginVertical { Bottom = 0, Top = 1 }
        public enum Origin90 { BottomLeft = 0, TopLeft = 1, TopRight = 2, BottomRight = 3 }
        public enum Origin180 { Bottom = 0, Left = 1, Top = 2, Right = 3 }
        public enum Origin360 { Bottom = 0, Right = 1, Top = 2, Left = 3 }

        protected Image() { }
        protected static Material s_ETC1DefaultUI;
        public Sprite sprite { get => throw null; set { } }
        public Sprite overrideSprite { get => throw null; set { } }
        public Type type { get => throw null; set { } }
        public bool preserveAspect { get => throw null; set { } }
        public bool fillCenter { get => throw null; set { } }
        public FillMethod fillMethod { get => throw null; set { } }
        public float fillAmount { get => throw null; set { } }
        public bool fillClockwise { get => throw null; set { } }
        public int fillOrigin { get => throw null; set { } }
        [Obsolete("eventAlphaThreshold has been deprecated. Use eventMinimumAlphaThreshold instead (UnityUpgradable) -> alphaHitTestMinimumThreshold")]
        public float eventAlphaThreshold { get => throw null; set { } }
        public float alphaHitTestMinimumThreshold { get => throw null; set { } }
        public bool useSpriteMesh { get => throw null; set { } }
        public static Material defaultETC1GraphicMaterial => throw null;
        public override Texture mainTexture => throw null;
        public bool hasBorder => throw null;
        public float pixelsPerUnitMultiplier { get => throw null; set { } }
        public float pixelsPerUnit => throw null;
        protected float multipliedPixelsPerUnit => throw null;
        public override Material material { get => throw null; set { } }
        public void DisableSpriteOptimizations() { }
        public virtual void OnBeforeSerialize() { }
        public virtual void OnAfterDeserialize() { }
        public override void SetNativeSize() { }
        protected override void OnPopulateMesh(VertexHelper toFill) { }
        protected override void UpdateMaterial() { }
        protected override void OnCanvasHierarchyChanged() { }
        public virtual void CalculateLayoutInputHorizontal() { }
        public virtual void CalculateLayoutInputVertical() { }
        public virtual float minWidth => throw null;
        public virtual float preferredWidth => throw null;
        public virtual float flexibleWidth => throw null;
        public virtual float minHeight => throw null;
        public virtual float preferredHeight => throw null;
        public virtual float flexibleHeight => throw null;
        public virtual int layoutPriority => throw null;
        public virtual bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera) => throw null;
        protected override void OnDidApplyAnimationProperties() { }
    }

    public class RawImage : MaskableGraphic
    {
        protected RawImage() { }
        public override Texture mainTexture => throw null;
        public Texture texture { get => throw null; set { } }
        public Rect uvRect { get => throw null; set { } }
        public override void SetNativeSize() { }
        protected override void OnPopulateMesh(VertexHelper vh) { }
        protected override void OnDidApplyAnimationProperties() { }
    }

    [Serializable]
    public class FontData : ISerializationCallbackReceiver
    {
        public static FontData defaultFontData => throw null;
        public Font font { get => throw null; set { } }
        public int fontSize { get => throw null; set { } }
        public FontStyle fontStyle { get => throw null; set { } }
        public bool bestFit { get => throw null; set { } }
        public int minSize { get => throw null; set { } }
        public int maxSize { get => throw null; set { } }
        public TextAnchor alignment { get => throw null; set { } }
        public bool alignByGeometry { get => throw null; set { } }
        public bool richText { get => throw null; set { } }
        public HorizontalWrapMode horizontalOverflow { get => throw null; set { } }
        public VerticalWrapMode verticalOverflow { get => throw null; set { } }
        public float lineSpacing { get => throw null; set { } }
        void ISerializationCallbackReceiver.OnBeforeSerialize() { }
        void ISerializationCallbackReceiver.OnAfterDeserialize() { }
    }

    [RequireComponent(typeof(CanvasRenderer))]
    public class Text : MaskableGraphic, ILayoutElement
    {
        protected static Material s_DefaultText;
        protected bool m_DisableFontTextureRebuiltCallback;
        protected string m_Text;
        protected Text() { }
        public TextGenerator cachedTextGenerator => throw null;
        public TextGenerator cachedTextGeneratorForLayout => throw null;
        public override Texture mainTexture => throw null;
        public void FontTextureChanged() { }
        public Font font { get => throw null; set { } }
        public virtual string text { get => throw null; set { } }
        public bool supportRichText { get => throw null; set { } }
        public bool resizeTextForBestFit { get => throw null; set { } }
        public int resizeTextMinSize { get => throw null; set { } }
        public int resizeTextMaxSize { get => throw null; set { } }
        public TextAnchor alignment { get => throw null; set { } }
        public bool alignByGeometry { get => throw null; set { } }
        public int fontSize { get => throw null; set { } }
        public HorizontalWrapMode horizontalOverflow { get => throw null; set { } }
        public VerticalWrapMode verticalOverflow { get => throw null; set { } }
        public float lineSpacing { get => throw null; set { } }
        public FontStyle fontStyle { get => throw null; set { } }
        public float pixelsPerUnit => throw null;
        protected override void OnEnable() { }
        protected override void OnDisable() { }
        protected override void UpdateGeometry() { }
        public TextGenerationSettings GetGenerationSettings(Vector2 extents) => throw null;
        public static Vector2 GetTextAnchorPivot(TextAnchor anchor) => throw null;
        protected override void OnPopulateMesh(VertexHelper toFill) { }
        public virtual void CalculateLayoutInputHorizontal() { }
        public virtual void CalculateLayoutInputVertical() { }
        public virtual float minWidth => throw null;
        public virtual float preferredWidth => throw null;
        public virtual float flexibleWidth => throw null;
        public virtual float minHeight => throw null;
        public virtual float preferredHeight => throw null;
        public virtual float flexibleHeight => throw null;
        public virtual int layoutPriority => throw null;
    }

    [Serializable]
    public struct ColorBlock : IEquatable<ColorBlock>
    {
        public Color normalColor { get => throw null; set { } }
        public Color highlightedColor { get => throw null; set { } }
        public Color pressedColor { get => throw null; set { } }
        public Color selectedColor { get => throw null; set { } }
        public Color disabledColor { get => throw null; set { } }
        public float colorMultiplier { get => throw null; set { } }
        public float fadeDuration { get => throw null; set { } }
        public static ColorBlock defaultColorBlock;
        public override bool Equals(object obj) => throw null;
        public bool Equals(ColorBlock other) => throw null;
        public static bool operator ==(ColorBlock point1, ColorBlock point2) => throw null;
        public static bool operator !=(ColorBlock point1, ColorBlock point2) => throw null;
        public override int GetHashCode() => throw null;
    }

    [Serializable]
    public struct SpriteState : IEquatable<SpriteState>
    {
        public Sprite highlightedSprite { get => throw null; set { } }
        public Sprite pressedSprite { get => throw null; set { } }
        public Sprite selectedSprite { get => throw null; set { } }
        public Sprite disabledSprite { get => throw null; set { } }
        public bool Equals(SpriteState other) => throw null;
    }

    [Serializable]
    public struct Navigation : IEquatable<Navigation>
    {
        [Flags]
        public enum Mode { None = 0, Horizontal = 1, Vertical = 2, Automatic = 3, Explicit = 4 }
        public Mode mode { get => throw null; set { } }
        public bool wrapAround { get => throw null; set { } }
        public Selectable selectOnUp { get => throw null; set { } }
        public Selectable selectOnDown { get => throw null; set { } }
        public Selectable selectOnLeft { get => throw null; set { } }
        public Selectable selectOnRight { get => throw null; set { } }
        public static Navigation defaultNavigation => throw null;
        public bool Equals(Navigation other) => throw null;
    }

    [Serializable]
    public class AnimationTriggers
    {
        public string normalTrigger { get => throw null; set { } }
        public string highlightedTrigger { get => throw null; set { } }
        public string pressedTrigger { get => throw null; set { } }
        public string selectedTrigger { get => throw null; set { } }
        public string disabledTrigger { get => throw null; set { } }
    }

    [ExecuteAlways]
    [SelectionBase]
    [DisallowMultipleComponent]
    public class Selectable : UIBehaviour, IMoveHandler, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        public enum Transition { None = 0, ColorTint = 1, SpriteSwap = 2, Animation = 3 }
        protected enum SelectionState { Normal = 0, Highlighted = 1, Pressed = 2, Selected = 3, Disabled = 4 }
        protected static Selectable[] s_Selectables;
        protected static int s_SelectableCount;
        protected Selectable() { }
        public static Selectable[] allSelectablesArray => throw null;
        public static int allSelectableCount => throw null;
        [Obsolete("Replaced with allSelectablesArray to have better performance when disabling a element", false)]
        public static List<Selectable> allSelectables => throw null;
        public static int AllSelectablesNoAlloc(Selectable[] selectables) => throw null;
        public Navigation navigation { get => throw null; set { } }
        public Transition transition { get => throw null; set { } }
        public ColorBlock colors { get => throw null; set { } }
        public SpriteState spriteState { get => throw null; set { } }
        public AnimationTriggers animationTriggers { get => throw null; set { } }
        public Graphic targetGraphic { get => throw null; set { } }
        public bool interactable { get => throw null; set { } }
        public Image image { get => throw null; set { } }
        public Animator animator => throw null;
        protected SelectionState currentSelectionState => throw null;
        protected override void Awake() { }
        protected override void OnCanvasGroupChanged() { }
        public virtual bool IsInteractable() => throw null;
        protected override void OnDidApplyAnimationProperties() { }
        protected override void OnEnable() { }
        protected override void OnTransformParentChanged() { }
        protected override void OnDisable() { }
        protected virtual void InstantClearState() { }
        protected virtual void DoStateTransition(SelectionState state, bool instant) { }
        public Selectable FindSelectable(Vector3 dir) => throw null;
        public virtual Selectable FindSelectableOnLeft() => throw null;
        public virtual Selectable FindSelectableOnRight() => throw null;
        public virtual Selectable FindSelectableOnUp() => throw null;
        public virtual Selectable FindSelectableOnDown() => throw null;
        public virtual void OnMove(AxisEventData eventData) { }
        protected bool IsHighlighted() => throw null;
        protected bool IsPressed() => throw null;
        public virtual void OnPointerDown(PointerEventData eventData) { }
        public virtual void OnPointerUp(PointerEventData eventData) { }
        public virtual void OnPointerEnter(PointerEventData eventData) { }
        public virtual void OnPointerExit(PointerEventData eventData) { }
        public virtual void OnSelect(BaseEventData eventData) { }
        public virtual void OnDeselect(BaseEventData eventData) { }
        public virtual void Select() { }
#if UNITY_EDITOR
        protected override void OnValidate() { }
        protected override void Reset() { }
#endif
    }

    public class Button : Selectable, IPointerClickHandler, ISubmitHandler
    {
        [Serializable] public class ButtonClickedEvent : UnityEvent { }
        protected Button() { }
        public ButtonClickedEvent onClick { get => throw null; set { } }
        public virtual void OnPointerClick(PointerEventData eventData) { }
        public virtual void OnSubmit(BaseEventData eventData) { }
    }

    [RequireComponent(typeof(RectTransform))]
    public class Toggle : Selectable, IPointerClickHandler, ISubmitHandler, ICanvasElement
    {
        public enum ToggleTransition { None = 0, Fade = 1 }
        [Serializable] public class ToggleEvent : UnityEvent<bool> { }
        public ToggleTransition toggleTransition;
        public Graphic graphic;
        public ToggleEvent onValueChanged;
        protected Toggle() { }
        public ToggleGroup group { get => throw null; set { } }
        public virtual void Rebuild(CanvasUpdate executing) { }
        public virtual void LayoutComplete() { }
        public virtual void GraphicUpdateComplete() { }
        protected override void OnDestroy() { }
        protected override void OnEnable() { }
        protected override void OnDisable() { }
        protected override void OnDidApplyAnimationProperties() { }
        public bool isOn { get => throw null; set { } }
        public void SetIsOnWithoutNotify(bool value) { }
        public virtual void OnPointerClick(PointerEventData eventData) { }
        public virtual void OnSubmit(BaseEventData eventData) { }
    }

    [DisallowMultipleComponent]
    public class ToggleGroup : UIBehaviour
    {
        protected ToggleGroup() { }
        public bool allowSwitchOff { get => throw null; set { } }
        protected List<Toggle> m_Toggles;
        protected override void Start() { }
        protected override void OnEnable() { }
        public void NotifyToggleOn(Toggle toggle, bool sendCallback = true) { }
        public void UnregisterToggle(Toggle toggle) { }
        public void RegisterToggle(Toggle toggle) { }
        public void EnsureValidState() { }
        public bool AnyTogglesOn() => throw null;
        public IEnumerable<Toggle> ActiveToggles() => throw null;
        public Toggle GetFirstActiveToggle() => throw null;
        public void SetAllTogglesOff(bool sendCallback = true) { }
    }

    [RequireComponent(typeof(RectTransform))]
    public class Slider : Selectable, IDragHandler, IInitializePotentialDragHandler, ICanvasElement
    {
        public enum Direction { LeftToRight = 0, RightToLeft = 1, BottomToTop = 2, TopToBottom = 3 }
        [Serializable] public class SliderEvent : UnityEvent<float> { }
        protected Slider() { }
        public RectTransform fillRect { get => throw null; set { } }
        public RectTransform handleRect { get => throw null; set { } }
        public Direction direction { get => throw null; set { } }
        public float minValue { get => throw null; set { } }
        public float maxValue { get => throw null; set { } }
        public bool wholeNumbers { get => throw null; set { } }
        public virtual float value { get => throw null; set { } }
        public virtual void SetValueWithoutNotify(float input) { }
        public float normalizedValue { get => throw null; set { } }
        public SliderEvent onValueChanged { get => throw null; set { } }
        protected float stepSize => throw null;
        public virtual void Rebuild(CanvasUpdate executing) { }
        public virtual void LayoutComplete() { }
        public virtual void GraphicUpdateComplete() { }
        protected override void OnEnable() { }
        protected override void OnDisable() { }
        protected virtual void Update() { }
        protected override void OnDidApplyAnimationProperties() { }
        protected virtual void Set(float input, bool sendCallback = true) { }
        protected override void OnRectTransformDimensionsChange() { }
        public override void OnPointerDown(PointerEventData eventData) { }
        public virtual void OnDrag(PointerEventData eventData) { }
        public override void OnMove(AxisEventData eventData) { }
        public override Selectable FindSelectableOnLeft() => throw null;
        public override Selectable FindSelectableOnRight() => throw null;
        public override Selectable FindSelectableOnUp() => throw null;
        public override Selectable FindSelectableOnDown() => throw null;
        public virtual void OnInitializePotentialDrag(PointerEventData eventData) { }
        public void SetDirection(Direction direction, bool includeRectLayouts) { }
    }

    [RequireComponent(typeof(RectTransform))]
    public class Scrollbar : Selectable, IBeginDragHandler, IDragHandler, IInitializePotentialDragHandler, ICanvasElement
    {
        public enum Direction { LeftToRight = 0, RightToLeft = 1, BottomToTop = 2, TopToBottom = 3 }
        [Serializable] public class ScrollEvent : UnityEvent<float> { }
        protected Scrollbar() { }
        public RectTransform handleRect { get => throw null; set { } }
        public Direction direction { get => throw null; set { } }
        public float value { get => throw null; set { } }
        public virtual void SetValueWithoutNotify(float input) { }
        public float size { get => throw null; set { } }
        public int numberOfSteps { get => throw null; set { } }
        public ScrollEvent onValueChanged { get => throw null; set { } }
        protected float stepSize => throw null;
        public virtual void Rebuild(CanvasUpdate executing) { }
        public virtual void LayoutComplete() { }
        public virtual void GraphicUpdateComplete() { }
        protected override void OnEnable() { }
        protected override void OnDisable() { }
        protected virtual void Set(float input, bool sendCallback = true) { }
        protected override void OnRectTransformDimensionsChange() { }
        public virtual void OnBeginDrag(PointerEventData eventData) { }
        public virtual void OnDrag(PointerEventData eventData) { }
        public override void OnPointerDown(PointerEventData eventData) { }
        public override void OnPointerUp(PointerEventData eventData) { }
        public override void OnMove(AxisEventData eventData) { }
        public virtual void OnInitializePotentialDrag(PointerEventData eventData) { }
        public void SetDirection(Direction direction, bool includeRectLayouts) { }
    }

    [SelectionBase]
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class ScrollRect : UIBehaviour, IInitializePotentialDragHandler, IBeginDragHandler, IEndDragHandler, IDragHandler, IScrollHandler, ICanvasElement, ILayoutElement, ILayoutGroup
    {
        public enum MovementType { Unrestricted = 0, Elastic = 1, Clamped = 2 }
        public enum ScrollbarVisibility { Permanent = 0, AutoHide = 1, AutoHideAndExpandViewport = 2 }
        [Serializable] public class ScrollRectEvent : UnityEvent<Vector2> { }
        protected ScrollRect() { }
        public RectTransform content { get => throw null; set { } }
        public bool horizontal { get => throw null; set { } }
        public bool vertical { get => throw null; set { } }
        public MovementType movementType { get => throw null; set { } }
        public float elasticity { get => throw null; set { } }
        public bool inertia { get => throw null; set { } }
        public float decelerationRate { get => throw null; set { } }
        public float scrollSensitivity { get => throw null; set { } }
        public RectTransform viewport { get => throw null; set { } }
        public Scrollbar horizontalScrollbar { get => throw null; set { } }
        public Scrollbar verticalScrollbar { get => throw null; set { } }
        public ScrollbarVisibility horizontalScrollbarVisibility { get => throw null; set { } }
        public ScrollbarVisibility verticalScrollbarVisibility { get => throw null; set { } }
        public float horizontalScrollbarSpacing { get => throw null; set { } }
        public float verticalScrollbarSpacing { get => throw null; set { } }
        public ScrollRectEvent onValueChanged { get => throw null; set { } }
        protected RectTransform viewRect => throw null;
        protected Bounds m_ContentBounds;
        protected Vector2 m_ContentStartPosition;
        protected Bounds m_ViewBounds;
        public Vector2 velocity { get => throw null; set { } }
        public virtual void Rebuild(CanvasUpdate executing) { }
        public virtual void LayoutComplete() { }
        public virtual void GraphicUpdateComplete() { }
        protected override void OnEnable() { }
        protected override void OnDisable() { }
        public override bool IsActive() => throw null;
        public virtual void StopMovement() { }
        public virtual void OnScroll(PointerEventData data) { }
        public virtual void OnInitializePotentialDrag(PointerEventData eventData) { }
        public virtual void OnBeginDrag(PointerEventData eventData) { }
        public virtual void OnEndDrag(PointerEventData eventData) { }
        public virtual void OnDrag(PointerEventData eventData) { }
        protected virtual void SetContentAnchoredPosition(Vector2 position) { }
        protected virtual void LateUpdate() { }
        protected void UpdatePrevData() { }
        public Vector2 normalizedPosition { get => throw null; set { } }
        public float horizontalNormalizedPosition { get => throw null; set { } }
        public float verticalNormalizedPosition { get => throw null; set { } }
        protected virtual void SetNormalizedPosition(float value, int axis) { }
        protected override void OnRectTransformDimensionsChange() { }
        public virtual void CalculateLayoutInputHorizontal() { }
        public virtual void CalculateLayoutInputVertical() { }
        public virtual float minWidth => throw null;
        public virtual float preferredWidth => throw null;
        public virtual float flexibleWidth => throw null;
        public virtual float minHeight => throw null;
        public virtual float preferredHeight => throw null;
        public virtual float flexibleHeight => throw null;
        public virtual int layoutPriority => throw null;
        public virtual void SetLayoutHorizontal() { }
        public virtual void SetLayoutVertical() { }
        protected void UpdateBounds() { }
        protected void SetDirty() { }
        protected void SetDirtyCaching() { }
    }

    [DisallowMultipleComponent]
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public abstract class LayoutGroup : UIBehaviour, ILayoutElement, ILayoutGroup
    {
        protected RectOffset m_Padding;
        protected TextAnchor m_ChildAlignment;
        protected LayoutGroup() { }
        public RectOffset padding { get => throw null; set { } }
        public TextAnchor childAlignment { get => throw null; set { } }
        protected RectTransform rectTransform => throw null;
        protected List<RectTransform> rectChildren => throw null;
        public virtual void CalculateLayoutInputHorizontal() { }
        public abstract void CalculateLayoutInputVertical();
        public virtual float minWidth => throw null;
        public virtual float preferredWidth => throw null;
        public virtual float flexibleWidth => throw null;
        public virtual float minHeight => throw null;
        public virtual float preferredHeight => throw null;
        public virtual float flexibleHeight => throw null;
        public virtual int layoutPriority => throw null;
        public abstract void SetLayoutHorizontal();
        public abstract void SetLayoutVertical();
        protected override void OnEnable() { }
        protected override void OnDisable() { }
        protected override void OnDidApplyAnimationProperties() { }
        protected float GetTotalMinSize(int axis) => throw null;
        protected float GetTotalPreferredSize(int axis) => throw null;
        protected float GetTotalFlexibleSize(int axis) => throw null;
        protected float GetStartOffset(int axis, float requiredSpaceWithoutPadding) => throw null;
        protected float GetAlignmentOnAxis(int axis) => throw null;
        protected void SetLayoutInputForAxis(float totalMin, float totalPreferred, float totalFlexible, int axis) { }
        protected void SetChildAlongAxis(RectTransform rect, int axis, float pos) { }
        protected void SetChildAlongAxisWithScale(RectTransform rect, int axis, float pos, float scaleFactor) { }
        protected void SetChildAlongAxis(RectTransform rect, int axis, float pos, float size) { }
        protected void SetChildAlongAxisWithScale(RectTransform rect, int axis, float pos, float size, float scaleFactor) { }
        protected override void OnRectTransformDimensionsChange() { }
        protected virtual void OnTransformChildrenChanged() { }
        protected void SetProperty<T>(ref T currentValue, T newValue) { }
        protected void SetDirty() { }
    }

    public abstract class HorizontalOrVerticalLayoutGroup : LayoutGroup
    {
        protected float m_Spacing;
        public float spacing { get => throw null; set { } }
        public bool childForceExpandWidth { get => throw null; set { } }
        public bool childForceExpandHeight { get => throw null; set { } }
        public bool childControlWidth { get => throw null; set { } }
        public bool childControlHeight { get => throw null; set { } }
        public bool childScaleWidth { get => throw null; set { } }
        public bool childScaleHeight { get => throw null; set { } }
        public bool reverseArrangement { get => throw null; set { } }
        protected void CalcAlongAxis(int axis, bool isVertical) { }
        protected void SetChildrenAlongAxis(int axis, bool isVertical) { }
    }

    public class HorizontalLayoutGroup : HorizontalOrVerticalLayoutGroup
    {
        protected HorizontalLayoutGroup() { }
        public override void CalculateLayoutInputHorizontal() { }
        public override void CalculateLayoutInputVertical() { }
        public override void SetLayoutHorizontal() { }
        public override void SetLayoutVertical() { }
    }

    public class VerticalLayoutGroup : HorizontalOrVerticalLayoutGroup
    {
        protected VerticalLayoutGroup() { }
        public override void CalculateLayoutInputHorizontal() { }
        public override void CalculateLayoutInputVertical() { }
        public override void SetLayoutHorizontal() { }
        public override void SetLayoutVertical() { }
    }

    public class GridLayoutGroup : LayoutGroup
    {
        public enum Corner { UpperLeft = 0, UpperRight = 1, LowerLeft = 2, LowerRight = 3 }
        public enum Axis { Horizontal = 0, Vertical = 1 }
        public enum Constraint { Flexible = 0, FixedColumnCount = 1, FixedRowCount = 2 }
        protected GridLayoutGroup() { }
        public Corner startCorner { get => throw null; set { } }
        public Axis startAxis { get => throw null; set { } }
        public Vector2 cellSize { get => throw null; set { } }
        public Vector2 spacing { get => throw null; set { } }
        public Constraint constraint { get => throw null; set { } }
        public int constraintCount { get => throw null; set { } }
        public override void CalculateLayoutInputHorizontal() { }
        public override void CalculateLayoutInputVertical() { }
        public override void SetLayoutHorizontal() { }
        public override void SetLayoutVertical() { }
    }

    [RequireComponent(typeof(RectTransform))]
    [ExecuteAlways]
    public class LayoutElement : UIBehaviour, ILayoutElement, ILayoutIgnorer
    {
        protected LayoutElement() { }
        public virtual bool ignoreLayout { get => throw null; set { } }
        public virtual void CalculateLayoutInputHorizontal() { }
        public virtual void CalculateLayoutInputVertical() { }
        public virtual float minWidth { get => throw null; set { } }
        public virtual float minHeight { get => throw null; set { } }
        public virtual float preferredWidth { get => throw null; set { } }
        public virtual float preferredHeight { get => throw null; set { } }
        public virtual float flexibleWidth { get => throw null; set { } }
        public virtual float flexibleHeight { get => throw null; set { } }
        public virtual int layoutPriority { get => throw null; set { } }
        protected override void OnEnable() { }
        protected override void OnTransformParentChanged() { }
        protected override void OnDisable() { }
        protected override void OnDidApplyAnimationProperties() { }
        protected override void OnBeforeTransformParentChanged() { }
        protected void SetDirty() { }
    }

    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public class ContentSizeFitter : UIBehaviour, ILayoutSelfController
    {
        public enum FitMode { Unconstrained = 0, MinSize = 1, PreferredSize = 2 }
        protected ContentSizeFitter() { }
        public FitMode horizontalFit { get => throw null; set { } }
        public FitMode verticalFit { get => throw null; set { } }
        protected override void OnEnable() { }
        protected override void OnDisable() { }
        protected override void OnRectTransformDimensionsChange() { }
        public virtual void SetLayoutHorizontal() { }
        public virtual void SetLayoutVertical() { }
        protected void SetDirty() { }
    }

    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public class AspectRatioFitter : UIBehaviour, ILayoutSelfController
    {
        public enum AspectMode { None = 0, WidthControlsHeight = 1, HeightControlsWidth = 2, FitInParent = 3, EnvelopeParent = 4 }
        protected AspectRatioFitter() { }
        public AspectMode aspectMode { get => throw null; set { } }
        public float aspectRatio { get => throw null; set { } }
        protected override void OnEnable() { }
        protected override void Start() { }
        protected override void OnDisable() { }
        protected override void OnTransformParentChanged() { }
        protected virtual void Update() { }
        protected override void OnRectTransformDimensionsChange() { }
        public virtual void SetLayoutHorizontal() { }
        public virtual void SetLayoutVertical() { }
        protected void SetDirty() { }
        public bool IsComponentValidOnObject() => throw null;
        public bool IsAspectModeValid() => throw null;
    }

    public class LayoutRebuilder : ICanvasElement
    {
        public Transform transform => throw null;
        public bool IsDestroyed() => throw null;
        public static void ForceRebuildLayoutImmediate(RectTransform layoutRoot) { }
        public void Rebuild(CanvasUpdate executing) { }
        public static void MarkLayoutForRebuild(RectTransform rect) { }
        public void LayoutComplete() { }
        public void GraphicUpdateComplete() { }
        public override int GetHashCode() => throw null;
        public override bool Equals(object obj) => throw null;
        public override string ToString() => throw null;
    }

    public static class LayoutUtility
    {
        public static float GetMinSize(RectTransform rect, int axis) => throw null;
        public static float GetPreferredSize(RectTransform rect, int axis) => throw null;
        public static float GetFlexibleSize(RectTransform rect, int axis) => throw null;
        public static float GetMinWidth(RectTransform rect) => throw null;
        public static float GetPreferredWidth(RectTransform rect) => throw null;
        public static float GetFlexibleWidth(RectTransform rect) => throw null;
        public static float GetMinHeight(RectTransform rect) => throw null;
        public static float GetPreferredHeight(RectTransform rect) => throw null;
        public static float GetFlexibleHeight(RectTransform rect) => throw null;
        public static float GetLayoutProperty(RectTransform rect, Func<ILayoutElement, float> property, float defaultValue) => throw null;
        public static float GetLayoutProperty(RectTransform rect, Func<ILayoutElement, float> property, float defaultValue, out ILayoutElement source) => throw null;
    }

    [RequireComponent(typeof(Canvas))]
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class CanvasScaler : UIBehaviour
    {
        public enum ScaleMode { ConstantPixelSize = 0, ScaleWithScreenSize = 1, ConstantPhysicalSize = 2 }
        public enum ScreenMatchMode { MatchWidthOrHeight = 0, Expand = 1, Shrink = 2 }
        public enum Unit { Centimeters = 0, Millimeters = 1, Inches = 2, Points = 3, Picas = 4 }
        protected CanvasScaler() { }
        public ScaleMode uiScaleMode { get => throw null; set { } }
        protected float m_ReferencePixelsPerUnit;
        public float referencePixelsPerUnit { get => throw null; set { } }
        protected float m_ScaleFactor;
        public float scaleFactor { get => throw null; set { } }
        protected Vector2 m_ReferenceResolution;
        public Vector2 referenceResolution { get => throw null; set { } }
        protected ScreenMatchMode m_ScreenMatchMode;
        public ScreenMatchMode screenMatchMode { get => throw null; set { } }
        protected float m_MatchWidthOrHeight;
        public float matchWidthOrHeight { get => throw null; set { } }
        protected Unit m_PhysicalUnit;
        public Unit physicalUnit { get => throw null; set { } }
        protected float m_FallbackScreenDPI;
        public float fallbackScreenDPI { get => throw null; set { } }
        protected float m_DefaultSpriteDPI;
        public float defaultSpriteDPI { get => throw null; set { } }
        protected float m_DynamicPixelsPerUnit;
        public float dynamicPixelsPerUnit { get => throw null; set { } }
        protected override void OnEnable() { }
        protected override void OnDisable() { }
        protected virtual void Update() { }
        protected virtual void Handle() { }
        protected virtual void HandleWorldCanvas() { }
        protected virtual void HandleConstantPixelSize() { }
        protected virtual void HandleScaleWithScreenSize() { }
        protected virtual void HandleConstantPhysicalSize() { }
        protected void SetScaleFactor(float scaleFactor) { }
        protected void SetReferencePixelsPerUnit(float referencePixelsPerUnit) { }
    }

    [AddComponentMenu("Event/Graphic Raycaster")]
    [RequireComponent(typeof(Canvas))]
    public class GraphicRaycaster : BaseRaycaster
    {
        public enum BlockingObjects { None = 0, TwoD = 1, ThreeD = 2, All = 3 }
        protected const int kNoEventMaskSet = -1;
        protected LayerMask m_BlockingMask;
        protected GraphicRaycaster() { }
        public override int sortOrderPriority => throw null;
        public override int renderOrderPriority => throw null;
        public bool ignoreReversedGraphics { get => throw null; set { } }
        public BlockingObjects blockingObjects { get => throw null; set { } }
        public LayerMask blockingMask { get => throw null; set { } }
        public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList) { }
        public override Camera eventCamera => throw null;
    }

    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public class Mask : UIBehaviour, ICanvasRaycastFilter, IMaterialModifier
    {
        protected Mask() { }
        public RectTransform rectTransform => throw null;
        public bool showMaskGraphic { get => throw null; set { } }
        public Graphic graphic => throw null;
        public virtual bool MaskEnabled() => throw null;
        [Obsolete("Not used anymore.")]
        public virtual void OnSiblingGraphicEnabledDisabled() { }
        protected override void OnEnable() { }
        protected override void OnDisable() { }
        public virtual bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera) => throw null;
        public virtual Material GetModifiedMaterial(Material baseMaterial) => throw null;
    }

    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class RectMask2D : UIBehaviour, IClipper, ICanvasRaycastFilter
    {
        protected RectMask2D() { }
        public Vector4 padding { get => throw null; set { } }
        public Vector2Int softness { get => throw null; set { } }
        public Rect canvasRect => throw null;
        public RectTransform rectTransform => throw null;
        protected override void OnEnable() { }
        protected override void OnDisable() { }
        protected override void OnDestroy() { }
        public virtual bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera) => throw null;
        public virtual void PerformClipping() { }
        public virtual void UpdateClipSoftness() { }
        public void AddClippable(IClippable clippable) { }
        public void RemoveClippable(IClippable clippable) { }
        protected override void OnTransformParentChanged() { }
        protected override void OnCanvasHierarchyChanged() { }
    }

    [ExecuteAlways]
    public abstract class BaseMeshEffect : UIBehaviour, IMeshModifier
    {
        protected Graphic graphic => throw null;
        protected override void OnEnable() { }
        protected override void OnDisable() { }
        protected override void OnDidApplyAnimationProperties() { }
        public virtual void ModifyMesh(Mesh mesh) { }
        public abstract void ModifyMesh(VertexHelper vh);
    }

    public class Shadow : BaseMeshEffect
    {
        public const float kMaxEffectDistance = 600f;
        protected Shadow() { }
        public Color effectColor { get => throw null; set { } }
        public Vector2 effectDistance { get => throw null; set { } }
        public bool useGraphicAlpha { get => throw null; set { } }
        protected void ApplyShadowZeroAlloc(List<UIVertex> verts, Color32 color, int start, int end, float x, float y) { }
        protected void ApplyShadow(List<UIVertex> verts, Color32 color, int start, int end, float x, float y) { }
        public override void ModifyMesh(VertexHelper vh) { }
    }

    public class Outline : Shadow
    {
        protected Outline() { }
        public override void ModifyMesh(VertexHelper vh) { }
    }

    public class PositionAsUV1 : BaseMeshEffect
    {
        protected PositionAsUV1() { }
        public override void ModifyMesh(VertexHelper vh) { }
    }

    public static class Clipping
    {
        public static Rect FindCullAndClipWorldRect(List<RectMask2D> rectMaskParents, out bool validRect) => throw null;
    }
}
