using UnityEngine;

namespace EchoIslands
{
    public sealed class IcePatch : MonoBehaviour
    {
        void OnTriggerStay(Collider other)
        {
            var fps = other.GetComponentInParent<FirstPersonController>();
            if (fps != null) fps.SetSlippery(true);
        }
        void OnTriggerExit(Collider other)
        {
            var fps = other.GetComponentInParent<FirstPersonController>();
            if (fps != null) fps.SetSlippery(false);
        }
    }
}
