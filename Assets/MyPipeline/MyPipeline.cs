using UnityEngine;
using UnityEngine.Rendering;
using Conditional = System.Diagnostics.ConditionalAttribute;

public class MyPipeline : RenderPipeline
{
    private CullingResults _cull;
    private CommandBuffer _commandBuffer = new CommandBuffer
    {
        name = "Render Camera"
    };

    private Material _errorMat;
    
    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        foreach (var camera in cameras)
        {
            Render(context, camera);
        }
    }

    private void Render(ScriptableRenderContext context, Camera camera)
    {
        ScriptableCullingParameters cullingParameters;
        if (!camera.TryGetCullingParameters(out cullingParameters))
        {
            return;
        }

        #if UNITY_EDITOR
        if (camera.cameraType == CameraType.SceneView)
        {
            ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
        }
        #endif

        _cull = context.Cull(ref cullingParameters);
        
        context.SetupCameraProperties(camera);

        CameraClearFlags clearFlags = camera.clearFlags;
        _commandBuffer.ClearRenderTarget(
            clearFlags == CameraClearFlags.Depth || clearFlags == CameraClearFlags.Skybox,
            clearFlags == CameraClearFlags.Color || clearFlags == CameraClearFlags.Skybox,
            camera.backgroundColor);
        _commandBuffer.BeginSample("Render Camera");
        context.ExecuteCommandBuffer(_commandBuffer);
        _commandBuffer.Clear();

        var sortingSettings = new SortingSettings(camera)
        {
            criteria = SortingCriteria.CommonOpaque
        };

        var drawSettings = new DrawingSettings(new ShaderTagId("SRPDefaultUnlit"), sortingSettings);
        
        var filterSettings = new FilteringSettings(RenderQueueRange.opaque);
        context.DrawRenderers(_cull, ref drawSettings, ref filterSettings);
        context.DrawSkybox(camera);

        sortingSettings.criteria = SortingCriteria.CommonTransparent;
        drawSettings.sortingSettings = sortingSettings;
        filterSettings.renderQueueRange = RenderQueueRange.transparent;
        context.DrawRenderers(_cull, ref drawSettings, ref filterSettings);

        DrawDefaultPipeline(context, camera);
        
        _commandBuffer.EndSample("Render Camera");
        context.ExecuteCommandBuffer(_commandBuffer);
        _commandBuffer.Clear();
        
        context.Submit();
    }

    [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
    private void DrawDefaultPipeline(ScriptableRenderContext context, Camera camera)
    {
        if (_errorMat == null)
        {
            Shader errorShader = Shader.Find("Hidden/InternalErrorShader");
            _errorMat = new Material(errorShader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
        }
        
        var sortingSettings = new SortingSettings(camera)
        {
            criteria = SortingCriteria.CommonOpaque
        };
        var drawSettings = new DrawingSettings(new ShaderTagId("ForwardBase"), sortingSettings);
        drawSettings.SetShaderPassName(1, new ShaderTagId("PrepassBase"));
        drawSettings.SetShaderPassName(2, new ShaderTagId("Always"));
        drawSettings.SetShaderPassName(3, new ShaderTagId("Vertex"));
        drawSettings.SetShaderPassName(4, new ShaderTagId("VertexLMRGBM"));
        drawSettings.SetShaderPassName(5, new ShaderTagId("VertexLM"));
        drawSettings.overrideMaterial = _errorMat;
        var filterSettings = new FilteringSettings(RenderQueueRange.all);
        context.DrawRenderers(_cull, ref drawSettings, ref filterSettings);
    }
}
