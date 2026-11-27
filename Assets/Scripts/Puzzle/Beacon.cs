using UnityEngine;

namespace EchoIslands
{
    public class Beacon : MonoBehaviour
    {
        public float radius = 7f;
        public WorldKind forced = WorldKind.Dark;
        public bool lit;

        int slot = -1;
        Renderer glow;

        void Awake()
        {
            var child = transform.Find("Flame");
            if (child != null) glow = child.GetComponent<Renderer>();
            ApplyVisual();
        }

        public void Toggle()
        {
            lit = !lit;
            if (WorldState.Instance == null) return;
            if (lit)
            {
                if (slot < 0) slot = WorldState.Instance.AllocMask();
                if (slot >= 0)
                    WorldState.Instance.SetMask(slot, transform.position, radius, forced == WorldKind.Dark ? 1f : 0f);
            }
            else if (slot >= 0)
            {
                WorldState.Instance.ClearMask(slot);
                slot = -1;
            }
            ApplyVisual();
        }

        void Update()
        {
            if (!lit || slot < 0 || WorldState.Instance == null) return;
            WorldState.Instance.SetMask(slot, transform.position, radius, forced == WorldKind.Dark ? 1f : 0f);
        }

        void ApplyVisual()
        {
            if (glow == null) return;
            glow.enabled = lit;
        }
    }
}
