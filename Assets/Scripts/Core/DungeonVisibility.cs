using System.Collections.Generic;
using UnityEngine;

namespace EchoIslands
{
    [DefaultExecutionOrder(90)]
    public sealed class DungeonVisibility : MonoBehaviour
    {
        struct Sector
        {
            public GameObject root;
            public Bounds bounds;
            public PortalFrame portal;
        }

        readonly List<Sector> sectors = new();
        Transform player;
        float range;
        float nextRefresh;

        public void Configure(Transform target, float visibleRange)
        {
            player = target;
            range = visibleRange;
        }

        public void Add(Transform sector, Vector3 center, Vector3 size)
        {
            // The rooms and cells rotate only around Y.
            Vector3 right = sector.right * size.x;
            Vector3 forward = sector.forward * size.z;
            Vector3 extent = new(Mathf.Abs(right.x) + Mathf.Abs(forward.x), 0f,
                Mathf.Abs(right.z) + Mathf.Abs(forward.z));
            Vector3 position = sector.TransformPoint(center);
            position.y = 0f;
            sectors.Add(new Sector
            {
                root = sector.gameObject,
                bounds = new Bounds(position, extent),
                portal = sector.GetComponentInChildren<PortalFrame>(true)
            });
        }

        public void Refresh(Vector3 position)
        {
            position.y = 0f;
            foreach (var sector in sectors)
            {
                bool active = sector.root.activeSelf;
                float limit = range + (active ? 5f : 0f);
                bool keep = sector.bounds.SqrDistance(position) <= limit * limit
                    || (sector.portal != null && sector.portal.IsAbsorbing);
                if (active != keep) sector.root.SetActive(keep);
            }
            nextRefresh = Time.time + .15f;
        }

        void LateUpdate()
        {
            if (player != null && Time.time >= nextRefresh) Refresh(player.position);
        }
    }
}
