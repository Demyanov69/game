using System.Collections.Generic;
using UnityEngine;

namespace EchoIslands
{
    public enum WorldPresence
    {
        Both = 0,
        Light = 1,
        Dark = 2
    }

    [DisallowMultipleComponent]
    public class WorldVariant : MonoBehaviour
    {
        public WorldPresence presence = WorldPresence.Both;
        public bool includeChildren = true;
        public bool affectRenderers = true;
        public bool affectColliders = true;
        public bool affectLights = true;
        public bool affectParticles = true;
        public bool useTransitionWave = true;
        [Min(0.25f)] public float transitionWaveWidth = 2.5f;
        public bool useSoftFade = true;
        [Min(0.05f)] public float fadeDuration = 0.72f;
        [Range(0.05f, 0.95f)] public float colliderThreshold = 0.45f;
        [Tooltip("Small props grow in. Large meshes only fade.")]
        public float scaleAnimMaxExtent = 4.5f;

        Renderer[] renderers;
        Collider[] colliders;
        Terrain[] terrains;
        TerrainCollider[] terrainColliders;
        Light[] lights;
        ParticleSystem[] particles;
        bool initialized;
        bool subscribed;
        bool visible = true;
        bool fading;
        float currentFade = 1f;
        float targetFade = 1f;
        Vector3 baseScale;
        bool baseScaleCaptured;
        bool scaleAnim;
        readonly Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
        readonly Dictionary<Renderer, Material[]> fadeMaterials = new Dictionary<Renderer, Material[]>();
        readonly Dictionary<Light, float> originalLightIntensity = new Dictionary<Light, float>();

        void Awake()
        {
            Cache();
            CaptureBaseScale();
            initialized = true;
            bool shouldShow = IsPresent(WorldState.Instance != null ? WorldState.Instance.Current : WorldKind.Light);
            visible = shouldShow;
            currentFade = shouldShow ? 1f : 0f;
            ApplyImmediate(shouldShow);
        }

        void Start()
        {
            Subscribe();
            bool shouldShow = IsPresent(WorldState.Instance != null ? WorldState.Instance.Current : WorldKind.Light);
            visible = shouldShow;
            currentFade = shouldShow ? 1f : 0f;
            ApplyImmediate(shouldShow);
        }

        void OnEnable()
        {
            Subscribe();
            if (!initialized) return;
            bool shouldShow = IsPresent(WorldState.Instance != null ? WorldState.Instance.Current : WorldKind.Light);
            currentFade = shouldShow ? 1f : 0f;
            ApplyImmediate(shouldShow);
        }

        void OnDisable()
        {
            Unsubscribe();
            RestoreOriginalMaterials();
        }

        void Update()
        {
            if (!initialized || WorldState.Instance == null || presence == WorldPresence.Both)
                return;

            var state = WorldState.Instance;
            if (!state.IsTransitioning)
            {
                bool show = IsPresent(state.Current);
                if (fading || visible != show) FinalizeVisibility(show);
                return;
            }
            fading = true;
            bool shouldExistAtEnd = IsPresent(state.Target);
            float fade = useTransitionWave
                ? ComputeWaveFade(state, shouldExistAtEnd)
                : Mathf.SmoothStep(0f, 1f, state.TransitionProgress);

            if (!shouldExistAtEnd) fade = 1f - fade;
            targetFade = Mathf.Clamp01(fade);

            if (useSoftFade)
                ApplySoftFade(targetFade, shouldExistAtEnd);
            else
                ApplyBinaryByThreshold(targetFade, shouldExistAtEnd);
        }

        float ComputeWaveFade(WorldState state, bool targetExists)
        {
            float distance = Vector3.Distance(transform.position, state.TransitionOrigin);
            return state.GetTransitionWave(distance, transitionWaveWidth);
        }

        void Subscribe()
        {
            if (subscribed || WorldState.Instance == null) return;
            WorldState.Instance.Changed += HandleWorldChanged;
            subscribed = true;
        }

        void Unsubscribe()
        {
            if (!subscribed || WorldState.Instance == null) return;
            WorldState.Instance.Changed -= HandleWorldChanged;
            subscribed = false;
        }

        void HandleWorldChanged(WorldKind world)
        {
            if (WorldState.Instance == null || WorldState.Instance.IsTransitioning) return;
            FinalizeVisibility(IsPresent(world));
        }

