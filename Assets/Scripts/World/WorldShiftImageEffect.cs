using UnityEngine;

namespace EchoIslands
{
    [RequireComponent(typeof(Camera))]
    public class WorldShiftImageEffect : MonoBehaviour
    {
        public Shader shader;
        public Color lightSoft = new Color(0.97f, 0.91f, 0.94f, 1f);
        public Color lightPink = new Color(1f, 0.43f, 0.68f, 1f);
        public Color lightGold = new Color(1f, 0.73f, 0.28f, 1f);
        public Color darkAccent = new Color(0.05f, 0.62f, 0.52f, 1f);
        public float stableStrength = 0.58f;

        Material material;
        float blend;
        float pulse;
        float transition = 1f;
        float direction;
        Vector4 origin;
        bool initialized;

        void Awake()
        {
            if (shader == null) shader = Shader.Find("Echo/WorldShift");
            if (shader != null) material = new Material(shader);
            initialized = true;
        }

        public void Trigger(Vector3 worldOrigin, float transitionDirection)
        {
            origin = new Vector4(worldOrigin.x, worldOrigin.y, worldOrigin.z, 1f);
            direction = transitionDirection;
            pulse = 1f;
            transition = 0f;
        }

        public void SetState(float worldBlend, float worldPulse, float progress, float transitionDirection)
        {
            blend = Mathf.Clamp01(worldBlend);
            pulse = Mathf.Max(worldPulse, pulse * 0.90f);
            transition = progress;
            direction = transitionDirection;
        }

        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (!initialized || material == null)
            {
                Graphics.Blit(source, destination);
                return;
            }

            material.SetFloat("_Blend", blend);
            material.SetFloat("_Pulse", Mathf.Clamp01(pulse));
            material.SetFloat("_Transition", Mathf.Clamp01(transition));
            material.SetFloat("_Direction", direction);
            material.SetFloat("_StableStrength", stableStrength);
            material.SetColor("_LightSoft", lightSoft);
            material.SetColor("_LightPink", lightPink);
            material.SetColor("_LightGold", lightGold);
            material.SetColor("_DarkAccent", darkAccent);
            material.SetVector("_Origin", origin);
            Graphics.Blit(source, destination, material);
            pulse = Mathf.Max(0f, pulse - Time.deltaTime * 2.2f);
        }

        void OnDestroy()
        {
            if (material != null) Destroy(material);
        }
    }
}
