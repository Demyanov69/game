using System.Collections.Generic;
using UnityEngine;

namespace EchoIslands
{
    public static class StaffSurface
    {
        static readonly Dictionary<int, Texture2D> cache = new Dictionary<int, Texture2D>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            var hook = new GameObject("StaffSurfaceHook");
            hook.hideFlags = HideFlags.HideAndDontSave;
            hook.AddComponent<StaffSurfaceHook>();
        }

        sealed class StaffSurfaceHook : MonoBehaviour
        {
            float elapsed;

            void Update()
            {
                elapsed += Time.deltaTime;
                var renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
                for (int i = 0; i < renderers.Length; i++)
                {
                    var r = renderers[i];
                    if (r == null) continue;
                    string n = r.gameObject.name;
                    string root = r.transform.root.name;
                    if (n.IndexOf("staff", System.StringComparison.OrdinalIgnoreCase) < 0 &&
                        root.IndexOf("staff", System.StringComparison.OrdinalIgnoreCase) < 0)
                        continue;
                    Apply(r.gameObject);
                }
                if (elapsed > 3f) Destroy(gameObject);
            }
        }

        public static void Apply(GameObject root)
        {
            if (root == null) return;
            var shader = Shader.Find("Echo/StaffLit");
            if (shader == null) shader = Shader.Find("Unlit/Texture");
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            bool glowAdded = root.GetComponentInChildren<Light>() != null;
            for (int i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null) continue;
                var sources = renderer.sharedMaterials;
                if (sources == null || sources.Length == 0) continue;
                var next = new Material[sources.Length];
                for (int m = 0; m < sources.Length; m++)
                {
                    int kind = Classify(renderer.gameObject.name, sources[m], m);
                    Color tint = kind == 2
                        ? new Color(0.45f, 0.22f, 0.72f)
                        : kind == 1
                            ? new Color(0.72f, 0.58f, 0.28f)
                            : kind == 3
                                ? new Color(0.55f, 0.72f, 0.68f)
                                : new Color(0.34f, 0.20f, 0.11f);
                    var tex = GetTexture(kind, tint);
                    var mat = new Material(shader != null ? shader : sources[m].shader);
                    mat.name = "StaffSurface_" + kind;
                    if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
                    if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
                    if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
                    if (mat.HasProperty("_Emission"))
                    {
                        Color emission = kind == 2 ? new Color(0.12f, 0.62f, 0.55f, 1f) : Color.black;
                        mat.SetColor("_Emission", emission);
                        mat.SetFloat("_EmissionStrength", kind == 2 ? 0.55f : 0f);
                    }
                    next[m] = mat;
                }
                renderer.materials = next;
            }

