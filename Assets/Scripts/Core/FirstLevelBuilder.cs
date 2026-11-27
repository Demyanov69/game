using UnityEngine;

namespace EchoIslands
{
    public static class FirstLevelBuilder
    {
        static readonly Color WarmStone = new Color(0.72f, 0.64f, 0.53f);
        static readonly Color WarmFloor = new Color(0.44f, 0.39f, 0.34f);
        static readonly Color Wood = new Color(0.30f, 0.19f, 0.12f);
        static readonly Color Metal = new Color(0.18f, 0.20f, 0.22f);
        static readonly Color Gold = new Color(0.78f, 0.58f, 0.20f);
        static readonly Color Teal = new Color(0.10f, 0.78f, 0.72f);

        public static Transform Build(Transform root)
        {
            if (root == null) return null;
            Clear(root);

            Material stone = Material("L1_Stone", WarmStone, new Color(0.96f, 0.66f, 0.60f), 0.16f, 0.45f);
            Material floor = Material("L1_Floor", WarmFloor, new Color(0.90f, 0.55f, 0.65f), 0.08f, 0.30f);
            Material wood = Material("L1_Wood", Wood, new Color(0.28f, 0.88f, 0.80f), 0.22f, 0.70f);
            Material metal = Material("L1_Metal", Metal, new Color(0.24f, 0.88f, 0.82f), 0.30f, 0.95f);
            Material gold = Material("L1_Gold", Gold, new Color(1f, 0.80f, 0.32f), 0.45f, 1.10f);
            Material teal = Material("L1_Teal", Teal, new Color(0.15f, 1f, 0.90f), 0.75f, 2.40f);

            var level = new GameObject("__L1_BUILT").transform;
            level.SetParent(root, false);

            BuildEntrance(level, stone, floor, gold, teal);
            BuildTrial1(level, stone, floor, wood, teal);
            BuildTrial2(level, stone, floor, metal, teal);
            BuildTrial3(level, stone, floor, wood, gold, teal);

            Transform spawn = new GameObject("Spawn").transform;
            spawn.SetParent(level, false);
            spawn.localPosition = new Vector3(0f, 1.20f, -3.20f);
            spawn.localRotation = Quaternion.Euler(0f, 0f, 0f);

            return spawn;
        }

        static void BuildEntrance(Transform p, Material stone, Material floor, Material gold, Material teal)
        {
            Room(p, "Entrance", new Vector3(0f, 0f, 0f), new Vector3(12f, 4f, 10f), floor, stone, true, true, true, false);
            Box(p, "EntranceToTrial1", new Vector3(0f, 0f, 7.7f), new Vector3(10f, 0.5f, 5.6f), floor, null);
            Column(p, new Vector3(-4.4f, 0f, -1.8f), stone);
            Column(p, new Vector3(4.4f, 0f, -1.8f), stone);
            Column(p, new Vector3(-4.4f, 0f, 2.8f), stone);
            Column(p, new Vector3(4.4f, 0f, 2.8f), stone);
            Decor(p, "Pillar", new Vector3(-4.4f, 0f, -1.8f), 1.0f, 1.0f);
            Decor(p, "Pillar", new Vector3(4.4f, 0f, -1.8f), 1.0f, -1.0f);
            Decor(p, "Torch", new Vector3(-5.1f, 1.2f, 2.0f), 1.0f, 0f);
            Decor(p, "Torch", new Vector3(5.1f, 1.2f, 2.0f), 1.0f, 180f);
            Decor(p, "Banner", new Vector3(0f, 1.2f, 4.55f), 0.9f, 180f);

            Box(p, "StaffPedestal", new Vector3(0f, 0.35f, 2.5f), new Vector3(1.6f, 0.7f, 1.2f), stone, WorldKind.Light);
            var staff = Primitive(PrimitiveType.Cylinder, p, "Pickup_Staff", new Vector3(0f, 1.55f, 2.5f), new Vector3(0.14f, 0.72f, 0.14f), gold);
            staff.AddComponent<ToolPickup>().kind = ToolKind.Staff;
        }

