Shader "Echo/InfectedPortal"
{
    Properties
    {
        _Color ("Filament", Color) = (0.10, 0.78, 0.68, 1)
        _Core ("Void", Color) = (0.0, 0.012, 0.012, 1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _Color;
            float4 _Core;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float Noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = Hash21(i);
                float b = Hash21(i + float2(1, 0));
                float c = Hash21(i + float2(0, 1));
                float d = Hash21(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float Fbm(float2 p)
            {
                float v = 0.0;
                float a = 0.55;
                v += a * Noise(p); p = p * 2.05 + 3.1; a *= 0.5;
                v += a * Noise(p); p = p * 2.05; a *= 0.5;
                v += a * Noise(p);
                return v;
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float2 p = uv * 2.0 - 1.0;
                float t = _Time.y;
                float jag = (Fbm(float2(p.y * 3.0 + t * 0.15, t * 0.05)) - 0.5) * 0.22;
                float oval = length(float2((p.x + jag) / 0.62, p.y / 0.92));
                float inside = smoothstep(1.02, 0.78, oval);
                float edge = smoothstep(1.02, 0.90, oval) * smoothstep(0.72, 0.92, oval);

                float rise = uv.y * 4.0 - t * 0.55;
                float filaments = Fbm(float2(p.x * 6.0 + jag * 2.0, rise));
                filaments = smoothstep(0.62, 0.86, filaments);
                float cracks = smoothstep(0.72, 0.92, Fbm(float2(abs(p.x) * 9.0, rise * 0.65)));

                float3 col = _Core.rgb * inside;
                col += _Color.rgb * filaments * inside * 0.95;
                col += _Color.rgb * cracks * inside * 0.35;
                col += _Color.rgb * edge * 1.35;
                float alpha = saturate(inside * 0.88 + edge);
                alpha *= smoothstep(1.08, 0.92, oval);
                return float4(col, alpha);
            }
            ENDCG
        }
    }
    FallBack Off
}
