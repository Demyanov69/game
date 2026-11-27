using UnityEngine;

namespace EchoIslands
{
    public class CheckpointTrigger : MonoBehaviour
    {
        public Transform checkpoint;
        bool activated;

        void OnTriggerEnter(Collider other)
        {
            if (activated || LevelFlow.Instance == null) return;
            if (other.GetComponent<FirstPersonController>() == null && other.GetComponentInParent<FirstPersonController>() == null) return;

            LevelFlow.Instance.SetCheckpoint(checkpoint != null ? checkpoint : transform);
            activated = true;
            var collider = GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
        }
    }
}