        static void BuildTrial1(Transform p, Material stone, Material floor, Material wood, Material teal)
        {
            Room(p, "Trial1_Start", new Vector3(0f, 0f, 14f), new Vector3(10f, 4f, 7f), floor, stone, true, true, false, false);

            Pit(p, new Vector3(0f, -2.8f, 21f), new Vector3(10f, 5f, 13f));
            Box(p, "StoneBridge", new Vector3(0f, 0.0f, 18.2f), new Vector3(3.0f, 0.45f, 8.4f), stone, WorldKind.Light);
            Box(p, "SafeLedge", new Vector3(0f, 0.0f, 22.5f), new Vector3(3.4f, 0.55f, 2.4f), stone, null);

            Box(p, "DarkBridge", new Vector3(0f, 0.0f, 26.8f), new Vector3(3.0f, 0.45f, 7.8f), wood, WorldKind.Dark);
            Box(p, "DarkPassageLeft", new Vector3(-3.9f, 1.8f, 28.2f), new Vector3(1.3f, 3.6f, 7.2f), teal, WorldKind.Dark);
            Box(p, "DarkPassageRight", new Vector3(3.9f, 1.8f, 28.2f), new Vector3(1.3f, 3.6f, 7.2f), teal, WorldKind.Dark);
            Box(p, "LightDeadEnd", new Vector3(0f, 1.8f, 24.3f), new Vector3(6.0f, 3.6f, 0.55f), stone, WorldKind.Light);
            Box(p, "CrackHint", new Vector3(0f, 1.6f, 24.0f), new Vector3(0.18f, 2.2f, 0.10f), teal, null);

            Box(p, "TransitionToTrial2", new Vector3(0f, 0f, 32.0f), new Vector3(6f, 0.5f, 4.0f), floor, null);
            Checkpoint(p, new Vector3(0f, 0.0f, 31.8f), new Vector3(5f, 0.6f, 1.5f), new Vector3(0f, 1.20f, 32.8f));
            Decor(p, "Pillar", new Vector3(-4.2f, 0f, 16.0f), 0.95f, 0f);
            Decor(p, "Pillar", new Vector3(4.2f, 0f, 16.0f), 0.95f, 0f);
            Decor(p, "Torch", new Vector3(-4.3f, 1.5f, 29f), 1f, 0f);
            Decor(p, "Torch", new Vector3(4.3f, 1.5f, 29f), 1f, 180f);
        }

        static void BuildTrial2(Transform p, Material stone, Material floor, Material metal, Material teal)
        {
            Room(p, "Trial2", new Vector3(0f, 0f, 41f), new Vector3(11f, 6f, 15f), floor, stone, true, true, true, false);
            Pit(p, new Vector3(0f, -3.0f, 40f), new Vector3(9.0f, 5f, 12f));

            Platform(p, "P1", new Vector3(0f, 0.85f, 34.7f), 0.85f, 0.85f, 0.22f, 0.8f, 0f, metal, teal);
            Platform(p, "P2", new Vector3(1.2f, 0.75f, 37.0f), 0.70f, 2.25f, 0.22f, 0.8f, 0.7f, metal, teal);
            Platform(p, "P3", new Vector3(-0.5f, 0.90f, 39.2f), 2.35f, 0.85f, 0.22f, 0.8f, 1.4f, metal, teal);
            Platform(p, "P4", new Vector3(1.0f, 0.90f, 41.5f), 2.25f, 2.25f, 0.20f, 0.75f, 2.0f, metal, teal);

            Box(p, "Trial2Goal", new Vector3(0f, 0.25f, 44.0f), new Vector3(5.5f, 0.5f, 4.5f), floor, null);
            Box(p, "HeightMarkLow", new Vector3(-4.0f, 0.72f, 36.3f), new Vector3(0.16f, 1.2f, 0.16f), stone, null);
            Box(p, "HeightMarkHigh", new Vector3(4.0f, 2.45f, 36.3f), new Vector3(0.16f, 4.6f, 0.16f), teal, null);

            Checkpoint(p, new Vector3(0f, 0.0f, 46.2f), new Vector3(5f, 0.6f, 1.2f), new Vector3(0f, 1.20f, 47.0f));
            Decor(p, "Pillar", new Vector3(-4.5f, 0f, 36f), 0.9f, 0f);
            Decor(p, "Pillar", new Vector3(4.5f, 0f, 36f), 0.9f, 0f);
            Decor(p, "Torch", new Vector3(-4.6f, 1.5f, 43.6f), 1f, 0f);
            Decor(p, "Torch", new Vector3(4.6f, 1.5f, 43.6f), 1f, 180f);
        }

