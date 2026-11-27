using UnityEngine;

namespace EchoIslands
{
    [DefaultExecutionOrder(-50)]
    public class WorldAtmosphere : MonoBehaviour
    {
        [Header("Light world - soft pastel")]
        public Color lightFog = new Color(0.73f, 0.78f, 0.90f, 1f);
        public Color lightAmbientSky = new Color(0.84f, 0.82f, 0.96f, 1f);
        public Color lightAmbientEquator = new Color(1.00f, 0.88f, 0.90f, 1f);
        public Color lightAmbientGround = new Color(0.30f, 0.24f, 0.31f, 1f);
        public Color lightDirectional = new Color(1.00f, 0.90f, 0.88f, 1f);
        public float lightAmbientIntensity = 0.70f;
        public float lightDirectionalIntensity = 0.72f;
        public float lightFogDensity = 0.0032f;

        [Header("Dark world - infected black / turquoise")]
        public Color darkFog = new Color(0.025f, 0.039f, 0.067f, 1f);
        public Color darkAmbientSky = new Color(0.15f, 0.20f, 0.31f, 1f);
        public Color darkAmbientEquator = new Color(0.11f, 0.17f, 0.25f, 1f);
        public Color darkAmbientGround = new Color(0.055f, 0.085f, 0.12f, 1f);
        public Color darkDirectional = new Color(0.38f, 0.61f, 0.78f, 1f);
        public float darkAmbientIntensity = 0.72f;
        public float darkDirectionalIntensity = 0.78f;
        public float darkFogDensity = 0.0053f;

        Light[] directionalLights;
        Material skyboxInstance;
        bool hasSkybox;
        float refreshTimer;
        Color originalFogColor;
        float originalFogDensity;
        bool originalFog;
        Color originalAmbientSky;
        Color originalAmbientEquator;
        Color originalAmbientGround;
        float originalAmbientIntensity;
        float originalReflectionIntensity;

        void Awake()
        {
            originalFog = RenderSettings.fog;
            originalFogColor = RenderSettings.fogColor;
            originalFogDensity = RenderSettings.fogDensity;
            originalAmbientSky = RenderSettings.ambientSkyColor;
            originalAmbientEquator = RenderSettings.ambientEquatorColor;
            originalAmbientGround = RenderSettings.ambientGroundColor;
            originalAmbientIntensity = RenderSettings.ambientIntensity;
            originalReflectionIntensity = RenderSettings.reflectionIntensity;

            if (RenderSettings.skybox != null)
            {
                skyboxInstance = new Material(RenderSettings.skybox);
                RenderSettings.skybox = skyboxInstance;
                hasSkybox = true;
            }
        }

        void Start()
        {
            RefreshLights();
            Apply(WorldState.Instance != null ? WorldState.Instance.blend : 0f);
        }

        void Update()
        {
            if (WorldState.Instance == null) return;
            refreshTimer -= Time.deltaTime;
            if (refreshTimer <= 0f)
            {
                RefreshLights();
                refreshTimer = 0.75f;
            }
            Apply(WorldState.Instance.blend);
        }

        void RefreshLights()
        {
            var all = FindObjectsByType<Light>(FindObjectsSortMode.None);
            var list = new System.Collections.Generic.List<Light>();
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] == null || all[i].type != LightType.Directional || !all[i].gameObject.activeInHierarchy)
                    continue;
                list.Add(all[i]);
            }
            directionalLights = list.ToArray();
        }

        void Apply(float rawBlend)
        {
            float blend = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(rawBlend));
            RenderSettings.fog = true;
            RenderSettings.fogColor = Color.Lerp(lightFog, darkFog, blend);
            RenderSettings.fogDensity = Mathf.Lerp(lightFogDensity, darkFogDensity, blend);
            RenderSettings.ambientSkyColor = Color.Lerp(lightAmbientSky, darkAmbientSky, blend);
            RenderSettings.ambientEquatorColor = Color.Lerp(lightAmbientEquator, darkAmbientEquator, blend);
            RenderSettings.ambientGroundColor = Color.Lerp(lightAmbientGround, darkAmbientGround, blend);
            RenderSettings.ambientIntensity = Mathf.Lerp(lightAmbientIntensity, darkAmbientIntensity, blend);
            RenderSettings.reflectionIntensity = Mathf.Lerp(0.75f, 0.45f, blend);

            if (directionalLights != null)
            {
                for (int i = 0; i < directionalLights.Length; i++)
                {
                    var light = directionalLights[i];
                    if (light == null) continue;
                    light.color = Color.Lerp(lightDirectional, darkDirectional, blend);
                    light.intensity = Mathf.Lerp(lightDirectionalIntensity, darkDirectionalIntensity, blend);
                }
            }

            if (hasSkybox && skyboxInstance != null)
            {
                Color skyTint = Color.Lerp(
                    new Color(0.94f, 0.90f, 0.96f, 1f),
                    new Color(0.012f, 0.019f, 0.037f, 1f),
                    blend);
                if (skyboxInstance.HasProperty("_Tint")) skyboxInstance.SetColor("_Tint", skyTint);
                if (skyboxInstance.HasProperty("_SkyTint")) skyboxInstance.SetColor("_SkyTint", skyTint);
                if (skyboxInstance.HasProperty("_GroundColor"))
                    skyboxInstance.SetColor("_GroundColor", Color.Lerp(
                        new Color(0.30f, 0.24f, 0.31f),
                        new Color(0.011f, 0.020f, 0.038f),
                        blend));
            }

            Shader.SetGlobalColor("_EchoLightPalette", Color.Lerp(
                new Color(0.84f, 0.86f, 0.94f, 1f),
                new Color(0.90f, 0.98f, 0.98f, 1f),
                0.18f + blend * 0.08f));
            Shader.SetGlobalColor("_EchoDarkPalette", Color.Lerp(
                new Color(0.01f, 0.012f, 0.012f, 1f),
                new Color(0.02f, 0.45f, 0.40f, 1f),
                blend));
            Shader.SetGlobalFloat("_EchoWorldBlend", blend);
        }

        void OnDestroy()
        {
            if (skyboxInstance != null)
                Destroy(skyboxInstance);
            RenderSettings.fog = originalFog;
            RenderSettings.fogColor = originalFogColor;
            RenderSettings.fogDensity = originalFogDensity;
            RenderSettings.ambientSkyColor = originalAmbientSky;
            RenderSettings.ambientEquatorColor = originalAmbientEquator;
            RenderSettings.ambientGroundColor = originalAmbientGround;
            RenderSettings.ambientIntensity = originalAmbientIntensity;
            RenderSettings.reflectionIntensity = originalReflectionIntensity;
        }
    }
}
