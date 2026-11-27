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
        #pragma surface surf Standard fullforwardshadows
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
        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed3 original = tex2D(_MainTex, IN.uv_MainTex).rgb * _Color.rgb;
            // Retain original atlas UVs. Project additional wear in WORLD-space,
            // so dark-world materials have a genuinely different surface texture,
            // not just the same image with lowered brightness.
            float3 w = pow(abs(normalize(IN.worldNormal)), 6.0);
            w /= max(0.001, w.x + w.y + w.z);
            float s = _DarkTileScale;
            fixed3 erosion = tex2D(_DarkTex, IN.worldPos.zy*s).rgb*w.x
                + tex2D(_DarkTex, IN.worldPos.xz*s).rgb*w.y
                + tex2D(_DarkTex, IN.worldPos.xy*s).rgb*w.z;
            float luminance = dot(original, float3(.22,.67,.11));
            float3 faded = float3(luminance, luminance, luminance);
            // Keep silhouettes/UV art readable, but visibly age and stain it.
            fixed3 shadowOriginal = lerp(original, faded, .38) * _DarkTint.rgb;
            fixed3 night = lerp(shadowOriginal, erosion * _DarkTint.rgb,
                saturate(_DarkMix));
            float t = smoothstep(.04, .96, saturate(_EchoWorldBlend));
            o.Albedo = lerp(original, night, t);
            o.Metallic = 0;
            o.Smoothness = _Glossiness;
            o.Alpha = 1;
        }
        ENDCG
    }
    Fallback "Diffuse"
}
