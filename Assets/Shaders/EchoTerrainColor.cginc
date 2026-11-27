#ifndef ECHO_TERRAIN_COLOR
#define ECHO_TERRAIN_COLOR
sampler2D _EchoTerrainDark;
float _EchoWorldBlend;

fixed3 EchoTerrainColor(fixed3 albedo, float2 terrainUV)
{
    half night = smoothstep(.04, .96, saturate(_EchoWorldBlend));
    [branch] if (night > 0)
    {
        fixed3 earth = tex2D(_EchoTerrainDark, terrainUV * 32.0).rgb;
        return lerp(albedo, lerp(albedo, earth, .78) * fixed3(.55,.69,.76), night);
    }
    return albedo;
}
#endif
