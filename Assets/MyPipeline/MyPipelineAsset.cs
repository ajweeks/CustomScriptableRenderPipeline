using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/MyPipeline")]
public class MyPipelineAsset : RenderPipelineAsset
{
    [SerializeField] private bool _dynamicBatching;
    [SerializeField] private bool _instancing;
    
    protected override RenderPipeline CreatePipeline()
    {
        return new MyPipeline(_dynamicBatching, _instancing);
    }
}
