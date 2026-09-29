// STUB of TextMesh Pro as shipped inside com.unity.ugui 2.0 (Unity 6) - namespace TMPro.
// Compile-only. Covers TMP_Text / TextMeshProUGUI / TextMeshPro / TMP_InputField and the common enums.
// Members marked // GUESS were not verified against the package source.
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TMPro
{
    [Flags]
    public enum FontStyles
    {
        Normal = 0x0, Bold = 0x1, Italic = 0x2, Underline = 0x4, LowerCase = 0x8, UpperCase = 0x10, SmallCaps = 0x20,
        Strikethrough = 0x40, Superscript = 0x80, Subscript = 0x100, Highlight = 0x200
    }

    public enum FontWeight { Thin = 100, ExtraLight = 200, Light = 300, Regular = 400, Medium = 500, SemiBold = 600, Bold = 700, Heavy = 800, Black = 900 }

    public enum HorizontalAlignmentOptions { Left = 0x1, Center = 0x2, Right = 0x4, Justified = 0x8, Flush = 0x10, Geometry = 0x20 }
    public enum VerticalAlignmentOptions { Top = 0x100, Middle = 0x200, Bottom = 0x400, Baseline = 0x800, Geometry = 0x1000, Capline = 0x2000 }

    public enum TextAlignmentOptions
    {
        TopLeft = 257, Top = 258, TopRight = 260, TopJustified = 264, TopFlush = 272, TopGeoAligned = 288,
        Left = 513, Center = 514, Right = 516, Justified = 520, Flush = 528, CenterGeoAligned = 544,
        BottomLeft = 1025, Bottom = 1026, BottomRight = 1028, BottomJustified = 1032, BottomFlush = 1040, BottomGeoAligned = 1056,
        BaselineLeft = 2049, Baseline = 2050, BaselineRight = 2052, BaselineJustified = 2056, BaselineFlush = 2064, BaselineGeoAligned = 2080,
        MidlineLeft = 4097, Midline = 4098, MidlineRight = 4100, MidlineJustified = 4104, MidlineFlush = 4112, MidlineGeoAligned = 4128,
        CaplineLeft = 8193, Capline = 8194, CaplineRight = 8196, CaplineJustified = 8200, CaplineFlush = 8208, CaplineGeoAligned = 8224,
        Converted = 65535
    }

    public enum TextOverflowModes { Overflow = 0, Ellipsis = 1, Masking = 2, Truncate = 3, ScrollRect = 4, Page = 5, Linked = 6 }
    public enum TextWrappingModes { NoWrap = 0, Normal = 1, PreserveWhitespace = 2, PreserveWhitespaceNoWrap = 3 }
    public enum TextureMappingOptions { Character = 0, Line = 1, Paragraph = 2, MatchAspect = 3 }
    public enum TextRenderFlags { DontRender = 0x0, Render = 0xFF }
    public enum TMP_TextElementType { Character = 0, Sprite = 1 }
    public enum VertexSortingOrder { Normal = 0, Reverse = 1 }
    [Flags]
    public enum TMP_VertexDataUpdateFlags { None = 0x0, Vertices = 0x1, Uv0 = 0x2, Uv2 = 0x4, Uv4 = 0x8, Colors32 = 0x10, All = 0xFF }

    [Serializable]
    public struct VertexGradient
    {
        public Color topLeft;
        public Color topRight;
        public Color bottomLeft;
        public Color bottomRight;
        public VertexGradient(Color color) { topLeft = topRight = bottomLeft = bottomRight = color; }
        public VertexGradient(Color color0, Color color1, Color color2, Color color3) { topLeft = color0; topRight = color1; bottomLeft = color2; bottomRight = color3; }
    }

    public abstract class TMP_Asset : ScriptableObject
    {
        public int instanceID => throw null;
        public int hashCode { get => throw null; set { } }
        public Material material { get => throw null; set { } }
        public int materialHashCode { get => throw null; set { } }
    }

    public class TMP_FontAsset : TMP_Asset
    {
        public static TMP_FontAsset CreateFontAsset(Font font) => throw null;
        public List<TMP_FontAsset> fallbackFontAssetTable { get => throw null; set { } }
        public Texture2D[] atlasTextures { get => throw null; set { } }
        public bool HasCharacter(int character) => throw null;
        public bool HasCharacter(char character, bool searchFallbacks = false, bool tryAddCharacter = false) => throw null;
        public bool HasCharacters(string text, out List<char> missingCharacters) => throw null;
        public bool HasCharacters(string text) => throw null;
        public bool TryAddCharacters(string characters, bool includeFontFeatures = false) => throw null;
        public bool TryAddCharacters(string characters, out string missingCharacters, bool includeFontFeatures = false) => throw null;
        public void ClearFontAssetData(bool setAtlasSizeToZero = false) { }
    }

    public class TMP_SpriteAsset : TMP_Asset
    {
        public Texture spriteSheet;
        public List<TMP_SpriteAsset> fallbackSpriteAssets { get => throw null; set { } }
        public int GetSpriteIndexFromName(string name) => throw null;
    }

    public class TMP_ColorGradient : ScriptableObject
    {
        public Color topLeft;
        public Color topRight;
        public Color bottomLeft;
        public Color bottomRight;
    }

    public class TMP_StyleSheet : ScriptableObject { }

    public struct TMP_CharacterInfo
    {
        public char character;
        public int index;
        public int stringLength;
        public TMP_TextElementType elementType;
        public TMP_FontAsset fontAsset;
        public TMP_SpriteAsset spriteAsset;
        public int spriteIndex;
        public Material material;
        public int materialReferenceIndex;
        public bool isUsingAlternateTypeface;
        public float pointSize;
        public int lineNumber;
        public int pageNumber;
        public int vertexIndex;
        public Vector3 topLeft;
        public Vector3 bottomLeft;
        public Vector3 topRight;
        public Vector3 bottomRight;
        public float origin;
        public float xAdvance;
        public float ascender;
        public float baseLine;
        public float descender;
        public float aspectRatio;
        public float scale;
        public Color32 color;
        public Color32 underlineColor;
        public Color32 strikethroughColor;
        public Color32 highlightColor;
        public FontStyles style;
        public bool isVisible;
    }

    public struct TMP_MeshInfo
    {
        public Mesh mesh;
        public int vertexCount;
        public Vector3[] vertices;
        public Vector3[] normals;
        public Vector4[] tangents;
        public Vector4[] uvs0;
        public Vector2[] uvs2;
        public Color32[] colors32;
        public int[] triangles;
        public Material material;
    }

    public struct TMP_LineInfo
    {
        public int characterCount;
        public int visibleCharacterCount;
        public int spaceCount;
        public int wordCount;
        public int firstCharacterIndex;
        public int firstVisibleCharacterIndex;
        public int lastCharacterIndex;
        public int lastVisibleCharacterIndex;
        public float length;
        public float lineHeight;
        public float ascender;
        public float baseline;
        public float descender;
        public float width;
    }

    public struct TMP_WordInfo
    {
        public TMP_Text textComponent;
        public int firstCharacterIndex;
        public int lastCharacterIndex;
        public int characterCount;
        public string GetWord() => throw null;
    }

    public struct TMP_LinkInfo
    {
        public TMP_Text textComponent;
        public int hashCode;
        public int linkIdFirstCharacterIndex;
        public int linkIdLength;
        public int linkTextfirstCharacterIndex;
        public int linkTextLength;
        public string GetLinkText() => throw null;
        public string GetLinkID() => throw null;
    }

    [Serializable]
    public class TMP_TextInfo
    {
        public TMP_Text textComponent;
        public int characterCount;
        public int spriteCount;
        public int spaceCount;
        public int wordCount;
        public int linkCount;
        public int lineCount;
        public int pageCount;
        public int materialCount;
        public TMP_CharacterInfo[] characterInfo;
        public TMP_WordInfo[] wordInfo;
        public TMP_LinkInfo[] linkInfo;
        public TMP_LineInfo[] lineInfo;
        public TMP_MeshInfo[] meshInfo;
        public TMP_TextInfo() { }
        public TMP_TextInfo(TMP_Text textComponent) { }
        public void ClearMeshInfo(bool updateMesh) { }
        public TMP_MeshInfo[] CopyMeshInfoVertexData() => throw null;
    }

    public abstract class TMP_Text : MaskableGraphic
    {
        protected TMP_Text() { }
        public virtual string text { get => throw null; set { } }
        public bool isRightToLeftText { get => throw null; set { } }
        public TMP_FontAsset font { get => throw null; set { } }
        public virtual Material fontSharedMaterial { get => throw null; set { } }
        public virtual Material[] fontSharedMaterials { get => throw null; set { } }
        public Material fontMaterial { get => throw null; set { } }
        public virtual Material[] fontMaterials { get => throw null; set { } }
        public override Color color { get => throw null; set { } }
        public float alpha { get => throw null; set { } }
        public bool enableVertexGradient { get => throw null; set { } }
        public VertexGradient colorGradient { get => throw null; set { } }
        public TMP_ColorGradient colorGradientPreset { get => throw null; set { } }
        public TMP_SpriteAsset spriteAsset { get => throw null; set { } }
        public bool tintAllSprites { get => throw null; set { } }
        public TMP_StyleSheet styleSheet { get => throw null; set { } }
        public bool overrideColorTags { get => throw null; set { } }
        public Color32 faceColor { get => throw null; set { } }
        public Color32 outlineColor { get => throw null; set { } }
        public float outlineWidth { get => throw null; set { } }
        public float fontSize { get => throw null; set { } }
        public FontWeight fontWeight { get => throw null; set { } }
        public float pixelsPerUnit => throw null;
        public bool enableAutoSizing { get => throw null; set { } }
        public float fontSizeMin { get => throw null; set { } }
        public float fontSizeMax { get => throw null; set { } }
        public FontStyles fontStyle { get => throw null; set { } }
        public bool isUsingBold => throw null;
        public HorizontalAlignmentOptions horizontalAlignment { get => throw null; set { } }
        public VerticalAlignmentOptions verticalAlignment { get => throw null; set { } }
        public TextAlignmentOptions alignment { get => throw null; set { } }
        public float characterSpacing { get => throw null; set { } }
        public float characterHorizontalScale { get => throw null; set { } } // GUESS
        public float wordSpacing { get => throw null; set { } }
        public float lineSpacing { get => throw null; set { } }
        public float lineSpacingAdjustment { get => throw null; set { } }
        public float paragraphSpacing { get => throw null; set { } }
        public float characterWidthAdjustment { get => throw null; set { } }
        [Obsolete("The enabledWordWrapping property is now obsolete. Please use the textWrappingMode property instead.")]
        public bool enableWordWrapping { get => throw null; set { } }
        public TextWrappingModes textWrappingMode { get => throw null; set { } }
        public float wordWrappingRatios { get => throw null; set { } }
        public TextOverflowModes overflowMode { get => throw null; set { } }
        public bool isTextOverflowing => throw null;
        public int firstOverflowCharacterIndex => throw null;
        public TMP_Text linkedTextComponent { get => throw null; set { } }
        public bool isTextTruncated => throw null;
        [Obsolete("The \"enableKerning\" property has been deprecated. Use the \"fontFeatures\" property to control what features are enabled on the text component.")]
        public bool enableKerning { get => throw null; set { } }
        public bool extraPadding { get => throw null; set { } }
        public bool richText { get => throw null; set { } }
        public bool emojiFallbackSupport { get => throw null; set { } } // GUESS
        public bool parseCtrlCharacters { get => throw null; set { } }
        public bool isOverlay { get => throw null; set { } }
        public bool isOrthographic { get => throw null; set { } }
        public bool enableCulling { get => throw null; set { } }
        public bool ignoreVisibility { get => throw null; set { } }
        public TextureMappingOptions horizontalMapping { get => throw null; set { } }
        public TextureMappingOptions verticalMapping { get => throw null; set { } }
        public float mappingUvLineOffset { get => throw null; set { } }
        public TextRenderFlags renderMode { get => throw null; set { } }
        public VertexSortingOrder geometrySortingOrder { get => throw null; set { } }
        public bool isTextObjectScaleStatic { get => throw null; set { } }
        public bool vertexBufferAutoSizeReduction { get => throw null; set { } }
        public int firstVisibleCharacter { get => throw null; set { } }
        public int maxVisibleCharacters { get => throw null; set { } }
        public int maxVisibleWords { get => throw null; set { } }
        public int maxVisibleLines { get => throw null; set { } }
        public bool useMaxVisibleDescender { get => throw null; set { } }
        public int pageToDisplay { get => throw null; set { } }
        public virtual Vector4 margin { get => throw null; set { } }
        public TMP_TextInfo textInfo => throw null;
        public bool havePropertiesChanged { get => throw null; set { } }
        public bool isUsingLegacyAnimationComponent { get => throw null; set { } }
        public new Transform transform => throw null;
        public new RectTransform rectTransform => throw null;
        public virtual bool autoSizeTextContainer { get => throw null; set { } }
        public virtual Mesh mesh => throw null;
        public bool isVolumetricText { get => throw null; set { } }
        public Bounds bounds => throw null;
        public Bounds textBounds => throw null;
        public float flexibleHeight => throw null;
        public float flexibleWidth => throw null;
        public float minWidth => throw null;
        public float minHeight => throw null;
        public float maxWidth => throw null;
        public float maxHeight => throw null;
        public virtual float preferredWidth => throw null;
        public virtual float preferredHeight => throw null;
        public virtual float renderedWidth => throw null;
        public virtual float renderedHeight => throw null;
        public int layoutPriority => throw null;

        public event Action<TMP_TextInfo> OnPreRenderText;

        public virtual void ForceMeshUpdate(bool ignoreActiveState = false, bool forceTextReparsing = false) { }
        public virtual void UpdateGeometry(Mesh mesh, int index) { }
        public virtual void UpdateVertexData(TMP_VertexDataUpdateFlags flags) { }
        public virtual void UpdateVertexData() { }
        public virtual void SetVertices(Vector3[] vertices) { }
        public virtual void UpdateMeshPadding() { }
        public override void CrossFadeColor(Color targetColor, float duration, bool ignoreTimeScale, bool useAlpha) { }
        public override void CrossFadeAlpha(float alpha, float duration, bool ignoreTimeScale) { }
        public void SetText(string sourceText) { }
        [Obsolete("Use the SetText(string, bool) is obsolete. Use SetText(string) instead.")] // GUESS (exact message)
        public void SetText(string sourceText, bool syncTextInputBox = true) { }
        public void SetText(string sourceText, float arg0) { }
        public void SetText(string sourceText, float arg0, float arg1) { }
        public void SetText(string sourceText, float arg0, float arg1, float arg2) { }
        public void SetText(string sourceText, float arg0, float arg1, float arg2, float arg3) { }
        public void SetText(string sourceText, float arg0, float arg1, float arg2, float arg3, float arg4) { }
        public void SetText(string sourceText, float arg0, float arg1, float arg2, float arg3, float arg4, float arg5) { }
        public void SetText(string sourceText, float arg0, float arg1, float arg2, float arg3, float arg4, float arg5, float arg6) { }
        public void SetText(string sourceText, float arg0, float arg1, float arg2, float arg3, float arg4, float arg5, float arg6, float arg7) { }
        public void SetText(StringBuilder sourceText) { }
        public void SetText(char[] sourceText) { }
        public void SetText(char[] sourceText, int start, int length) { }
        public void SetCharArray(char[] sourceText) { }
        public void SetCharArray(char[] sourceText, int start, int length) { }
        public Vector2 GetPreferredValues() => throw null;
        public Vector2 GetPreferredValues(float width, float height) => throw null;
        public Vector2 GetPreferredValues(string text) => throw null;
        public Vector2 GetPreferredValues(string text, float width, float height) => throw null;
        public Vector2 GetRenderedValues() => throw null;
        public Vector2 GetRenderedValues(bool onlyVisibleCharacters) => throw null;
        public TMP_TextInfo GetTextInfo(string text) => throw null;
        public virtual void ComputeMarginSize() { }
        public virtual void ClearMesh() { }
        public virtual void ClearMesh(bool uploadGeometry) { }
        public virtual string GetParsedText() => throw null;
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasRenderer))]
    [ExecuteAlways]
    public class TextMeshProUGUI : TMP_Text, ILayoutElement
    {
        public override Material materialForRendering => throw null;
        public override bool autoSizeTextContainer { get => throw null; set { } }
        public override Mesh mesh => throw null;
        public new CanvasRenderer canvasRenderer => throw null;
        public void CalculateLayoutInputHorizontal() { }
        public void CalculateLayoutInputVertical() { }
        public override void SetVerticesDirty() { }
        public override void SetLayoutDirty() { }
        public override void SetMaterialDirty() { }
        public override void SetAllDirty() { }
        public override void Rebuild(CanvasUpdate update) { }
        public override void RecalculateClipping() { }
        public override void Cull(Rect clipRect, bool validRect) { }
        public override void UpdateMeshPadding() { }
        public override void ForceMeshUpdate(bool ignoreActiveState = false, bool forceTextReparsing = false) { }
        public override void UpdateGeometry(Mesh mesh, int index) { }
        public override void UpdateVertexData(TMP_VertexDataUpdateFlags flags) { }
        public override void UpdateVertexData() { }
        public override Material GetModifiedMaterial(Material baseMaterial) => throw null;
        public override void ClearMesh() { }
        public override void ComputeMarginSize() { }
        protected override void OnEnable() { }
        protected override void OnDisable() { }
        protected override void OnDestroy() { }
        protected override void OnCanvasHierarchyChanged() { }
        protected override void OnTransformParentChanged() { }
        protected override void OnRectTransformDimensionsChange() { }
        protected override void OnDidApplyAnimationProperties() { }
        protected override void UpdateMaterial() { }
        public override void SetNativeSize() { } // GUESS: inherited from Graphic in some versions
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshRenderer))]
    [ExecuteAlways]
    public class TextMeshPro : TMP_Text, ILayoutElement
    {
        public int sortingLayerID { get => throw null; set { } }
        public int sortingOrder { get => throw null; set { } }
        public override bool autoSizeTextContainer { get => throw null; set { } }
        [Obsolete("The TextContainer is now obsolete. Use the RectTransform instead.")]
        public TextContainer textContainer => throw null;
        public new Transform transform => throw null;
        public new Renderer renderer => throw null;
        public override Mesh mesh => throw null;
        public MeshFilter meshFilter => throw null;
        public MaskingTypes maskType { get => throw null; set { } }
        public void SetMask(MaskingTypes type, Vector4 maskCoords) { }
        public void SetMask(MaskingTypes type, Vector4 maskCoords, float softnessX, float softnessY) { }
        public override void SetVerticesDirty() { }
        public override void SetLayoutDirty() { }
        public override void SetMaterialDirty() { }
        public override void SetAllDirty() { }
        public override void Rebuild(CanvasUpdate update) { }
        protected override void UpdateMaterial() { }
        public override void UpdateMeshPadding() { }
        public override void ForceMeshUpdate(bool ignoreActiveState = false, bool forceTextReparsing = false) { }
        public override void UpdateGeometry(Mesh mesh, int index) { }
        public override void UpdateVertexData(TMP_VertexDataUpdateFlags flags) { }
        public override void UpdateVertexData() { }
        public void UpdateFontAsset() { }
        public void CalculateLayoutInputHorizontal() { }
        public void CalculateLayoutInputVertical() { }
        public override void ComputeMarginSize() { }
        public override void ClearMesh(bool updateMesh) { }
        protected override void OnEnable() { }
        protected override void OnDisable() { }
        protected override void OnDestroy() { }
        protected override void OnTransformParentChanged() { }
        protected override void OnRectTransformDimensionsChange() { }
        protected override void OnDidApplyAnimationProperties() { }
    }

    public enum MaskingTypes { MaskOff = 0, MaskHard = 1, MaskSoft = 2 }

    [Obsolete("The TextContainer is now obsolete. Use the RectTransform instead.")]
    public class TextContainer : UIBehaviour { }

    [AddComponentMenu("UI/TextMeshPro - Input Field", 11)]
    public class TMP_InputField : Selectable, IUpdateSelectedHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, ISubmitHandler, ICancelHandler, ICanvasElement, ILayoutElement, IScrollHandler
    {
        public enum ContentType { Standard = 0, Autocorrected = 1, IntegerNumber = 2, DecimalNumber = 3, Alphanumeric = 4, Name = 5, EmailAddress = 6, Password = 7, Pin = 8, Custom = 9 }
        public enum InputType { Standard = 0, AutoCorrect = 1, Password = 2 }
        public enum CharacterValidation { None = 0, Digit = 1, Integer = 2, Decimal = 3, Alphanumeric = 4, Name = 5, Regex = 6, EmailAddress = 7, CustomValidator = 8 }
        public enum LineType { SingleLine = 0, MultiLineSubmit = 1, MultiLineNewline = 2 }
        public delegate char OnValidateInput(string text, int charIndex, char addedChar);
        [Serializable] public class SubmitEvent : UnityEvent<string> { }
        [Serializable] public class OnChangeEvent : UnityEvent<string> { }
        [Serializable] public class SelectionEvent : UnityEvent<string> { }
        [Serializable] public class TextSelectionEvent : UnityEvent<string, int, int> { }
        [Serializable] public class TouchScreenKeyboardEvent : UnityEvent<TouchScreenKeyboard.Status> { }

        protected TMP_InputField() { }
        public string text { get => throw null; set { } }
        public void SetTextWithoutNotify(string input) { }
        public bool isFocused => throw null;
        public TMP_Text textComponent { get => throw null; set { } }
        public RectTransform textViewport { get => throw null; set { } }
        public Graphic placeholder { get => throw null; set { } }
        public Scrollbar verticalScrollbar { get => throw null; set { } }
        public float scrollSensitivity { get => throw null; set { } }
        public Color caretColor { get => throw null; set { } }
        public bool customCaretColor { get => throw null; set { } }
        public Color selectionColor { get => throw null; set { } }
        public SubmitEvent onEndEdit { get => throw null; set { } }
        public SubmitEvent onSubmit { get => throw null; set { } }
        public SelectionEvent onSelect { get => throw null; set { } }
        public SelectionEvent onDeselect { get => throw null; set { } }
        public TextSelectionEvent onTextSelection { get => throw null; set { } }
        public TextSelectionEvent onEndTextSelection { get => throw null; set { } }
        public OnChangeEvent onValueChanged { get => throw null; set { } }
        public TouchScreenKeyboardEvent onTouchScreenKeyboardStatusChanged { get => throw null; set { } }
        public OnValidateInput onValidateInput { get => throw null; set { } }
        public int characterLimit { get => throw null; set { } }
        public float pointSize { get => throw null; set { } }
        public TMP_FontAsset fontAsset { get => throw null; set { } }
        public bool onFocusSelectAll { get => throw null; set { } }
        public bool resetOnDeActivation { get => throw null; set { } }
        public bool restoreOriginalTextOnEscape { get => throw null; set { } }
        public bool isRichTextEditingAllowed { get => throw null; set { } }
        public ContentType contentType { get => throw null; set { } }
        public LineType lineType { get => throw null; set { } }
        public int lineLimit { get => throw null; set { } }
        public InputType inputType { get => throw null; set { } }
        public TouchScreenKeyboardType keyboardType { get => throw null; set { } }
        public CharacterValidation characterValidation { get => throw null; set { } }
        public bool readOnly { get => throw null; set { } }
        public bool richText { get => throw null; set { } }
        public bool multiLine => throw null;
        public char asteriskChar { get => throw null; set { } }
        public bool wasCanceled => throw null;
        public int caretPosition { get => throw null; set { } }
        public int selectionAnchorPosition { get => throw null; set { } }
        public int selectionFocusPosition { get => throw null; set { } }
        public int stringPosition { get => throw null; set { } }
        public bool shouldHideMobileInput { get => throw null; set { } }
        public bool shouldHideSoftKeyboard { get => throw null; set { } }
        public void MoveTextEnd(bool shift) { }
        public void MoveTextStart(bool shift) { }
        public void ActivateInputField() { }
        public void DeactivateInputField(bool clearSelection = false) { }
        public void ForceLabelUpdate() { }
        public override void OnSelect(BaseEventData eventData) { }
        public virtual void OnPointerClick(PointerEventData eventData) { }
        public void OnControlClick() { }
        public virtual void OnBeginDrag(PointerEventData eventData) { }
        public virtual void OnDrag(PointerEventData eventData) { }
        public virtual void OnEndDrag(PointerEventData eventData) { }
        public virtual void OnUpdateSelected(BaseEventData eventData) { }
        public virtual void OnScroll(PointerEventData eventData) { }
        public virtual void OnSubmit(BaseEventData eventData) { }
        public virtual void OnCancel(BaseEventData eventData) { }
        public override void OnDeselect(BaseEventData eventData) { }
        public virtual void Rebuild(CanvasUpdate update) { }
        public virtual void LayoutComplete() { }
        public virtual void GraphicUpdateComplete() { }
        public virtual void CalculateLayoutInputHorizontal() { }
        public virtual void CalculateLayoutInputVertical() { }
        public virtual float minWidth => throw null;
        public virtual float preferredWidth => throw null;
        public virtual float flexibleWidth => throw null;
        public virtual float minHeight => throw null;
        public virtual float preferredHeight => throw null;
        public virtual float flexibleHeight => throw null;
        public virtual int layoutPriority => throw null;
        protected override void OnEnable() { }
        protected override void OnDisable() { }
        protected override void OnDestroy() { } // GUESS
        protected override void DoStateTransition(SelectionState state, bool instant) { }
    }
}
