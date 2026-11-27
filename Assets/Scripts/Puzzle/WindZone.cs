using UnityEngine;

namespace EchoIslands
{
    public class WindZone : MonoBehaviour
    {
        public float gustForce = 2.4f;
        public float gustPeriod = 4.5f;
        public float gustDuration = 1.35f;
        public Vector3 direction = Vector3.right;

        ParticleSystem particles;

        void Awake()
        {
            particles = GetComponentInChildren<ParticleSystem>();
        }

        void Update()
        {
            if (particles == null || WorldState.Instance == null) return;
            var emission = particles.emission;
            emission.enabled = WorldState.Instance.Current == WorldKind.Light;
        }

        void OnTriggerStay(Collider other)
        {
            if (WorldState.Instance == null || WorldState.Instance.Current != WorldKind.Light) return;
            var controller = other.GetComponent<FirstPersonController>();
            if (controller == null) controller = other.GetComponentInParent<FirstPersonController>();
            if (controller == null) return;

            float phase = Mathf.Repeat(Time.time, gustPeriod);
            if (phase > gustDuration) return;
            controller.ApplyExternalMotion(direction.normalized * gustForce * Time.deltaTime);
        }
    }
}
