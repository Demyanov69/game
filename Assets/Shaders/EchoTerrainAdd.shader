Shader "Hidden/Echo/TerrainAdd"
{
    Properties { [HideInInspector] _TerrainHolesTexture ("Terrain holes", 2D) = "white" {} }
    SubShader
    {
        Tags { "Queue"="Geometry-99" "RenderType"="Opaque" "IgnoreProjector"="True" }
        CGPROGRAM
        #pragma target 3.0
        #pragma surface surf Lambert decal:add vertex:SplatmapVert finalcolor:SplatmapFinalColor noforwardadd nolightmap nometa
        #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap
        #pragma multi_compile_fog
        #pragma multi_compile_local_fragment __ _ALPHATEST_ON
        #define TERRAIN_SPLAT_ADDPASS
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
    }
    Fallback Off
}
