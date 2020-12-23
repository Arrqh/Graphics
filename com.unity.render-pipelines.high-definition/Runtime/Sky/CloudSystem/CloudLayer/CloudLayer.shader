Shader "Hidden/HDRP/Sky/CloudLayer"
{
    HLSLINCLUDE

    #pragma vertex Vert

    //#pragma enable_d3d11_debug_symbols
    #pragma editor_sync_compilation
    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone vulkan metal switch

    #pragma multi_compile_local _ USE_CLOUD_MOTION
    #pragma multi_compile_local _ USE_FLOWMAP
    #pragma multi_compile_local _ USE_SECOND_CLOUD_LAYER
    #pragma multi_compile_local _ USE_SECOND_CLOUD_MOTION
    #pragma multi_compile_local _ USE_SECOND_FLOWMAP

	#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonLighting.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Sky/CloudSystem/CloudLayer/CloudLayerCommon.hlsl"
	#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Sky/PhysicallyBasedSky/PhysicallyBasedSkyCommon.hlsl"
	#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/AtmosphericScattering/AtmosphericScattering.hlsl"

    struct Attributes
    {
        uint vertexID : SV_VertexID;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        UNITY_VERTEX_OUTPUT_STEREO
    };

	float3   _WorldSpaceCameraPos1;
    float4x4 _ViewMatrix1;
    #undef UNITY_MATRIX_V
    #define UNITY_MATRIX_V _ViewMatrix1

	float _Anisotropy;

    Varyings Vert(Attributes input)
    {
        Varyings output;
        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
        output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID, UNITY_RAW_FAR_CLIP_VALUE);
        return output;
    }

	float4 GenerateClouds(float2 positionCS, bool exposure)
	{
		return RenderClouds(positionCS.xy);
	}

    float4 FragBaking(Varyings input) : SV_Target
    {
        return RenderClouds(input.positionCS.xy);
    }

    float4 FragRender(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        float4 cloudAlbedo = RenderClouds(input.positionCS.xy);
		float4 color = float4(0, 0, 0, cloudAlbedo.a);
        
		//we need to compute the lighting component on the clouds
		//this means:
		//1) evaluate directional lights correctly
		//2) include a mie scattering term
		//3) ambient light?

		//it would be cool if we could generate some sort of normals for the clouds? not sure how, but probably during the precompute step (or we could just make a texture ahead of time)
		//this could let us get a better ambient term as well as add the "powder" effect some volumetric systems use to give the clouds a little more detail

		float3 V = GetSkyViewDirWS(input.positionCS);

		float anisotropy = _Anisotropy;

		for (uint i = 0; i < _DirectionalLightCount; i++)
        {
			DirectionalLightData light = _DirectionalLightDatas[i];

			float3 X = _WorldSpaceCameraPos1;
			float3 C = _PlanetCenterPosition.xyz;
			float3 L = -light.forward;

			float r2        = distance(X, C);
			float cosHoriz = ComputeCosineOfHorizonAngle(r2);
			float cosTheta = dot(X - C, L) * rcp(r2); // Normalize

			if (cosTheta >= cosHoriz) // Above horizon
			{
				float3 oDepth = ComputeAtmosphericOpticalDepth(r2, cosTheta, true);
				// Cannot do this once for both the sky and the fog because the sky may be desaturated. :-(
				float3 transm  = TransmittanceFromOpticalDepth(oDepth);
				float3 opacity = 1 - transm;
				light.color.rgb *= 1 - (Desaturate(opacity, _AlphaSaturation) * _AlphaMultiplier);
			}
			else
			{
				// return 0; // Kill the light. This generates a warning, so can't early out. :-(
			  light. color = 0;
			}

			float cosTheta2 = dot(L, V);
            float phase    = CornetteShanksPhasePartVarying(anisotropy, cosTheta2);
			color.rgb += cloudAlbedo.rgb * light.color.rgb * GetCurrentExposureMultiplier() * phase;
		}

		
		float cloudHeight = 18000;
		//we need to calcuate the distance from the ground to the cloud layer
		//lets just assume for clouds that we are definitely between the ground and the edge of the atmosphere

		const float R = _PlanetaryRadius;
		const float3 O = _WorldSpaceCameraPos1 - _PlanetCenterPosition.xyz;
		float3 N; float r; // These params correspond to the entry point
        float tEntry = IntersectAtmosphere(O, V, N, r).x;

		float NdotV  = dot(N, V);
        float cosChi = -NdotV;

		float tGround = tEntry + IntersectSphere(R, cosChi, r).x;
		float tCloud = tEntry + IntersectSphere(R + cloudHeight, cosChi, r).x;

		float3 skyColor;
		float3 skyOpacity;
		EvaluatePbrAtmosphere(_WorldSpaceCameraPos1, V, tCloud, false, skyColor, skyOpacity);

		color.rgb = color.rgb * (1.0 - skyOpacity.rgb) + skyColor.rgb;

		color.rgb *= GetCurrentExposureMultiplier();

        return color;
    }

    ENDHLSL

    SubShader
    {
        Pass
        {
            ZWrite Off
            ZTest Always
            Blend One OneMinusSrcAlpha // Premultiplied alpha
            Cull Off

            HLSLPROGRAM
                #pragma fragment FragBaking
            ENDHLSL

        }

        Pass
        {
            ZWrite Off
            ZTest LEqual
            Blend One OneMinusSrcAlpha // Premultiplied alpha
            Cull Off

            HLSLPROGRAM
                #pragma fragment FragRender
            ENDHLSL
        }

    }
    Fallback Off
}