        void Cache()
        {
            if (!includeChildren)
            {
                renderers = GetComponents<Renderer>();
                colliders = GetComponents<Collider>();
                terrains = GetComponents<Terrain>();
                terrainColliders = GetComponents<TerrainCollider>();
                lights = GetComponents<Light>();
                particles = GetComponents<ParticleSystem>();
            }
            else
            {
                renderers = GetComponentsInChildren<Renderer>(true);
                colliders = GetComponentsInChildren<Collider>(true);
                terrains = GetComponentsInChildren<Terrain>(true);
                terrainColliders = GetComponentsInChildren<TerrainCollider>(true);
                lights = GetComponentsInChildren<Light>(true);
                particles = GetComponentsInChildren<ParticleSystem>(true);
            }

            if (lights != null)
            {
                originalLightIntensity.Clear();
                for (int i = 0; i < lights.Length; i++)
                    if (lights[i] != null) originalLightIntensity[lights[i]] = lights[i].intensity;
            }

            scaleAnim = EstimateMaxExtent() <= scaleAnimMaxExtent;
        }

        void CaptureBaseScale()
        {
            if (baseScaleCaptured) return;
            baseScale = transform.localScale;
            if (baseScale.sqrMagnitude < 0.000001f) baseScale = Vector3.one;
            baseScaleCaptured = true;
        }

        float EstimateMaxExtent()
        {
            if (renderers == null || renderers.Length == 0) return 1f;
            Bounds b = new Bounds(transform.position, Vector3.zero);
            bool any = false;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                if (!any) { b = renderers[i].bounds; any = true; }
                else b.Encapsulate(renderers[i].bounds);
            }
            if (!any) return 1f;
            return Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
        }

        public void Refresh()
        {
            Cache();
            bool shouldShow = IsPresent(WorldState.Instance != null ? WorldState.Instance.Current : WorldKind.Light);
            currentFade = shouldShow ? 1f : 0f;
            FinalizeVisibility(shouldShow);
        }

        bool IsPresent(WorldKind world)
        {
            return presence == WorldPresence.Both ||
                   (presence == WorldPresence.Light && world == WorldKind.Light) ||
                   (presence == WorldPresence.Dark && world == WorldKind.Dark);
        }

