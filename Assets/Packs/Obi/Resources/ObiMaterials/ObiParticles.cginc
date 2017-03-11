#ifndef OBIPARTICLES_INCLUDED
#define OBIPARTICLES_INCLUDED

#include "UnityCG.cginc"
#include "AutoLight.cginc"

float4x4 _Camera_to_World;
float4x4 _World_to_Camera;
float4x4 _InvProj;

float3 _FarCorner;

float3 BillboardSphereNormals(float2 texcoords)
{
	float3 n;
	n.xy = texcoords*2.0-1.0;
	float r2 = dot(n.xy, n.xy);
	clip (1 - r2);   // clip pixels outside circle
	n.z = sqrt(1.0 - r2);
	return n;
}

float BillboardSphereThickness(float2 texcoords)
{
	float2 n = texcoords*2.0-1.0;
	float r2 = dot(n.xy, n.xy);
	clip (1 - r2);   // clip pixels outside circle
	return sqrt(1.0 - r2)*2.0f*exp(-r2*2.0f);
}

half3 SampleSphereAmbientWorld(float3 worldNormal)
{
	#if UNITY_SHOULD_SAMPLE_SH 
		#if (SHADER_TARGET < 30)
			return ShadeSH9(half4(worldNormal, 1.0));
		#else
			// Optimization: L2 per-vertex, L0..L1 per-pixel
			return ShadeSH3Order(half4(worldNormal, 1.0));
		#endif
	#else
		return UNITY_LIGHTMODEL_AMBIENT;
	#endif
}

half3 SampleSphereAmbient(float3 eyeNormal)
{
	#if UNITY_SHOULD_SAMPLE_SH
		half3 worldNormal = mul(transpose((float3x3)UNITY_MATRIX_V),eyeNormal);  
		#if (SHADER_TARGET < 30)
			return ShadeSH9(half4(worldNormal, 1.0));
		#else
			// Optimization: L2 per-vertex, L0..L1 per-pixel
			return ShadeSH3Order(half4(worldNormal, 1.0));
		#endif
	#else
		return UNITY_LIGHTMODEL_AMBIENT;
	#endif
}

inline float3 EyeSpaceLightDir( in float3 v )
{
	float3 eyeSpaceLightPos = mul(_World_to_Camera, _WorldSpaceLightPos0).xyz;
	#ifndef USING_LIGHT_MULTI_COMPILE
		return eyeSpaceLightPos.xyz - v.xyz * _WorldSpaceLightPos0.w;
	#else
		#ifndef USING_DIRECTIONAL_LIGHT
		return eyeSpaceLightPos.xyz - v.xyz;
		#else
		return eyeSpaceLightPos.xyz;
		#endif
	#endif
}

float3 EyePosFromDepth(float2 uv,float depth){

	float3 ray = (half3(-0.5f,-0.5f,0) + half3(uv.xy,-1)) * _FarCorner;
	return ray * Linear01Depth(depth);

}

#endif
