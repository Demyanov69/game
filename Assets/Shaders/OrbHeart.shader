Shader "Echo/OrbHeart"
{
    Properties
    {
        _Color ("Radiance", Color) = (.14,1,.9,1)
        _Brightness ("Light output", Range(.1,8)) = 2.4
        _Shell ("Outer layer", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color;
            float _Brightness, _Shell;
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f { float4 pos : SV_POSITION; float3 normal : TEXCOORD0; float3 viewDir : TEXCOORD1; };
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                float3 world = mul(unity_ObjectToWorld,v.vertex).xyz;
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.viewDir = _WorldSpaceCameraPos.xyz - world;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float facing = saturate(dot(normalize(i.normal),normalize(i.viewDir)));
                float rim = pow(1.0-facing,2.4);
                float pulse = .86 + .14*sin(_Time.y*3.1);
                float a = lerp(.9,.06+.38*rim,_Shell) * pulse;
                fixed3 hot = lerp(fixed3(.95,1.0,.99), _Color.rgb, saturate(rim+_Shell*.65));
                return fixed4(hot * _Brightness * pulse, a);
            }
            ENDCG
        }
    }
    Fallback "Unlit/Color"
}