        static void BuildTrial3(Transform p, Material stone, Material floor, Material wood, Material gold, Material teal)
        {
            Room(p, "Trial3OrbRoom", new Vector3(0f, 0f, 53f), new Vector3(10f, 4.5f, 9f), floor, stone, true, false, false, true);
            Box(p, "DarkOrbPedestal", new Vector3(-2.8f, 0.55f, 54.8f), new Vector3(1.5f, 1.1f, 1.5f), stone, WorldKind.Dark);
            var orbGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orbGo.name = "Orb";
            orbGo.transform.SetParent(p, false);
            orbGo.transform.localPosition = new Vector3(-2.8f, 1.8f, 54.8f);
            orbGo.transform.localScale = Vector3.one * 0.42f;
            var orbRenderer = orbGo.GetComponent<Renderer>();
            if (orbRenderer != null) orbRenderer.sharedMaterial = teal;
            var orbWorld = orbGo.AddComponent<WorldObject>();
            orbWorld.home = WorldKind.Dark;
            var orb = orbGo.AddComponent<Orb>();
            orb.home = WorldKind.Dark;
            Box(p, "NormalOrbLight", new Vector3(-2.8f, 2.3f, 54.8f), new Vector3(0.12f, 1.8f, 0.12f), teal, WorldKind.Dark);


            CorridorLight(p, floor, stone, teal);

            var pedestal = Box(p, "PedestalNormal", new Vector3(13.0f, 0.6f, 53f), new Vector3(2.2f, 1.2f, 2.2f), gold, WorldKind.Light);

            Box(p, "PortalFloor", new Vector3(16.0f, 0f, 53f), new Vector3(4.0f, 0.5f, 4.0f), floor, null);
            PortalFrame portal = Portal(p, new Vector3(16.0f, 1.8f, 53f), gold, teal);
            var socketGo = new GameObject("OrbSocket");
            socketGo.transform.SetParent(pedestal.transform, false);
            socketGo.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            var socket = socketGo.transform;
            var receiver = pedestal.AddComponent<OrbSocket>();
            receiver.socket = socket;
            receiver.targetPortal = portal;
        }

        static void CorridorLight(Transform p, Material floor, Material stone, Material teal)
        {
            Box(p, "WindFloor", new Vector3(6.6f, 0f, 53f), new Vector3(16.5f, 0.45f, 4.0f), floor, WorldKind.Light);
            Box(p, "WindWallL", new Vector3(6.6f, 2.0f, 50.9f), new Vector3(16.5f, 4f, 0.45f), stone, WorldKind.Dark);
            Box(p, "WindWallR", new Vector3(6.6f, 2.0f, 55.1f), new Vector3(16.5f, 4f, 0.45f), stone, WorldKind.Dark);

            SafePad(p, new Vector3(4.0f, 0.26f, 53f), teal);
            SafePad(p, new Vector3(9.0f, 0.26f, 53f), teal);
            SafePad(p, new Vector3(13.0f, 0.26f, 53f), teal);

            Wind(p, new Vector3(6.2f, 1.0f, 53f), new Vector3(4.2f, 2f, 3.5f), Vector3.right);
            Wind(p, new Vector3(10.8f, 1.0f, 53f), new Vector3(3.2f, 2f, 3.5f), Vector3.right);
        }

