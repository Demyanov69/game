using UnityEngine;

namespace EchoIslands
{
    public class FallCatch : MonoBehaviour
    {
        void OnTriggerEnter(Collider other)
        {
            if (LevelFlow.Instance == null) return;
            if (other.GetComponent<FirstPersonController>() == null && other.GetComponentInParent<FirstPersonController>() == null)
                return;
            LevelFlow.Instance.Respawn();
        }
    }
}
