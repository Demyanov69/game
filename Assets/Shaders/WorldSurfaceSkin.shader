Shader "Echo/WorldSurfaceSkin"
{
    Properties
    {
        _MainTex ("Original asset albedo atlas", 2D) = "white" {}
        _Color ("Original asset tint", Color) = (1,1,1,1)
        _DarkTex ("Dark-world erosion atlas", 2D) = "gray" {}
        _DarkTint ("Dark-world mineral coloration", Color) = (0.54,0.65,0.73,1)
        _DarkTileScale ("Erosion scale", Range(.15,5)) = 1.0
        _DarkMix ("Dark-world texture replacement amount", Range(0,1)) = .46
        _Glossiness ("Gloss", Range(0,1)) = .14
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 220
        CGPROGRAM
        #pragma surface surf Lambert noforwardadd nolightmap noshadow
        #pragma target 3.0
        #include "UnityCG.cginc"
        sampler2D _MainTex;
        sampler2D _DarkTex;
        fixed4 _Color, _DarkTint;
        half _DarkTileScale, _DarkMix, _Glossiness;
        float _EchoWorldBlend;
        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
            float3 worldNormal;
        };
        void surf (Input IN, inout SurfaceOutput o)
        {
            fixed3 original = tex2D(_MainTex, IN.uv_MainTex).rgb * _Color.rgb;
            half world = smoothstep(.04, .96, saturate(_EchoWorldBlend));
            o.Albedo = original;
            [branch] if (world > 0)
            {
                float3 n = abs(IN.worldNormal);
                float2 uv = (n.y >= max(n.x,n.z) ? IN.worldPos.xz :
                             n.x >= n.z ? IN.worldPos.zy : IN.worldPos.xy) * _DarkTileScale;
                fixed3 wear = tex2D(_DarkTex, uv).rgb;
                half luminance = dot(original, float3(.22,.67,.11));
                fixed3 shadow = lerp(original, luminance.xxx, .38);
                fixed3 night = lerp(shadow, wear, saturate(_DarkMix)) * _DarkTint.rgb;
                o.Albedo = lerp(original, night, world);
            }
            o.Alpha = 1;
        }
        ENDCG
    }
    Fallback "Diffuse"
}
