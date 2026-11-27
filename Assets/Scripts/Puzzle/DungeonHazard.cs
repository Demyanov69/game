using UnityEngine;

namespace EchoIslands
{
    public sealed class DungeonHazard : MonoBehaviour
    {
        void OnTriggerEnter(Collider other) { Kill(other); }
        void OnTriggerStay(Collider other) { Kill(other); }

        void Kill(Collider other)
        {
            var flow = LevelFlow.Instance;
            if (flow != null && flow.Controller != null &&
                other.GetComponentInParent<FirstPersonController>() == flow.Controller) flow.Respawn();
        }
    }
}
