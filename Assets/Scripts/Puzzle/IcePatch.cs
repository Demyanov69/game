using UnityEngine;

namespace EchoIslands
{
    public sealed class IcePatch : MonoBehaviour
    {
        public Collider surface;

        void OnTriggerStay(Collider other)
        {
            if (surface != null && !surface.enabled) return;
            var fps = other.GetComponentInParent<FirstPersonController>();
            if (fps != null) fps.SetSlippery(true);
        }
    }
}
