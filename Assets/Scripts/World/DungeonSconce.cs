using UnityEngine;

namespace EchoIslands
{
    public sealed class DungeonSconce : MonoBehaviour
    {
        Light lamp;
        Color intact;
        Color cursed;
        float intensity;
        float offset;

        public void Configure(Light source, Color color, float power)
        {
            lamp = source;
            intact = color;
            intensity = power;
            offset = Mathf.Repeat(transform.position.x*1.31f + transform.position.z*.407f,37f);
            cursed = Color.Lerp(new Color(.18f,.82f,.72f),new Color(.47f,.23f,.83f),offset/37f);
            if (intact.r > .9f && intact.g < .4f) cursed = new Color(.78f,.16f,.20f);
        }

        void Update()
        {
            float blend = WorldState.Instance != null ? WorldState.Instance.blend : 0f;
            float flicker = Mathf.PerlinNoise(offset, Time.time*4.5f);
            lamp.color = Color.Lerp(intact,cursed,blend);
            lamp.intensity = intensity * Mathf.Lerp(1f,.45f + .52f*flicker,blend);
        }
    }
}
