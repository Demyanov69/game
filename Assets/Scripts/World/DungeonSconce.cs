using UnityEngine;

namespace EchoIslands
{
    public sealed class DungeonSconce : MonoBehaviour
    {
        Light lamp;
        Color intact, cursed;
        float intensity;
        WorldState state;

        public void Configure(Light source, Color color, float power)
        {
            lamp = source;
            intact = color;
            intensity = power;
            float hue = Mathf.Repeat(transform.position.x * 1.31f + transform.position.z * .407f, 37f) / 37f;
            cursed = color.r > .9f && color.g < .4f ? new Color(.78f,.16f,.20f)
                : Color.Lerp(new Color(.18f,.82f,.72f), new Color(.47f,.23f,.83f), hue);
        }

        void OnEnable()
        {
            state = WorldState.Instance;
            if (state != null) state.Changed += Apply;
            Apply(state != null ? state.Current : WorldKind.Light);
        }

        void OnDisable()
        {
            if (state != null) state.Changed -= Apply;
        }

        void Apply(WorldKind world)
        {
            if (lamp == null) return;
            bool night = world == WorldKind.Dark;
            lamp.color = night ? cursed : intact;
            lamp.intensity = night ? intensity * .72f : intensity;
        }
    }
}
