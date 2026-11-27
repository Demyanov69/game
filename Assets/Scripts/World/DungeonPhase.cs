using UnityEngine;

namespace EchoIslands
{
    public sealed class DungeonPhase : MonoBehaviour
    {
        WorldPresence presence;
        WorldState state;
        Collider surface;
        Renderer[] visuals;

        public void Configure(WorldPresence mode, Collider collider, Renderer[] renderers)
        {
            presence = mode;
            surface = collider;
            visuals = renderers;
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
            if (visuals == null) return;
            bool visible = presence == WorldPresence.Both ||
                (presence == WorldPresence.Dark) == (world == WorldKind.Dark);
            if (surface != null) surface.enabled = visible;
            foreach (var visual in visuals) visual.enabled = visible;
        }
    }
}