        static void Room(Transform p, string name, Vector3 center, Vector3 size, Material floor, Material wall, bool left, bool right, bool back, bool front)
        {
            Box(p, name + "_Floor", new Vector3(center.x, 0f, center.z), new Vector3(size.x, 0.5f, size.z), floor, null);
            if (left) Box(p, name + "_Left", new Vector3(center.x - size.x * 0.5f, size.y * 0.5f, center.z), new Vector3(0.5f, size.y, size.z), wall, null);
            if (right) Box(p, name + "_Right", new Vector3(center.x + size.x * 0.5f, size.y * 0.5f, center.z), new Vector3(0.5f, size.y, size.z), wall, null);
            if (back) Box(p, name + "_Back", new Vector3(center.x, size.y * 0.5f, center.z - size.z * 0.5f), new Vector3(size.x, size.y, 0.5f), wall, null);
            if (front) Box(p, name + "_Front", new Vector3(center.x, size.y * 0.5f, center.z + size.z * 0.5f), new Vector3(size.x, size.y, 0.5f), wall, null);
        }

        static void Platform(Transform p, string name, Vector3 pos, float lightHeight, float darkHeight, float amp, float speed, float phase, Material metal, Material teal)
        {
            var go = Box(p, name, new Vector3(pos.x, lightHeight, pos.z), new Vector3(1.8f, 0.42f, 1.8f), metal, null);
            var platform = go.AddComponent<WorldPlatform>();
            platform.lightHeight = lightHeight;
            platform.darkHeight = darkHeight;
            platform.bobAmplitude = amp;
            platform.bobSpeed = speed;
            platform.phase = phase;
            platform.horizontalSize = new Vector2(0.9f, 0.9f);
            var marker = Primitive(PrimitiveType.Cube, go.transform, name + "_Glow", new Vector3(0f, 0.24f, 0f), new Vector3(1.2f, 0.04f, 1.2f), teal);
            Object.Destroy(marker.GetComponent<Collider>());
        }

        static void Wind(Transform p, Vector3 pos, Vector3 scale, Vector3 direction)
        {
            var go = Box(p, "WindZone", pos, scale, null, null);
            var c = go.GetComponent<Collider>();
            c.isTrigger = true;
            var w = go.AddComponent<WindZone>();
            w.direction = direction;
            WindParticles(go.transform);
        }

        static void SafePad(Transform p, Vector3 pos, Material teal)
        {
            Box(p, "SafePad", pos, new Vector3(2.0f, 0.35f, 2.6f), teal, null);
        }

        static void Checkpoint(Transform p, Vector3 zonePos, Vector3 zoneSize, Vector3 checkpointPos)
        {
            var zone = Primitive(PrimitiveType.Cube, p, "Checkpoint", zonePos, zoneSize, null);
            var col = zone.GetComponent<Collider>();
            col.isTrigger = true;
            var point = new GameObject("CheckpointPoint").transform;
            point.SetParent(p, false);
            point.localPosition = checkpointPos;
            point.localRotation = Quaternion.identity;
            var trigger = zone.AddComponent<CheckpointTrigger>();
            trigger.checkpoint = point;
            var r = zone.GetComponent<Renderer>();
            if (r != null) r.enabled = false;
        }

        static void Pit(Transform p, Vector3 pos, Vector3 scale)
        {
            var go = Primitive(PrimitiveType.Cube, p, "Fall", pos, scale, null);
            var c = go.GetComponent<Collider>();
            c.isTrigger = true;
            go.AddComponent<FallCatch>();
            var r = go.GetComponent<Renderer>();
            if (r != null) r.enabled = false;
        }

