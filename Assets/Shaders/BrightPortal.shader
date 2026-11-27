Shader "Echo/BrightPortal"
{
    Properties
    {
        _Color ("Rift energy", Color) = (.06,.95,.85,1)
        _Activation ("Portal energy", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
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
            float _Activation;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert(appdata v) {v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;return o;}
            float noise(float2 p)
            {
                return frac(sin(dot(floor(p),float2(127.1,311.7)))*43758.5453);
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float2 p = i.uv*2.0-1.0;
                float time = _Time.y;
                float wiggle = .045*sin(p.y*15.0+time*3.8)+.025*sin(p.y*25.0-time*2.7);
                float ellipse = length(float2((p.x+wiggle)*1.12,p.y*.91));
                float mask = 1.0-smoothstep(.86,1.0,ellipse);
                float edge = smoothstep(.63,.92,ellipse)*(1.0-smoothstep(.96,1.01,ellipse));
                float flowing = sin(p.x*19.0+sin(p.y*17.0+time*2.0)*2.5+time*4.0);
                float filaments = pow(saturate(.5+.5*flowing),5.0);
                float spiral = sin(16.0*length(p)+atan2(p.y,p.x)*5.0-time*3.8);
                float star = noise(i.uv*110.0+time*.22);
                float arcs = smoothstep(.80,.98,sin((p.y+sin(p.x*4.0+time))*29.0+time*6.0));
                fixed3 bright = lerp(float3(.02,.13,.20),float3(.13,1.0,.82),filaments*.8+.35);
                bright += float3(.48,1.0,1.0)*edge*2.8;
                bright += float3(.7,.9,1.0)*(spiral*.5+.5)*.25;
                bright += float3(.5,1.0,.8)*arcs*.2;
                bright += float3(1.0,1.0,1.0)*step(.998,star)*.7;
                bright *= _Color.rgb * (1.15 + _Activation * 2.5);
                float alpha = saturate(mask * (.75 + filaments*.22) + edge*.85);
                return fixed4(bright, alpha * saturate(_Activation));
            }
            ENDCG
        }
    }
    Fallback "Unlit/Transparent"
}
