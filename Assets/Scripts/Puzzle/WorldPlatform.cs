using UnityEngine;

namespace EchoIslands
{
    public class WorldPlatform : MonoBehaviour
    {
        public float lightHeight;
        public float darkHeight;
        public float bobAmplitude = 0.22f;
        public float bobSpeed = 0.8f;
        public float phase;
        public float moveSpeed = 2.8f;
        public Vector2 horizontalSize = new Vector2(1.6f, 1.6f);

        FirstPersonController player;
        CharacterController playerController;
        Vector3 lastPosition;

        void Start()
        {
            player = FindFirstObjectByType<FirstPersonController>();
            if (player != null) playerController = player.GetComponent<CharacterController>();
            lastPosition = transform.position;
        }

        void Update()
        {
            if (WorldState.Instance == null) return;
            if (player == null)
            {
                player = FindFirstObjectByType<FirstPersonController>();
                if (player != null) playerController = player.GetComponent<CharacterController>();
            }

            float baseHeight = WorldState.Instance.Current == WorldKind.Dark ? darkHeight : lightHeight;
            float bob = Mathf.Sin(Time.time * bobSpeed + phase) * bobAmplitude;
            Vector3 target = transform.position;
            target.y = baseHeight + bob;
            Vector3 before = transform.position;
            transform.position = Vector3.MoveTowards(before, target, moveSpeed * Time.deltaTime);
            Vector3 delta = transform.position - before;

            if (player != null && playerController != null && playerController.enabled && playerController.isGrounded && delta.sqrMagnitude > 0.000001f)
            {
                Vector3 p = player.transform.position;
                float dx = Mathf.Abs(p.x - transform.position.x);
                float dz = Mathf.Abs(p.z - transform.position.z);
                float top = transform.position.y + 0.25f;
                if (dx <= horizontalSize.x && dz <= horizontalSize.y && Mathf.Abs(p.y - top) < 1.1f)
                    playerController.Move(delta);
            }

            lastPosition = transform.position;
        }
    }
}
