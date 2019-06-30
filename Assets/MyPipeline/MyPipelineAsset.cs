using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/MyPipeline")]
public class MyPipelineAsset : RenderPipelineAsset
{
    [SerializeField] bool dynamicBatching;
    [SerializeField] bool instancing;
    
    protected override RenderPipeline CreatePipeline()
    {
        return new MyPipeline(dynamicBatching, instancing);
    }
}
