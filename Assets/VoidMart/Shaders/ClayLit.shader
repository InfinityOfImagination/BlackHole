// Void Mart - stylised "clay" surface shader for URP.
// Wrapped diffuse + tinted shade colour + rim light gives the soft modelling-clay
// read the art direction asks for, at a fraction of the cost of a PBR pass.
// Stencil state is exposed so the same shader can be the punched ground plane,
// the pit interior, or an ordinary prop.
Shader "VoidMart/ClayLit"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Colour", Color) = (1,1,1,1)
        _ShadeColor ("Shade Tint", Color) = (0.45,0.42,0.62,1)
        _RimColor ("Rim Colour", Color) = (0.6,0.9,1,1)
        _EmissionColor ("Emission", Color) = (0,0,0,1)
        _Wrap ("Diffuse Wrap", Range(0,1)) = 0.55
        _ShadeStrength ("Shade Strength", Range(0,1)) = 0.45
        _RimStrength ("Rim Strength", Range(0,2)) = 0.35
        _RimPower ("Rim Power", Range(0.5,16)) = 3.2
        _SpecStrength ("Specular", Range(0,1)) = 0.18
        _Smoothness ("Smoothness", Range(0,1)) = 0.5
        _AmbientBoost ("Ambient Boost", Range(0,2)) = 1.0
        _VertexColorAmount ("Vertex Colour", Range(0,1)) = 1.0
        _ShadowTint ("Shadow Tint", Color) = (0.35,0.32,0.5,1)

        [Header(Stencil)]
        _StencilRef ("Stencil Ref", Range(0,255)) = 0
        _StencilComp ("Stencil Compare", Float) = 8   // Always
        _StencilOp ("Stencil Pass Op", Float) = 0     // Keep
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Stencil
        {
            Ref [_StencilRef]
            Comp [_StencilComp]
            Pass [_StencilOp]
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull [_Cull]
            ZWrite On

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma target 3.0

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _ShadeColor;
                half4 _RimColor;
                half4 _EmissionColor;
                half4 _ShadowTint;
                half _Wrap;
                half _ShadeStrength;
                half _RimStrength;
                half _RimPower;
                half _SpecStrength;
                half _Smoothness;
                half _AmbientBoost;
                half _VertexColorAmount;
                half _StencilRef;
                half _StencilComp;
                half _StencilOp;
                half _Cull;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half3 normalWS : TEXCOORD2;
                half4 color : TEXCOORD3;
                float fogFactor : TEXCOORD4;
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normals = GetVertexNormalInputs(input.normalOS);

                output.positionCS = positions.positionCS;
                output.positionWS = positions.positionWS;
                output.normalWS = normals.normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.color = input.color;
                output.fogFactor = ComputeFogFactor(positions.positionCS.z);
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                half4 texel = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half3 vertexTint = lerp(half3(1, 1, 1), input.color.rgb, _VertexColorAmount);
                half3 albedo = texel.rgb * _BaseColor.rgb * vertexTint;

                half3 normalWS = normalize(input.normalWS);
                half3 viewWS = normalize(GetWorldSpaceViewDir(input.positionWS));

                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);

                // Wrapped lambert: the signature soft falloff of clay/putty shading.
                half ndotl = dot(normalWS, mainLight.direction);
                half wrapped = saturate((ndotl + _Wrap) / (1.0h + _Wrap));
                half shadow = lerp(1.0h, mainLight.shadowAttenuation, 0.85h);
                half lightMask = wrapped * shadow;

                half3 shadeTint = lerp(_ShadeColor.rgb, half3(1, 1, 1), lightMask);
                shadeTint = lerp(half3(1, 1, 1), shadeTint, _ShadeStrength);

                half3 ambient = SampleSH(normalWS) * _AmbientBoost;
                half3 shadowed = lerp(_ShadowTint.rgb, half3(1, 1, 1), shadow);
                half3 lighting = ambient * shadowed + mainLight.color * lightMask;

                half3 color = albedo * shadeTint * lighting;

                // Specular pop keeps the vinyl-toy sheen on curved props.
                half3 halfVector = normalize(mainLight.direction + viewWS);
                half specular = pow(saturate(dot(normalWS, halfVector)), lerp(8.0h, 96.0h, _Smoothness));
                color += mainLight.color * specular * _SpecStrength * shadow;

                // Rim separates objects from the desaturated street behind them.
                half rim = pow(saturate(1.0h - saturate(dot(normalWS, viewWS))), _RimPower);
                color += _RimColor.rgb * rim * _RimStrength;

#ifdef _ADDITIONAL_LIGHTS
                int lightCount = GetAdditionalLightsCount();
                for (int i = 0; i < lightCount; i++)
                {
                    Light light = GetAdditionalLight((uint)i, input.positionWS);
                    half additionalWrap = saturate((dot(normalWS, light.direction) + _Wrap) / (1.0h + _Wrap));
                    color += albedo * light.color * additionalWrap * light.distanceAttenuation * light.shadowAttenuation;
                }
#endif

                color += _EmissionColor.rgb;
                color = MixFog(color, input.fogFactor);
                return half4(color, 1.0h);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex ShadowVertex
            #pragma fragment ShadowFragment
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _ShadeColor;
                half4 _RimColor;
                half4 _EmissionColor;
                half4 _ShadowTint;
                half _Wrap;
                half _ShadeStrength;
                half _RimStrength;
                half _RimPower;
                half _SpecStrength;
                half _Smoothness;
                half _AmbientBoost;
                half _VertexColorAmount;
                half _StencilRef;
                half _StencilComp;
                half _StencilOp;
                half _Cull;
            CBUFFER_END

            float3 _LightDirection;

            struct ShadowAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct ShadowVaryings
            {
                float4 positionCS : SV_POSITION;
            };

            ShadowVaryings ShadowVertex(ShadowAttributes input)
            {
                ShadowVaryings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));

#if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
#else
                positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
#endif
                output.positionCS = positionCS;
                return output;
            }

            half4 ShadowFragment(ShadowVaryings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex DepthVertex
            #pragma fragment DepthFragment
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _ShadeColor;
                half4 _RimColor;
                half4 _EmissionColor;
                half4 _ShadowTint;
                half _Wrap;
                half _ShadeStrength;
                half _RimStrength;
                half _RimPower;
                half _SpecStrength;
                half _Smoothness;
                half _AmbientBoost;
                half _VertexColorAmount;
                half _StencilRef;
                half _StencilComp;
                half _StencilOp;
                half _Cull;
            CBUFFER_END

            struct DepthAttributes { float4 positionOS : POSITION; };
            struct DepthVaryings { float4 positionCS : SV_POSITION; };

            DepthVaryings DepthVertex(DepthAttributes input)
            {
                DepthVaryings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 DepthFragment(DepthVaryings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/Unlit"
}
