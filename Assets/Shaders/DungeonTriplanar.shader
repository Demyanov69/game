Shader "Echo/DungeonTriplanar"
{
    Properties
    {
        _Color ("Light world tint", Color) = (1,1,1,1)
        _DarkColor ("Dark world tint", Color) = (0.84,0.9,1,1)
        _MainTex ("Original light-world texture", 2D) = "white" {}
        _DarkTex ("Alternative dark-world texture", 2D) = "gray" {}
        _TileScale ("Texture repetitions per metre", Range(.2,5)) = 1.5
        _Glossiness ("Smoothness", Range(0,1)) = .20
        _DarkGlow ("Subtle dark-world mineral glow", Range(0,1)) = .02
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Lambert noforwardadd nolightmap noshadow
        #pragma target 3.0
        sampler2D _MainTex;
        sampler2D _DarkTex;
        fixed4 _Color;
        fixed4 _DarkColor;
        half _TileScale, _Glossiness, _DarkGlow;
        float _EchoWorldBlend;
        struct Input { float3 worldPos; float3 worldNormal; };
        void surf (Input IN, inout SurfaceOutput o)
        {
            // Masonry faces have a clear projection axis. Use a single sample
            // in a stable world and blend two samples only during the shift.
            float3 n = abs(IN.worldNormal);
            float2 uv = (n.y >= max(n.x,n.z) ? IN.worldPos.xz :
                         n.x >= n.z ? IN.worldPos.zy : IN.worldPos.xy) * _TileScale;
            half world = smoothstep(.08, .92, saturate(_EchoWorldBlend));
            fixed3 dark = 0;
            [branch] if (world > 0) dark = tex2D(_DarkTex, uv).rgb;
            fixed3 albedo = dark * _DarkColor.rgb;
            [branch] if (world < 1)
                albedo = lerp(tex2D(_MainTex, uv).rgb * _Color.rgb, albedo, world);
            o.Albedo = albedo;
            half fissure = saturate((dark.r - dark.g * 1.35) * 3.0);
            o.Emission = world * (dark * float3(.14,.54,.48) * _DarkGlow +
                fissure * float3(.30,.055,.11));
            o.Alpha = 1;
        }
        ENDCG
    }
    Fallback "Diffuse"
}
