#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EchoIslands.EditorTools
{
    public static class PrototypeBuilder
    {
        [MenuItem("Echo Islands/Build Legacy Prototype (old)")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var shader = Shader.Find("Echo/DualWorldLit");
            var lightMat = MakeMat(shader, new Color(0.93f, 0.90f, 0.84f), new Color(0.96f, 0.65f, 0.76f), 0.35f, 0.4f);
            var pinkMat = MakeMat(shader, new Color(0.96f, 0.75f, 0.82f), new Color(0.98f, 0.55f, 0.72f), 0.8f, 0.3f);
            var blueMat = MakeMat(shader, new Color(0.62f, 0.82f, 0.93f), new Color(0.45f, 0.75f, 0.95f), 0.7f, 0.5f);
            var goldMat = MakeMat(shader, new Color(0.96f, 0.86f, 0.45f), new Color(0.98f, 0.75f, 0.25f), 0.85f, 1.2f);
            var darkBridge = MakeMat(shader, new Color(0.07f, 0.08f, 0.09f), new Color(0.18f, 0.9f, 0.84f), 0.15f, 2.2f);

            var sun = new GameObject("Sun");
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            light.color = new Color(1f, 0.96f, 0.9f);
            sun.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = 0.012f;
            RenderSettings.fogColor = new Color(0.80f, 0.86f, 0.92f);

            var systems = new GameObject("Systems");
            systems.AddComponent<WorldState>();
            var flow = systems.AddComponent<LevelFlow>();

            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            Object.DestroyImmediate(player.GetComponent<Collider>());
            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.radius = 0.35f;
            player.transform.position = new Vector3(0f, 2f, -6f);
            var camGo = new GameObject("Camera");
            camGo.transform.SetParent(player.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 1.55f, 0f);
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 250f;
            camGo.AddComponent<AudioListener>();
            var fps = player.AddComponent<FirstPersonController>();
            fps.cameraPivot = camGo.transform;
            var tools = player.AddComponent<ToolController>();
            tools.maskOrigin = camGo.transform;
            player.GetComponent<Renderer>().enabled = false;

            var roots = new GameObject[5];
            var spawns = new Transform[5];
            for (int i = 0; i < 5; i++)
            {
                var root = new GameObject("Level_" + (i + 1));
                root.transform.position = new Vector3(i * 80f, 0f, 0f);
                roots[i] = root;
                Island(root.transform, Vector3.zero, new Vector3(10f, 1.2f, 10f), lightMat, "StartIsland");
                var spawn = new GameObject("Spawn").transform;
                spawn.SetParent(root.transform, false);
                spawn.position = root.transform.position + new Vector3(0f, 2.2f, -3.2f);
                spawns[i] = spawn;
            }

            BuildLevel1(roots[0].transform, lightMat, pinkMat, darkBridge, goldMat);
            BuildLevel2(roots[1].transform, lightMat, blueMat, darkBridge, goldMat);
            BuildLevel3(roots[2].transform, lightMat, pinkMat, darkBridge, goldMat);
            BuildLevel4(roots[3].transform, lightMat, blueMat, darkBridge, goldMat);
            BuildLevel5(roots[4].transform, lightMat, pinkMat, blueMat, darkBridge, goldMat);

            flow.player = player.transform;
            flow.tools = tools;
            flow.levelRoots = roots;
            flow.spawns = spawns;

            BuildMenu(flow, fps, tools);

            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/Main.unity");
            Debug.Log("Echo Islands prototype saved to Assets/Scenes/Main.unity");
        }

        static void BuildLevel1(Transform root, Material stone, Material accent, Material bridgeMat, Material orbMat)
        {
            var pad = root.Find("StartIsland");
            if (pad != null) Object.DestroyImmediate(pad.gameObject);
            var spawn = root.Find("Spawn");
            if (spawn != null)
            {
                spawn.localPosition = new Vector3(0f, 1.35f, -6f);
                spawn.localRotation = Quaternion.identity;
            }

            Floor(root, new Vector3(0f, 0f, -4f), new Vector3(12f, 0.5f, 10f), stone);
            Wall(root, new Vector3(0f, 1.6f, -9.2f), new Vector3(12f, 3.2f, 0.4f), stone);
            Wall(root, new Vector3(-6.2f, 1.6f, -6.2f), new Vector3(0.4f, 3.2f, 4f), stone);
            Wall(root, new Vector3(6.2f, 1.6f, -6.2f), new Vector3(0.4f, 3.2f, 4f), stone);
            Column(root, new Vector3(-3.2f, 1.5f, -5f), accent);
            Column(root, new Vector3(3.2f, 1.5f, -5f), accent);
            Column(root, new Vector3(-3.2f, 1.5f, -2.2f), accent);
            Column(root, new Vector3(3.2f, 1.5f, -2.2f), accent);

            Floor(root, new Vector3(10f, 0f, -4f), new Vector3(8f, 0.5f, 4.5f), stone);
            Floor(root, new Vector3(16f, 0f, -4f), new Vector3(6f, 0.5f, 6f), accent);
            Wall(root, new Vector3(16f, 1.6f, -7.2f), new Vector3(6f, 3.2f, 0.4f), stone);
            Wall(root, new Vector3(19.2f, 1.6f, -4f), new Vector3(0.4f, 3.2f, 6f), stone);
            Column(root, new Vector3(14.2f, 1.4f, -5.6f), accent);
            Column(root, new Vector3(17.8f, 1.4f, -5.6f), accent);
            Pickup(root, new Vector3(16f, 1.15f, -4f), ToolKind.Staff, orbMat);

            Floor(root, new Vector3(-9f, 0f, -4f), new Vector3(6f, 0.5f, 4.5f), stone);
            Floor(root, new Vector3(-9f, 0f, 2.2f), new Vector3(6f, 0.5f, 4f), accent);
            var lesson = Floor(root, new Vector3(-9f, 0.15f, -0.7f), new Vector3(2f, 0.28f, 2.4f), bridgeMat);
            lesson.AddComponent<WorldObject>().home = WorldKind.Dark;
            Column(root, new Vector3(-9f, 1.5f, 3.2f), accent);
            Wall(root, new Vector3(-9f, 1.6f, 4.4f), new Vector3(6f, 3.2f, 0.4f), stone);

            Floor(root, new Vector3(-4f, 0f, 2.4f), new Vector3(4f, 0.5f, 3f), stone);
            Floor(root, new Vector3(4f, 0f, 2.4f), new Vector3(4f, 0.5f, 3f), stone);
            Wall(root, new Vector3(-4f, 1.6f, 3.6f), new Vector3(4f, 3.2f, 0.4f), stone);
            Wall(root, new Vector3(4f, 1.6f, 3.6f), new Vector3(4f, 3.2f, 0.4f), stone);
            var bridge = Floor(root, new Vector3(0f, 0.15f, 3.4f), new Vector3(2.2f, 0.28f, 5.2f), bridgeMat);
            bridge.AddComponent<WorldObject>().home = WorldKind.Dark;

            Floor(root, new Vector3(0f, 0f, 10f), new Vector3(14f, 0.5f, 10f), stone);
            Wall(root, new Vector3(-7.2f, 1.6f, 10f), new Vector3(0.4f, 3.2f, 10f), stone);
            Wall(root, new Vector3(7.2f, 1.6f, 10f), new Vector3(0.4f, 3.2f, 10f), stone);
            Wall(root, new Vector3(0f, 1.6f, 15.2f), new Vector3(10f, 3.2f, 0.4f), stone);
            Sphere(root, new Vector3(-4.2f, 1.15f, 11.5f), orbMat, WorldKind.Light);

            var fakeGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            fakeGo.name = "FalseRelic";
            fakeGo.transform.SetParent(root, false);
            fakeGo.transform.localPosition = new Vector3(4.4f, 1.2f, 12.4f);
            fakeGo.transform.localScale = new Vector3(0.45f, 1.1f, 0.45f);
            fakeGo.GetComponent<Renderer>().sharedMaterial = bridgeMat;
            fakeGo.AddComponent<WorldObject>().home = WorldKind.Dark;

            Portal(root, new Vector3(0f, 1.45f, 13.6f), 1, 2);

            Ceiling(root, new Vector3(0f, 3.5f, -4f), new Vector3(12f, 0.25f, 10f), stone);
            Ceiling(root, new Vector3(16f, 3.5f, -4f), new Vector3(6f, 0.25f, 6f), stone);
            Ceiling(root, new Vector3(0f, 3.5f, 10f), new Vector3(14f, 0.25f, 10f), stone);

            var pit = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pit.name = "Fall";
            pit.transform.SetParent(root, false);
            pit.transform.localPosition = new Vector3(0f, -6f, 4f);
            pit.transform.localScale = new Vector3(48f, 1f, 48f);
            Object.DestroyImmediate(pit.GetComponent<Renderer>());
            pit.GetComponent<Collider>().isTrigger = true;
            pit.AddComponent<FallCatch>();
        }

        static void BuildLevel2(Transform root, Material stone, Material accent, Material bridgeMat, Material orbMat)
        {
            Island(root, new Vector3(0f, 0f, 9f), new Vector3(7f, 1.2f, 6f), stone);
            Pickup(root, new Vector3(-2f, 2f, -2f), ToolKind.Staff, orbMat);
            Pickup(root, new Vector3(2f, 2f, -2f), ToolKind.Flashlight, accent);
            Island(root, new Vector3(0f, 2.4f, 16f), new Vector3(6f, 1f, 6f), accent);
            var local = Island(root, new Vector3(0f, 1.3f, 13f), new Vector3(2f, 0.3f, 2.4f), bridgeMat, "FlashBridge");
            local.AddComponent<WorldObject>().home = WorldKind.Dark;
            Sphere(root, new Vector3(0f, 4.2f, 16f), orbMat, WorldKind.Dark);
            Portal(root, new Vector3(0f, 3.6f, 18.2f), 1, 3);
        }

        static void BuildLevel3(Transform root, Material stone, Material accent, Material bridgeMat, Material orbMat)
        {
            Island(root, new Vector3(0f, 0f, 7f), new Vector3(6f, 1.1f, 5f), stone);
            Pickup(root, new Vector3(-1.6f, 1.8f, -2f), ToolKind.Staff, orbMat);
            Pickup(root, new Vector3(0f, 1.8f, -2f), ToolKind.Flashlight, accent);
            Pickup(root, new Vector3(1.6f, 1.8f, -2f), ToolKind.Clock, orbMat);
            Island(root, new Vector3(5.5f, 0f, 14f), new Vector3(6f, 1.1f, 6f), accent);
            var timed = Island(root, new Vector3(3f, 0.4f, 10.5f), new Vector3(3.2f, 0.3f, 2.2f), bridgeMat, "ClockBridge");
            timed.AddComponent<WorldObject>().home = WorldKind.Dark;
            Sphere(root, new Vector3(6.5f, 2.2f, 14f), orbMat, WorldKind.Light);
            Portal(root, new Vector3(5.5f, 1.6f, 16.5f), 1, 4);
        }

        static void BuildLevel4(Transform root, Material stone, Material accent, Material bridgeMat, Material orbMat)
        {
            Island(root, new Vector3(-4f, 0f, 8f), new Vector3(5f, 1.1f, 5f), stone);
            Pickup(root, new Vector3(-5f, 1.8f, 6f), ToolKind.Staff, orbMat);
            Pickup(root, new Vector3(-4f, 1.8f, 6f), ToolKind.Flashlight, accent);
            Pickup(root, new Vector3(-3f, 1.8f, 6f), ToolKind.Clock, orbMat);
            Island(root, new Vector3(4f, 0f, 14f), new Vector3(6f, 1.1f, 6f), accent);
            var plate = Island(root, new Vector3(0f, 0.3f, 11f), new Vector3(3f, 0.28f, 3f), bridgeMat, "BeaconPlate");
            plate.AddComponent<WorldObject>().home = WorldKind.Dark;
            Beacon(root, new Vector3(-4f, 1.8f, 6f), 8f, WorldKind.Dark);
            Sphere(root, new Vector3(4f, 2.3f, 13f), orbMat, WorldKind.Dark);
            Portal(root, new Vector3(4f, 1.6f, 16.4f), 1, 5);
        }

        static void BuildLevel5(Transform root, Material stone, Material a, Material b, Material bridgeMat, Material orbMat)
        {
            Island(root, new Vector3(-3f, 1.5f, 8f), new Vector3(4.5f, 1f, 4.5f), a);
            Pickup(root, new Vector3(-4f, 2.6f, 6.5f), ToolKind.Staff, orbMat);
            Pickup(root, new Vector3(-3f, 2.6f, 6.5f), ToolKind.Flashlight, a);
            Pickup(root, new Vector3(-2f, 2.6f, 6.5f), ToolKind.Clock, orbMat);
            Island(root, new Vector3(3.5f, 0.2f, 12f), new Vector3(5f, 1f, 5f), b);
            Island(root, new Vector3(0f, 3.2f, 18f), new Vector3(6f, 1f, 5f), stone);
            var br1 = Island(root, new Vector3(-0.2f, 1.6f, 10.2f), new Vector3(2.2f, 0.25f, 2f), bridgeMat);
            br1.AddComponent<WorldObject>().home = WorldKind.Dark;
            var br2 = Island(root, new Vector3(2f, 2.2f, 15.2f), new Vector3(2.2f, 0.25f, 2.4f), bridgeMat);
            br2.AddComponent<WorldObject>().home = WorldKind.Light;
            Beacon(root, new Vector3(3.5f, 1.6f, 10.5f), 6.5f, WorldKind.Dark);
            Sphere(root, new Vector3(-3f, 3.1f, 8f), orbMat, WorldKind.Light);
            Sphere(root, new Vector3(4.6f, 1.8f, 12.4f), orbMat, WorldKind.Dark);
            Portal(root, new Vector3(0f, 4.4f, 19.6f), 2, 6);
        }


        static GameObject Floor(Transform parent, Vector3 local, Vector3 scale, Material mat)
        {
            return Island(parent, local, scale, mat, "Floor");
        }

        static void Wall(Transform parent, Vector3 local, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Wall";
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
        }

        static void Ceiling(Transform parent, Vector3 local, Vector3 scale, Material mat)
        {
            Wall(parent, local, scale, mat);
        }

        static void Pickup(Transform parent, Vector3 local, ToolKind kind, Material mat)
        {
            var go = GameObject.CreatePrimitive(kind == ToolKind.Staff ? PrimitiveType.Cylinder : PrimitiveType.Cube);
            go.name = "Pickup_" + kind;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            go.transform.localScale = kind == ToolKind.Staff
                ? new Vector3(0.12f, 0.7f, 0.12f)
                : new Vector3(0.28f, 0.28f, 0.28f);
            go.GetComponent<Renderer>().sharedMaterial = mat;
            var p = go.AddComponent<ToolPickup>();
            p.kind = kind;
        }

        static GameObject Island(Transform parent, Vector3 local, Vector3 scale, Material mat, string name = "Island")
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local + new Vector3(0f, scale.y * 0.5f, 0f);
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        static void Column(Transform parent, Vector3 local, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "Column";
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            go.transform.localScale = new Vector3(0.45f, 1.3f, 0.45f);
            go.GetComponent<Renderer>().sharedMaterial = mat;
        }

        static Orb Sphere(Transform parent, Vector3 local, Material mat, WorldKind home)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Orb";
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            go.transform.localScale = Vector3.one * 0.55f;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            var wo = go.AddComponent<WorldObject>();
            wo.home = home;
            var orb = go.AddComponent<Orb>();
            orb.home = home;
            return orb;
        }

        static void Beacon(Transform parent, Vector3 local, float radius, WorldKind forced)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "Beacon";
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            go.transform.localScale = new Vector3(0.35f, 0.8f, 0.35f);
            var flame = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flame.name = "Flame";
            flame.transform.SetParent(go.transform, false);
            flame.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            flame.transform.localScale = Vector3.one * 0.6f;
            Object.DestroyImmediate(flame.GetComponent<Collider>());
            var b = go.AddComponent<Beacon>();
            b.radius = radius;
            b.forced = forced;
        }

        static void Portal(Transform parent, Vector3 local, int required, int next)
        {
            var frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = "Portal";
            frame.transform.SetParent(parent, false);
            frame.transform.localPosition = local;
            frame.transform.localScale = new Vector3(2.4f, 2.6f, 0.35f);
            var portal = frame.AddComponent<PortalFrame>();
            portal.requiredOrbs = required;
            portal.nextLevel = next;
            var sockets = new Transform[required];
            for (int i = 0; i < required; i++)
            {
                var s = new GameObject("Socket" + i).transform;
                s.SetParent(frame.transform, false);
                float x = required == 1 ? 0f : (i == 0 ? -0.25f : 0.25f);
                s.localPosition = new Vector3(x, 0.15f, 0.7f);
                sockets[i] = s;
            }
            portal.sockets = sockets;
            var spawn = new GameObject("Arrive").transform;
            spawn.SetParent(frame.transform, false);
            spawn.localPosition = new Vector3(0f, -0.4f, 1.6f);
            portal.playerSpawnOnNext = spawn;
        }

        static Material MakeMat(Shader shader, Color albedo, Color accent, float accentAmt, float emission)
        {
            var m = new Material(shader != null ? shader : Shader.Find("Universal Render Pipeline/Lit"));
            if (shader != null)
            {
                m.SetColor("_LightColor", albedo);
                m.SetColor("_LightAccent", accent);
                m.SetColor("_DarkColor", new Color(0.05f, 0.06f, 0.07f));
                m.SetColor("_DarkEmission", new Color(0.18f, 0.90f, 0.84f));
                m.SetFloat("_AccentAmount", accentAmt);
                m.SetFloat("_EmissionStrength", emission);
            }
            else m.color = albedo;
            return m;
        }

        static void BuildMenu(LevelFlow flow, FirstPersonController fps, ToolController tools)
        {
            var canvasGo = new GameObject("UI");
            var menu = canvasGo.AddComponent<MainMenu>();
            menu.fps = fps;
            menu.flow = flow;
            var hud = canvasGo.AddComponent<GameHUD>();
            hud.menu = menu;
            hud.tools = tools;
        }
    }
}
#endif
