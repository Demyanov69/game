using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EchoIslands
{
    /// <summary>
    /// Gives the ORIGINAL opaque scene/prefab meshes a second, grimy night-world
    /// texture without rewriting their imported materials or their UV atlases.
    /// Generated dungeon geometry already uses Echo/DungeonTriplanar and is left
    /// intact. Each dungeon rescans itself when enabled after being built.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WorldSurfaceSkins : MonoBehaviour
    {
        struct OriginalBinding
        {
            public MeshRenderer renderer;
            public Material[] originals;
        }

        readonly List<OriginalBinding> bindings = new List<OriginalBinding>();
        readonly Dictionary<Material, Material> cloned = new Dictionary<Material, Material>();
        Texture2D stone, forest, ground;
        Shader skinShader;
        Coroutine scanRoutine;

        void OnEnable()
        {
            scanRoutine = StartCoroutine(ScanAfterSceneSetup());
        }

        IEnumerator ScanAfterSceneSetup()
        {
            // Level-1 prefabs spawn at runtime; scene-authored renderers have
            // completed Awake/Start by the time this first pass runs.
            yield return null;
            var keep = Resources.Load<Material>("EchoShaderKeep/WorldSurfaceSkin");
            skinShader = keep != null ? keep.shader : Shader.Find("Echo/WorldSurfaceSkin");
            stone = Resources.Load<Texture2D>("EchoDungeon/Textures/StoneBlocks_Dark");
            forest = Resources.Load<Texture2D>("EchoWorldGround/DarkGrass");
            ground = Resources.Load<Texture2D>("EchoWorldGround/DarkGround");
            if (skinShader == null || stone == null) yield break;

            if (GetComponent<WorldState>() != null)
            {
                var renderers = FindObjectsByType<MeshRenderer>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var r in renderers)
                    if (r != null && r.gameObject.activeInHierarchy &&
                        !IsGeneratedDungeon(r.transform)) Bind(r);
            }
            else
            {
                // Only this particular generated level, never stale scene roots.
                foreach (var r in GetComponentsInChildren<MeshRenderer>(true)) Bind(r);
            }
            scanRoutine = null;
        }

        static bool IsGeneratedDungeon(Transform tr)
        {
            for (var t = tr; t != null; t = t.parent)
                if (t.name.Contains("_COMPLETE_DUNGEON")) return true;
            return false;
        }

        static bool IsSpecialObject(Transform tr)
        {
            if (tr.GetComponentInParent<Orb>() != null ||
                tr.GetComponentInParent<PortalFrame>() != null ||
                tr.GetComponentInParent<FirstPersonController>() != null ||
                tr.GetComponentInParent<ToolPickup>() != null) return true;
            string n = tr.name.ToLowerInvariant();
            return n.Contains("portal") || n.Contains("orbvfx") ||
                n.Contains("particle") || n.Contains("aurora") ||
                n.Contains("rift") || n.Contains("glow") ||
                n.Contains("held") || n.Contains("aether");
        }

        void Bind(MeshRenderer renderer)
        {
            if (renderer == null || IsSpecialObject(renderer.transform)) return;
            var original = renderer.sharedMaterials;
            if (original == null || original.Length == 0) return;
            Material[] replacements = null;
            bool changed = false;
            for (int i = 0; i < original.Length; i++)
            {
                var source = original[i];
                if (!CanConvert(source)) continue;
                if (replacements == null) replacements = (Material[])original.Clone();
                if (!cloned.TryGetValue(source, out var replacement))
                {
                    replacement = MakeNightVariant(source, renderer.gameObject.name);
                    cloned[source] = replacement;
                }
                replacements[i] = replacement;
                changed = true;
            }
            if (!changed) return;
            bindings.Add(new OriginalBinding { renderer = renderer, originals = original });
            renderer.sharedMaterials = replacements;
        }

        bool CanConvert(Material source)
        {
            if (source == null || source.shader == null) return false;
            string shaderName = source.shader.name;
            if (shaderName.StartsWith("Echo/") ||
                shaderName.Contains("Particle") || shaderName.Contains("Sprite") ||
                shaderName.Contains("Unlit") || shaderName.Contains("Transparent")) return false;
            if (source.renderQueue >= (int)UnityEngine.Rendering.RenderQueue.Transparent) return false;
            if (shaderName.Contains("Nature/") || shaderName.Contains("Tree") ||
                source.GetTag("RenderType", false, "") == "TransparentCutout") return false;
            if (source.HasProperty("_Color") && source.GetColor("_Color").a < .96f) return false;
            if (source.HasProperty("_BaseColor") && source.GetColor("_BaseColor").a < .96f) return false;
            if (source.HasProperty("_Mode") && source.GetFloat("_Mode") > 0.1f) return false;
            if (source.HasProperty("_Surface") && source.GetFloat("_Surface") > 0.1f) return false;
            if (source.HasProperty("_Cutoff") && source.GetFloat("_Cutoff") > 0.1f &&
                shaderName.Contains("Cutout")) return false;
            if (source.HasProperty("_EmissionColor") &&
                source.GetColor("_EmissionColor").maxColorComponent > .12f) return false;
            return true;
        }

        Material MakeNightVariant(Material source, string objectName)
        {
            var replacement = new Material(skinShader)
            {
                name = source.name + " (two-world runtime skin)",
                hideFlags = HideFlags.DontSave
            };
            var diffuse = source.HasProperty("_MainTex") ? source.GetTexture("_MainTex") :
                source.HasProperty("_BaseMap") ? source.GetTexture("_BaseMap") : null;
            if (diffuse != null) replacement.SetTexture("_MainTex", diffuse);
            if (source.HasProperty("_MainTex"))
            {
                replacement.SetTextureScale("_MainTex", source.GetTextureScale("_MainTex"));
                replacement.SetTextureOffset("_MainTex", source.GetTextureOffset("_MainTex"));
            }
            else if (source.HasProperty("_BaseMap"))
            {
                replacement.SetTextureScale("_MainTex", source.GetTextureScale("_BaseMap"));
                replacement.SetTextureOffset("_MainTex", source.GetTextureOffset("_BaseMap"));
            }
            var tint = source.HasProperty("_Color") ? source.GetColor("_Color") :
                source.HasProperty("_BaseColor") ? source.GetColor("_BaseColor") : Color.white;
            replacement.SetColor("_Color", tint);
            replacement.SetFloat("_Glossiness", source.HasProperty("_Glossiness") ?
                Mathf.Min(.28f, source.GetFloat("_Glossiness")) : .15f);

            string n = (source.name + " " + objectName).ToLowerInvariant();
            bool foliage = n.Contains("grass") || n.Contains("leaf") ||
                n.Contains("plant") || n.Contains("fern") || n.Contains("bush") ||
                n.Contains("tree") || n.Contains("moss");
            bool earthy = n.Contains("dirt") || n.Contains("ground") ||
                n.Contains("earth") || n.Contains("sand") || n.Contains("soil");
            replacement.SetTexture("_DarkTex", foliage && forest != null ? forest :
                earthy && ground != null ? ground : stone);
            replacement.SetColor("_DarkTint", foliage ? new Color(.49f,.68f,.70f) :
                earthy ? new Color(.57f,.65f,.72f) : new Color(.65f,.72f,.78f));
            replacement.SetFloat("_DarkMix", foliage ? .66f : .82f);
            replacement.SetFloat("_DarkTileScale", foliage ? .85f : 1.15f);
            return replacement;
        }

        void OnDisable()
        {
            if (scanRoutine != null) { StopCoroutine(scanRoutine); scanRoutine = null; }
            foreach (var b in bindings)
                if (b.renderer != null) b.renderer.sharedMaterials = b.originals;
            bindings.Clear();
            foreach (var m in cloned.Values)
                if (m != null) Destroy(m);
            cloned.Clear();
        }
    }
}
