using System;
using System.Collections;
using UnityEngine;

namespace EchoIslands
{
    public enum ToolKind
    {
        Staff,
        Flashlight,
        Clock
    }

    public class ToolPickup : MonoBehaviour
    {
        public static event Action<ToolKind> Picked;
        public ToolKind kind = ToolKind.Staff;
        public string hint = "Посох";
        public float pickupDuration = 0.42f;
        public bool animatePickup = true;

        bool busy;
        Collider[] cachedColliders;
        Renderer[] cachedRenderers;
        Vector3 baseScale;
        Vector3 baseLocalPosition;
        float idleTime;

        void Awake()
        {
            cachedColliders = GetComponentsInChildren<Collider>(true);
            cachedRenderers = GetComponentsInChildren<Renderer>(true);
            baseScale = transform.localScale;
            baseLocalPosition = transform.localPosition;
        }

        void Update()
        {
            if (busy) return;
            idleTime += Time.deltaTime;
            float bob = Mathf.Sin(idleTime * 2.2f) * 0.04f;
            transform.localPosition = baseLocalPosition + Vector3.up * bob;
            transform.Rotate(Vector3.up, 22f * Time.deltaTime, Space.World);
        }

        public void Take(ToolController tools)
        {
            if (busy || tools == null) return;
            if (kind == ToolKind.Staff && tools.staff || kind == ToolKind.Clock && tools.clock)
            {
                gameObject.SetActive(false);
                return;
            }
            busy = true;
            if (cachedColliders == null) cachedColliders = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < cachedColliders.Length; i++)
                if (cachedColliders[i] != null) cachedColliders[i].enabled = false;

            if (animatePickup)
                StartCoroutine(PickupRoutine(tools));
            else
            {
                tools.Give(kind);
                FinishPickup();
            }
        }

        IEnumerator PickupRoutine(ToolController tools)
        {
            Transform hold = tools.maskOrigin;
            if (hold == null && Camera.main != null) hold = Camera.main.transform;
            if (hold == null)
            {
                tools.Give(kind);
                FinishPickup();
                yield break;
            }

            Vector3 start = transform.position;
            Quaternion startRot = transform.rotation;
            Vector3 startScale = transform.localScale;
            Vector3 control = start + Vector3.up * 0.75f + hold.forward * 0.25f;
            float elapsed = 0f;
            SpawnPickupBurst(start);
            SpawnExternalPickupFX(start);

            while (elapsed < pickupDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / Mathf.Max(0.05f, pickupDuration));
                float e = t * t * (3f - 2f * t);
                Vector3 p0 = Vector3.Lerp(start, control, e);
                Vector3 p1 = Vector3.Lerp(control, hold.position, e);
                transform.position = Vector3.Lerp(p0, p1, e);
                transform.rotation = Quaternion.Slerp(startRot, hold.rotation, e);
                transform.Rotate(Vector3.up, 540f * Time.deltaTime, Space.Self);
                transform.localScale = Vector3.Lerp(startScale, startScale * 0.68f, e);
                yield return null;
            }

            SpawnPickupBurst(hold.position);
            SpawnExternalPickupFX(hold.position);
            tools.Give(kind);
            FinishPickup();
        }

        void FinishPickup()
        {
            Picked?.Invoke(kind);
            if (LevelFlow.Instance != null) LevelFlow.Instance.SaveCurrent();
            Destroy(gameObject);
        }

        void SpawnExternalPickupFX(Vector3 position)
        {
            var prefab = Resources.Load<GameObject>("EchoL1/StaffPickupFX");
            if (prefab == null) return;
            var fx = Instantiate(prefab, position, Quaternion.identity);
            fx.name = "StaffPickup_HovlFX";
            fx.transform.localScale *= 0.85f;
            Destroy(fx, 1.6f);
        }

        void SpawnPickupBurst(Vector3 position)
        {
            var go = new GameObject("PickupSparkBurst");
            go.transform.position = position;
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 0.5f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.65f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 2.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.018f, 0.065f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.86f, 0.72f, 0.75f),
                new Color(1f, 0.42f, 0.70f, 1f));
            var emission = ps.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 22) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.06f;
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            var shader = Shader.Find("Legacy Shaders/Particles/Additive");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader != null) renderer.sharedMaterial = new Material(shader);
            ps.Play();
            Destroy(go, 1.2f);
        }
    }
}
