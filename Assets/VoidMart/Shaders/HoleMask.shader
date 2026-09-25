// Void Mart - stencil writer for the black hole.
// A circular quad that tracks the player writes a reference value into the stencil
// buffer with colour and depth writes disabled. The ground then refuses to draw
// where the stencil matches, revealing the pit mesh sitting under the street.
Shader "VoidMart/HoleMask"
{
    Properties
    {
        _StencilRef ("Stencil Ref", Range(0,255)) = 1
        _Radius ("Circle Radius (UV)", Range(0.05,0.5)) = 0.5
        _Squash ("Edge Squash", Range(0,1)) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry-100"
        }

        Pass
        {
            Name "StencilWrite"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            ZWrite Off
            ZTest Always
            Cull Off
            ColorMask 0

            Stencil
            {
                Ref [_StencilRef]
                Comp Always
                Pass Replace
            }

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half _StencilRef;
                half _Radius;
                half _Squash;
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
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                float2 centred = input.uv - 0.5;
                centred.y *= 1.0 + _Squash;
                clip(_Radius - length(centred));
                return 0;
            }
            ENDHLSL
        }
    }
}
