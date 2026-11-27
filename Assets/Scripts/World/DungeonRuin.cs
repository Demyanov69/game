using UnityEngine;

namespace EchoIslands
{
    public sealed class DungeonRuin : MonoBehaviour
    {
        public GameObject intact, ruined;
        WorldState state;
        public void Configure(GameObject daylight, GameObject night)
        {
            intact = daylight;
            ruined = night;
            SetWorld(WorldState.Instance != null ? WorldState.Instance.Current : WorldKind.Light);
        }

        void OnEnable()
        {
            state = WorldState.Instance;
            if (state == null) return;
            state.Changed += SetWorld;
            SetWorld(state.Current);
        }
        void OnDisable()
        {
            if (state != null) state.Changed -= SetWorld;
        }
        void SetWorld(WorldKind world)
        {
            if (intact != null) intact.SetActive(world == WorldKind.Light);
            if (ruined != null) ruined.SetActive(world == WorldKind.Dark);
        }
    }
}
