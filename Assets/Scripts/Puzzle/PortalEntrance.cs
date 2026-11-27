using UnityEngine;

namespace EchoIslands
{
    /// <summary>Collider lives on a child, so the portal itself receives the player's entry.</summary>
    public sealed class PortalEntrance : MonoBehaviour
    {
        void OnTriggerEnter(Collider other) { Enter(other); }
        void OnTriggerStay(Collider other) { Enter(other); }
        void Enter(Collider other)
        {
            var flow = LevelFlow.Instance;
            if (flow == null || flow.Controller == null || other.GetComponentInParent<FirstPersonController>() != flow.Controller) return;
            var portal = GetComponentInParent<PortalFrame>();
            if (portal != null) portal.TryEnter();
        }
    }
}
