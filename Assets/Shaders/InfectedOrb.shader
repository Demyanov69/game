Shader "Echo/InfectedOrb"
{
    Properties
    {
        _Core ("Core", Color) = (0.02, 0.05, 0.05, 1)
        _Glow ("Glow", Color) = (0.15, 0.95, 0.82, 1)
        _Hot ("Hot", Color) = (0.55, 1.0, 0.92, 1)
        _Intensity ("Intensity", Range(0, 4)) = 1.6
        _Pulse ("Pulse", Range(0, 1)) = 0.25
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _Core;
            float4 _Glow;
            float4 _Hot;
            float _Intensity;
            float _Pulse;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 local : TEXCOORD0;
                float3 normal : TEXCOORD1;
                float3 viewDir : TEXCOORD2;
            };

            float Hash31(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            float Noise3(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float n000 = Hash31(i + float3(0,0,0));
                float n100 = Hash31(i + float3(1,0,0));
                float n010 = Hash31(i + float3(0,1,0));
                float n110 = Hash31(i + float3(1,1,0));
                float n001 = Hash31(i + float3(0,0,1));
                float n101 = Hash31(i + float3(1,0,1));
                float n011 = Hash31(i + float3(0,1,1));
                float n111 = Hash31(i + float3(1,1,1));
                float x00 = lerp(n000, n100, f.x);
                float x10 = lerp(n010, n110, f.x);
                float x01 = lerp(n001, n101, f.x);
                float x11 = lerp(n011, n111, f.x);
                return lerp(lerp(x00, x10, f.y), lerp(x01, x11, f.y), f.z);
            }

            float Fbm(float3 p)
            {
                float v = 0.0;
                float a = 0.5;
                v += Noise3(p) * a; p *= 2.03; a *= 0.5;
                v += Noise3(p) * a; p *= 2.07; a *= 0.5;
                v += Noise3(p) * a;
                return v;
            }

            v2f vert(appdata v)
            {
                float t = _Time.y;
                float3 n = normalize(v.normal);
                float bump = sin(t * 3.2 + v.vertex.x * 8.0) * 0.012 * _Pulse;
                bump += sin(t * 2.1 + v.vertex.y * 6.0) * 0.01 * _Pulse;
                float4 vert = v.vertex;
                vert.xyz += n * bump;

                v2f o;
                o.pos = UnityObjectToClipPos(vert);
                o.local = v.vertex.xyz;
                o.normal = UnityObjectToWorldNormal(v.normal);
                float3 world = mul(unity_ObjectToWorld, vert).xyz;
                o.viewDir = _WorldSpaceCameraPos.xyz - world;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 n = normalize(i.normal);
                float3 v = normalize(i.viewDir);
                float ndv = saturate(dot(n, v));
                float fres = pow(1.0 - ndv, 2.4);
                float fres2 = pow(1.0 - ndv, 5.0);

                float t = _Time.y;
                float3 lp = i.local * 2.2;
                // rotating swirl
                float ang = t * 0.7;
                float ca = cos(ang);
                float sa = sin(ang);
                float3 swirlP = float3(lp.x * ca - lp.z * sa, lp.y + t * 0.25, lp.x * sa + lp.z * ca);
                float flow = Fbm(swirlP * 1.8);
                float arcs = smoothstep(0.58, 0.9, flow);
                float filaments = smoothstep(0.7, 0.95, Fbm(swirlP * 3.4 + 2.0));
                float corePulse = 0.7 + 0.3 * sin(t * 2.4);

                float3 col = _Core.rgb * (0.15 + fres * 0.2);
                col += _Glow.rgb * arcs * 0.9 * _Intensity;
                col += _Hot.rgb * filaments * 1.2 * _Intensity;
                col += _Glow.rgb * fres * 1.35 * corePulse * _Intensity;
                col += _Hot.rgb * fres2 * 0.8 * _Intensity;

                // soft sphere falloff so it reads as volume energy
                float radial = length(i.local);
                float shell = smoothstep(0.55, 0.25, radial) * 0.35 + smoothstep(0.15, 0.45, radial);
                col *= shell + fres;

                float alpha = saturate(0.15 + fres * 0.9 + arcs * 0.45 + filaments * 0.35);
                // additive: premultiply
                return float4(col * alpha, alpha);
            }
            ENDCG
        }
    }
    FallBack Off
}
