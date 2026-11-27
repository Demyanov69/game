using UnityEngine;

namespace EchoIslands
{
    [DefaultExecutionOrder(-20)]
    public class WorldAmbientVFX : MonoBehaviour
    {
        public Transform player;
        public Camera targetCamera;
        public float radius = 10f;

        ParticleSystem lightStars;
        ParticleSystem darkMotes;
        ParticleSystem darkSparks;
        Material lightMaterial;
        Material darkMaterial;
        Material darkSparkMaterial;

        void Start()
        {
            ResolveReferences();
            CreateSystems();
            Apply(0f);
        }

        void Update()
        {
            if (WorldState.Instance == null) return;
            ResolveReferences();
            FollowCamera();
            Apply(WorldState.Instance.blend);
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

        void FollowCamera()
        {
            if (targetCamera != null)
            {
                if (lightStars != null) lightStars.transform.SetPositionAndRotation(targetCamera.transform.position, targetCamera.transform.rotation);
                if (darkMotes != null) darkMotes.transform.SetPositionAndRotation(targetCamera.transform.position, targetCamera.transform.rotation);
                if (darkSparks != null) darkSparks.transform.SetPositionAndRotation(targetCamera.transform.position, targetCamera.transform.rotation);
            }
        }

        void CreateSystems()
        {
            lightStars = CreateParticleSystem("Light_Stardust", false, out lightMaterial, 180);
            darkMotes = CreateParticleSystem("Dark_Spores", true, out darkMaterial, 80);

            ConfigureStars(lightStars);
            ConfigureSpores(darkMotes);
        }

        ParticleSystem CreateParticleSystem(string name, bool additive, out Material material, int maxParticles)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.maxParticles = maxParticles;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.stopAction = ParticleSystemStopAction.None;
            var emission = ps.emission;
            emission.enabled = true;
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = radius;
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            string shaderName = additive ? "Legacy Shaders/Particles/Additive" : "Legacy Shaders/Particles/Alpha Blended";
            var shader = Shader.Find(shaderName);
            if (shader == null) shader = Shader.Find("Sprites/Default");
            material = shader != null ? new Material(shader) : null;
            if (material != null) renderer.sharedMaterial = material;
            ps.Play();
            return ps;
        }

        void ConfigureStars(ParticleSystem ps)
        {
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 14f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.01f, 0.10f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.018f, 0.055f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.97f, 0.97f, 0.80f),
                new Color(1f, 0.90f, 0.76f, 0.95f));

            var emission = ps.emission;
            emission.rateOverTime = 18f;
            var shape = ps.shape;
            shape.radius = radius;
            shape.randomDirectionAmount = 0.25f;

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.12f;
            noise.frequency = 0.15f;
            noise.scrollSpeed = 0.08f;
            noise.quality = ParticleSystemNoiseQuality.Low;
        }

        void ConfigureSpores(ParticleSystem ps)
        {
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(4.5f, 8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.04f, 0.18f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.11f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.01f, 0.12f, 0.11f, 0.15f),
                new Color(0.05f, 0.38f, 0.34f, 0.45f));
            var emission = ps.emission;
            emission.rateOverTime = 7f;
            var shape = ps.shape;
            shape.radius = radius * 0.8f;
            shape.randomDirectionAmount = 0.4f;
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.18f;
            noise.frequency = 0.12f;
            noise.scrollSpeed = 0.05f;
        }

        void Apply(float blend)
        {
            blend = Mathf.Clamp01(blend);
            if (lightStars != null)
            {
                var emission = lightStars.emission;
                emission.rateOverTime = Mathf.Lerp(22f, 0f, blend);
                lightStars.gameObject.SetActive(blend < 0.99f);
            }
            if (darkMotes != null)
            {
                var emission = darkMotes.emission;
                emission.rateOverTime = Mathf.Lerp(0f, 8f, blend);
                darkMotes.gameObject.SetActive(blend > 0.02f);
            }
        }

        void OnDestroy()
        {
            if (lightMaterial != null) Destroy(lightMaterial);
            if (darkMaterial != null) Destroy(darkMaterial);
            if (darkSparkMaterial != null) Destroy(darkSparkMaterial);
        }
    }
}
