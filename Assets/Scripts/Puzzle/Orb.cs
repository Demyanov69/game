using System;
using System.Collections;
using UnityEngine;

namespace EchoIslands
{
    public class Orb : MonoBehaviour
    {
        public WorldKind home = WorldKind.Light;
        public bool carried;
        public float pickupDuration = 0.38f;
        public float carryBob = 0.025f;
        public float carrySpin = 45f;

        WorldObject presence;
        WorldVariant variant;
        Collider[] colliders;
        Transform hold;
        float holdTime;
        // Camera local X is negative on the player's left, positive on the right.
        // The aether orb lives in the left hand; staffs and tools stay on the right.
        static readonly Vector3 LeftHandPosition = new Vector3(-0.38f, -0.25f, 0.74f);
        bool animating;

        void Awake()
        {
            presence = GetComponent<WorldObject>();
            variant = GetComponent<WorldVariant>();
            colliders = GetComponentsInChildren<Collider>(true);
        }

        void Start()
        {
            EchoVisualOverhaul.ReskinOrb(this);
            variant = GetComponent<WorldVariant>();
            if (variant != null && !carried) variant.Refresh();
        }

        void Update()
        {
            if (!carried || hold == null || animating) return;
            holdTime += Time.deltaTime;
            transform.localPosition = LeftHandPosition + Vector3.up * (Mathf.Sin(holdTime * 3.2f) * carryBob);
            transform.localRotation = Quaternion.Euler(
                Mathf.Sin(holdTime * 1.7f) * 7f,
                holdTime * carrySpin,
                Mathf.Cos(holdTime * 2.3f) * 6f);
        }

        public void Pick(Transform targetHold)
        {
            if (carried || targetHold == null || animating) return;
            hold = targetHold;
            if (variant == null) variant = GetComponent<WorldVariant>();
            carried = true;
            holdTime = 0f;

            if (variant != null) variant.enabled = false;
            foreach (var mesh in GetComponentsInChildren<MeshRenderer>(true))
                if (mesh != null && mesh.gameObject.name != "LuminousHeart" &&
                    mesh.gameObject.name != "GlassAura") mesh.enabled = false;
            foreach (var mesh in GetComponentsInChildren<MeshRenderer>(true))
                if (mesh != null && (mesh.gameObject.name == "LuminousHeart" ||
                    mesh.gameObject.name == "GlassAura")) mesh.enabled = true;
            foreach (var ps in GetComponentsInChildren<ParticleSystem>(true))
            {
                if (ps == null) continue;
                var psRenderer = ps.GetComponent<ParticleSystemRenderer>();
                if (psRenderer != null) psRenderer.enabled = true;
                if (!ps.isPlaying) ps.Play();
            }
            foreach (var glow in GetComponentsInChildren<Light>(true))
                if (glow != null) glow.enabled = true;
            if (presence != null)
            {
                presence.enabled = false;
                if (presence.renderers == null || presence.renderers.Length == 0)
                    presence.renderers = GetComponentsInChildren<Renderer>(true);
                foreach (var r in presence.renderers)
                    if (r != null && (r is ParticleSystemRenderer ||
                        r.gameObject.name == "LuminousHeart" || r.gameObject.name == "GlassAura"))
                        r.enabled = true;
            }
            if (colliders == null) colliders = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
                if (colliders[i] != null) colliders[i].enabled = false;

            StartCoroutine(PickRoutine(targetHold));
        }

        IEnumerator PickRoutine(Transform targetHold)
        {
            animating = true;
            Vector3 startPosition = transform.position;
            Quaternion startRotation = transform.rotation;
            Vector3 startScale = transform.localScale;
            float elapsed = 0f;
            SpawnOrbBurst(startPosition, new Color(1f, 0.45f, 0.72f, 1f), new Color(0.0f, 1f, 0.88f, 1f));

            transform.SetParent(targetHold, true);
            Vector3 startLocal = targetHold.InverseTransformPoint(startPosition);
            Quaternion startLocalRot = Quaternion.Inverse(targetHold.rotation) * startRotation;
            Vector3 endLocal = LeftHandPosition;
            Quaternion endLocalRot = Quaternion.identity;

            while (elapsed < pickupDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / Mathf.Max(0.05f, pickupDuration));
                float e = t * t * (3f - 2f * t);
                Vector3 p = Vector3.Lerp(startLocal, endLocal, e);
                p += Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.22f;
                transform.localPosition = p;
                transform.localRotation = Quaternion.Slerp(startLocalRot, endLocalRot, e) * Quaternion.Euler(0f, t * 260f, 0f);
                transform.localScale = Vector3.Lerp(startScale, startScale * 0.78f, e);
                yield return null;
            }

