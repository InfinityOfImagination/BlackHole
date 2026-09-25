// Void Mart - dusk gradient skybox with a soft sun bloom and a faint star field.
Shader "VoidMart/SkyGradient"
{
    Properties
    {
        _TopColor ("Zenith", Color) = (0.10,0.08,0.20,1)
        _HorizonColor ("Horizon", Color) = (1.0,0.55,0.42,1)
        _GroundColor ("Below", Color) = (0.12,0.11,0.18,1)
        _HorizonHeight ("Horizon Height", Range(-1,1)) = 0.0
        _HorizonSharpness ("Horizon Sharpness", Range(0.2,12)) = 2.2
        _SunColor ("Sun Colour", Color) = (1,0.85,0.7,1)
        _SunSize ("Sun Size", Range(0.9,0.9999)) = 0.995
        _SunGlow ("Sun Glow", Range(1,64)) = 18
        _StarStrength ("Star Strength", Range(0,2)) = 0.45
        _StarDensity ("Star Density", Range(20,400)) = 160
        _Exposure ("Exposure", Range(0,4)) = 1
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Background" "Queue" = "Background" }

        Pass
        {
            Name "Sky"
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _TopColor;
                half4 _HorizonColor;
                half4 _GroundColor;
                half4 _SunColor;
                half _HorizonHeight;
                half _HorizonSharpness;
                half _SunSize;
                half _SunGlow;
                half _StarStrength;
                half _StarDensity;
                half _Exposure;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 direction : TEXCOORD0;
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.direction = input.positionOS.xyz;
                return output;
            }

            float Hash(float3 p)
            {
                p = frac(p * 0.3183099 + float3(0.71, 0.113, 0.419));
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                float3 direction = normalize(input.direction);
                half height = direction.y - _HorizonHeight;

                half up = saturate(pow(saturate(height), 1.0 / max(0.01, _HorizonSharpness)));
                half3 sky = lerp(_HorizonColor.rgb, _TopColor.rgb, up);
                half below = saturate(-height * 4.0);
                sky = lerp(sky, _GroundColor.rgb, below);

                half3 sunDirection = normalize(_MainLightPosition.xyz);
                half sunDot = saturate(dot(direction, sunDirection));
                half disc = smoothstep(_SunSize, 1.0, sunDot);
                half glow = pow(sunDot, _SunGlow);
                sky += _SunColor.rgb * (disc * 2.0 + glow * 0.55);

                // Sparse stars, only above the horizon and away from the sun.
                float3 cell = floor(direction * _StarDensity);
                float star = Hash(cell);
                half starMask = step(0.9985, star) * saturate(height * 3.0) * (1.0 - glow);
                sky += starMask * _StarStrength;

                return half4(sky * _Exposure, 1);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
