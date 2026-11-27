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
    public static partial class DungeonCampaignBuilder
    {
        static Material stone, floor, metal, wood, platformTile, cyan, gold, lava, ice, darkTile, lightTile;
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
            stone = TileMat("Vault carved blocks", "StoneBlocks", new Color(1f,.88f,.70f), .44f);
            floor = TileMat("Worn stone pavers", "StonePavers", new Color(.98f,.88f,.74f), .58f);
            metal = TileMat("Aged dark iron", "DarkIron", new Color(.76f,.83f,.9f), 1.1f);
            wood = TileMat("Oak boards", "OakBoards", new Color(.87f,.87f,.86f), 1.7f);
            platformTile = TileMat("Etched iron stepping tiles", "RuneTiles", new Color(.97f,.97f,1f), 0.85f);
            cyan = Mat("Aether Cyan", new Color(.055f,.76f,.74f), 1.65f);
            gold = Mat("Ancient Gold", new Color(.86f,.62f,.29f), .65f);
            lava = Mat("Molten stone", new Color(.98f,.24f,.06f), 1.8f);
            ice = TileMat("Frosted carved ice tiles", "RuneTiles", new Color(.58f,.87f,1f), .80f);
            lightTile = TileMat("Amber seal", "RuneTiles", new Color(1f,.76f,.42f), .84f);
            darkTile = TileMat("Cursed seal", "RuneTiles", new Color(.64f,.98f,1f), .84f);
            lightTile.SetColor("_DarkColor", new Color(.82f,.55f,.34f));
            darkTile.SetColor("_DarkColor", new Color(.30f,1f,.91f));
            if (ice.HasProperty("_Glossiness")) ice.SetFloat("_Glossiness", .72f);
        }

        static Material TileMat(string title, string textureName, Color tint, float tilesPerMetre)
        {
            var template = Resources.Load<Material>("EchoShaderKeep/DungeonStone");
            Shader shader = template != null ? template.shader : Shader.Find("Echo/DungeonTriplanar");
            var mat = new Material(shader) { name = title };
            var texture = Resources.Load<Texture2D>("EchoDungeon/Textures/" + textureName);
            if (texture != null) { mat.SetTexture("_MainTex", texture); texture.wrapMode = TextureWrapMode.Repeat; }
            var shadowTexture = Resources.Load<Texture2D>("EchoDungeon/Textures/" + textureName + "_Dark");
            if (mat.HasProperty("_DarkTex")) mat.SetTexture("_DarkTex", shadowTexture != null ? shadowTexture : texture);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", tint);
            if (mat.HasProperty("_DarkColor")) mat.SetColor("_DarkColor", new Color(.50f,.64f,.72f,1f));
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
            lightRoot.gameObject.AddComponent<DungeonSconce>().Configure(l, tint, strength);
        }

        static void Hazard(GameObject go)
        {
            var collider = go.GetComponent<Collider>();
            if (collider != null) collider.isTrigger = true;
            go.layer = 2;
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
            pivot.gameObject.layer = 2;
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

        static GameObject Exhibit(Transform root, string modelName, Vector3 center, Vector3 size, float yaw = 0f, float lean = 0f)
        {
            var go = Prop(root, modelName, center, new Vector3(0f,yaw,0f));
            if (go == null) return null;
            var meshes = go.GetComponentsInChildren<MeshRenderer>(true);
            if (meshes.Length == 0) return go;
            Bounds model = new Bounds();
            bool first = true;
            foreach (var mesh in meshes)
            {
                var filter = mesh.GetComponent<MeshFilter>();
                var bounds = filter != null && filter.sharedMesh != null ? filter.sharedMesh.bounds : mesh.localBounds;
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
            go.transform.position += root.TransformPoint(center) - go.transform.TransformPoint(model.center);
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
            Exhibit(dark,"Dungeon_Custom_Border_Arched",new Vector3(.95f,1.6f,.10f),new Vector3(3.3f,3f,.44f),0f,-22f);
            Exhibit(dark,"Skull",new Vector3(-1.1f,.22f,-.46f),new Vector3(.48f,.42f,.56f),25f);
            var swap = bay.gameObject.AddComponent<DungeonRuin>();
            swap.intact = light.gameObject;
            swap.ruined = dark.gameObject;
            bool cursed = WorldState.Instance != null && WorldState.Instance.Current == WorldKind.Dark;
            light.gameObject.SetActive(!cursed);
            dark.gameObject.SetActive(cursed);
        }

        const float Width = 12f;
        const float DoorWidth = 4.4f;
        const float DoorHeight = 3.4f;

        enum Course { Banks, Lifts, Blades, Beams, Frost, Forge, Collapse, Branch, Gravity }
        readonly struct Room
        {
            public readonly Course course;
            public readonly string title, hint;
            public readonly int turn;
            public Room(Course course, string title, string hint, int turn = 0)
            {
                this.course = course;
                this.title = title;
                this.hint = hint;
                this.turn = turn;
            }
        }

        static readonly Room[] Trials = {
            new(Course.Banks,"Расколотая галерея","Тёмные плиты появляются после смены мира.", 90),
            new(Course.Lifts,"Колёсный зал","Дождитесь, пока подъёмник подойдёт к краю."),
            new(Course.Beams,"Мосты дозорных","На тонких балках отпускайте клавиши перед приземлением.", -90),
            new(Course.Blades,"Зал маятников","Следите за концами вращающихся лезвий.", -90),
            new(Course.Frost,"Зимний склеп","На льду заранее сбрасывайте скорость."),
            new(Course.Forge,"Плавильня","Смените мир в прыжке, чтобы открыть следующий пролёт.", 90),
            new(Course.Branch,"Зеркальная колоннада","Два пути вокруг колонн. У каждой стороны свои плиты.", 90),
            new(Course.Collapse,"Обвальный архив","Камень дрожит перед падением. Не задерживайтесь под ним."),
            new(Course.Lifts,"Цепная шахта","Ход подъёмников теперь идёт поперёк пролёта.", -90),
            new(Course.Blades,"Оружейная стражи","У края лезвие проходит быстрее. Выбирайте момент.", -90),
            new(Course.Frost,"Замёрзший водосброс","Здесь требуется смена мира на скользком ходу."),
            new(Course.Beams,"Сломанный акведук","Узкие балки соединяют светлые и тёмные опоры.", 90),
            new(Course.Forge,"Горнило","На опорах видны безопасные края между шипами.", 90),
            new(Course.Collapse,"Падающий свод","Ищите проход сбоку от падающих плит."),
            new(Course.Branch,"Разбитый бастион","Часть пути проходит за разрушенными решётками.", -90),
            new(Course.Banks,"Последний пролёт","Последовательность опор меняется от света к тьме.", 90)
        };

        static readonly Room[] Temple = {
            new(Course.Gravity,"Врата левой стены","Пройдите через светящийся барьер. Он перенесёт вас на стену.",90),
            new(Course.Gravity,"Потолок хранителей","Гравитационные плиты срабатывают под ногами."),
            new(Course.Gravity,"Обратная галерея","Переключите мир, чтобы запитать первую руну.",-90),
            new(Course.Gravity,"Храм перевёрнутых арок","Следующая тропа начинается на грани, к которой ведёт руна.",-90),
            new(Course.Gravity,"Висячие фонари","Цвет обода показывает, в каком мире действует печать."),
            new(Course.Gravity,"Подвесная оружейная","Обойдите лезвие на стене, прежде чем наступить на плиту.",90),
            new(Course.Gravity,"Склеп северной опоры","Сохраняйте направление вдоль зала после разворота.",90),
            new(Course.Gravity,"Ледяной потолок","На обледеневшей грани тормозите заранее."),
            new(Course.Gravity,"Арки отражения","Последовательно ищите путь на стене и потолке.",-90),
            new(Course.Gravity,"Дозор над бездной","Пропустите лезвие на сменной опоре.",-90),
            new(Course.Gravity,"Хранилище рун","Последняя плитка возвращает гравитацию на пол."),
            new(Course.Gravity,"Святилище четырёх граней","Переключайте мир на нейтральных плитах.",90)
        };

        static readonly Room[] Countdown = {
            new(Course.Banks,"Вестибюль хронометра","Возьмите часы. Нажмите Q перед тёмными опорами.",90),
            new(Course.Beams,"Порог обратного хода","Ждите окончания импульса перед янтарной плитой."),
            new(Course.Gravity,"Часовая стрелка","Q питает барьер. После поворота сразу идите по стене.",-90),
            new(Course.Lifts,"Спуск противовесов","Запускайте часы, когда движущийся мост окажется рядом.",-90),
            new(Course.Blades,"Ходовой механизм","Совместите импульс часов с безопасным проходом лезвий."),
            new(Course.Forge,"Раскалённые минуты","Тёмная опора рассыплется, когда погаснут часы.",90),
            new(Course.Gravity,"Стена обратного хода","Смените гравитацию в тёмном мире, пока идёт импульс.",90),
            new(Course.Frost,"Замёрзшие секунды","На льду перед светлой опорой тормозите заранее."),
            new(Course.Branch,"Развилка циферблата","Янтарный проход откроется, как только погаснет импульс.",-90),
            new(Course.Collapse,"Осыпающийся ход","Запустите часы на безопасной площадке.",-90),
            new(Course.Gravity,"Часовой свод","Часы открывают путь по граням. На серой опоре можно переждать."),
            new(Course.Blades,"Удары маятника","Считайте время между взмахами лезвий.",90),
            new(Course.Forge,"Сломанные часы","По обе стороны светлой площадки нужны отдельные импульсы."),
            new(Course.Gravity,"Последний оборот","Доберитесь до сферы и откройте выход, пока идёт общий отсчёт.",90)
        };

        public static GameObject Build(int number, out Transform spawn)
        {
            Materials();
            var root = new GameObject("Level_" + number + "_Dungeon");
            root.SetActive(false);
            root.transform.position = Origins[number];
            if (number == 4) BuildMaze(root.transform, out spawn);
            else BuildCourse(root.transform, number, out spawn);
            root.AddComponent<WorldSurfaceSkins>();
            return root;
        }

        static void BuildCourse(Transform root, int level, out Transform spawn)
        {
            bool timed = level == 5;
            Room[] rooms = level == 2 ? Trials : timed ? Countdown : Temple;
            Vector3 entry = Vector3.zero;
            int heading = 0;
            spawn = Empty(root, "StartSpawn", new Vector3(0f,.10f,1.25f));
            for (int index = 0; index < rooms.Length; index++)
            {
                Room plan = rooms[index];
                bool gravityRoom = plan.course == Course.Gravity;
                float length = gravityRoom ? 59f : 53f;
                float height = gravityRoom ? 10.9f : 7.4f + index % 3 * .7f;
                var room = Empty(root, plan.title, entry);
                room.localRotation = Quaternion.Euler(0f, heading, 0f);
                Shell(room, length, height, gravityRoom, index);
                Walkable(room, "Входная площадка", 0f, timed && index == 0 ? 9f : 7.1f);
                Walkable(room, "Площадка у выхода", gravityRoom ? 52f : 45.3f, length);
                if (index == 0) Wall(room,Vector3.back,0f,height,false);
                if (timed && index == 0) PlaceClock(room);
                SavePad(room, new Vector3(0f, 0f, 2.1f), index + 1, plan.title, plan.hint);

                if (gravityRoom) BuildGravity(room, index, timed);
                else BuildParkour(room, plan.course, index, timed);

                Vector3 direction = room.localRotation * Vector3.forward;
                Vector3 nextDirection = Quaternion.Euler(0f, heading + plan.turn, 0f) * Vector3.forward;
                Vector3 end = entry + direction * length;
                Connector(root, end + direction * 6f, -direction, nextDirection, index);
                entry = end + (direction + nextDirection) * 6f;
                heading += plan.turn;
            }

            var finale = Empty(root, "Святилище выходного портала", entry);
            finale.localRotation = Quaternion.Euler(0f,heading,0f);
            Shell(finale, 22f, 7.6f, false, rooms.Length);
            Wall(finale, Vector3.forward, 22f, 7.6f, false);
            Walkable(finale, "Пол святилища", 0f, 22f);
            SavePad(finale, new Vector3(0f,0f,2f), rooms.Length + 1, "Святилище портала",
                timed ? "Q — проявить сферу. Затем отнесите её к порталу." : "Проявите сферу в тёмном мире и отнесите к порталу.");
            PlaceOrb(finale, new Vector3(-2.35f,1.52f,9f));
            PlacePortal(finale, new Vector3(0f,0f,17.5f), level + 1);
            Exhibit(finale, "Banner", new Vector3(4.8f,3.5f,16f), new Vector3(.3f,2.7f,1.4f), 90f);
            Exhibit(finale, "Cauldron_Full", new Vector3(3.8f,.7f,8f), new Vector3(1.45f,1.4f,1.45f));
        }

        static void Connector(Transform root, Vector3 center, Vector3 incoming, Vector3 outgoing, int index)
        {
            var hall = Empty(root,"Поворот между залами",center);
            Box(hall,"Плиты коридора",new Vector3(0f,-.32f,0f),new Vector3(12.04f,.64f,12.04f),floor);
            Box(hall,"Свод коридора",new Vector3(0f,4.75f,0f),new Vector3(12f,.6f,12f),stone);
            foreach (Vector3 face in new[] { Vector3.forward, Vector3.right, Vector3.back, Vector3.left })
                Wall(hall, face, 6f, 4.5f, Vector3.Dot(face,incoming) > .99f || Vector3.Dot(face,outgoing) > .99f, .65f);
            Vector3 decor = -(incoming + outgoing) * 2.5f;
            Exhibit(hall, index % 2 == 0 ? "Pillar" : "Chandelier", decor + Vector3.up * 3.05f, new Vector3(1.4f,1.5f,1.4f));
            Glow(hall,"Свет на повороте",decor + Vector3.up * 2.7f,new Color(1f,.67f,.35f),2.2f,11f);
        }

        static void Wall(Transform root, Vector3 direction, float distance, float height, bool opening, float thickness = .64f)
        {
            var panel = Empty(root,"Каменная перегородка",direction * distance);
            panel.localRotation = Quaternion.LookRotation(direction);
            if (!opening)
            {
                Box(panel,"Кладка",Vector3.up * height*.5f,new Vector3(Width+.16f,height,thickness),stone);
                return;
            }

            float shoulder = (Width - DoorWidth) * .5f;
            for (int sign = -1; sign <= 1; sign += 2)
                Box(panel,"Простенок",new Vector3(sign*(DoorWidth*.5f+shoulder*.5f),height*.5f,0f),
                    new Vector3(shoulder+.08f,height,thickness),stone);
            Box(panel,"Перемычка",new Vector3(0f,(height+DoorHeight)*.5f,0f),
                new Vector3(DoorWidth+.12f,height-DoorHeight,thickness),stone);
            Exhibit(panel,"Dungeon_Custom_Border_Arched",new Vector3(0f,1.60f,-.48f),
                new Vector3(DoorWidth+.9f,3.8f,.64f));
        }

        static void Walkable(Transform room, string title, float from, float to)
        {
            Box(room,title,new Vector3(0f,-.36f,(from+to)*.5f),new Vector3(Width,.72f,to-from+.03f),floor);
        }

        static void Shell(Transform root, float length, float height, bool gravityRoom, int seed)
        {
            float depth = gravityRoom ? 3.6f : 4.8f;
            for (int sign = -1; sign <= 1; sign += 2)
            {
                Box(root,"Опорная кладка",new Vector3(sign*Width*.5f,(height-depth)*.5f,length*.5f),
                    new Vector3(.58f,height+depth,length+.10f),stone);
                for (int bay = 0; bay < (length > 30f ? 4 : 2); bay++)
                {
                    float z = 4f + bay * (length-8f)/(length > 30f ? 3f : 1f);
                    Alcove(root,new Vector3(sign*5.55f,-.02f,z),sign*90f,seed+bay);
                    Exhibit(root,"Pillar",new Vector3(sign*5.55f,height*.5f,z+2.6f),
                        new Vector3(.72f,height,.72f));
                    Exhibit(root,"Torch_Wall",new Vector3(sign*5.18f,2.6f,z),new Vector3(.38f,.82f,.52f),sign*90f);
                    Glow(root,"Факел",new Vector3(sign*4.7f,2.85f,z),new Color(1f,.68f,.40f),2.65f,12f);
                    if (bay % 2 == 0)
                        Exhibit(root,seed%3==0?"Jail_Wall":"Dungeon_Wall_Window",
                            new Vector3(sign*5.64f,height-2f,z+2f),new Vector3(.36f,2.35f,3.45f),sign*90f);
                }
            }

            Box(root,"Каменный свод",new Vector3(0f,height+.25f,length*.5f),
                new Vector3(Width+.2f,.64f,length+.15f),stone);
            Wall(root,Vector3.back,0f,height,true);
            if (length > 30f) Wall(root,Vector3.forward,length,height,true);
            Exhibit(root,"Chandelier",new Vector3(0f,height-1.25f,length*.52f),new Vector3(2.35f,2.15f,2.35f));
            Glow(root,"Свет верхнего свода",new Vector3(0f,height-2.1f,length*.52f),new Color(1f,.77f,.52f),3.1f,14f);

            if (!gravityRoom)
            {
                string prop = seed % 3 == 0 ? "Amphora" : seed % 3 == 1 ? "Book_Open" : "Cauldron_Full";
                Exhibit(root,prop,new Vector3(-3.85f,.44f,3.8f),new Vector3(1.0f,.88f,1.0f));
                Exhibit(root,"Banner",new Vector3(4.5f,3f,length-4f),new Vector3(1.18f,2.4f,.24f));
            }
        }

        static void SavePad(Transform floorSpace, Vector3 spot, int id, string title, string hint, Vector3? heading = null, float triggerWidth = 11.25f)
        {
            var zone = Empty(floorSpace,"Контрольная точка " + id,spot);
            if (heading.HasValue) zone.localRotation = Quaternion.LookRotation(heading.Value);
            zone.gameObject.layer = 2;
            var trigger = zone.gameObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(triggerWidth,2.1f,2.45f);
            trigger.center = Vector3.up * .95f;
            TriggerBody(zone.gameObject);
            var save = zone.gameObject.AddComponent<CheckpointTrigger>();
            save.checkpoint = Empty(zone,"Позиция возрождения",new Vector3(0f,.12f,.12f));
            save.id = id;
            save.roomTitle = title;
            save.roomHint = hint;
            Decoration(zone,"Бронзовая метка",new Vector3(0f,.014f,0f),new Vector3(2.7f,.027f,.95f),gold);
            Decoration(zone,"Светящаяся руна",new Vector3(0f,.038f,0f),new Vector3(1.8f,.025f,.12f),cyan);
        }

        static void TriggerBody(GameObject go)
        {
            var body = go.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
        }

        static GameObject Decoration(Transform root, string name, Vector3 position, Vector3 size, Material material)
        {
            var go = Box(root,name,position,size,material);
            go.GetComponent<Collider>().enabled = false;
            return go;
        }

        static void KillVolume(Transform root, string name, Vector3 position, Vector3 size)
        {
            var zone = Empty(root,name,position).gameObject;
            var trigger = zone.AddComponent<BoxCollider>();
            trigger.size = size;
            Hazard(zone);
        }

        static void PlaceClock(Transform room)
        {
            Exhibit(room,"CrystalBall_Stand",new Vector3(0f,.47f,5.3f),new Vector3(1.16f,.94f,1.16f));
            var clock = Empty(room,"Парящие часы",new Vector3(0f,1.42f,5.3f));
            var col = clock.gameObject.AddComponent<SphereCollider>();
            col.radius = .48f;
            col.isTrigger = true;
            var face = Empty(clock,"Циферблат",Vector3.zero);
            face.localScale = Vector3.one * 1.6f;
            ToolController.BuildWatchFace(face);
            Glow(clock,"Свечение часов",Vector3.zero,new Color(1f,.68f,.32f),1.7f,4.5f);
            var pickup = clock.gameObject.AddComponent<ToolPickup>();
            pickup.kind = ToolKind.Clock;
            pickup.hint = "E — взять часы";
        }

        static void PlaceOrb(Transform root, Vector3 position)
        {
            Exhibit(root,"CrystalBall_Stand",new Vector3(position.x,.53f,position.z),new Vector3(1.35f,1.06f,1.35f));
            var orbRoot = Empty(root,"Сфера частиц",position);
            var collider = orbRoot.gameObject.AddComponent<SphereCollider>();
            collider.radius = .64f;
            collider.isTrigger = true;
            var world = orbRoot.gameObject.AddComponent<WorldVariant>();
            world.presence = WorldPresence.Dark;
            world.useTransitionWave = false;
            world.scaleAnimMaxExtent = 0f;
            var orb = orbRoot.gameObject.AddComponent<Orb>();
            orb.home = WorldKind.Dark;
        }

        static PortalFrame PlacePortal(Transform root, Vector3 position, int next, bool needsStaff = false)
        {
            var gate = Empty(root,"Выходной портал",position);
            var portal = gate.gameObject.AddComponent<PortalFrame>();
            portal.nextLevel = next;
            portal.requiresStaff = needsStaff;
            Exhibit(gate,"Dungeon_Custom_Border_Arched",new Vector3(0f,1.99f,.20f),new Vector3(4.86f,4.12f,.94f));
            Box(gate,"Опора портала",new Vector3(0f,.07f,.02f),new Vector3(4.85f,.14f,2.76f),stone);
            var target = gate.gameObject.AddComponent<BoxCollider>();
            target.isTrigger = true;
            target.center = new Vector3(0f,1.62f,-.06f);
            target.size = new Vector3(2.75f,3.18f,.50f);
            Glow(gate,"Жаровня у портала",new Vector3(2.55f,2.2f,-1.6f),new Color(1f,.65f,.40f),2.4f,7f);

            if (needsStaff)
            {
                Exhibit(gate,"CrystalBall_Stand",new Vector3(2.65f,.57f,-1.85f),new Vector3(1.23f,1.14f,1.23f));
                var stand = Empty(gate,"Пьедестал посоха",new Vector3(2.65f,1.28f,-1.85f));
                var targetCollider = stand.gameObject.AddComponent<SphereCollider>();
                targetCollider.radius = .66f;
                targetCollider.isTrigger = true;
                stand.gameObject.AddComponent<StaffPedestal>().portal = portal;
            }
            return portal;
        }
    }
}
