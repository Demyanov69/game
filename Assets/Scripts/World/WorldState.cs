using System;
using UnityEngine;

namespace EchoIslands
{
    public enum WorldKind
    {
        Light = 0,
        Dark = 1
    }

    [DefaultExecutionOrder(-100)]
    public class WorldState : MonoBehaviour
    {
        public static WorldState Instance { get; private set; }

        [Range(0f, 1f)] public float blend;
        [Tooltip("Legacy field kept for compatibility with older scenes.")]
        public float blendSpeed = 1.6f;
        [Min(0.2f)] public float transitionDuration = 0.90f;
        [Min(5f)] public float transitionRadius = 70f;

        [Header("Temporary debug control")]
        public bool debugToggleEnabled = true;
        public KeyCode debugToggleKey = KeyCode.T;

        public WorldKind Current => blend >= 0.5f ? WorldKind.Dark : WorldKind.Light;
        public WorldKind Target { get; private set; } = WorldKind.Light;
        public bool IsTransitioning { get; private set; }
        public float TransitionProgress { get; private set; } = 1f;
        public Vector3 TransitionOrigin { get; private set; }
        public float TransitionStartTime { get; private set; }

        float transitionStartBlend;
        float transitionTargetBlend;
        float transitionElapsed;

        readonly Vector4[] masks = new Vector4[4];
        readonly float[] targets = new float[4];

        public event Action<WorldKind> TransitionStarted;
        public event Action<WorldKind> Changed;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            transitionStartBlend = blend;
            transitionTargetBlend = blend;
            Target = blend >= 0.5f ? WorldKind.Dark : WorldKind.Light;
            EnsureVisualSystems();
            PushMasks();
            PushShader();
        }

        void Update()
        {
            if (debugToggleEnabled && Input.GetKeyDown(debugToggleKey))
                Toggle();

            if (!IsTransitioning || Time.deltaTime <= 0f) return;

            transitionElapsed += Time.deltaTime;
            float duration = Mathf.Max(0.2f, transitionDuration);
            TransitionProgress = Mathf.Clamp01(transitionElapsed / duration);
            float eased = Smooth01(TransitionProgress);
            float before = blend;
            blend = Mathf.Lerp(transitionStartBlend, transitionTargetBlend, eased);

            PushShader();

            if (TransitionProgress >= 1f - 0.0001f)
            {
                blend = transitionTargetBlend;
                TransitionProgress = 1f;
                IsTransitioning = false;
                PushShader();
                if ((before < 0.5f) != (blend < 0.5f))
                    Changed?.Invoke(Current);
            }
            else if ((before < 0.5f) != (blend < 0.5f))
            {
                Changed?.Invoke(Current);
            }
        }

        public void SetWorld(WorldKind kind)
        {
            float target = kind == WorldKind.Dark ? 1f : 0f;
            if (!IsTransitioning && Mathf.Abs(blend - target) < 0.0001f)
            {
                Target = kind;
                return;
            }

            transitionStartBlend = blend;
            transitionTargetBlend = target;
            transitionElapsed = 0f;
            TransitionProgress = 0f;
            Target = kind;
            IsTransitioning = true;
            TransitionOrigin = ResolveTransitionOrigin();
            TransitionStartTime = Time.time;
            TransitionStarted?.Invoke(kind);
            PushShader();
        }

        public void SnapTo(WorldKind world)
        {
            if (Target == world && !IsTransitioning) return;
            Target = world;
            blend = world == WorldKind.Dark ? 1f : 0f;
            TransitionProgress = 1f;
            IsTransitioning = false;
            PushShader();
            Changed?.Invoke(world);
        }

        public void Toggle() => SetWorld(Target == WorldKind.Light ? WorldKind.Dark : WorldKind.Light);

        Vector3 ResolveTransitionOrigin()
        {
            var flow = LevelFlow.Instance;
            var player = flow != null ? flow.player : null;
            if (player != null) return player.position + player.up * .9f;
            var cam = Camera.main;
            if (cam != null) return cam.transform.position;
            return transform.position;
        }

