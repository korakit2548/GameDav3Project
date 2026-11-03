Shader "Game/Interaction Outline"
{
    Properties
    {
        _BaseMap ("Source alpha", 2D) = "white" {}
        [HDR] _OutlineColor ("Outline color", Color) = (3,2.7,1.8,1)
        _OutlineWidth ("Width in pixels", Float) = 1
        [HideInInspector] _Mode ("Mode", Float) = 1
        [HideInInspector] _ColorMask ("Color mask", Float) = 15
        [HideInInspector] _StencilComp ("Stencil comparison", Float) = 6
        [HideInInspector] _StencilPass ("Stencil operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil write mask", Float) = 0
        [HideInInspector] _Cull ("Cull", Float) = 1
        [HideInInspector] _Planar ("Planar mesh", Float) = 0
        [HideInInspector] _MeshCenter ("Mesh center", Vector) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "Queue"="Transparent+101" "RenderType"="Transparent" "DisableBatching"="True" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Offset -1, -1
            Cull [_Cull]
            ColorMask [_ColorMask]
            Stencil
            {
                Ref 128
                ReadMask 128
                WriteMask [_StencilWriteMask]
                Comp [_StencilComp]
                Pass [_StencilPass]
            }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _BaseMap;
            float4 _BaseMap_ST;
            float4 _OutlineColor;
            float _OutlineWidth;
            float _Mode;
            float _Planar;
            float4 _MeshCenter;
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; };
            struct v2f { float4 position : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert(appdata v)
            {
                v2f o;
                o.position = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                if (_Mode > 0.5 && _Mode < 1.5)
                {
                    float3 normalVS = mul((float3x3)UNITY_MATRIX_V, UnityObjectToWorldNormal(v.normal));
                    float2 direction = mul((float2x2)UNITY_MATRIX_P, normalVS.xy);
                    if (_Planar > 0.5)
                    {
                        float4 center = UnityObjectToClipPos(float4(_MeshCenter.xyz, 1));
                        direction = (o.position.xy / o.position.w - center.xy / center.w) * _ScreenParams.xy;
                    }
                    direction /= max(length(direction), 0.0001);
                    o.position.xy += direction * (_OutlineWidth * 2.0 / _ScreenParams.xy) * o.position.w;
                    // Lift the edge slightly off coplanar floors and walls.
                    #if defined(UNITY_REVERSED_Z)
                    o.position.z += 0.0002 * o.position.w;
                    #else
                    o.position.z -= 0.0002 * o.position.w;
                    #endif
                }
                return o;
            }
            half4 frag(v2f i) : SV_Target
            {
                float alpha = tex2D(_BaseMap, i.uv).a;
                if (_Mode > 1.5)
                {
                    // Screen derivatives keep the contour width stable as the player approaches.
                    float2 dx = ddx(i.uv) * (_OutlineWidth * 0.5);
                    float2 dy = ddy(i.uv) * (_OutlineWidth * 0.5);
                    float lo = smoothstep(0.02, 0.06, alpha), hi = lo;
                    float a0 = smoothstep(0.02, 0.06, tex2D(_BaseMap, i.uv + dx).a);
                    float a1 = smoothstep(0.02, 0.06, tex2D(_BaseMap, i.uv - dx).a);
                    float a2 = smoothstep(0.02, 0.06, tex2D(_BaseMap, i.uv + dy).a);
                    float a3 = smoothstep(0.02, 0.06, tex2D(_BaseMap, i.uv - dy).a);
                    float a4 = smoothstep(0.02, 0.06, tex2D(_BaseMap, i.uv + dx + dy).a);
                    float a5 = smoothstep(0.02, 0.06, tex2D(_BaseMap, i.uv - dx - dy).a);
                    float a6 = smoothstep(0.02, 0.06, tex2D(_BaseMap, i.uv + dx - dy).a);
                    float a7 = smoothstep(0.02, 0.06, tex2D(_BaseMap, i.uv - dx + dy).a);
                    lo = min(lo, min(min(a0, a1), min(a2, a3)));
                    lo = min(lo, min(min(a4, a5), min(a6, a7)));
                    hi = max(hi, max(max(a0, a1), max(a2, a3)));
                    hi = max(hi, max(max(a4, a5), max(a6, a7)));
                    float edge = saturate((hi - lo) * 2.0);
                    clip(edge - 0.04);
                    return half4(_OutlineColor.rgb, _OutlineColor.a * edge);
                }
                clip(alpha - 0.1);
                return _OutlineColor;
            }
            ENDCG
        }
    }
}
