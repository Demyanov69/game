using UnityEngine;

namespace EchoIslands
{
    [DefaultExecutionOrder(-10)]
    public class WorldTransitionVFX : MonoBehaviour
    {
        public Camera targetCamera;
        public Transform player;
        public float waveRadius = 70f;
        public float duration = 1.45f;
        public float cameraKick = 0.16f;
        public float shakeFrequency = 22f;
        public Color lightSoft = new Color(1f, 0.88f, 0.93f, 1f);
        public Color lightPink = new Color(1f, 0.46f, 0.72f, 1f);
        public Color lightGold = new Color(1f, 0.80f, 0.32f, 1f);
        public Color darkAccent = new Color(0.05f, 0.62f, 0.55f, 1f);
        public Color darkDeep = new Color(0.01f, 0.08f, 0.07f, 1f);

        ParticleSystem burst;
        ParticleSystem dust;
        ParticleSystem sparks;
        Material particleMaterial;
        LineRenderer ring;
        LineRenderer innerRing;
        WorldShiftImageEffect imageEffect;
        Vector3 cameraLocalOrigin;
        Quaternion cameraLocalRotation;
        Vector3 origin;
        WorldKind observedTarget;
        bool transitioning;
        float localTime;
        GameObject externalBurstInstance;

        void Start()
        {
            ResolveReferences();
            if (targetCamera != null)
            {
                cameraLocalOrigin = targetCamera.transform.localPosition;
                cameraLocalRotation = targetCamera.transform.localRotation;
                imageEffect = targetCamera.GetComponent<WorldShiftImageEffect>();
                if (imageEffect == null) imageEffect = targetCamera.gameObject.AddComponent<WorldShiftImageEffect>();
                imageEffect.lightSoft = lightSoft;
                imageEffect.lightPink = lightPink;
                imageEffect.lightGold = lightGold;
                imageEffect.darkAccent = darkAccent;
            }

            if (WorldState.Instance != null)
            {
                observedTarget = WorldState.Instance.Target;
                WorldState.Instance.TransitionStarted += OnTransitionStarted;
            }

            CreateRings();
        }

        void Update()
        {
            if (WorldState.Instance == null) return;
            ResolveReferences();
            if (WorldState.Instance.IsTransitioning)
            {
                if (!transitioning) StartTransition(WorldState.Instance.Target);
                UpdateTransition(WorldState.Instance.TransitionProgress);
            }
            else
            {
                transitioning = false;
                if (targetCamera != null)
                {
                    targetCamera.transform.localPosition = Vector3.Lerp(
                        targetCamera.transform.localPosition,
                        cameraLocalOrigin,
                        Time.deltaTime * 14f);
                }
                if (ring != null) ring.enabled = false;
                if (innerRing != null) innerRing.enabled = false;
                if (targetCamera != null)
                    targetCamera.transform.localRotation = Quaternion.Slerp(targetCamera.transform.localRotation, cameraLocalRotation, Time.deltaTime * 10f);
                if (imageEffect != null) imageEffect.SetState(WorldState.Instance.blend, 0f, 1f, observedTarget == WorldKind.Dark ? 1f : -1f);
            }
        }

        void ResolveReferences()
        {
            if (targetCamera == null) targetCamera = Camera.main;
            if (player == null)
            {
                var fp = FindAnyObjectByType<FirstPersonController>();
                if (fp != null) player = fp.transform;
                else if (targetCamera != null) player = targetCamera.transform.root;
            }
        }

        void OnTransitionStarted(WorldKind target)
        {
            observedTarget = target;
            StartTransition(target);
        }

        void StartTransition(WorldKind target)
        {
            ResolveReferences();
            transitioning = true;
            localTime = 0f;
            origin = player != null ? player.position + Vector3.up * 0.8f : (targetCamera != null ? targetCamera.transform.position : transform.position);
            if (imageEffect != null)
                imageEffect.Trigger(origin, target == WorldKind.Dark ? 1f : -1f);

            bool toDark = target == WorldKind.Dark;
            if (imageEffect != null)
                imageEffect.Trigger(origin, toDark ? 1f : -1f);
        }

        void UpdateTransition(float progress)
        {
            localTime += Time.deltaTime;
            float pulse = Mathf.Sin(progress * Mathf.PI);
            float shake = pulse * cameraKick;
            if (targetCamera != null)
            {
                Vector3 offset = new Vector3(
                    Mathf.Sin(Time.time * shakeFrequency) * shake,
                    Mathf.Cos(Time.time * shakeFrequency * 1.37f) * shake * 0.72f,
                    Mathf.Sin(Time.time * shakeFrequency * 0.71f) * shake * 0.35f);
                targetCamera.transform.localPosition = Vector3.Lerp(
                    targetCamera.transform.localPosition,
                    cameraLocalOrigin + offset,
                    Time.deltaTime * 22f);
                float roll = Mathf.Sin(Time.time * shakeFrequency * 0.83f) * pulse * 1.15f;
                float pitch = Mathf.Cos(Time.time * shakeFrequency * 1.1f) * pulse * 0.55f;
                targetCamera.transform.localRotation = cameraLocalRotation * Quaternion.Euler(pitch, 0f, roll);
            }

            if (imageEffect != null)
                imageEffect.SetState(WorldState.Instance.blend, pulse, progress, observedTarget == WorldKind.Dark ? 1f : -1f);
        }

