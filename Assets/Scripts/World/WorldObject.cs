using UnityEngine;

namespace EchoIslands
{
    [DisallowMultipleComponent]
    public class WorldObject : MonoBehaviour
    {
        public WorldKind home = WorldKind.Light;
        public Renderer[] renderers;

        WorldVariant variant;

        void Awake()
        {
            variant = GetComponent<WorldVariant>();
            if (variant == null)
                variant = gameObject.AddComponent<WorldVariant>();

            variant.presence = home == WorldKind.Dark ? WorldPresence.Dark : WorldPresence.Light;
            variant.includeChildren = true;
            variant.useTransitionWave = true;
            variant.useSoftFade = true;
            variant.fadeDuration = 0.72f;
        }

        void Start()
        {
            if (variant == null) return;
            variant.presence = home == WorldKind.Dark ? WorldPresence.Dark : WorldPresence.Light;
            variant.Refresh();
        }

        public void Refresh()
        {
            if (variant == null) return;
            variant.presence = home == WorldKind.Dark ? WorldPresence.Dark : WorldPresence.Light;
            variant.Refresh();
        }
    }
}