        void ApplyScale(float fade)
        {
            if (!scaleAnim) return;
            CaptureBaseScale();
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(fade));
            float s = Mathf.Lerp(0.04f, 1f, t);
            transform.localScale = baseScale * s;
        }

        void ApplyImmediate(bool show)
        {
            if (renderers != null && affectRenderers)
            {
                for (int i = 0; i < renderers.Length; i++)
                    if (renderers[i] != null) renderers[i].enabled = show;
            }
            if (colliders != null && affectColliders)
            {
                for (int i = 0; i < colliders.Length; i++)
                    if (colliders[i] != null) colliders[i].enabled = show;
            }
            if (terrainColliders != null && affectColliders)
            {
                for (int i = 0; i < terrainColliders.Length; i++)
                    if (terrainColliders[i] != null) terrainColliders[i].enabled = show;
            }
            if (terrains != null && affectRenderers)
            {
                for (int i = 0; i < terrains.Length; i++)
                    if (terrains[i] != null) terrains[i].drawHeightmap = show;
            }
            if (lights != null && affectLights)
            {
                for (int i = 0; i < lights.Length; i++)
                    if (lights[i] != null) lights[i].enabled = show;
            }
            if (particles != null && affectParticles)
            {
                for (int i = 0; i < particles.Length; i++)
                {
                    if (particles[i] == null) continue;
                    if (show)
                    {
                        if (!particles[i].isPlaying) particles[i].Play();
                    }
                    else particles[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }
            ApplyScale(show ? 1f : 0f);
        }

        void ApplySoftFade(float alpha, bool targetExists)
        {
            alpha = Mathf.Clamp01(alpha);
            float speed = Mathf.Max(0.05f, fadeDuration);
            currentFade = Mathf.MoveTowards(currentFade, alpha, Time.deltaTime / speed);
            ApplyScale(currentFade);

            if (currentFade > 0.001f && affectRenderers)
            {
                EnsureFadeMaterials(currentFade <= 0.001f ? 0f : currentFade);
                if (renderers != null)
                {
                    for (int i = 0; i < renderers.Length; i++)
                    {
                        var r = renderers[i];
                        if (r == null) continue;
                        r.enabled = true;
                        SetRendererAlpha(r, currentFade);
                    }
                }
            }

            if (affectColliders)
            {
                bool colliderOn = currentFade >= colliderThreshold;
                SetColliders(colliderOn);
            }

            if (affectLights && lights != null)
            {
                for (int i = 0; i < lights.Length; i++)
                {
                    var l = lights[i];
                    if (l == null) continue;
                    if (!originalLightIntensity.ContainsKey(l)) originalLightIntensity[l] = l.intensity;
                    l.enabled = currentFade > 0.02f;
                    l.intensity = originalLightIntensity[l] * currentFade;
                }
            }

            if (affectParticles && particles != null && currentFade > 0.05f)
            {
                for (int i = 0; i < particles.Length; i++)
                    if (particles[i] != null && !particles[i].isPlaying) particles[i].Play();
            }

            if (!targetExists && currentFade <= 0.001f)
            {
                FinalizeVisibility(false);
            }
            else if (targetExists && currentFade >= 0.999f)
            {
                FinalizeVisibility(true);
            }
        }

        void ApplyBinaryByThreshold(float alpha, bool targetExists)
        {
            bool show = targetExists ? alpha > 0.02f : alpha >= 0.98f;
            if (show != visible) ApplyImmediate(show);
            visible = show;
        }

        void EnsureFadeMaterials(float alpha)
        {
            if (renderers == null) return;
            for (int i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                if (r == null || fadeMaterials.ContainsKey(r)) continue;
                var originals = r.sharedMaterials;
                if (originals == null || originals.Length == 0) continue;
                var clones = new Material[originals.Length];
                bool anyFadeable = false;
                for (int m = 0; m < originals.Length; m++)
                {
                    var source = originals[m];
                    if (source == null) { clones[m] = null; continue; }
                    var clone = new Material(source);
                    if (ConfigureTransparent(clone)) anyFadeable = true;
                    clones[m] = clone;
                }
                if (anyFadeable)
                {
                    originalMaterials[r] = originals;
                    fadeMaterials[r] = clones;
                    r.sharedMaterials = clones;
                    SetRendererAlpha(r, alpha);
                }
                else
                {
                    for (int m = 0; m < clones.Length; m++) if (clones[m] != null) Destroy(clones[m]);
                }
            }
        }

        bool ConfigureTransparent(Material mat)
        {
            // Every asset material (including atlas shaders) is converted only DURING
            // the transition, then restored. The dither mask is opaque and depth-written:
            // it does not cause the typical transparent bridge sorting artifacts.
            Shader shader = Shader.Find("Echo/PhaseDissolve");
            if (shader == null) return false;
            Texture texture = null;
            Vector2 tiling = Vector2.one;
            Vector2 offset = Vector2.zero;
            Color tint = Color.white;
            if (mat.HasProperty("_MainTex"))
            {
                texture = mat.GetTexture("_MainTex");
                tiling = mat.GetTextureScale("_MainTex");
                offset = mat.GetTextureOffset("_MainTex");
            }
            else if (mat.HasProperty("_BaseMap"))
            {
                texture = mat.GetTexture("_BaseMap");
                tiling = mat.GetTextureScale("_BaseMap");
                offset = mat.GetTextureOffset("_BaseMap");
            }
            if (mat.HasProperty("_Color")) tint = mat.GetColor("_Color");
            else if (mat.HasProperty("_BaseColor")) tint = mat.GetColor("_BaseColor");
            bool isWorldTiled = mat.shader != null && mat.shader.name == "Echo/DungeonTriplanar";
            float tileScale = isWorldTiled && mat.HasProperty("_TileScale") ? mat.GetFloat("_TileScale") : 1f;
            mat.shader = shader;
            mat.SetFloat("_UseWorldProjection", isWorldTiled ? 1f : 0f);
            mat.SetFloat("_TileScale", tileScale);
            if (texture != null)
            {
                mat.SetTexture("_MainTex", texture);
                mat.SetTextureScale("_MainTex", tiling);
                mat.SetTextureOffset("_MainTex", offset);
            }
            mat.SetColor("_Color", tint);
            mat.SetColor("_EdgeColor", presence == WorldPresence.Dark
                ? new Color(0.08f, 2.5f, 2.1f, 1f)
                : new Color(2.1f, 0.55f, 2.0f, 1f));
            mat.SetFloat("_PhaseVisibility", 1f);
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
            return true;
        }

        void SetRendererAlpha(Renderer r, float alpha)
        {
            if (!fadeMaterials.TryGetValue(r, out var materials) || materials == null) return;
            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] != null && materials[i].HasProperty("_PhaseVisibility"))
                    materials[i].SetFloat("_PhaseVisibility", alpha);
            }
        }

        void SetColliders(bool on)
        {
            if (colliders != null)
                for (int i = 0; i < colliders.Length; i++)
                    if (colliders[i] != null) colliders[i].enabled = on;
            if (terrainColliders != null)
                for (int i = 0; i < terrainColliders.Length; i++)
                    if (terrainColliders[i] != null) terrainColliders[i].enabled = on;
        }

        void FinalizeVisibility(bool show)
        {
            fading = false;
            currentFade = show ? 1f : 0f;
            visible = show;
            RestoreOriginalMaterials();
            ApplyImmediate(show);
            if (show) ApplyScale(1f);
        }

        void RestoreOriginalMaterials()
        {
            foreach (var pair in originalMaterials)
            {
                var r = pair.Key;
                if (r != null) r.sharedMaterials = pair.Value;
            }
            foreach (var pair in fadeMaterials)
            {
                var mats = pair.Value;
                if (mats == null) continue;
                for (int i = 0; i < mats.Length; i++)
                    if (mats[i] != null) Destroy(mats[i]);
            }
            originalMaterials.Clear();
            fadeMaterials.Clear();
        }
    }
}
