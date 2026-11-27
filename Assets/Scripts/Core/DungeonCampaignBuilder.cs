using System;
using System.Collections.Generic;
using UnityEngine;

namespace EchoIslands
{
    /// <summary>
    /// Builds large, deterministic, replayable dungeon courses from original LowPolyDungeon
    /// assets. No custom external package, addressables or scene-load dependency is required.
    /// All measurements below are metres (Unity units).
    /// </summary>
    public static class DungeonCampaignBuilder
    {
        static Material stone, floor, metal, wood, platformTile, cyan, magenta, lava, gold, ice;
        static Mesh spikeMesh;
        static readonly Dictionary<string, GameObject> props = new Dictionary<string, GameObject>();
        static readonly Vector3[] Origins = {
            Vector3.zero, Vector3.zero, new Vector3(480f, 0f, 0f),
            new Vector3(1020f, 0f, 0f), new Vector3(1570f, 0f, 0f),
            new Vector3(2140f, 0f, 0f)
        };

        static void Materials()
        {
            if (stone != null) return;
            // Original LowPolyDungeon prefabs keep their own atlas UVs and materials.
            // Large primitive walls/floors MUST NOT map the entire asset atlas onto a cube.
            stone = TileMat("Vault carved blocks", "StoneBlocks", new Color(.77f,.83f,.91f), .44f);
            floor = TileMat("Worn stone pavers", "StonePavers", new Color(.80f,.86f,.93f), .58f);
            metal = TileMat("Aged dark iron", "DarkIron", new Color(.76f,.83f,.9f), 1.1f);
            wood = TileMat("Oak boards", "OakBoards", new Color(.87f,.87f,.86f), 1.7f);
            platformTile = TileMat("Etched iron stepping tiles", "RuneTiles", new Color(.97f,.97f,1f), 0.85f);
            cyan = Mat("Aether Cyan", new Color(.055f,.76f,.74f), 1.65f);
            magenta = Mat("Aether Rose", new Color(.75f,.21f,.76f), 1.3f);
            gold = Mat("Ancient Gold", new Color(.86f,.62f,.29f), .65f);
            lava = Mat("Molten stone", new Color(.98f,.24f,.06f), 1.8f);
            ice = TileMat("Frosted carved ice tiles", "RuneTiles", new Color(.58f,.87f,1f), .80f);
            if (ice.HasProperty("_Glossiness")) ice.SetFloat("_Glossiness", .72f);
        }

        static Material TileMat(string title, string textureName, Color tint, float tilesPerMetre)
        {
            Shader shader = Resources.Load<Material>("EchoShaderKeep/DungeonStone").shader;
            var mat = new Material(shader) { name = title };
            var texture = Resources.Load<Texture2D>("EchoDungeon/Textures/" + textureName);
            if (texture != null) { mat.SetTexture("_MainTex", texture); texture.wrapMode = TextureWrapMode.Repeat; }
            var shadowTexture = Resources.Load<Texture2D>("EchoDungeon/Textures/" + textureName + "_Dark");
            if (mat.HasProperty("_DarkTex")) mat.SetTexture("_DarkTex", shadowTexture != null ? shadowTexture : texture);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", tint);
            if (mat.HasProperty("_DarkColor")) mat.SetColor("_DarkColor", new Color(.84f,.90f,1f,1f));
            if (mat.HasProperty("_TileScale")) mat.SetFloat("_TileScale", tilesPerMetre);
            if (mat.HasProperty("_DarkGlow")) mat.SetFloat("_DarkGlow", textureName == "RuneTiles" ? .48f : .12f);
            return mat;
        }

        static Material Mat(string name, Color tint, float glow = 0f)
        {
            Shader s = Shader.Find("Standard") ?? Shader.Find("Unlit/Color");
            var mat = new Material(s) { name = name };
            if (mat.HasProperty("_Color")) mat.color = tint;
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", tint * glow);
            }
            return mat;
        }

