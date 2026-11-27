using UnityEngine;

namespace EchoIslands
{
    public class CheckpointTrigger : MonoBehaviour
    {
        public Transform checkpoint;
        public int id;
        public string hint;
        bool activated;

        void OnTriggerEnter(Collider other)
        {
            var flow = LevelFlow.Instance;
            if (activated || flow == null || flow.Controller == null || !flow.HasBegun ||
                other.GetComponentInParent<FirstPersonController>() != flow.Controller) return;
            if (checkpoint == null) checkpoint = transform;
            flow.SetCheckpoint(this);
            activated = true;
            var collider = GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
        }
    }
}
