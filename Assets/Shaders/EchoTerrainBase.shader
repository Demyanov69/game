Shader "Hidden/Echo/TerrainBase"
{
    Properties
    {
        _MainTex ("Basemap", 2D) = "white" {}
        [HideInInspector] _TerrainHolesTexture ("Terrain holes", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        CGPROGRAM
        #pragma target 3.0
        #pragma surface surf Lambert vertex:SplatmapVert noforwardadd nolightmap
        #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap
        #pragma multi_compile_local_fragment __ _ALPHATEST_ON
        #define TERRAIN_BASE_PASS
        #include "TerrainSplatmapCommon.cginc"
        #include "EchoTerrainColor.cginc"
        sampler2D _MainTex;

        void surf(Input input, inout SurfaceOutput output)
        {
            #ifdef _ALPHATEST_ON
            ClipHoles(input.tc.xy);
            #endif
            output.Albedo = EchoTerrainColor(tex2D(_MainTex, input.tc.xy).rgb, input.tc.xy);
            output.Alpha = 1;
        }
        ENDCG
        UsePass "Hidden/Nature/Terrain/Utilities/PICKING"
        UsePass "Hidden/Nature/Terrain/Utilities/SELECTION"
    }
    Fallback "Hidden/TerrainEngine/Splatmap/Diffuse-Base"
}
