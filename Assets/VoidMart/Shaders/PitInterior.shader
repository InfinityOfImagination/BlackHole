// Void Mart - the cavity under the street.
// Renders only where the hole mask wrote its stencil value, so the player sees a
// real 3D shaft rather than a decal. Unlit on purpose: the void should read as an
// absence of light with a hot event-horizon ring at the lip.
Shader "VoidMart/PitInterior"
{
    Properties
    {
        _TopColor ("Rim Colour", Color) = (0.16,0.10,0.31,1)
        _DeepColor ("Deep Colour", Color) = (0.02,0.02,0.05,1)
        _RingColor ("Event Horizon", Color) = (0,0.9,1,1)
        _RingWidth ("Ring Width", Range(0.001,0.4)) = 0.08
        _RingPower ("Ring Falloff", Range(0.5,8)) = 2.4
        _SwirlSpeed ("Swirl Speed", Range(0,4)) = 0.8
        _SwirlStrength ("Swirl Strength", Range(0,1)) = 0.25
        _Depth ("Gradient Depth", Range(0.1,4)) = 1.0
        _StencilRef ("Stencil Ref", Range(0,255)) = 1
        _StencilComp ("Stencil Compare", Float) = 3   // Equal
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry+1"
        }

        Stencil
        {
            Ref [_StencilRef]
            Comp [_StencilComp]
            Pass Keep
        }

        Pass
        {
            Name "PitUnlit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Front       // we are looking at the inside of the shaft
            ZWrite On

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma target 3.0
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _TopColor;
                half4 _DeepColor;
                half4 _RingColor;
                half _RingWidth;
                half _RingPower;
                half _SwirlSpeed;
                half _SwirlStrength;
                half _Depth;
                half _StencilRef;
                half _StencilComp;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionOS : TEXCOORD1;
                float fogFactor : TEXCOORD2;
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positions.positionCS;
                output.uv = input.uv;
                output.positionOS = input.positionOS.xyz;
                output.fogFactor = ComputeFogFactor(positions.positionCS.z);
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                // 0 at the lip, 1 deep down the shaft.
                half depth = saturate((0.5 - input.positionOS.y) / max(0.001, _Depth));

                half swirl = sin(atan2(input.positionOS.z, input.positionOS.x) * 3.0
                                 + depth * 9.0 - _Time.y * _SwirlSpeed * 3.0);
                half swirlMask = saturate(swirl * 0.5 + 0.5) * _SwirlStrength * (1.0 - depth);

                half3 body = lerp(_TopColor.rgb, _DeepColor.rgb, saturate(depth * 1.6));
                body += _RingColor.rgb * swirlMask * 0.35;

                half ring = pow(saturate(1.0 - depth / max(0.001, _RingWidth)), _RingPower);
                half3 color = body + _RingColor.rgb * ring;

                color = MixFog(color, input.fogFactor);
                return half4(color, 1);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
