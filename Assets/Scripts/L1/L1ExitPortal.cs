using UnityEngine;

namespace EchoIslands
{
    [RequireComponent(typeof(Collider))]
    public class L1ExitPortal : MonoBehaviour
    {
        public bool active;
        public float pulseSpeed = 2.4f;
        public float pulseAmount = 0.12f;
        bool completed;
        Renderer[] renderers;
        Vector3 baseScale;
        Transform field;

        void Awake()
        {
            renderers = GetComponentsInChildren<Renderer>(true);
            field = transform.Find("PortalField");
            baseScale = field != null ? field.localScale : Vector3.one;
        }

        public void SetActive(bool value)
        {
            active = value;
            if (renderers == null) renderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i] != null) renderers[i].enabled = value;
            var colliders = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
                if (colliders[i] != null) colliders[i].enabled = value;
        }

        void Update()
        {
            if (!active || field == null) return;
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            field.localScale = new Vector3(baseScale.x * pulse, baseScale.y * (1f + (pulse - 1f) * 0.55f), baseScale.z);
            field.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 0.75f) * 3f);
        }

        void OnTriggerEnter(Collider other)
        {
            if (!active || completed) return;
            if (!other.GetComponentInParent<FirstPersonController>()) return;
            completed = true;
            var hud = FindAnyObjectByType<GameHUD>();
            if (hud != null) hud.ShowWin();
        }
    }
}