        void SpawnExternalTransitionBurst(bool dark)
        {
            if (externalBurstInstance != null)
                Destroy(externalBurstInstance);

            string resource = dark ? "EchoL1/TransitionDark" : "EchoL1/TransitionLight";
            var prefab = Resources.Load<GameObject>(resource);
            if (prefab == null) return;

            externalBurstInstance = Instantiate(prefab, origin, Quaternion.identity);
            externalBurstInstance.name = dark ? "HovlTransition_Dark" : "HovlTransition_Light";
            externalBurstInstance.transform.localScale *= 1.25f;
            Destroy(externalBurstInstance, Mathf.Max(1.5f, duration + 0.6f));
        }

        void SetParticleTheme(bool dark)
        {
            if (burst == null || sparks == null || dust == null) return;
            ParticleSystem.MinMaxGradient mainGradient = dark
                ? new ParticleSystem.MinMaxGradient(darkDeep, darkAccent)
                : new ParticleSystem.MinMaxGradient(lightSoft, lightPink);
            ParticleSystem.MinMaxGradient sparkGradient = dark
                ? new ParticleSystem.MinMaxGradient(new Color(0f, 0.65f, 0.60f, 0.55f), darkAccent)
                : new ParticleSystem.MinMaxGradient(lightGold, lightPink);
            var burstMain = burst.main;
            burstMain.startColor = mainGradient;
            var dustMain = dust.main;
            dustMain.startColor = mainGradient;
            var sparksMain = sparks.main;
            sparksMain.startColor = sparkGradient;
        }

        void Fire(ParticleSystem ps, int count, float speed)
        {
            if (ps == null) return;
            ps.transform.position = origin;
            var main = ps.main;
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.45f, speed);
            var emission = ps.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
            ps.Clear(true);
            ps.Play(true);
        }

        void CreateParticles()
        {
            burst = CreateSystem("WorldShift_Burst", true);
            dust = CreateSystem("WorldShift_Dust", true);
            sparks = CreateSystem("WorldShift_Sparks", true);

            ConfigureBurst(burst);
            ConfigureDust(dust);
            ConfigureSparks(sparks);
        }

        ParticleSystem CreateSystem(string name, bool worldSpace)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.maxParticles = 500;
            main.simulationSpace = worldSpace ? ParticleSystemSimulationSpace.World : ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            var shader = Shader.Find("Legacy Shaders/Particles/Additive");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                if (particleMaterial == null) particleMaterial = new Material(shader);
                renderer.sharedMaterial = particleMaterial;
            }
            return ps;
        }

        void ConfigureBurst(ParticleSystem ps)
        {
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 1.05f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 4.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.085f);
            main.startColor = new ParticleSystem.MinMaxGradient(lightSoft, lightPink);
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.12f;
            shape.randomDirectionAmount = 1f;
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.18f;
            noise.frequency = 0.22f;
        }

        void ConfigureDust(ParticleSystem ps)
        {
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.75f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.06f);
            main.startColor = new ParticleSystem.MinMaxGradient(lightSoft, lightPink);
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.22f;
            shape.randomDirectionAmount = 1f;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        void ConfigureSparks(ParticleSystem ps)
        {
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 1.35f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.0f, 5.0f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.012f, 0.045f);
            main.startColor = new ParticleSystem.MinMaxGradient(lightGold, lightPink);
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.15f;
            shape.radiusThickness = 0.2f;
            shape.rotation = new Vector3(0f, 0f, 90f);
        }

        void CreateRings()
        {
            ring = CreateRing("WorldShift_Ring", 64);
            innerRing = CreateRing("WorldShift_InnerRing", 48);
            ring.enabled = false;
            innerRing.enabled = false;
        }

        LineRenderer CreateRing(string name, int segments)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.loop = true;
            lr.positionCount = segments;
            lr.widthMultiplier = 0.04f;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader != null)
            {
                var mat = new Material(shader);
                lr.sharedMaterial = mat;
            }
            return lr;
        }

        void UpdateRing(LineRenderer lr, float progress, float radius, float width)
        {
            if (lr == null) return;
            float eased = Mathf.SmoothStep(0f, 1f, progress);
            lr.transform.position = origin + Vector3.up * 0.06f;
            lr.widthMultiplier = width * Mathf.Sin(progress * Mathf.PI * 0.95f);
            float r = Mathf.Lerp(0.2f, radius, eased);
            for (int i = 0; i < lr.positionCount; i++)
            {
                float a = i / (float)lr.positionCount * Mathf.PI * 2f;
                lr.SetPosition(i, new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r));
            }
            bool dark = observedTarget == WorldKind.Dark;
            Color accent = dark ? darkAccent : lightPink;
            lr.startColor = new Color(accent.r, accent.g, accent.b, 0.72f * (1f - progress * 0.85f));
            lr.endColor = new Color(lightGold.r, lightGold.g, lightGold.b, 0.10f * (1f - progress));
        }

        void OnDestroy()
        {
            if (WorldState.Instance != null)
                WorldState.Instance.TransitionStarted -= OnTransitionStarted;
            if (particleMaterial != null) Destroy(particleMaterial);
        }
    }
}
