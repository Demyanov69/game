using UnityEngine;

namespace EchoIslands
{
    public class OrbSocket : MonoBehaviour
    {
        public Transform socket;
        public PortalFrame targetPortal;
        public bool filled;
        bool busy;

        void Awake()
        {
            var collider = GetComponent<SphereCollider>();
            if (collider == null) collider = gameObject.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = 0.68f;
        }

        public void TryInsertHeld()
        {
            if (filled || busy) return;
            var orbs = FindObjectsByType<Orb>(FindObjectsSortMode.None);
            Orb carried = null;
            for (int i = 0; i < orbs.Length; i++)
            {
                if (orbs[i] != null && orbs[i].carried)
                {
                    carried = orbs[i];
                    break;
                }
            }
            if (carried == null) return;

            busy = true;
            Transform target = targetPortal != null ? targetPortal.GetAbsorptionPoint()
                : (socket != null ? socket : transform);
            carried.Place(target, FinishInsert);
        }

        void FinishInsert()
        {
            filled = true;
            busy = false;
            if (targetPortal != null) targetPortal.Activate();
        }
    }
}
