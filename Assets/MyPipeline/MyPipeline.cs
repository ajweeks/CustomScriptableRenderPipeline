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

    private bool _dynamicBatching = false;
    private bool _instancing = false;

    const int _maxVisibleLights = 4;
    static int _visibleLightColorsID = Shader.PropertyToID("_VisibleLightColors");
    static int _visibleLightDirectionsOrPositionsID = Shader.PropertyToID("_VisibleLightDirectionsOrPositions");
    static int _visibleLightAttenuationsID = Shader.PropertyToID("_VisibleLightAttenuations");

    private Vector4[] _visibleLightColors = new Vector4[_maxVisibleLights];
    private Vector4[] _visibleLightDirectionsOrPositions = new Vector4[_maxVisibleLights];
    private Vector4[] _visibleLightAttenuations = new Vector4[_maxVisibleLights];

    public MyPipeline(bool dynamicBatching, bool instancing)
    {
        GraphicsSettings.lightsUseLinearIntensity = true;
        _dynamicBatching = dynamicBatching;
        _instancing = instancing;
    }
    
    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        for (int i = 0; i < cameras.Length; ++i)
        {
            Render(context, cameras[i]);
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

        ConfigureLights();

        _commandBuffer.BeginSample("Render Camera");
        _commandBuffer.SetGlobalVectorArray(_visibleLightColorsID, _visibleLightColors);
        _commandBuffer.SetGlobalVectorArray(_visibleLightDirectionsOrPositionsID, _visibleLightDirectionsOrPositions);
        _commandBuffer.SetGlobalVectorArray(_visibleLightAttenuationsID, _visibleLightAttenuations);
        context.ExecuteCommandBuffer(_commandBuffer);
        _commandBuffer.Clear();

        var sortingSettings = new SortingSettings(camera)
        {
            criteria = SortingCriteria.CommonOpaque
        };
	
        var drawSettings = new DrawingSettings(new ShaderTagId("SRPDefaultUnlit"), sortingSettings);
        drawSettings.enableDynamicBatching = _dynamicBatching;
        drawSettings.enableInstancing = _instancing;
        
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

    void ConfigureLights()
    {
        int visibleLightCount = Mathf.Min(_maxVisibleLights, _cull.visibleLights.Length);
        for (int i = 0; i < visibleLightCount; ++i)
        {
            VisibleLight light = _cull.visibleLights[i];
            _visibleLightColors[i] = light.finalColor;
            Vector4 attenuation = Vector4.zero;
            if (light.lightType == LightType.Directional)
            {
                Vector4 lightDir = light.localToWorldMatrix.GetColumn(2);
                lightDir.x = -lightDir.x;
                lightDir.y = -lightDir.y;
                lightDir.z = -lightDir.z;
                _visibleLightDirectionsOrPositions[i] = lightDir;
            }
            else
            {
                _visibleLightDirectionsOrPositions[i] = light.localToWorldMatrix.GetColumn(3);
                attenuation.x = 1.0f / Mathf.Max(light.range * light.range, 0.00001f);
            }
            _visibleLightAttenuations[i] = attenuation;
        }
        for (int i = visibleLightCount; i < _maxVisibleLights; ++i)
        {
            _visibleLightColors[i] = Color.clear;
        }
    }
}