            transform.localPosition = endLocal;
            transform.localRotation = endLocalRot;
            transform.localScale = startScale * 0.78f;
            SpawnOrbBurst(transform.position, new Color(1f, 0.84f, 0.92f, 1f), new Color(0.05f, 1f, 0.85f, 1f));
            animating = false;
        }

        public void Place(Transform socket, Action onArrived = null)
        {
            if (socket == null)
            {
                onArrived?.Invoke();
                return;
            }
            StopAllCoroutines();
            StartCoroutine(PlaceRoutine(socket, onArrived));
        }

        IEnumerator PlaceRoutine(Transform socket, Action onArrived)
        {
            animating = true;
            Transform oldParent = transform.parent;
            Vector3 startWorld = transform.position;
            Quaternion startWorldRot = transform.rotation;
            Vector3 startScale = transform.lossyScale;
            transform.SetParent(socket, true);
            Vector3 targetScale = new Vector3(startScale.x, startScale.y, startScale.z) * 0.82f;
            float elapsed = 0f;

            SpawnOrbBurst(startWorld, new Color(1f, 0.86f, 0.95f, 0.8f), new Color(0.0f, 1f, 0.85f, 1f));
            while (elapsed < pickupDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / Mathf.Max(0.05f, pickupDuration));
                float e = t * t * (3f - 2f * t);
                transform.position = Vector3.Lerp(startWorld, socket.position + Vector3.up * 0.08f, e);
                transform.rotation = Quaternion.Slerp(startWorldRot, socket.rotation, e) * Quaternion.Euler(0f, e * 360f, 0f);
                transform.localScale = Vector3.Lerp(startScale, targetScale, e);
                yield return null;
            }

                        transform.SetParent(socket, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one * 0.82f;
            carried = false;
            hold = null;
            if (variant != null) variant.enabled = false;
            if (colliders == null) colliders = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
                if (colliders[i] != null) colliders[i].enabled = false;

            // Dissolve into the portal instead of sitting as a solid ball.
            float dissolve = 0f;
            Vector3 dissolveStart = transform.localScale;
            SpawnOrbBurst(socket.position, new Color(0.3f, 1f, 0.9f, 1f), new Color(0.05f, 0.8f, 0.7f, 1f));
            // Boost particle emission briefly.
            var systems = GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < systems.Length; i++)
            {
                if (systems[i] == null) continue;
                var em = systems[i].emission;
                em.rateOverTime = 48f;
            }
            while (dissolve < 0.85f)
            {
                dissolve += Time.deltaTime;
                float k = Mathf.Clamp01(dissolve / 0.85f);
                float e = k * k * (3f - 2f * k);
                transform.localScale = Vector3.Lerp(dissolveStart, Vector3.zero, e);
                transform.Rotate(0f, 220f * Time.deltaTime, 120f * Time.deltaTime, Space.Self);
                yield return null;
            }

            // Stop visuals, keep object for portal logic if needed
            for (int i = 0; i < systems.Length; i++)
                if (systems[i] != null) systems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            foreach (var r in GetComponentsInChildren<Renderer>(true))
                if (r != null) r.enabled = false;
            var light = GetComponentInChildren<Light>();
            if (light != null) light.enabled = false;
            transform.localScale = Vector3.zero;
            animating = false;
            onArrived?.Invoke();
        }

        void SpawnOrbBurst(Vector3 position, Color colorA, Color colorB)
        {
            var go = new GameObject("OrbVFX_Burst");
            go.transform.position = position;
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 0.65f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.75f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.35f, 2.1f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.018f, 0.06f);
            main.startColor = new ParticleSystem.MinMaxGradient(colorA, colorB);
            var emission = ps.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 18) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.08f;
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            var shader = Shader.Find("Legacy Shaders/Particles/Additive");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader != null) renderer.sharedMaterial = new Material(shader);
            ps.Play();
            Destroy(go, 1.2f);
        }
    }
}
