Shader "Game/Shoeprint Soft Glow"
{
    Properties
    {
        [MainTexture] _BaseMap("Shoe Tread", 2D) = "white" {}
        [MainColor] _BaseColor("Dark Gray Tread", Color) = (0.015, 0.015, 0.015, 0.45)
        [HDR] _GlowColor("Gray Glow", Color) = (1.8, 1.8, 1.8, 1)
        _GlowStrength("Glow Strength", Range(0, 3)) = 0.9
        _GlowRadius("Soft Edge Width", Range(0.5, 4)) = 1.5
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        // An unlit pass works with URP and the built-in preview renderer.
        Pass
        {
            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            sampler2D _BaseMap;
            float4 _BaseMap_TexelSize;
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _GlowColor;
                float _GlowStrength;
                float _GlowRadius;
            CBUFFER_END

            struct Attributes { float4 position : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 position : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.position = UnityObjectToClipPos(input.position);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            float Weight(int offset)
            {
                int distance = abs(offset);
                return distance == 0 ? 6.0 : (distance == 1 ? 4.0 : 1.0);
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float tread = tex2D(_BaseMap, input.uv).a;
                float blurred = 0;
                float2 spread = _BaseMap_TexelSize.xy * _GlowRadius;
                [unroll] for (int y = -2; y <= 2; y++)
                {
                    [unroll] for (int x = -2; x <= 2; x++)
                    {
                        blurred += tex2D(_BaseMap, input.uv + float2(x, y) * spread).a
                            * Weight(x) * Weight(y) / 256.0;
                    }
                }
                float coreAlpha = tread * _BaseColor.a;
                float glowAlpha = saturate(blurred - tread) * _GlowStrength * _BaseColor.a;
                float alpha = saturate(coreAlpha + glowAlpha);
                float3 color = (_BaseColor.rgb * coreAlpha + _GlowColor.rgb * glowAlpha)
                    / max(coreAlpha + glowAlpha, 0.0001);
                return float4(color, alpha);
            }
            ENDCG
        }
    }
    Fallback Off
}
