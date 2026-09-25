// Void Mart - screen-space circular iris used for scene transitions.
// The quad covers the screen and punches a hole of _Radius pixels around _Center, so the
// wipe can be centred on the player's hole exactly as the UX spec describes.
Shader "VoidMart/IrisWipe"
{
    Properties
    {
        _Color ("Colour", Color) = (0.043,0.039,0.078,1)
        _Center ("Centre (pixels)", Vector) = (0,0,0,0)
        _Radius ("Radius (pixels)", Float) = 2000
        _Softness ("Edge Softness", Float) = 6
        _RingColor ("Ring Colour", Color) = (0,0.9,1,1)
        _RingWidth ("Ring Width", Float) = 10
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Overlay"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                float4 color : COLOR;
            };

            fixed4 _Color;
            float4 _Center;
            float _Radius;
            float _Softness;
            fixed4 _RingColor;
            float _RingWidth;

            v2f vert(appdata input)
            {
                v2f output;
                output.position = UnityObjectToClipPos(input.vertex);
                output.color = input.color;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float2 pixel = input.position.xy;
                float distance = length(pixel - _Center.xy);

                float cover = smoothstep(_Radius - _Softness, _Radius + _Softness, distance);
                float ring = 1.0 - smoothstep(0.0, max(1.0, _RingWidth), abs(distance - _Radius));

                fixed4 color = _Color * input.color;
                color.rgb = lerp(color.rgb, _RingColor.rgb, saturate(ring) * 0.9);
                color.a = saturate(max(cover, ring * 0.65)) * _Color.a * input.color.a;
                return color;
            }
            ENDCG
        }
    }
}
