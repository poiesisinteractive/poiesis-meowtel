// STUB of com.unity.render-pipelines.universal 17.3 (Unity 6) - namespace UnityEngine.Rendering.Universal.
// Compile-only, minimal: UniversalRenderPipeline.asset + common UniversalRenderPipelineAsset settings, Light2D basics.
// Members marked // GUESS were not verified against the package source.
using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering.Universal
{
    public enum MsaaQuality { Disabled = 1, _2x = 2, _4x = 4, _8x = 8 }
    public enum UpscalingFilterSelection { Auto = 0, Linear = 1, Point = 2, FSR = 3, STP = 4 } // GUESS: member list

    public partial class UniversalRenderPipelineAsset : RenderPipelineAsset
    {
        protected override RenderPipeline CreatePipeline() => throw null;
        public float renderScale { get => throw null; set { } }
        public int msaaSampleCount { get => throw null; set { } }
        public bool supportsHDR { get => throw null; set { } }
        public bool supportsCameraDepthTexture { get => throw null; set { } }
        public bool supportsCameraOpaqueTexture { get => throw null; set { } }
        public float shadowDistance { get => throw null; set { } }
        public int shadowCascadeCount { get => throw null; set { } }
        public bool useSRPBatcher { get => throw null; set { } }
        public bool supportsDynamicBatching { get => throw null; set { } }
        public UpscalingFilterSelection upscalingFilter { get => throw null; set { } }
        public int maxAdditionalLightsCount { get => throw null; set { } }
    }

    public sealed partial class UniversalRenderPipeline : RenderPipeline
    {
        public const string k_ShaderTagName = "UniversalPipeline";
        public UniversalRenderPipeline(UniversalRenderPipelineAsset asset) { }
        public static UniversalRenderPipelineAsset asset => throw null;
        public static float maxShadowBias => throw null;
        public static float minRenderScale => throw null;
        public static float maxRenderScale => throw null;
        protected override void Render(ScriptableRenderContext renderContext, Camera[] cameras) { }
    }

    [DisallowMultipleComponent]
    public sealed partial class Light2D : UnityEngine.U2D.Light2DBase
    {
        public enum LightType { Parametric = 0, Freeform = 1, Sprite = 2, Point = 3, Global = 4 }
        public LightType lightType { get => throw null; set { } }
        public Color color { get => throw null; set { } }
        public float intensity { get => throw null; set { } }
        public float falloffIntensity { get => throw null; set { } }
        public float pointLightInnerRadius { get => throw null; set { } }
        public float pointLightOuterRadius { get => throw null; set { } }
        public float pointLightInnerAngle { get => throw null; set { } }
        public float pointLightOuterAngle { get => throw null; set { } }
        public bool shadowsEnabled { get => throw null; set { } } // GUESS
        public float shadowIntensity { get => throw null; set { } }
        public int blendStyleIndex { get => throw null; set { } }
        public Sprite lightCookieSprite { get => throw null; set { } }
    }

}
