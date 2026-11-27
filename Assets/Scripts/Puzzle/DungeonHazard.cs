using UnityEngine;

namespace EchoIslands
{
    public sealed class DungeonHazard : MonoBehaviour
    {
        float lastHit;
        void OnTriggerEnter(Collider other) { Kill(other); }
        void OnTriggerStay(Collider other) { Kill(other); }
        void Kill(Collider other)
        {
            if (Time.time - lastHit < 0.35f || LevelFlow.Instance == null) return;
            if (other.GetComponentInParent<FirstPersonController>() == null) return;
            lastHit = Time.time;
            LevelFlow.Instance.Respawn();
        }
    }
}
