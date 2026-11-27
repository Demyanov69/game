using UnityEngine;
using System.Collections.Generic;

namespace EchoIslands
{
    /// <summary>
    /// Applies the visual pass to existing scene objects after gameplay objects are created.
    /// Orbs are dense particle spheres with a small stable emissive heart.
    /// </summary>
    public static class EchoVisualOverhaul
    {
        static readonly Color Infection = new Color(0.10f, 0.72f, 0.62f, 1f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            var go = new GameObject("EchoVisualOverhaulHook");
            Object.DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            go.AddComponent<EchoVisualOverhaulHook>();
        }

        public static void ReskinAll()
        {
            var portals = Object.FindObjectsByType<PortalFrame>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < portals.Length; i++)
                ReskinPortal(portals[i]);

            var orbs = Object.FindObjectsByType<Orb>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < orbs.Length; i++)
                ReskinOrb(orbs[i]);
        }

        public static void ReskinPortal(PortalFrame portal)
        {
            if (portal == null) return;
            Transform root = portal.transform;
            portal.GetAbsorptionPoint();
            TexturePortalFrame(root);
            // The original Main.unity still has a 3rd-party HovlPortal prefab,
            // visually separate from the new membrane. Its green pool on the
            // floor is the unwanted extra effect; retain ONLY the clean tear.
            foreach (string old in new[] { "RiftHalo", "RiftPool", "HovlPortal", "PortalGreen", "PortalEnergy" })
            {
                var obsolete = root.Find(old);
                if (obsolete != null)
                {
                    obsolete.gameObject.SetActive(false);
                    Object.Destroy(obsolete.gameObject);
                }
            }
            Transform surface = root.Find("RiftTear");
            if (surface == null)
            {
                Shader shader = Shader.Find("Echo/BrightPortal") ?? Shader.Find("Echo/InfectedPortal");
                Material energy = shader != null ? new Material(shader) : MakeFallback(Infection);
                energy.name = "Animated Aurora Rift";
                if (energy.HasProperty("_Color")) energy.SetColor("_Color", new Color(.26f,1f,.90f,1f));
                surface = MakeQuad(root, "RiftTear", new Vector3(0f,1.72f,-0.04f),
                    new Vector3(2.48f,3.35f,1f),Quaternion.identity,energy).transform;
            }
            else
            {
                var mesh = surface.GetComponent<Renderer>();
                if (mesh != null && (mesh.sharedMaterial == null || mesh.sharedMaterial.shader == null ||
                    mesh.sharedMaterial.shader.name != "Echo/BrightPortal"))
                {
                    Shader shader = Shader.Find("Echo/BrightPortal");
                    if (shader != null)
                        mesh.sharedMaterial = new Material(shader) { name = "Animated Aurora Rift" };
                }
                surface.localPosition = new Vector3(0f,1.72f,-0.04f);
                surface.localRotation = Quaternion.identity;
                surface.localScale = new Vector3(2.48f,3.35f,1f);
            }

            var membrane = surface.GetComponent<Renderer>();
            portal.activeVisuals = membrane != null ? new Renderer[] { membrane } : new Renderer[0];
            if (membrane != null) membrane.enabled = portal.IsActivated;
        }

        static Material portalStonework;

        static void TexturePortalFrame(Transform root)
        {
            // Level 1's three scene-authored frame cubes had a village atlas material
            // without suitable UVs; it rendered as plain, untextured stone. Reuse
            // the project's existing seamless stone texture and triplanar shader.
            if (portalStonework == null)
            {
                var stone = Resources.Load<Texture2D>("EchoDungeon/Textures/StoneBlocks");
                var shader = Shader.Find("Echo/DungeonTriplanar");
                if (stone == null || shader == null) return;
                portalStonework = new Material(shader) { name = "Portal frame - textured carved stone" };
                portalStonework.SetTexture("_MainTex", stone);
                var darkStone = Resources.Load<Texture2D>("EchoDungeon/Textures/StoneBlocks_Dark");
                if (darkStone != null) portalStonework.SetTexture("_DarkTex", darkStone);
                portalStonework.SetColor("_Color", new Color(.90f, .86f, .78f, 1f));
                portalStonework.SetFloat("_TileScale", 1.35f);
                portalStonework.SetFloat("_Glossiness", .20f);
            }

            foreach (string name in new[] { "PortalFrameLeft", "PortalFrameRight", "PortalFrameTop" })
            {
                var stonePart = root.Find(name);
                if (stonePart == null) continue;
                var renderer = stonePart.GetComponent<MeshRenderer>();
                if (renderer != null && renderer.sharedMaterial != portalStonework)
                    renderer.sharedMaterial = portalStonework;
            }
        }

        public static void ReskinOrb(Orb orb)
        {
            if (orb == null) return;
            Transform root = orb.transform;
            // Existing scene orb has a plain sphere mesh; only that old sphere is hidden.
            // Do not disable the new luminous heart, as the old implementation did each frame.
            var oldMesh = root.GetComponent<MeshRenderer>();
            if (oldMesh != null)
            {
                oldMesh.enabled = false;
                Object.Destroy(oldMesh); // stale source sphere must never be re-enabled by WorldVariant
            }
            Transform visualRoot = root.Find("ReflektOrbParticles");
            if (visualRoot != null) return;

            var visuals = new GameObject("ReflektOrbParticles").transform;
            visuals.SetParent(root, false);
            visuals.localPosition = Vector3.zero;
            visuals.localRotation = Quaternion.identity;
            visuals.localScale = Vector3.one;

            // The small opaque-looking core is a fallback for missing particle shaders.
            // The transparent glow and two particle shells define the sphere.
            MakeOrbHeart(visuals, "LuminousHeart", .28f, 3.2f, 0f);
            MakeOrbHeart(visuals, "GlassAura", .95f, 1.7f, 1f);
            var motion = visuals.gameObject.AddComponent<OrbVisualAnimator>();
            motion.rotationSpeed = 12f;

            BuildParticleCore(visuals);
            BuildParticleOrbit(visuals, "OrbOrbitA", 0f, .45f, .95f);
            BuildParticleOrbit(visuals, "OrbOrbitB", 2.1f, .39f, -1.2f);
            BuildParticleShell(visuals,"Dense spectral surface",.48f,100,new Color(.12f,1f,.90f));
            BuildParticleShell(visuals,"Iridescent comet dust",.39f,48,new Color(.84f,.54f,1f));
            BuildOrbLight(visuals);
            // WorldVariant was first cached before these visuals existed.
            // Refresh includes the new renderers, lights and particle systems.
            var variant = root.GetComponent<WorldVariant>();
            if (variant != null && !orb.carried)
            {
                variant.includeChildren = true;
                variant.affectParticles = true;
                variant.affectLights = true;
                variant.Refresh();
            }
        }

        static void MakeOrbHeart(Transform root,string name,float size,float brightness,float shell)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.transform.SetParent(root,false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale = Vector3.one * size;
            var collider=go.GetComponent<Collider>();
            if(collider!=null)Object.Destroy(collider);
            var shader=Shader.Find("Echo/OrbHeart") ?? Shader.Find("Unlit/Color");
            var mat=new Material(shader) { name = name + " energy" };
            if(mat.HasProperty("_Color"))mat.SetColor("_Color",new Color(.17f,1f,.87f,.95f));
            if(mat.HasProperty("_Shell"))mat.SetFloat("_Shell",shell);
            if(mat.HasProperty("_Brightness"))mat.SetFloat("_Brightness",brightness);
            go.GetComponent<MeshRenderer>().sharedMaterial=mat;
        }

        static void BuildParticleCore(Transform parent)
        {
            var go = new GameObject("OrbParticleCore");
            go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.14f, 0.34f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.015f, 0.12f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.045f, 0.11f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.35f, 1f, 0.9f, 1f), new Color(0.92f, 1f, 1f, 1f));
            main.maxParticles = 26;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;

            var emission = ps.emission;
            emission.rateOverTime = 24f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.035f;
            shape.radiusThickness = 1f;



            var color = ps.colorOverLifetime;
            color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(new Color(0.85f, 1f, 1f), 0f), new GradientColorKey(new Color(0.05f, 1f, 0.84f), 0.65f), new GradientColorKey(new Color(0.05f, 0.55f, 0.5f), 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.12f), new GradientAlphaKey(0.8f, 0.7f), new GradientAlphaKey(0f, 1f) });
            color.color = gradient;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            var shader = Shader.Find("Legacy Shaders/Particles/Additive") ?? Shader.Find("Particles/Additive") ?? Shader.Find("Sprites/Default");
            if (shader != null)
            {
                var mat = new Material(shader);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);
                renderer.sharedMaterial = mat;
            }
            ps.Play();
            ps.Emit(14);
        }

        static void BuildParticleOrbit(Transform parent, string name, float zRotation, float radius, float orbitalSpeed)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localRotation = Quaternion.Euler(0f, 0f, zRotation * Mathf.Rad2Deg);

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = 1.3f;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.028f, 0.06f);
            main.startColor = new Color(0.25f, 1f, 0.9f, 0.95f);
            main.maxParticles = 18;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;

            var emission = ps.emission;
            emission.rateOverTime = 10f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = radius;
            shape.arc = 360f;

            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.orbitalY = orbitalSpeed;
            velocity.radial = -0.03f;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            var shader = Shader.Find("Legacy Shaders/Particles/Additive") ?? Shader.Find("Particles/Additive") ?? Shader.Find("Sprites/Default");
            if (shader != null) renderer.sharedMaterial = new Material(shader);
            ps.Play();
            ps.Emit(20);
        }

        static void BuildParticleShell(Transform parent, string name, float radius, int count, Color tint)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(3.8f, 6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0f, 0.045f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.024f, 0.067f);
            main.startColor = new ParticleSystem.MinMaxGradient(tint, Color.white);
            main.maxParticles = count + 30;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            var em = ps.emission;
            em.rateOverTime = count / 5f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = radius;
            shape.radiusThickness = 0.2f;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(tint, 0f), new GradientColorKey(Color.white, .4f),
                        new GradientColorKey(tint, 1f) },
                new[] { new GradientAlphaKey(0f, 0f),new GradientAlphaKey(.95f, .12f),
                        new GradientAlphaKey(.85f, .78f),new GradientAlphaKey(0f, 1f) });
            col.color = gradient;
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            Shader shader = Shader.Find("Legacy Shaders/Particles/Additive") ??
                            Shader.Find("Particles/Additive") ?? Shader.Find("Sprites/Default");
            if (shader != null) renderer.sharedMaterial = new Material(shader);
            ps.Play();
            ps.Emit(count / 2);
        }

        static void BuildOrbLight(Transform parent)
        {
            var go = new GameObject("Spectral heartbeat light");
            go.transform.SetParent(parent, false);
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(.24f, .98f, .86f);
            light.intensity = 4.0f;
            light.range = 5.0f;
            light.shadows = LightShadows.None;
            light.renderMode = LightRenderMode.ForceVertex;
        }

        /// <summary>
        /// Creates a flat visual surface for the portal (without a physics collider).
        /// The surface uses the supplied shared material, so its shader properties
        /// are preserved on the tear, halo, and pool.
        /// </summary>
        static GameObject MakeQuad(Transform parent, string name, Vector3 localPosition,
            Vector3 localScale, Quaternion localRotation, Material material)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = name;
            quad.transform.SetParent(parent, false);
            quad.transform.localPosition = localPosition;
            quad.transform.localRotation = localRotation;
            quad.transform.localScale = localScale;

            var collider = quad.GetComponent<Collider>();
            if (collider != null)
                Object.Destroy(collider);

            var renderer = quad.GetComponent<MeshRenderer>();
            if (renderer != null && material != null)
                renderer.sharedMaterial = material;

            return quad;
        }

        static Material MakeFallback(Color c)
        {
            var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
            var mat = new Material(shader);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
            return mat;
        }
    }
}
