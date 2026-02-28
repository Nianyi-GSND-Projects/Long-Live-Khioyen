Shader "Long Live Khioyen/Polis Ground"
{
	Properties
	{
		_Size("Size", Vector) = (1, 1, 0, 0)
		_Orientation("Orientation", Float) = 0

		[Header(Base Layer)]
		[NoScaleOffset] _BaseTex("Base Texture", 2D) = "white" {}
		_BaseNearTiling("Base Near Tiling", Float) = 20
		_BaseFarTiling("Base Far Tiling", Float) = 80
		_BaseBlendStrength("Base Blend Strength", Float) = 1.2

		[Header(Wearness Overlay)]
		[NoScaleOffset] _Wearness_Map("Wearness Map", 2D) = "black" {}
		_Wearness_Scale("Wearness Scale", Float) = 1
		_WearStripeFrequency("Wear Stripe Frequency", Float) = 20
		_WearColorA("Wear Color A", Color) = (0.25, 0.20, 0.15, 1)
		_WearColorB("Wear Color B", Color) = (0.55, 0.48, 0.40, 1)

		[Header(Border)]
		_BorderThickness("Border Thickness", Range(0, 0.5)) = 0.08
		_BorderColor("Border Color", Color) = (0.45, 0.38, 0.28, 1)
	}

	SubShader
	{
		Tags
		{
			"RenderPipeline" = "UniversalRenderPipeline"
			"RenderType" = "Opaque"
			"Queue" = "Geometry"
		}

		Pass
		{
			Name "GBuffer"
			Tags { "LightMode" = "UniversalGBuffer" }
			Cull Off
			ZWrite On
			ZTest LEqual

			HLSLPROGRAM
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag

			#pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
			#pragma multi_compile_fragment _ _RENDER_PASS_ENABLED

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityGBuffer.hlsl"

			TEXTURE2D(_BaseTex);
			SAMPLER(sampler_BaseTex);
			TEXTURE2D(_Wearness_Map);
			SAMPLER(sampler_Wearness_Map);

			CBUFFER_START(UnityPerMaterial)
				float4 _Size;
				float _Orientation;
				float _Wearness_Scale;
				float _BaseNearTiling;
				float _BaseFarTiling;
				float _BaseBlendStrength;
				float _WearStripeFrequency;
				float4 _WearColorA;
				float4 _WearColorB;
				float _BorderThickness;
				float4 _BorderColor;
			CBUFFER_END

			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
				float4 tangentOS : TANGENT;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float3 positionWS : TEXCOORD0;
				float3 normalWS : TEXCOORD1;
			};

			float2 Rotate2D(float2 v, float radians)
			{
				float s = sin(radians);
				float c = cos(radians);
				return float2(c * v.x - s * v.y, s * v.x + c * v.y);
			}

			float2 PolisGroundPosition(float3 positionWS)
			{
				// 把世界坐标转到城邦局部平面，并按朝向旋转。
				float2 planar = positionWS.xz;
				float angleRad = radians(_Orientation);
				return Rotate2D(planar, angleRad);
			}

			float2 QueryWearness(float2 polisPosition)
			{
				// 与数据层一致：按格点采样磨损贴图。
				float2 safeSize = max(_Size.xy, 1e-4.xx);
				float2 uv = polisPosition / safeSize;
				float4 sampleValue = SAMPLE_TEXTURE2D(_Wearness_Map, sampler_Wearness_Map, uv);
				return sampleValue.rg;
			}

			void CalculateBase(float2 polisPosition, out float3 albedo, out float height)
			{
				float nearTiling = max(_BaseNearTiling, 1e-4);
				float farTiling = max(_BaseFarTiling, 1e-4);
				float2 uvNear = polisPosition / nearTiling;
				float2 uvFar = polisPosition / farTiling;
				float3 c0 = SAMPLE_TEXTURE2D(_BaseTex, sampler_BaseTex, uvNear).rgb;
				float3 c1 = SAMPLE_TEXTURE2D(_BaseTex, sampler_BaseTex, uvFar).rgb;
				float blend = saturate(dot(c0, float3(0.3333, 0.3333, 0.3333)) * _BaseBlendStrength);
				albedo = lerp(c0, c1, blend);
				height = albedo.r;
			}

			void CalculateWearnessOverlay(float2 wearness, float2 polisPosition, out float3 albedo, out float heightDelta, out float alpha)
			{
				// 方向来自 wearness 向量，强度来自其长度。
				float magnitude = length(wearness);
				float2 direction = magnitude > 1e-5 ? normalize(wearness) : float2(1.0, 0.0);

				float2 gridPos = frac(polisPosition);
				float2 tangent = float2(gridPos.y, -gridPos.x);
				float stripe = cos(dot(tangent, direction) * _WearStripeFrequency);
				float signedStripe = 0.5 + 0.5 * stripe;

				alpha = saturate(magnitude);
				heightDelta = abs(stripe) * alpha;
				albedo = lerp(_WearColorA.rgb, _WearColorB.rgb, signedStripe);
			}

			float SmoothedBorderWeight(float2 polisPosition)
			{
				// 简化为仅厚度控制：单元格边缘硬阈值蒙版。
				float2 local = abs(frac(polisPosition) - 0.5);
				float maxLocal = max(local.x, local.y);
				float threshold = saturate(0.5 - _BorderThickness);
				return maxLocal >= threshold ? 1.0 : 0.0;
			}

			Varyings vert(Attributes IN)
			{
				Varyings OUT;
				VertexPositionInputs posInput = GetVertexPositionInputs(IN.positionOS.xyz);
				VertexNormalInputs normalInput = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

				OUT.positionCS = posInput.positionCS;
				OUT.positionWS = posInput.positionWS;
				OUT.normalWS = normalInput.normalWS;
				return OUT;
			}

			FragmentOutput frag(Varyings IN)
			{
				float2 polisPosition = PolisGroundPosition(IN.positionWS);

				float3 baseAlbedo;
				float baseHeight;
				CalculateBase(polisPosition, baseAlbedo, baseHeight);

				float2 wearness = QueryWearness(polisPosition) * _Wearness_Scale;
				float3 overlayAlbedo;
				float overlayHeight;
				float overlayAlpha;
				CalculateWearnessOverlay(wearness, polisPosition, overlayAlbedo, overlayHeight, overlayAlpha);

				float wearBlend = saturate(overlayAlpha);
				float3 albedo = lerp(baseAlbedo, overlayAlbedo, wearBlend);

				float borderMask = SmoothedBorderWeight(polisPosition);
				float borderBlend = saturate(borderMask * _BorderColor.a);
				albedo = lerp(albedo, _BorderColor.rgb, borderBlend);

				InputData inputData = (InputData)0;
				inputData.positionWS = IN.positionWS;
				inputData.normalWS = normalize(IN.normalWS);
				inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);
				inputData.shadowCoord = float4(0, 0, 0, 0);
				inputData.fogCoord = 0;
				inputData.vertexLighting = half3(0, 0, 0);
				inputData.bakedGI = SampleSH(inputData.normalWS);
				inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);
				inputData.shadowMask = half4(1, 1, 1, 1);
				inputData.positionCS = IN.positionCS;

				SurfaceData surfaceData = (SurfaceData)0;
				surfaceData.albedo = albedo;
				surfaceData.specular = half3(0.04, 0.04, 0.04);
				surfaceData.metallic = 0;
				surfaceData.smoothness = 0.5;
				surfaceData.normalTS = half3(0, 0, 1);
				surfaceData.occlusion = 1;
				surfaceData.emission = half3(0, 0, 0);
				surfaceData.alpha = 1;
				surfaceData.clearCoatMask = 0;
				surfaceData.clearCoatSmoothness = 0;

				half3 gi = albedo * inputData.bakedGI;
				return SurfaceDataToGbuffer(surfaceData, inputData, gi, kLightingLit);
			}
			ENDHLSL
		}
	}

	FallBack Off
}