        static GameObject Box(Transform root, string name, Vector3 position, Vector3 scale, Material mat, WorldPresence phase = WorldPresence.Both)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(root, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null) renderer.sharedMaterial = mat;
            if (phase != WorldPresence.Both)
            {
                var variant = go.AddComponent<WorldVariant>();
                variant.presence = phase;
                variant.fadeDuration = 0.48f;
                variant.colliderThreshold = 0.24f;
                variant.scaleAnimMaxExtent = 0f; // full-size collision geometry while material fades
                variant.useSoftFade = true;
                variant.useTransitionWave = false; // deterministic collider timing for precise jumps
                variant.Refresh();
            }
            return go;
        }

        static GameObject Prop(Transform root, string name, Vector3 localPos, Vector3 rotation, float scale = 1f)
        {
            if (!props.TryGetValue(name, out var prefab))
            {
                prefab = Resources.Load<GameObject>("EchoDungeon/" + name);
                props[name] = prefab;
            }
            if (prefab == null) return null;
            var go = UnityEngine.Object.Instantiate(prefab, root, false);
            go.name = "Asset_" + name;
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(rotation);
            go.transform.localScale = Vector3.one * scale;
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) c.enabled = false;
            return go;
        }

        static Transform Empty(Transform root, string name, Vector3 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            go.transform.localPosition = pos;
            return go.transform;
        }

        static void Glow(Transform root, string name, Vector3 p, Color tint, float strength = 1.6f, float range = 9f)
        {
            var lightRoot = Empty(root, name, p);
            var l = lightRoot.gameObject.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = tint;
            l.intensity = strength;
            l.range = range;
            l.shadows = LightShadows.None;
        }

        static void Hazard(GameObject go)
        {
            var collider = go.GetComponent<Collider>();
            if (collider != null) collider.isTrigger = true;
            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            go.AddComponent<DungeonHazard>();
        }

        // Actual pointed spikes rather than featureless lethal boxes. Collision is a
        // small trigger *inside* the mesh; the player sees the real pointed hazard.
        static Mesh SpikeMesh()
        {
            if (spikeMesh != null) return spikeMesh;
            var mesh = new Mesh { name = "Four-sided forged iron spike" };
            var vertices = new Vector3[12];
            Vector3 top = new Vector3(0f, 1.18f, 0f);
            Vector3[] baseRing = {
                new Vector3(-.43f,0f,-.43f), new Vector3(.43f,0f,-.43f),
                new Vector3(.43f,0f,.43f), new Vector3(-.43f,0f,.43f)
            };
            for (int i = 0; i < 4; i++)
            {
                vertices[i*3] = baseRing[i];
                vertices[i*3+1] = baseRing[(i+1)%4];
                vertices[i*3+2] = top;
            }
            mesh.vertices = vertices;
            mesh.triangles = new int[] { 0,1,2, 3,4,5, 6,7,8, 9,10,11 };
            var uv = new Vector2[12];
            for(int i=0;i<4;i++)
            {
                uv[i*3]=new Vector2(0f,0f);
                uv[i*3+1]=new Vector2(1f,0f);
                uv[i*3+2]=new Vector2(.5f,1f);
            }
            mesh.uv=uv;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            spikeMesh=mesh;
            return mesh;
        }

        static void ForgeSpike(Transform root, Vector3 basePos, float height = .85f)
        {
            var pivot = Empty(root, "Forged pointed death spike", basePos);
            // A modest trigger avoids unfair collisions before the pointed object.
            var trigger = pivot.gameObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f,.33f,0f);
            trigger.size = new Vector3(.65f,.70f,.65f);
            var rigid = pivot.gameObject.AddComponent<Rigidbody>();
            rigid.isKinematic = true;
            rigid.useGravity = false;
            pivot.gameObject.AddComponent<DungeonHazard>();
            var body = Empty(pivot, "Triangular iron spike mesh", Vector3.zero);
            body.localScale = new Vector3(.82f, height, .82f);
            var meshFilter = body.gameObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh=SpikeMesh();
            body.gameObject.AddComponent<MeshRenderer>().sharedMaterial=metal;
            var socket=Box(pivot,"Spike socket",new Vector3(0f,.035f,0f),
                new Vector3(.82f,.07f,.82f),platformTile);
            var socketCollider=socket.GetComponent<Collider>();
            if(socketCollider!=null)socketCollider.enabled=false;
        }

        static void Pit(Transform root, float zCenter, float length, float width = 14f)
        {
            var go = Empty(root, "Lethal abyss", new Vector3(0f, -6.8f, zCenter));
            var c = go.gameObject.AddComponent<BoxCollider>();
            c.isTrigger = true;
            c.size = new Vector3(width, 3f, length);
            go.gameObject.AddComponent<DungeonHazard>();
        }

        static GameObject Exhibit(Transform root, string modelName, Vector3 center, Vector3 size, float yaw = 0f, float lean = 0f)
        {
            var go = Prop(root, modelName, center, new Vector3(0f,yaw,0f));
            if (go == null) return null;
            var meshes = go.GetComponentsInChildren<MeshRenderer>();
            if (meshes.Length == 0) return go;
            Bounds model = new Bounds();
            bool first = true;
            foreach (var mesh in meshes)
            {
                var bounds = mesh.localBounds;
                for (int i=0;i<8;i++)
                {
                    var sign = new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1);
                    Vector3 corner = mesh.transform.TransformPoint(bounds.center+Vector3.Scale(bounds.extents,sign));
                    Vector3 point = go.transform.InverseTransformPoint(corner);
                    if (first) { model = new Bounds(point,Vector3.zero); first=false; }
                    else model.Encapsulate(point);
                }
            }
            Vector3 target = Mathf.Abs(Mathf.Sin(yaw * Mathf.Deg2Rad)) > .9f
                ? new Vector3(size.z,size.y,size.x) : size;
            Vector3 local = model.size;
            go.transform.localScale = new Vector3(target.x/Mathf.Max(.01f,local.x),
                target.y/Mathf.Max(.01f,local.y), target.z/Mathf.Max(.01f,local.z));
            go.transform.localRotation *= Quaternion.Euler(0f,0f,lean);
            var fitted = meshes[0].bounds;
            foreach (var mesh in meshes) fitted.Encapsulate(mesh.bounds);
            go.transform.position += root.TransformPoint(center) - fitted.center;
            return go;
        }

        static void Alcove(Transform root, Vector3 center, float yaw, int index)
        {
            var bay = Empty(root,"Recessed dungeon arcade",center);
            bay.localRotation = Quaternion.Euler(0f,yaw,0f);
            var light = Empty(bay,"Intact stonework",Vector3.zero);
            var dark = Empty(bay,"Collapsed stonework",Vector3.zero);
            Exhibit(light,"Dungeon_Custom_Border_Arched",new Vector3(0f,2.1f,0f),new Vector3(5f,4.2f,.46f));
            Exhibit(light,index%2==0?"Jail_Wall":"Dungeon_Wall_Window",new Vector3(0f,1.85f,.24f),new Vector3(3.9f,3.65f,.25f));
            Exhibit(light,index%2==0?"Shield":"Banner",new Vector3(-2f,2.6f,-.30f),new Vector3(.75f,1.35f,.16f));
            Exhibit(dark,"Jail_Wall_broken",new Vector3(.25f,1.45f,0f),new Vector3(4.5f,2.8f,.5f),0f,-8f);
            Exhibit(dark,"Pillar",new Vector3(-1.9f,.8f,-.18f),new Vector3(.65f,1.65f,.65f),0f,13f);
            Exhibit(dark,"Plant_Root",new Vector3(1.3f,2.4f,-.24f),new Vector3(1.9f,2.5f,.37f),0f,-27f);
            Exhibit(dark,"Skull",new Vector3(-1.1f,.22f,-.46f),new Vector3(.48f,.42f,.56f),25f);
            var swap = bay.gameObject.AddComponent<DungeonRuin>();
            swap.intact = light.gameObject;
            swap.ruined = dark.gameObject;
            bool cursed = WorldState.Instance != null && WorldState.Instance.Current == WorldKind.Dark;
            light.gameObject.SetActive(!cursed);
            dark.gameObject.SetActive(cursed);
        }

        static void DecorateHall(Transform root, float z, float width, int index)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                for (int bay = 0; bay < 3; bay++)
                    Alcove(root,new Vector3(side*(width*.5f-.75f),0f,z+4f+bay*11.5f),
                        side<0?-90f:90f,index+bay);
                Prop(root, "Pillar", new Vector3(side * (width * .5f - .48f), .1f, z + .3f), Vector3.zero, .92f);
                Prop(root, "Dungeon_Hall_Wall",new Vector3(side*(width*.5f+.02f),0f,z+7.0f),
                    new Vector3(0f,side<0?90f:-90f,0f),.9f);
                if ((index & 1) == 0)
                    Prop(root, "Torch_Wall", new Vector3(side * (width * .5f - .35f), 2.1f, z + 4.6f),
                        new Vector3(0f, side < 0 ? 90f : -90f, 0f), .9f);
            }
            Glow(root, "Vault sconce " + index, new Vector3(index % 2 == 0 ? -width*.43f : width*.43f, 2.8f, z+3.6f),
                index%3 == 0 ? new Color(.98f,.60f,.39f) : new Color(.27f,.89f,.86f), 2.4f, 13f);
            if(index%3 == 0)Prop(root,"Chandelier",new Vector3(0f,4.85f,z+9f),Vector3.zero,.67f);
        }

        static void Frame(Transform root, float z, float width = 12f)
        {
            Box(root, "Carved arch L", new Vector3(-width*.5f+.45f,2.15f,z), new Vector3(.9f,4.3f,.85f),stone);
            Box(root, "Carved arch R", new Vector3(width*.5f-.45f,2.15f,z), new Vector3(.9f,4.3f,.85f),stone);
            const int stones = 12;
            for (int i=0; i<stones; i++)
            {
                float a = Mathf.PI*i/stones, b = Mathf.PI*(i+1)/stones;
                Vector3 left = new Vector3(Mathf.Cos(a)*(width*.5f-.5f),2.25f+Mathf.Sin(a)*2.8f,z);
                Vector3 right = new Vector3(Mathf.Cos(b)*(width*.5f-.5f),2.25f+Mathf.Sin(b)*2.8f,z);
                Vector3 chord = right-left;
                var block = Box(root,"Vault rib voussoir",(left+right)*.5f,
                    new Vector3(chord.magnitude+.07f,.44f,.82f),stone);
                block.transform.localRotation = Quaternion.Euler(0f,0f,Mathf.Atan2(chord.y,chord.x)*Mathf.Rad2Deg);
                block.GetComponent<Collider>().enabled=false;
            }
        }

        static void SavePad(Transform root, float z, int number, float x = 0f)
        {
            var rootMark = Empty(root, "Checkpoint_" + number, new Vector3(x, .95f, z));
            var trigger = rootMark.gameObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(6f, 2f, 1.4f);
            var script = rootMark.gameObject.AddComponent<CheckpointTrigger>();
            script.checkpoint = Empty(root, "CheckpointRespawn_" + number, new Vector3(x,1.3f,z+1.6f));
            // Both non-blocking visual plaques rest ABOVE the existing floor.
            var carved = Box(root,"Checkpoint textured plaque",new Vector3(x,.035f,z),
                new Vector3(2.5f,.07f,1.25f),platformTile);
            var rune = Box(root,"Checkpoint glowing rune",new Vector3(x,.077f,z),
                new Vector3(1.9f,.015f,.30f),cyan);
            var carvedCollider = carved.GetComponent<Collider>();
            if (carvedCollider != null) carvedCollider.enabled=false;
            var runeCollider = rune.GetComponent<Collider>();
            if (runeCollider != null) runeCollider.enabled=false;
            Glow(root,"Checkpoint glow",new Vector3(x,.9f,z),new Color(.15f,.9f,.78f),1.5f,5f);
        }

        static void StairDetail(Transform root, float x, float z, WorldPresence phase)
        {
            Box(root,"Rune plate",new Vector3(x,-.06f,z),new Vector3(1.7f,.07f,1.3f),phase==WorldPresence.Light?magenta:cyan,phase);
        }

        static Orb PlaceOrb(Transform root, Vector3 localPos)
        {
            var pedestal = Box(root,"Spectral sphere pedestal",new Vector3(localPos.x,.36f,localPos.z),new Vector3(1.6f,.72f,1.6f),stone);
            Box(root,"Spectral gilding",new Vector3(localPos.x,.75f,localPos.z),new Vector3(1.3f,.12f,1.3f),gold);
            var go = new GameObject("Collectible spectral particle sphere");
            go.transform.SetParent(root, false);
            go.transform.localPosition = localPos;
            var collider = go.AddComponent<SphereCollider>();
            collider.radius = .65f;
            collider.isTrigger = true;
            var orb = go.AddComponent<Orb>();
            orb.home = WorldKind.Dark;
            var world = go.AddComponent<WorldVariant>();
            world.presence = WorldPresence.Dark;
            world.Refresh();
            Glow(root,"Spectral orb backlight",localPos + Vector3.up*.15f,new Color(.08f,.92f,.80f),1.25f,4f);
            return orb;
        }

        static PortalFrame PlacePortal(Transform root, Vector3 pos, int next)
        {
            Box(root,"Gate entrance dais",new Vector3(pos.x,-.36f,pos.z),new Vector3(7f,.7f,7f),floor);
            var gate = new GameObject("Exit Portal -> level " + next);
            gate.transform.SetParent(root,false);
            gate.transform.localPosition=pos;
            var portal = gate.AddComponent<PortalFrame>();
            portal.requiredOrbs = 1;
            portal.nextLevel = next;
            portal.completeOnEnter = true;
            // EchoVisualOverhaul supplies the animated rift; the stone arch remains visible.
            Box(gate.transform,"Portal pillar left",new Vector3(-1.4f,1.8f,0f),new Vector3(.68f,3.8f,.85f),stone);
            Box(gate.transform,"Portal pillar right",new Vector3(1.4f,1.8f,0f),new Vector3(.68f,3.8f,.85f),stone);
            Box(gate.transform,"Portal crown",new Vector3(0f,3.7f,0f),new Vector3(3.5f,.62f,.95f),gold);
            Box(gate.transform,"Portal left rune",new Vector3(-1.4f,2.35f,-.48f),new Vector3(.26f,1.7f,.08f),cyan);
            Box(gate.transform,"Portal right rune",new Vector3(1.4f,2.35f,-.48f),new Vector3(.26f,1.7f,.08f),cyan);
            var socketGo = Empty(gate.transform, "PortalOrbSocket", new Vector3(2.4f,1.35f,-1.3f));
            var socket = socketGo.gameObject.AddComponent<OrbSocket>();
            socket.socket=socketGo;
            socket.targetPortal=portal;
            Box(gate.transform,"Socket plinth",new Vector3(2.4f,.4f,-1.3f),new Vector3(1.0f,.78f,1f),gold);
            var col = gate.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.center = new Vector3(0f,1.7f,0f);
            col.radius = 2.6f;
            EchoVisualOverhaul.ReskinPortal(portal);
            return portal;
        }

        public static GameObject Build(int number, out Transform spawn)
        {
            Materials();
            number = Mathf.Clamp(number,2,5);
            var root = new GameObject("Level_" + number + "_COMPLETE_DUNGEON");
            root.transform.position = Origins[number];
            spawn = Empty(root.transform,"StartSpawn",new Vector3(0f,1.3f,2.2f));
            switch(number)
            {
                case 2: BuildParkour(root.transform,2); break;
                case 3: BuildGravity(root.transform); break;
                case 4: BuildMaze(root.transform); break;
                case 5: BuildParkour(root.transform,5); break;
            }
            root.AddComponent<WorldSurfaceSkins>();
            root.SetActive(false);
            return root;
        }

        // A landing/jump takes ~0.55 s. At the first phase-only tile the player
        // can prepare a swap; during the run, a swap timed near take-off lets the
        // next platform finish materialising before landing. Safe tiles at both
        // ends and world-neutral checkpoints prevent unavoidable softlocks.
        static WorldPresence ParkourPresence(bool clockCourse, int course, int index, int section)
        {
            if (index == 0 || index == 5) return WorldPresence.Both;
            if (clockCourse)
            {
                // Clock requires a temporary Dark-world sprint. Alternate short
                // dark clusters with normal tiles: players choose when to press Q.
                if (index == 4 && course % 2 == 0) return WorldPresence.Light;
                if (course == 0 || course == 4) // mixed/ice rhythm
                    return index == 2 || index == 3 ? WorldPresence.Dark : WorldPresence.Both;
                if (course == 3) // narrow, one world change
                    return index >= 3 ? WorldPresence.Dark : WorldPresence.Both;
                return WorldPresence.Dark; // tight time trial through 1..4
            }
            switch (course)
            {
                case 0: // ordinary jumps: pacing break and movement warmup
                    return WorldPresence.Both;
                case 1: // moving platforms + a mid-flight swap
                    return index == 2 || index == 3 ? WorldPresence.Dark : WorldPresence.Both;
                case 2: // signature timing combination: Light/Dark/Light/Dark
                case 6: // finale: switch mid-jump in repeated pairs
                    return index % 2 == 0 ? WorldPresence.Dark : WorldPresence.Light;
                case 3: // narrow beams: pure footwork (no phase requirement)
                case 4: // ice: controlling momentum without swapping
                    return WorldPresence.Both;
                case 5: // traps with alternating colour-coded bridges
                    return index % 2 == 0 ? WorldPresence.Dark : WorldPresence.Light;
                default: return WorldPresence.Both;
            }
        }

        static void BuildParkour(Transform root, int level)
        {
            bool timed = level == 5;
            int sections = timed ? 28 : 26;
            float start = 12f, span = 37f, width = 12f;
            Box(root,"Entrance floor",new Vector3(0f,-.35f,5f),new Vector3(width,.7f,14f),floor);
            Box(root,"Entrance back wall",new Vector3(0f,2.2f,-2f),new Vector3(width,4.4f,.7f),stone);
            for(int side = -1;side<=1;side+=2)
                Box(root,"Entrance wall",new Vector3(side*width*.5f,2.15f,5f),new Vector3(.7f,4.3f,14f),stone);
            Box(root,"Entrance vault ceiling",new Vector3(0f,5.5f,5f),new Vector3(width,.7f,14f),stone);
            Glow(root,"Entrance chandelier",new Vector3(0f,3.8f,4f),new Color(.98f,.67f,.48f),3f,12f);
            Prop(root,"Chandelier",new Vector3(0f,3.65f,4f),Vector3.zero,.8f);

            if(timed)
            {
                // The clock is the ONLY world-switching tool on level five.
                var clock = Box(root,"Chronometer pickup",new Vector3(0f,1.5f,6f),new Vector3(.42f,.42f,.22f),gold);
                clock.transform.localScale = Vector3.one;
                clock.GetComponent<MeshRenderer>().enabled = false;
                clock.GetComponent<BoxCollider>().size = new Vector3(.6f,.7f,.22f);
                var face = Empty(clock.transform,"Engraved pocket watch",Vector3.zero);
                face.localScale = Vector3.one*1.4f;
                ToolController.BuildWatchFace(face);
                var pickup=clock.AddComponent<ToolPickup>();pickup.kind=ToolKind.Clock;pickup.hint="E - взять часы";
                Box(root,"Clock pedestal",new Vector3(0f,.42f,6f),new Vector3(1.2f,.84f,1.2f),stone);
            }

            for (int s=0;s<sections;s++)
            {
                float z=start+s*span;
                Box(root,"Safe entrance #"+s,new Vector3(0f,-.3f,z+2.8f),new Vector3(width,.6f,6.6f),floor);
                Box(root,"Safe landing #"+s,new Vector3(0f,-.3f,z+34.6f),new Vector3(width,.6f,6.4f),floor);
                for(int side=-1;side<=1;side+=2)
                    Box(root,"Abyss side wall",new Vector3(side*width*.5f,2.45f,z+18.5f),new Vector3(.6f,4.9f,37.2f),stone);
                Box(root,"Dungeon vault roof",new Vector3(0f,5.5f,z+18.5f),new Vector3(width,.7f,37.2f),stone);
                Frame(root,z+1.5f,width);
                DecorateHall(root,z,width,s);
                Pit(root,z+18.5f,span,width+3f);
                // Every stage has a solid checkpoint, not only every second stage.
                // The respawn point lies on the always-present exit floor.
                SavePad(root,z+34.4f,s+1);
                int course = s % 7;
                for(int i=0;i<6;i++)
                {
                    float x = course==3 ? (i%2==0?-1.15f:1.15f)
                        : i%3==1 ? 1.0f : i%3==2 ? -1.0f : 0f;
                    float pz = z+7.0f+i*4.95f;
                    float platformWidth = course==3 ? 1.60f : course==6 ? 2.08f : 2.40f;
                    // Obby rhythm: permanent platforms for preparation, then brief
                    // Light -> Dark -> Light timing runs. Crucially, NOT every stage
                    // is an alternating-tile challenge: moving, icy, narrow and trap
                    // courses remain genuinely different types of obstacle.
                    WorldPresence phase = ParkourPresence(timed, course, i, s);
                    var platform=Box(root,"Obby platform S"+s+" P"+i,
                        new Vector3(x,-.12f,pz),new Vector3(platformWidth,.42f,course==4 ? 3.0f : 2.8f),
                        platformTile,phase);
                    if(course==1 && (i==2||i==4))
                    {
                        var mover=platform.AddComponent<MovingDungeonPlatform>();
                        mover.travel = i == 2 ? new Vector3(3.1f,0f,0f) : new Vector3(0f,0f,2.25f);
                        mover.period = i == 2 ? 5.8f : 4.8f;
                    }
                    if(course==4 && (i==0||i==2||i==4))
                    {
                        // Ice physics lives on its trigger; base remains an ordinary solid tile.
                        // A separate trigger detects ice, leaving the platform collider solid.
                        var iceMark=Empty(platform.transform,"Ice trigger",Vector3.zero);
                        var iceCollider=iceMark.gameObject.AddComponent<BoxCollider>();
                        iceCollider.size=new Vector3(1f,1.4f,1f);
                        iceCollider.center=new Vector3(0f,.62f,0f);
                        iceCollider.isTrigger=true;
                        iceMark.gameObject.AddComponent<IcePatch>();
                        var r=platform.GetComponent<MeshRenderer>();if(r!=null)r.sharedMaterial=ice;
                    }
                }
                if(course==4 || course==6)
                {
                    var beam=Box(root,"Rotating lethal blade",new Vector3(0f,1.35f,z+19.8f),new Vector3(5.4f,.26f,.35f),metal);
                    Hazard(beam);
                    beam.AddComponent<SpinningDungeonHazard>().degreesPerSecond=course==4?80f:125f;
                    Box(root,"Blade housing",new Vector3(0f,.8f,z+19.8f),new Vector3(.5f,1.6f,.5f),gold);
                }
                if(course==5)
                {
                    Hazard(Box(root,"Molten pit below the bridge",new Vector3(0f,-5.8f,z+18.4f),new Vector3(width-1f,.34f,16f),lava));
                    // The landing has visible spikes, with an escape lane at either wall.
                    for(int j=0;j<5;j++)
                        ForgeSpike(root,new Vector3((j-2)*1.25f,0f,z+33.85f),.93f);
                }
                if(course==6)
                {
                    var falling=Box(root,"Falling ceiling block",new Vector3(.55f,4.0f,z+32.0f),new Vector3(2.1f,1.1f,2.1f),stone);
                    Hazard(falling);
                    falling.AddComponent<FallingDungeonBlock>();
                }
                if(!timed && (course==2||course==6))
                    StairDetail(root,0f,z+3.4f,course==2?WorldPresence.Dark:WorldPresence.Light);
            }
            float end = start + sections*span;
            FinalChamber(root,end,width,level+1);
        }

        static void FinalChamber(Transform root,float z,float width,int next)
        {
            Box(root,"Final gallery floor",new Vector3(0f,-.3f,z+12f),new Vector3(width,.6f,27f),floor);
            for(int side=-1;side<=1;side+=2)
                Box(root,"Final gallery wall",new Vector3(side*width*.5f,2.3f,z+12f),new Vector3(.6f,4.6f,27f),stone);
            Box(root,"Final back wall",new Vector3(0f,2.3f,z+25.5f),new Vector3(width,4.6f,.7f),stone);
            Box(root,"Final vault ceiling",new Vector3(0f,5.5f,z+12f),new Vector3(width,.7f,27f),stone);
            Frame(root,z+4f,width);
            DecorateHall(root,z-4f,width,17);
            Prop(root,"CrystalBall_Stand",new Vector3(-2.4f,0f,z+11f),Vector3.zero,1f);
            Prop(root,"Cauldron_Full",new Vector3(4.0f,.0f,z+7.0f),Vector3.zero,.9f);
            Prop(root,"Book_Open",new Vector3(-4.1f,.68f,z+5.0f),Vector3.zero,.8f);
            Prop(root,"Chandelier",new Vector3(0f,4.95f,z+16f),Vector3.zero,1.0f);
            PlaceOrb(root,new Vector3(-2.4f,1.63f,z+11f));
            PlacePortal(root,new Vector3(0f,0f,z+21f),next);
            Glow(root,"Final vault beacon",new Vector3(0f,3.3f,z+18f),new Color(.12f,1f,.78f),3.5f,14f);
            SavePad(root,z+3.0f,99);
        }

        static Vector3 FaceUp(GravitySide side)
        {
            switch (side)
            {
                case GravitySide.LeftWall: return Vector3.right;
                case GravitySide.RightWall: return Vector3.left;
                case GravitySide.Ceiling: return Vector3.down;
                default: return Vector3.up;
            }
        }

        static Vector3 FacePoint(GravitySide side, float z)
        {
            Vector3 up = FaceUp(side);
            return new Vector3(0f,4f,z) - up * (up.x == 0f ? 4f : 5.5f);
        }

        static void GravityAnchor(Transform root, GravitySide from, GravitySide to,
            float z, float nextZ, WorldKind world)
        {
            Vector3 up = FaceUp(from);
            var mark = Box(root,"Gravity seal",FacePoint(from,z)+up*.035f,
                new Vector3(1.7f,.07f,1.45f),world==WorldKind.Light?gold:cyan);
            mark.transform.localRotation = Quaternion.LookRotation(Vector3.forward,up);
            mark.GetComponent<Collider>().enabled=false;
            var pad = mark.AddComponent<GravityPad>();
            pad.source = from;
            pad.destination = to;
            pad.requiredWorld = world;
            pad.landingPosition = root.TransformPoint(FacePoint(to,nextZ) + FaceUp(to)*.08f);
        }

        static void BuildGravity(Transform root)
        {
            const int circuits = 9, tiles = 5;
            const float stride = 4.7f, start = 12f;
            Box(root,"Temple threshold",new Vector3(0f,-.3f,4.9f),new Vector3(10f,.6f,14f),floor);
            Box(root,"Temple entry wall",new Vector3(0f,2.5f,-2f),new Vector3(11f,5f,.6f),stone);
            for (int leg = 0; leg < circuits * 4; leg++)
            {
                var side = (GravitySide)(leg % 4);
                var next = (GravitySide)((leg + 1) % 4);
                Vector3 up = FaceUp(side);
                Quaternion rotation = Quaternion.LookRotation(Vector3.forward,up);
                float z = start + leg*tiles*stride;
                WorldKind gateWorld = leg%2==0 ? WorldKind.Dark : WorldKind.Light;
                for (int i=0; i<tiles; i++)
                {
                    float station = z+i*stride;
                    WorldPresence phase = WorldPresence.Both;
                    if (i==1) phase = leg%2==0 ? WorldPresence.Light : WorldPresence.Dark;
                    if (i==3) phase = leg%2==0 ? WorldPresence.Dark : WorldPresence.Light;
                    Vector3 center = FacePoint(side,station)-up*.25f;
                    if (i==1 || i==3) center += rotation*Vector3.right*(i==1?.65f:-.65f);
                    var tile = Box(root,"Temple stepping stone",center,
                        new Vector3(i==2?2.2f:3.5f,.5f,3.15f),
                        phase==WorldPresence.Both?floor:platformTile,phase);
                    tile.transform.localRotation = rotation;
                    if (i==2 && leg%5==3)
                    {
                        var blade=Box(root,"Temple rotating arm",FacePoint(side,station)+up*.48f,
                            new Vector3(2.9f,.16f,.18f),metal);
                        blade.transform.localRotation=rotation;
                        Hazard(blade);
                        blade.AddComponent<SpinningDungeonHazard>().degreesPerSecond=64f;
                    }
                }
                if (side==GravitySide.Floor && leg>0) SavePad(root,z-.6f,leg/4);
                GravityAnchor(root,side,next,z+(tiles-1)*stride,z+tiles*stride,gateWorld);
                // Shell meshes are scenery. Missing a stone must also be a fall on walls and ceiling.
                for (int sign=-1; sign<=1; sign+=2)
                {
                    Exhibit(root,"Dungeon_Wall_Window",new Vector3(sign*9.2f,4f,z+9f),
                        new Vector3(.7f,11f,tiles*stride),90f*sign);
                    Alcove(root,new Vector3(sign*8.4f,1.5f,z+8f),sign*90f,leg);
                }
                Exhibit(root,"Dungeon_Custom_Border_Arched",new Vector3(0f,10.8f,z+7f),
                    new Vector3(17f,2.4f,1.3f));
                Glow(root,"Temple lantern",FacePoint(side,z+8f)+up*2.7f,
                    leg%2==0?new Color(.98f,.64f,.32f):new Color(.35f,.69f,.81f),2f,14f);
            }
            float exit = start+circuits*4*tiles*stride;
            FinalChamber(root,exit,12f,4);
        }

        static void BuildMaze(Transform root)
        {
            const int n=12;
            const float step=9f, height=5.6f;
            float half=(n-1)*step*.5f;
            Box(root,"The endless labyrinth stone floor",new Vector3(0f,-.32f,half),new Vector3(n*step+.8f,.64f,n*step+.8f),floor);
            Box(root,"Labyrinth vaulted ceiling",new Vector3(0f,5.95f,half),new Vector3(n*step+.8f,.65f,n*step+.8f),stone);
            var rng=new System.Random(4124);
            bool[,,] open=new bool[n,n,4];
            bool[,] used=new bool[n,n];
            int[] dx={0,1,0,-1},dz={1,0,-1,0};
            var stack=new Stack<Vector2Int>();
            var ancestry=new Dictionary<Vector2Int,Vector2Int>();
            var start=new Vector2Int(0,0);
            stack.Push(start);used[0,0]=true;
            while(stack.Count>0)
            {
                var top=stack.Peek();
                var dirs=new List<int>();
                for(int i=0;i<4;i++)
                {
                    int x=top.x+dx[i],z=top.y+dz[i];
                    if(x>=0&&x<n&&z>=0&&z<n&&!used[x,z]) dirs.Add(i);
                }
                if(dirs.Count==0){stack.Pop();continue;}
                int choice=dirs[rng.Next(dirs.Count)];
                int tx=top.x+dx[choice],tz=top.y+dz[choice];
                open[top.x,top.y,choice]=true;
                open[tx,tz,(choice+2)%4]=true;
                used[tx,tz]=true;
                var next=new Vector2Int(tx,tz);
                ancestry[next]=top;
                stack.Push(next);
            }
            var path=new List<Vector2Int>();
            Vector2Int curr=new Vector2Int(n-1,n-1);
            path.Add(curr);
            while(curr!=start && ancestry.TryGetValue(curr,out var parent))
            {
                curr=parent;path.Add(curr);
            }
            path.Reverse();
            // Doors on carved passages are phased; solid masonry is permanent.
            for(int x=0;x<n;x++)for(int z=0;z<n;z++)
            {
                Vector3 c=new Vector3(x*step-half,0f,z*step);
                if(x==0)Box(root,"Maze west wall",c+new Vector3(-step*.5f,height*.5f,0f),new Vector3(.65f,height,step+.1f),stone);
                if(z==0)Box(root,"Maze south wall",c+new Vector3(0f,height*.5f,-step*.5f),new Vector3(step+.1f,height,.65f),stone);
                for(int dir=0;dir<2;dir++)
                {
                    int nx=x+dx[dir],nz=z+dz[dir];
                    Vector3 offset=dir==0?new Vector3(0f,height*.5f,step*.5f):new Vector3(step*.5f,height*.5f,0f);
                    Vector3 dims=dir==0?new Vector3(step+.08f,height,.65f):new Vector3(.65f,height,step+.08f);
                    if(nx>=n||nz>=n)
                    { Box(root,"Maze border",c+offset,dims,stone);continue; }
                    if(!open[x,z,dir])
                    {
                        Box(root,"Unbreakable maze wall",c+offset,dims,stone);
                        if ((x + 2*z + dir) % 3 == 0)
                            Alcove(root,c+offset-Vector3.up*(height*.5f)
                                - (dir==0?Vector3.forward:Vector3.right)*.61f,dir==0?0f:90f,x+z);
                    }
                    else if((x*17+z*11+dir)%5!=0)
                    {
                        WorldPresence phase=(x+z+dir)%2==0?WorldPresence.Dark:WorldPresence.Light;
                        Box(root,"Phased labyrinth doorway",c+offset,dims,phase==WorldPresence.Dark?cyan:magenta,phase);
                        // A small visible rune at the seam helps players identify shifted walls.
                        Box(root,"Maze rune lintel",c+offset+Vector3.up*2.25f,
                            dir==0?new Vector3(2.4f,.17f,.9f):new Vector3(.9f,.17f,2.4f),gold);
                    }
                }
                if((x+z*3)%4==0)
                {
                    Prop(root,"Pillar",c+new Vector3(-3f,.02f,-3f),Vector3.zero,.8f);
                    if((x+z)%2==0)
                    {
                        Glow(root,"Labyrinth lantern",c+new Vector3(-3f,3f,-3f),
                            (x+z)%3==0?new Color(.9f,.55f,.40f):new Color(.15f,.83f,.73f),1.8f,8.3f);
                        Prop(root,"Torch_Wall",c+new Vector3(-3f,2.3f,-3.3f),Vector3.zero,.75f);
                    }
                }
                if((x+z*5)%19==0)
                    Prop(root,"Amphora",c+new Vector3(2.8f,.04f,-2.8f),Vector3.zero,.8f);
            }
            // A long unique route exists by construction. The orb is on that route,
            // before the portal, so no random maze can make the game impossible.
            int orbIndex=Mathf.Clamp(path.Count*3/4,3,path.Count-2);
            Vector2Int orbCell=path[orbIndex];
            PlaceOrb(root,new Vector3(orbCell.x*step-half,1.55f,orbCell.y*step));
            Vector2Int end=path[path.Count-1];
            PlacePortal(root,new Vector3(end.x*step-half,0f,end.y*step+1.1f),5);
            // Savepoints on the real solution, with at least one per long stretch.
            for(int i=7;i<path.Count-2;i+=8)
            {
                var cell=path[i];
                SavePad(root,cell.y*step,100+i,cell.x*step-half);
            }
            var spawn= root.Find("StartSpawn");
            if(spawn!=null) spawn.localPosition=new Vector3(-half,1.3f,0f);
            Glow(root,"Maze entrance",new Vector3(-half,3f,0f),new Color(.98f,.75f,.44f),4f,10f);
        }
    }
}
