using UnityEngine;

namespace EchoIslands
{
    public class PlayerWorldGlow : MonoBehaviour
    {
        public Color glowColor = new Color(0.02f, 1f, 0.86f, 1f);
        public float intensity = 6.5f;
        public float range = 5.5f;
        public float secondaryIntensity = 1.8f;
        public float secondaryRange = 2.8f;

        Light mainLight;
        Light softLight;
        float blend;

        void Awake()
        {
            var mainGo = new GameObject("Player_TurquoiseGlow");
            mainGo.transform.SetParent(transform, false);
            mainGo.transform.localPosition = new Vector3(0f, 0.9f, 0.15f);
            mainLight = mainGo.AddComponent<Light>();
            mainLight.type = LightType.Point;
            mainLight.color = glowColor;
            mainLight.range = range;
            mainLight.intensity = intensity;
            mainLight.shadows = LightShadows.None;

            var softGo = new GameObject("Player_TurquoiseGlow_Soft");
            softGo.transform.SetParent(transform, false);
            softGo.transform.localPosition = new Vector3(0f, 1.0f, 0f);
            softLight = softGo.AddComponent<Light>();
            softLight.type = LightType.Point;
            softLight.color = new Color(0.08f, 0.75f, 0.68f, 1f);
            softLight.range = secondaryRange;
            softLight.intensity = secondaryIntensity;
            softLight.shadows = LightShadows.None;
        }

        void Update()
        {
            float target = WorldState.Instance != null
                ? Mathf.Clamp01(WorldState.Instance.blend)
                : 1f;
            blend = Mathf.Lerp(blend, target, 1f - Mathf.Exp(-8f * Time.deltaTime));
            float factor = Mathf.SmoothStep(0f, 1f, blend);
            if (mainLight != null) mainLight.intensity = intensity * factor;
            if (softLight != null) softLight.intensity = secondaryIntensity * factor;
        }
    }
}