        static float Smooth01(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }

        void EnsureVisualSystems()
        {
            if (GetComponent<WorldAtmosphere>() == null)
                gameObject.AddComponent<WorldAtmosphere>();
            if (GetComponent<WorldSurfaceSkins>() == null)
                gameObject.AddComponent<WorldSurfaceSkins>();
            if (GetComponent<WorldTerrainTextureSwap>() == null)
                gameObject.AddComponent<WorldTerrainTextureSwap>();
            if (GetComponent<WorldTransitionVFX>() == null)
                gameObject.AddComponent<WorldTransitionVFX>();
            if (GetComponent<WorldAmbientVFX>() == null)
                gameObject.AddComponent<WorldAmbientVFX>();
        }

        public int AllocMask()
        {
            for (int i = 0; i < 4; i++)
                if (masks[i].w <= 0.01f) return i;
            return -1;
        }

        public void SetMask(int slot, Vector3 center, float radius, float targetBlend)
        {
            if (slot < 0 || slot > 3) return;
            masks[slot] = new Vector4(center.x, center.y, center.z, Mathf.Max(0f, radius));
            targets[slot] = Mathf.Clamp01(targetBlend);
            PushMasks();
        }

        public void ClearMask(int slot)
        {
            if (slot < 0 || slot > 3) return;
            masks[slot] = Vector4.zero;
            targets[slot] = 0f;
            PushMasks();
        }

        public float BlendAt(Vector3 worldPos)
        {
            float b = blend;
            for (int i = 0; i < 4; i++)
            {
                float radius = masks[i].w;
                if (radius <= 0.01f) continue;
                float d = Vector3.Distance(worldPos, new Vector3(masks[i].x, masks[i].y, masks[i].z));
                float influence = Mathf.Clamp01(1f - d / radius);
                influence *= influence;
                b = Mathf.Lerp(b, targets[i], influence);
            }
            return Mathf.Clamp01(b);
        }

        public bool IsReal(WorldKind home, Vector3 worldPos)
        {
            float b = BlendAt(worldPos);
            return home == WorldKind.Dark ? b >= 0.55f : b <= 0.45f;
        }

        public float GetTransitionWave(float worldDistance, float width)
        {
            if (!IsTransitioning) return Target == WorldKind.Dark ? 1f : 0f;
            float traveled = TransitionProgress * transitionRadius;
            float front = worldDistance - traveled;
            float normalized = Mathf.InverseLerp(width, -width, front);
            return Mathf.SmoothStep(0f, 1f, normalized);
        }

        void PushShader()
        {
            Shader.SetGlobalFloat("_EchoWorldBlend", blend);
            Shader.SetGlobalVector("_EchoWorld", new Vector4(blend, 0, 0, 0));
            Shader.SetGlobalFloat("_EchoTransitionProgress", TransitionProgress);
            Shader.SetGlobalVector("_EchoTransitionOrigin", new Vector4(TransitionOrigin.x, TransitionOrigin.y, TransitionOrigin.z, 1f));
            Shader.SetGlobalFloat("_EchoTransitionRadius", transitionRadius);
            Shader.SetGlobalFloat("_EchoTransitionDirection", Target == WorldKind.Dark ? 1f : -1f);
            Shader.SetGlobalFloat("_EchoTransitionPulse", IsTransitioning ? Mathf.Sin(TransitionProgress * Mathf.PI) : 0f);
        }

        void PushMasks()
        {
            Shader.SetGlobalVector("_EchoMask0", masks[0]);
            Shader.SetGlobalVector("_EchoMask1", masks[1]);
            Shader.SetGlobalVector("_EchoMask2", masks[2]);
            Shader.SetGlobalVector("_EchoMask3", masks[3]);
            Shader.SetGlobalVector("_EchoMaskTarget", new Vector4(targets[0], targets[1], targets[2], targets[3]));
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
