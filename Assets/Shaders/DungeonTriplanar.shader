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
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        sampler2D _MainTex;
        sampler2D _DarkTex;
        fixed4 _Color;
        fixed4 _DarkColor;
        half _TileScale, _Glossiness, _DarkGlow;
        float _EchoWorldBlend;
        struct Input { float3 worldPos; float3 worldNormal; };
        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float3 weight = pow(abs(normalize(IN.worldNormal)), 5.0);
            weight /= max(weight.x + weight.y + weight.z, .001);
            float2 xy = IN.worldPos.xy * _TileScale;
            float2 xz = IN.worldPos.xz * _TileScale;
            float2 yz = IN.worldPos.zy * _TileScale;
            fixed3 lightSurface = tex2D(_MainTex,yz).rgb * weight.x
                + tex2D(_MainTex,xz).rgb * weight.y
                + tex2D(_MainTex,xy).rgb * weight.z;
            fixed3 darkSurface = tex2D(_DarkTex,yz).rgb * weight.x
                + tex2D(_DarkTex,xz).rgb * weight.y
                + tex2D(_DarkTex,xy).rgb * weight.z;
            float world = smoothstep(.08, .92, saturate(_EchoWorldBlend));
            o.Albedo = lerp(lightSurface * _Color.rgb,
                darkSurface * _DarkColor.rgb, world);
            // Glow is deliberately tiny: these are textured masonry and tiles,
            // not bright neon slabs (portals retain their own shader).
            o.Emission = darkSurface * float3(.14,.54,.48) * (_DarkGlow * world);
            half fissure = saturate((darkSurface.r - darkSurface.g * 1.35) * 3.0);
            o.Emission += world * fissure * float3(.30,.055,.11);
            o.Metallic = 0;
            o.Smoothness = _Glossiness;
            o.Alpha = 1;
        }
        ENDCG
    }
    Fallback "Diffuse"
}
