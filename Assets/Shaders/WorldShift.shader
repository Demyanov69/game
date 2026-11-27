Shader "Echo/WorldShift"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
        _LightSoft ("Light Soft", Color) = (0.97,0.91,0.94,1)
        _LightPink ("Light Pink", Color) = (1,0.43,0.68,1)
        _LightGold ("Light Gold", Color) = (1,0.73,0.28,1)
        _DarkAccent ("Dark Cyan", Color) = (0.0,0.62,0.55,1)
        _Blend ("World Blend", Range(0,1)) = 0
        _Pulse ("Pulse", Range(0,1)) = 0
        _Transition ("Transition", Range(0,1)) = 1
        _Direction ("Direction", Float) = 1
        _StableStrength ("Stable Strength", Range(0,1)) = 0.58
        _Origin ("Origin", Vector) = (0,0,0,1)
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float4 _LightSoft;
            float4 _LightPink;
            float4 _LightGold;
            float4 _DarkAccent;
            float _Blend;
            float _Pulse;
            float _Transition;
            float _Direction;
            float _StableStrength;
            float4 _Origin;

            float Luma(float3 c)
            {
                return dot(c, float3(0.2126, 0.7152, 0.0722));
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float SoftNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = Hash21(i);
                float b = Hash21(i + float2(1,0));
                float c = Hash21(i + float2(0,1));
                float d = Hash21(i + float2(1,1));
                return lerp(lerp(a,b,f.x), lerp(c,d,f.x), f.y);
            }

            float Fbm(float2 p)
            {
                float v = 0.0;
                float a = 0.5;
                for (int i = 0; i < 4; i++)
                {
                    v += a * SoftNoise(p);
                    p = p * 2.05 + float2(17.1, 9.2);
                    a *= 0.5;
                }
                return v;
            }

            fixed4 frag(v2f_img i) : SV_Target
            {
                float pulse = saturate(_Pulse);
                float2 uv = i.uv;
                float2 center = float2(0.5, 0.5);
                float2 fromCenter = uv - center;
                float radius = length(fromCenter);

                float t = _Time.y;
                float warp = Fbm(uv * 5.5 + t * 0.35);
                float2 dir = normalize(fromCenter + float2(0.0001, 0.0001));
                float ripple = sin(radius * 28.0 - t * 9.0 + warp * 6.283) * pulse;
                float2 distortion = dir * ripple * (0.012 + pulse * 0.02);
                distortion += (float2(warp, Fbm(uv * 8.0 - t * 0.2)) - 0.5) * pulse * 0.028;
                distortion *= lerp(0.35, 1.0, radius);

                float3 src = tex2D(_MainTex, uv + distortion).rgb;
                float l = Luma(src);

                float3 pastel = lerp(src, _LightSoft.rgb, 0.22);
                pastel = lerp(pastel, _LightPink.rgb * 0.55 + _LightGold.rgb * 0.45, 0.06 + l * 0.05);
                pastel *= 0.96;

                float3 infected = src;
                infected = lerp(infected, infected * float3(0.16, 0.28, 0.26), 0.72);
                infected *= lerp(0.22, 0.62, smoothstep(0.0, 0.55, l));
                float pools = Fbm(uv * 3.2);
                float veins = smoothstep(0.58, 0.82, pools);
                infected += _DarkAccent.rgb * veins * (0.10 + (1.0 - l) * 0.22);
                infected += _DarkAccent.rgb * 0.035 * (1.0 - l);

                float blot = Fbm(uv * float2(7.5, 6.0) + _Transition * 0.4);
                float thresh = lerp(-0.05, 1.05, saturate(_Blend));
                float darkMask = smoothstep(thresh + 0.16, thresh - 0.16, blot);
                float3 color = lerp(pastel, infected, darkMask);

                float aberr = pulse * 0.012;
                float2 aberrDir = dir * aberr;
                float rr = tex2D(_MainTex, uv + distortion + aberrDir).r;
                float bb = tex2D(_MainTex, uv + distortion - aberrDir).b;
                color = lerp(color, float3(rr, color.g, bb), pulse * 0.65);

                float vignette = smoothstep(0.95, 0.25, radius);
                color *= lerp(0.78 - pulse * 0.12, 1.0, vignette);
                color = saturate(color);
                return float4(color, 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