            if (!glowAdded)
            {
                var lightGo = new GameObject("StaffGlow");
                lightGo.transform.SetParent(root.transform, false);
                lightGo.transform.localPosition = new Vector3(0f, 0.85f, 0f);
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(0.20f, 0.75f, 0.66f);
                light.intensity = 0.45f;
                light.range = 3.2f;
                light.shadows = LightShadows.None;
            }
        }

        static int Classify(string objectName, Material source, int submesh)
        {
            string n = ((objectName ?? "") + " " + (source != null ? source.name : "")).ToLower();
            if (n.Contains("crystal") || n.Contains("gem")) return 2;
            if (n.Contains("ribbon") || n.Contains("cloth") || n.Contains("fabric")) return 3;
            if (n.Contains("keychain") || n.Contains("metal") || n.Contains("icon")) return 1;
            if (n.Contains("top") && submesh > 0) return 2;
            if (n.Contains("top")) return 1;
            return 0;
        }

        static Texture2D GetTexture(int kind, Color tint)
        {
            int key = kind * 1000
                + Mathf.RoundToInt(tint.r * 10f) * 100
                + Mathf.RoundToInt(tint.g * 10f) * 10
                + Mathf.RoundToInt(tint.b * 10f);
            Texture2D tex;
            if (cache.TryGetValue(key, out tex) && tex != null) return tex;
            if (kind == 2) tex = Crystal(tint);
            else if (kind == 1) tex = Metal(tint);
            else if (kind == 3) tex = Cloth(tint);
            else tex = Wood(tint);
            tex.name = "StaffTex_" + key;
            cache[key] = tex;
            return tex;
        }

        static Texture2D Wood(Color tint)
        {
            const int n = 256;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, true);
            var pixels = new Color[n * n];
            Color dark = tint * 0.45f;
            Color mid = tint;
            Color light = Color.Lerp(tint, new Color(0.85f, 0.75f, 0.62f), 0.35f) * 1.15f;
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float u = x / (float)n;
                    float v = y / (float)n;
                    float grain = Mathf.Sin((u * 18f + Mathf.PerlinNoise(v * 6f, 0.2f) * 2.4f) * Mathf.PI * 2f);
                    float ring = Mathf.PerlinNoise(u * 10f, v * 2.2f);
                    float knot = Mathf.PerlinNoise(u * 3f + 4f, v * 3f);
                    float g = Mathf.InverseLerp(-1f, 1f, grain);
                    Color c = Color.Lerp(dark, mid, g);
                    c = Color.Lerp(c, light, ring * 0.35f);
                    if (knot > 0.82f) c = Color.Lerp(c, dark * 0.7f, (knot - 0.82f) * 4f);
                    float noise = (Mathf.PerlinNoise(u * 40f, v * 40f) - 0.5f) * 0.05f;
                    c.r += noise;
                    c.g += noise;
                    c.b += noise;
                    pixels[y * n + x] = c;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply(true);
            tex.wrapMode = TextureWrapMode.Repeat;
            return tex;
        }

        static Texture2D Metal(Color tint)
        {
            const int n = 256;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, true);
            var pixels = new Color[n * n];
            Color dark = tint * 0.55f;
            Color light = Color.Lerp(tint, Color.white, 0.45f);
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float u = x / (float)n;
                    float v = y / (float)n;
                    float brush = Mathf.PerlinNoise(u * 22f, v * 4f);
                    float scratch = Mathf.Abs(Mathf.Sin((v * 70f + u * 3f) * 6.283f));
                    scratch = smooth01(Mathf.InverseLerp(0.97f, 1f, scratch));
                    Color c = Color.Lerp(dark, light, brush);
                    c = Color.Lerp(c, light * 1.2f, scratch * 0.65f);
                    float pit = Mathf.PerlinNoise(u * 30f + 8f, v * 30f);
                    if (pit > 0.86f) c *= 0.65f;
                    pixels[y * n + x] = c;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply(true);
            tex.wrapMode = TextureWrapMode.Repeat;
            return tex;
        }

        static Texture2D Crystal(Color tint)
        {
            const int n = 256;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, true);
            var pixels = new Color[n * n];
            Color deep = tint * 0.35f;
            deep.a = 1f;
            Color glow = Color.Lerp(tint, new Color(0.75f, 0.95f, 1f), 0.45f) * 1.35f;
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float u = x / (float)n;
                    float v = y / (float)n;
                    float facet = Mathf.PerlinNoise(u * 7f, v * 7f);
                    float vein = Mathf.PerlinNoise(u * 14f + v * 3f, v * 9f);
                    vein = smooth01(Mathf.InverseLerp(0.62f, 0.84f, vein));
                    Color c = Color.Lerp(deep, tint * 1.1f, facet);
                    c = Color.Lerp(c, glow, vein * 0.8f);
                    float spark = Mathf.PerlinNoise(u * 50f, v * 50f);
                    if (spark > 0.93f) c = Color.Lerp(c, Color.white, 0.55f);
                    pixels[y * n + x] = c;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply(true);
            tex.wrapMode = TextureWrapMode.Repeat;
            return tex;
        }

        static Texture2D Cloth(Color tint)
        {
            const int n = 128;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, true);
            var pixels = new Color[n * n];
            Color dark = tint * 0.45f;
            Color light = Color.Lerp(tint, Color.white, 0.35f);
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float u = x / (float)n;
                    float v = y / (float)n;
                    float stripe = Mathf.Sin((u * 10f + v * 2f) * Mathf.PI * 2f);
                    float weave = Mathf.PerlinNoise(u * 24f, v * 24f);
                    Color c = Color.Lerp(dark, light, Mathf.InverseLerp(-1f, 1f, stripe));
                    c = Color.Lerp(c, light, weave * 0.25f);
                    pixels[y * n + x] = c;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply(true);
            tex.wrapMode = TextureWrapMode.Repeat;
            return tex;
        }

        static float smooth01(float v)
        {
            v = Mathf.Clamp01(v);
            return v * v * (3f - 2f * v);
        }
    }
}
