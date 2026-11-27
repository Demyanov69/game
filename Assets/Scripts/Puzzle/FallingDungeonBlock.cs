using UnityEngine;

namespace EchoIslands
{
    public sealed class FallingDungeonBlock : MonoBehaviour
    {
        public float triggerDistance = 3.2f;
        Vector3 rest;
        float countdown = -1f, recovery;
        Transform player;
        void Start()
        {
            rest = transform.position;
            var fps = FindFirstObjectByType<FirstPersonController>();
            if (fps != null) player = fps.transform;
        }
        void Update()
        {
            if (player == null || Time.timeScale <= 0f) return;
            if (countdown < 0f && recovery <= 0f && Vector3.ProjectOnPlane(player.position - rest, Vector3.up).sqrMagnitude < triggerDistance * triggerDistance)
                countdown = 0.58f;
            if (countdown >= 0f)
            {
                countdown -= Time.deltaTime;
                if (countdown <= 0f) recovery = 2.25f;
                else transform.position = rest + Vector3.right * (Mathf.Sin(Time.time * 62f) * .09f);
            }
            if (recovery > 0f)
            {
                recovery -= Time.deltaTime;
                float drop = Mathf.Min(9.4f,(2.25f-recovery)*13f);
                transform.position = rest + Vector3.down * drop;
                if (recovery <= 0f) { transform.position = rest; countdown = -1f; }
            }
        }
    }
}
