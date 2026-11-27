Shader "Echo/PhaseDissolve"
{
    Properties
    {
        _Color ("Original tint", Color) = (1,1,1,1)
        _MainTex ("Original texture", 2D) = "white" {}
        _UseWorldProjection ("Triplanar surface", Float) = 0
        _TileScale ("Triplanar scale", Float) = 1
        _PhaseVisibility ("Presence", Range(0,1)) = 1
        _EdgeColor ("Phase fire", Color) = (.1,2,2,1)
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow
        #pragma target 3.0
        sampler2D _MainTex;
        fixed4 _Color, _EdgeColor;
        half _PhaseVisibility;
        half _UseWorldProjection, _TileScale;
        struct Input { float2 uv_MainTex; float3 worldPos; float3 worldNormal; };
        float hash31(float3 p) {p=frac(p*.1031);p+=dot(p,p.yzx+33.33);return frac((p.x+p.y)*p.z);}
        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float3 cell = floor(IN.worldPos * 8.0 + float3(0.37,0.13,0.73));
            float noise = hash31(cell);
            // The same world-space mask is used on every face. No transparent sorting.
            clip(saturate(_PhaseVisibility) - noise);
            fixed4 tex = tex2D(_MainTex, IN.uv_MainTex);
            if (_UseWorldProjection > 0.5)
            {
                float3 w=pow(abs(normalize(IN.worldNormal)),5.0);
                w/=max(w.x+w.y+w.z,.001);
                fixed4 x=tex2D(_MainTex,IN.worldPos.zy*_TileScale);
                fixed4 y=tex2D(_MainTex,IN.worldPos.xz*_TileScale);
                fixed4 z=tex2D(_MainTex,IN.worldPos.xy*_TileScale);
                tex=x*w.x+y*w.y+z*w.z;
            }
            tex*=_Color;
            o.Albedo = tex.rgb;
            o.Metallic = 0;
            o.Smoothness = .18;
            float edge = saturate(1.0 - abs(_PhaseVisibility-noise)*16.0);
            o.Emission = _EdgeColor.rgb * edge * .65;
            o.Alpha = 1;
        }
        ENDCG
    }
    Fallback "Diffuse"
}
