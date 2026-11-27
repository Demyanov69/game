using UnityEngine;

namespace EchoIslands
{
    [DefaultExecutionOrder(-15)]
    public sealed class MovingDungeonPlatform : MonoBehaviour
    {
        public Vector3 travel = new Vector3(3.1f,0f,0f);
        public float period = 5.8f;
        Vector3 origin;
        float elapsed;
        FirstPersonController rider;
        Collider deck;

        void Start()
        {
            origin = transform.position;
            deck = GetComponent<Collider>();
            rider = LevelFlow.Instance != null ? LevelFlow.Instance.Controller : null;
        }

        void Update()
        {
            if (Time.deltaTime <= 0f) return;
            elapsed += Time.deltaTime;
            Vector3 next = origin + travel * Mathf.Sin(elapsed * 2f*Mathf.PI / period);
            Vector3 delta = next - transform.position;
            bool carry = false;
            if (deck.enabled && rider != null && rider.enabled && !rider.IsTurningGravity &&
                (rider.transform.position - transform.position).sqrMagnitude <= 12f)
            {
                Vector3 foot = rider.transform.position;
                Vector3 up = rider.transform.up;
                carry = Physics.Raycast(foot+up*.16f,-up,out var hit,.33f,
                    ~0,QueryTriggerInteraction.Ignore) && hit.collider == deck;
            }
            transform.position = next;
            if (carry)
            {
                Physics.SyncTransforms();
                rider.ApplyExternalMotion(delta);
            }
        }
    }
}
