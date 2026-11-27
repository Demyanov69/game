using UnityEngine;

namespace EchoIslands
{
    public class WorldTransitionVFX : MonoBehaviour
    {
        WorldState state;
        ParticleSystem particles;
        Material material;

        void Start()
        {
            state = WorldState.Instance;
            if (state == null) return;

            var root = new GameObject("World shift sparks");
            root.transform.SetParent(transform, false);
            particles = root.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = .35f;
            main.maxParticles = 24;
            main.startSize = new ParticleSystem.MinMaxCurve(.025f,.065f);
            main.startLifetime = new ParticleSystem.MinMaxCurve(.28f,.61f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(.8f,2.5f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)18) });
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = .24f;
            var shader = Shader.Find("Legacy Shaders/Particles/Additive") ?? Shader.Find("Sprites/Default");
            if (shader != null)
            {
                material = new Material(shader);
                root.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
            }
            state.TransitionStarted += Play;
        }

        void Play(WorldKind world)
        {
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particles.transform.position = state.TransitionOrigin;
            var main = particles.main;
            main.startColor = world == WorldKind.Dark ? new Color(.20f,.92f,.78f) : new Color(1f,.74f,.41f);
            particles.Play();
        }

        void OnDestroy()
        {
            if (state != null) state.TransitionStarted -= Play;
            if (material != null) Destroy(material);
        }
    }
}
