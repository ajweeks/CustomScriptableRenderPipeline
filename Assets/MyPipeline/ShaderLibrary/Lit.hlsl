﻿
#ifndef MYRP_LIT_INCLUDED
#define MYRP_LIT_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

#define MAX_VISIBLE_LIGHTS 4

CBUFFER_START(UnityPerFrame)
    float4x4 unity_MatrixVP;
CBUFFER_END

CBUFFER_START(UnityPerDraw)
    float4x4 unity_ObjectToWorld;
CBUFFER_END

CBUFFER_START(_LightBuffer)
	float4 _VisibleLightColors[MAX_VISIBLE_LIGHTS];
	float4 _VisibleLightDirectionsOrPositions[MAX_VISIBLE_LIGHTS];
	float4 _VisibleLightAttenuations[MAX_VISIBLE_LIGHTS];
CBUFFER_END

#define UNITY_MATRIX_M unity_ObjectToWorld
// This include will redefine UNITY_MATRIX_M to a matrix array when using instancing 
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"

UNITY_INSTANCING_BUFFER_START(PerInstance)
    // This will be defined as an array when instancing, and just a single float4 otherwise  
    UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
UNITY_INSTANCING_BUFFER_END(PerInstance)


struct VertexInput 
{
    float4 pos : POSITION;
	float3 normal : NORMAL;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VertexOutput
{
    float4 clipPos : SV_POSITION;
	float3 normal : TEXCOORD0;
	float3 worldPos : TEXCOORD1;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

float3 DiffuseLight(int lightIndex, float3 normal, float3 worldPos)
{
	float3 lightColor = _VisibleLightColors[lightIndex].rgb;
	float4 lightDirectionOrPosition = _VisibleLightDirectionsOrPositions[lightIndex];
	float3 lightVector = lightDirectionOrPosition.xyz -worldPos * lightDirectionOrPosition.w;
	float lightAttenuation = _VisibleLightAttenuations[lightIndex];
	float rangeFade = dot(lightVector, lightVector) * lightAttenuation;
	rangeFade = saturate(1.0 - rangeFade * rangeFade);
	rangeFade *= rangeFade;
	float3 lightDirection = normalize(lightVector);
	float diffuse = saturate(dot(normal, lightDirection));
	float sqrDist = max(dot(lightVector, lightVector), 0.00001);
	diffuse *= rangeFade / sqrDist;
	return diffuse * lightColor;
}

VertexOutput LitPassVertex(VertexInput input)
{
    VertexOutput output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    float4 worldPos = mul(UNITY_MATRIX_M, float4(input.pos.xyz, 1.0));
	output.worldPos = worldPos.xyz;
    output.clipPos = mul(unity_MatrixVP, worldPos);
	output.normal = mul((float3x3)UNITY_MATRIX_M, input.normal);
    return output;
}

float4 LitPassFragment(VertexOutput input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
	input.normal = normalize(input.normal);
	
	float3 diffuseLight = 0;
	for (int i = 0; i < MAX_VISIBLE_LIGHTS; ++i)
	{
		diffuseLight += DiffuseLight(i, input.normal, input.worldPos);
	}

	float3 albedo = UNITY_ACCESS_INSTANCED_PROP(PerInstance, _Color).rgb;
    return float4(albedo * diffuseLight, 1);
}


#endif // MYRP_LIT_INCLUDED
