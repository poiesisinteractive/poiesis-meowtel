// EXPECT: CS0507
// RenderPipelineAsset.CreatePipeline is protected in Unity: overriding it as public must fail.
using UnityEngine.Rendering;
namespace Meowtel.HarnessSelfTest
{
    public class BadOverride : RenderPipelineAsset
    {
        public override RenderPipeline CreatePipeline() => null;
    }
}