        static PortalFrame Portal(Transform p, Vector3 pos, Material mat, Material glowMat)
        {
            var frame = new GameObject("Portal");
            frame.transform.SetParent(p, false);
            frame.transform.localPosition = pos;
            var box = frame.AddComponent<BoxCollider>();
            box.size = new Vector3(2.4f, 3.2f, 0.6f);
            box.center = new Vector3(0f, 0.2f, 0f);
            var portal = frame.AddComponent<PortalFrame>();
            portal.requiredOrbs = 1;
            portal.nextLevel = 2;
            var sockets = new Transform[1];
            var socket = new GameObject("Socket0").transform;
            socket.SetParent(frame.transform, false);
            socket.localPosition = new Vector3(0f, 0.2f, -0.38f);
            sockets[0] = socket;
            portal.sockets = sockets;
            var arrive = new GameObject("Arrive").transform;
            arrive.SetParent(frame.transform, false);
            arrive.localPosition = new Vector3(0f, -0.4f, 1.8f);
            portal.playerSpawnOnNext = arrive;

            var membrane = GameObject.CreatePrimitive(PrimitiveType.Quad);
            membrane.name = "PortalEnergy";
            membrane.transform.SetParent(frame.transform, false);
            membrane.transform.localPosition = new Vector3(0f, 0.15f, -0.05f);
            membrane.transform.localScale = new Vector3(1.8f, 2.5f, 1f);
            Object.Destroy(membrane.GetComponent<Collider>());
            var shader = Shader.Find("Echo/InfectedPortal");
            if (shader != null)
            {
                var gate = new Material(shader);
                gate.SetColor("_Color", new Color(0.05f, 0.55f, 0.48f, 1f));
                gate.SetColor("_Core", new Color(0f, 0.02f, 0.02f, 1f));
                membrane.GetComponent<Renderer>().sharedMaterial = gate;
            }
            else if (glowMat != null)
            {
                membrane.GetComponent<Renderer>().sharedMaterial = glowMat;
            }
            portal.activeVisuals = new Renderer[0];
            return portal;
        }

        static GameObject Box(Transform p, string name, Vector3 local, Vector3 scale, Material mat, WorldKind? world)
        {
            var go = Primitive(PrimitiveType.Cube, p, name, local, scale, mat);
            if (world.HasValue)
            {
                var wo = go.AddComponent<WorldObject>();
                wo.home = world.Value;
            }
            return go;
        }

        static GameObject Primitive(PrimitiveType type, Transform p, string name, Vector3 local, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(p, false);
            go.transform.localPosition = local;
            go.transform.localScale = scale;
            var r = go.GetComponent<Renderer>();
            if (r != null && mat != null) r.sharedMaterial = mat;
            return go;
        }

        static void Column(Transform p, Vector3 local, Material mat)
        {
            Primitive(PrimitiveType.Cylinder, p, "Column", local + new Vector3(0f, 1.4f, 0f), new Vector3(0.5f, 1.4f, 0.5f), mat);
        }


        static void WindParticles(Transform parent)
        {
            var go = new GameObject("GustParticles");
            go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = 0.8f;
            main.startSpeed = 2.4f;
            main.startSize = 0.035f;
            main.startColor = new Color(0.55f, 0.95f, 0.90f, 0.55f);
            var emission = ps.emission;
            emission.rateOverTime = 26f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(3.6f, 1.0f, 3.2f);
            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.x = 2.8f;
        }

        static void Decor(Transform parent, string resourceName, Vector3 local, float scale, float yRotation)
        {
            var prefab = Resources.Load<GameObject>("L1/" + resourceName);
            if (prefab == null) return;
            var go = Object.Instantiate(prefab, parent);
            go.name = "Decor_" + resourceName;
            go.transform.localPosition = local;
            go.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);
            go.transform.localScale = Vector3.one * scale;
        }

        static Material Material(string name, Color light, Color accent, float accentAmount, float emission)
        {
            var shader = Shader.Find("Echo/DualWorldLit");
            var m = new Material(shader != null ? shader : Shader.Find("Standard"));
            m.name = name;
            if (shader != null)
            {
                m.SetColor("_LightColor", light);
                m.SetColor("_LightAccent", accent);
                m.SetColor("_DarkColor", new Color(0.012f, 0.016f, 0.015f));
                m.SetColor("_DarkEmission", new Color(0.04f, 0.42f, 0.36f));
                m.SetFloat("_AccentAmount", accentAmount);
                m.SetFloat("_EmissionStrength", Mathf.Min(emission, 1.2f));
                m.SetFloat("_CorruptionScale", 0.9f);
            }
            else
            {
                m.color = light;
            }
            return m;
        }

        static void Clear(Transform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i);
                Object.Destroy(child.gameObject);
            }
        }
    }
}
