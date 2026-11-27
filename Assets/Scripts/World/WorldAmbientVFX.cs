using UnityEngine;

namespace EchoIslands
{
    public class WorldAmbientVFX : MonoBehaviour
    {
        ParticleSystem dust;
        Material material;
        WorldState state;

        void Start()
        {
            var camera = Camera.main;
            state = WorldState.Instance;
            if (camera == null || state == null) return;

            var root = new GameObject("Ambient dust");
            root.transform.SetParent(camera.transform, false);
            dust = root.AddComponent<ParticleSystem>();
            dust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = dust.main;
            main.loop = true;
            main.playOnAwake = false;
            main.maxParticles = 24;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(.015f,.06f);
            main.startSize = new ParticleSystem.MinMaxCurve(.015f,.040f);
            var shape = dust.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 6f;
            var shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended") ?? Shader.Find("Sprites/Default");
            if (shader != null)
            {
                material = new Material(shader);
                root.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
            }
            state.Changed += Apply;
            Apply(state.Current);
        }

        void Apply(WorldKind world)
        {
            if (dust == null) return;
            var main = dust.main;
            main.startColor = world == WorldKind.Dark ? new Color(.21f,.71f,.65f,.55f) : new Color(1f,.83f,.61f,.65f);
            var emission = dust.emission;
            emission.rateOverTime = world == WorldKind.Dark ? 4f : 3f;
            if (!dust.isPlaying) dust.Play();
        }

        void OnDestroy()
        {
            if (state != null) state.Changed -= Apply;
            if (dust != null) Destroy(dust.gameObject);
            if (material != null) Destroy(material);
        }
    }
}
