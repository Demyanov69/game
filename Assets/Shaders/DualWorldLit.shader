Shader "Echo/DualWorldLit"
{
    Properties
    {
        _LightColor ("Light Base", Color) = (0.91,0.90,0.94,1)
        _LightAccent ("Light Pink", Color) = (1.0,0.45,0.72,1)
        _LightGold ("Light Gold", Color) = (1.0,0.75,0.28,1)
        _DarkColor ("Dark Base", Color) = (0.012,0.016,0.016,1)
        _DarkEmission ("Dark Sculk", Color) = (0.0,0.55,0.48,1)
        _EmissionStrength ("Sculk Emission", Range(0,8)) = 1.15
        _AccentAmount ("Light Accent", Range(0,1)) = 0.24
        _RimPower ("Rim Power", Range(0.25,8)) = 3.2
        _RimStrength ("Rim Strength", Range(0,3)) = 0.45
        _CorruptionAmount ("Corruption Amount", Range(0,1)) = 0.85
        _CorruptionScale ("Corruption Scale", Range(0.2,20)) = 0.85
        _EchoFade ("Presence", Range(0,1)) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 250

        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            float4 _LightColor;
            float4 _LightAccent;
            float4 _LightGold;
            float4 _DarkColor;
            float4 _DarkEmission;
            float4 _EchoLightPalette;
            float4 _EchoDarkPalette;
            float _EmissionStrength;
            float _AccentAmount;
            float _RimPower;
            float _RimStrength;
            float _CorruptionAmount;
            float _CorruptionScale;
            float _EchoFade;
            float4 _EchoWorld;
            float _EchoTransitionPulse;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 world : TEXCOORD0;
                float3 normal : TEXCOORD1;
                float3 viewDir : TEXCOORD2;
                SHADOW_COORDS(3)
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
                float x00 = lerp(n000,n100,f.x);
                float x10 = lerp(n010,n110,f.x);
                float x01 = lerp(n001,n101,f.x);
                float x11 = lerp(n011,n111,f.x);
                return lerp(lerp(x00,x10,f.y),lerp(x01,x11,f.y),f.z);
            }

            float Fbm(float3 p)
            {
                float v = 0.0;
                float a = 0.55;
                v += a * Noise3(p); p = p * 2.05; a *= 0.5;
                v += a * Noise3(p); p = p * 2.05; a *= 0.5;
                v += a * Noise3(p);
                return v;
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.world = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.viewDir = _WorldSpaceCameraPos.xyz - o.world;
                TRANSFER_SHADOW(o);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float globalBlend = saturate(_EchoWorld.x);
                float blot = Fbm(i.world * max(0.15, _CorruptionScale));
                float thresh = lerp(-0.08, 1.08, globalBlend);
                float blend = smoothstep(thresh + 0.16, thresh - 0.16, blot);

                float3 n = normalize(i.normal);
                float3 v = normalize(i.viewDir);
                float fresnel = pow(1.0 - saturate(dot(n, v)), _RimPower);
                float ndotl = saturate(dot(n, normalize(_WorldSpaceLightPos0.xyz)));
                float shadow = SHADOW_ATTENUATION(i);

                float3 lightBase = lerp(_LightColor.rgb, _EchoLightPalette.rgb, 0.45);
                float3 lightAccent = lerp(_LightGold.rgb, _LightAccent.rgb, _AccentAmount);
                float3 lightSurface = lightBase * 0.96 + fresnel * lightAccent * _RimStrength * 0.35;

                float growth = Fbm(i.world * 1.7 + 4.2);
                float sores = smoothstep(0.62, 0.86, growth);
                float film = smoothstep(0.40, 0.72, Fbm(i.world * 0.55));
                float infection = saturate(sores * 0.75 + film * 0.45) * _CorruptionAmount;

                float3 darkBase = _DarkColor.rgb;
                darkBase = lerp(darkBase, _DarkEmission.rgb * 0.07, film);
                float3 darkRim = _DarkEmission.rgb * fresnel * infection * 0.55;
                float3 surface = lerp(lightSurface, darkBase + darkRim, blend);

                float3 lit = surface * (_LightColor0.rgb * ndotl * shadow + UNITY_LIGHTMODEL_AMBIENT.rgb * 0.85);
                float3 emission = _DarkEmission.rgb * _EmissionStrength * infection * blend;
                emission *= 0.35 + fresnel * 0.25;
                lit += emission;
                lit = lerp(lit, lit * 1.08 + _DarkEmission.rgb * 0.04, blend * _EchoTransitionPulse);
                return fixed4(saturate(lit), 1);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
