Shader "Echo/StaffLit"
{
    Properties
    {
        _MainTex ("Albedo", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Emission ("Emission", Color) = (0,0,0,1)
        _EmissionStrength ("Emission Strength", Range(0,3)) = 0
        _Shade ("Shade", Range(0,1)) = 0.45
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float4 _Emission;
            float _EmissionStrength;
            float _Shade;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 n = normalize(i.normal);
                float ndotl = saturate(dot(n, normalize(_WorldSpaceLightPos0.xyz)));
                float3 albedo = tex2D(_MainTex, i.uv).rgb * _Color.rgb;
                float wrap = lerp(0.35, 1.0, _Shade);
                float3 lit = albedo * (wrap * 0.55 + ndotl * 0.65);
                lit += _Emission.rgb * _EmissionStrength;
                return float4(saturate(lit), 1);
            }
            ENDCG
        }
    }
    FallBack Off
}
