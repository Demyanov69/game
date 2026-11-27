Shader "Echo/Terrain"
{
    Properties
    {
        [HideInInspector] _MainTex ("Basemap", 2D) = "white" {}
        [HideInInspector] _TerrainHolesTexture ("Terrain holes", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "Queue"="Geometry-100" "RenderType"="Opaque" "TerrainCompatible"="True" }
        CGPROGRAM
        #pragma target 3.0
        #pragma surface surf Lambert vertex:SplatmapVert finalcolor:SplatmapFinalColor noforwardadd nolightmap
        #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap
        #pragma multi_compile_fog
        #pragma multi_compile_local_fragment __ _ALPHATEST_ON
        #include "TerrainSplatmapCommon.cginc"
        #include "EchoTerrainColor.cginc"

        void surf(Input input, inout SurfaceOutput output)
        {
            half4 layers;
            half weight;
            fixed4 painted;
            SplatmapMix(input, layers, weight, painted, output.Normal);
            output.Albedo = EchoTerrainColor(painted.rgb, input.tc.xy);
            output.Alpha = weight;
        }
        ENDCG
        UsePass "Hidden/Nature/Terrain/Utilities/PICKING"
        UsePass "Hidden/Nature/Terrain/Utilities/SELECTION"
    }
    Dependency "AddPassShader" = "Hidden/Echo/TerrainAdd"
    Dependency "BaseMapShader" = "Hidden/Echo/TerrainBase"
    Dependency "BaseMapGenShader" = "Hidden/TerrainEngine/Splatmap/Diffuse-BaseGen"
    Dependency "Details0" = "Hidden/TerrainEngine/Details/Vertexlit"
    Dependency "Details1" = "Hidden/TerrainEngine/Details/WavingDoublePass"
    Dependency "Details2" = "Hidden/TerrainEngine/Details/BillboardWavingDoublePass"
    Dependency "Tree0" = "Hidden/TerrainEngine/BillboardTree"
    Fallback "Nature/Terrain/Diffuse"
}
