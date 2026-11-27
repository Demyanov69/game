using UnityEngine;

namespace EchoIslands
{
    public static partial class DungeonCampaignBuilder
    {
        static Material DeckMaterial(WorldPresence phase, Material neutral)
        {
            if (phase == WorldPresence.Dark) return darkTile;
            if (phase == WorldPresence.Light) return lightTile;
            return neutral;
        }

        static GameObject Deck(Transform room, string name, Vector3 surface, Vector3 size,
            WorldPresence world, Material neutral, Quaternion rotation)
        {
            var tile = Box(room,name,surface - rotation * Vector3.up * (size.y*.5f),size,
                DeckMaterial(world, neutral),world);
            tile.transform.localRotation = rotation;
            return tile;
        }

        static WorldPresence TrialPhase(int index, int station, bool timed)
        {
            if (timed)
            {
                if (station == 3 || station == 4 || station == 8 || station == 9)
                    return WorldPresence.Dark;
                return station == 6 ? WorldPresence.Light : WorldPresence.Both;
            }
            if (station == 2 || station == 3 || station == 8)
                return index < 5 || index % 2 == 0 ? WorldPresence.Dark : WorldPresence.Light;
            if (station == 5 || (index > 7 && station == 9))
                return index < 5 || index % 2 == 0 ? WorldPresence.Light : WorldPresence.Dark;
            return WorldPresence.Both;
        }

        static void BuildParkour(Transform room, Course course, int index, bool timed)
        {
            bool hot = course == Course.Forge || course == Course.Collapse || index % 5 == 2;
            if (hot)
            {
                Hazard(Box(room,"Лава в основании шахты",new Vector3(0f,-3.15f,26.5f),
                    new Vector3(11.4f,.28f,39.5f),lava));
                Glow(room,"Отсвет расплава",new Vector3(0f,-1.1f,25f),new Color(1f,.31f,.12f),2.5f,18f);
                for (int rib = 0; rib < 5; rib++)
                    Decoration(room,"Остывшая корка",new Vector3(rib%2==0?2.6f:-2.4f,-3.0f,12f+rib*7f),
                        new Vector3(3.2f,.16f,1.28f),stone).transform.localRotation = Quaternion.Euler(0f,31f+rib*12f,0f);
            }
            KillVolume(room,"Падение в шахту",new Vector3(0f,-4.7f,26.5f),new Vector3(13f,2.3f,48f));

            float[] lateral = course switch
            {
                Course.Beams => new[] { 0f,-.65f,-.65f,.20f,.20f,.90f,.90f,0f,-.68f,-.68f,0f },
                Course.Frost => new[] { 0f,.32f,.80f,1.14f,.65f,0f,-.62f,-1.10f,-.80f,-.35f,0f },
                Course.Forge => new[] { 0f,.60f,1.27f,1.27f,.55f,-.24f,-1.05f,-1.2f,-.56f,0f,0f },
                Course.Banks => new[] { 0f,.88f,1.23f,.57f,-.40f,-1.08f,-1.08f,-.32f,.52f,.65f,0f },
                _ => new[] { 0f,0f,.36f,.75f,.37f,0f,-.36f,-.75f,-.37f,0f,0f }
            };

            float[] stations = course switch
            {
                Course.Beams => new[] { 8.5f,11.6f,15.5f,19.5f,22.6f,26.6f,30.6f,34.4f,38.4f,41.6f,44.5f },
                Course.Frost => new[] { 8.5f,11.8f,15.0f,18.9f,22.8f,26.1f,29.5f,33.4f,37.4f,41.4f,44.5f },
                Course.Blades => new[] { 8.5f,12.4f,16.2f,20.4f,23.7f,27.2f,30.9f,34.9f,38.1f,41.2f,44.5f },
                _ => new[] { 8.5f,12.3f,15.8f,19.4f,23.3f,26.9f,30.3f,33.9f,37.9f,41.4f,44.5f }
            };

            float mirror = index%2 == 0 ? 1f : -1f;
            for (int i = 0; i < stations.Length; i++)
            {
                float x = lateral[i] * mirror;
                float z = stations[i];
                WorldPresence phase = TrialPhase(index,i,timed);
                bool slippery = course == Course.Frost && i > 0 && i < 10 && i != 6;
                float y = course == Course.Banks && i > 0 && i < 10 ? .24f * (i % 3) : 0f;
                float w = course == Course.Beams ? 1.22f : course == Course.Blades || course == Course.Collapse ? 4.70f : 2.64f;
                float depth = course == Course.Beams || slippery ? 3.38f : 2.85f;
                if (course == Course.Branch) w = 3.5f;
                var stage = Deck(room,"Опора над бездной",new Vector3(x,y,z),new Vector3(w,.43f,depth),
                    phase,slippery?ice:course==Course.Beams?wood:platformTile,Quaternion.identity);
                if (slippery) AddIce(stage);
                if (course == Course.Lifts && (i == 2 || i == 4 || i == 7 || i == 9))
                {
                    var mover = stage.AddComponent<MovingDungeonPlatform>();
                    mover.travel = index%3 == 1 && i%2 == 0 ? room.up * .36f : room.right * (i%2==0?1.6f:1.05f);
                    mover.period = 3.8f + i%3 * .55f;
                    Exhibit(room,"Pillar",new Vector3(x,-2.05f,z),new Vector3(.62f,2.6f,.62f));
                }
                if (course == Course.Blades && (i == 1 || i == 5 || i == 7))
                    Blade(room,new Vector3(x,.69f,z), Quaternion.identity, 3.4f, 65f + index%6*12f);
                if (course == Course.Collapse && (i == 2 || i == 5 || i == 7 || i == 8))
                    FallingBlock(room,new Vector3(x + mirror*.65f,5.6f,z));
                if (course == Course.Forge && (i == 1 || i == 5 || i == 7 || i == 10))
                {
                    ForgeSpike(room,new Vector3(x - mirror*.92f,y,z+.3f),.72f);
                    if (index > 7) ForgeSpike(room,new Vector3(x + mirror*.90f,y,z-.45f),.72f);
                }
                if (course == Course.Branch && (i == 2 || i == 5 || i == 8))
                {
                    // A short side route rejoins the next common landing.
                    WorldPresence side = phase == WorldPresence.Dark ? WorldPresence.Light : WorldPresence.Dark;
                    float sideX = ((i == 5 ? -1f : 1f)*3.12f)*mirror;
                    if (!timed || i == 5)
                        Deck(room,"Боковая галерея",new Vector3(sideX,y,z),new Vector3(2.55f,.43f,3.08f),
                            side,wood,Quaternion.identity);
                    Exhibit(room,"Pillar",new Vector3(sideX*1.48f,2.57f,z),new Vector3(.66f,5.15f,.66f));
                }
            }
        }

        static void AddIce(GameObject deck)
        {
            var mark = Empty(deck.transform,"Скользкая поверхность",Vector3.zero).gameObject;
            mark.layer = 2;
            var trigger = mark.AddComponent<BoxCollider>();
            trigger.center = new Vector3(0f,.80f,0f);
            trigger.size = new Vector3(1f,1.20f,1f);
            trigger.isTrigger = true;
            TriggerBody(mark);
            mark.AddComponent<IcePatch>().surface = deck.GetComponent<Collider>();
        }

        static void Blade(Transform room, Vector3 center, Quaternion rotation, float span, float speed)
        {
            var hub = Empty(room,"Ось лезвия",center);
            hub.localRotation = rotation;
            var beam = Box(hub,"Вращающееся лезвие",Vector3.zero,new Vector3(span,.19f,.32f),metal);
            Hazard(beam);
            hub.gameObject.AddComponent<SpinningDungeonHazard>().degreesPerSecond = speed;
            Decoration(room,"Крепление лезвия",center,new Vector3(.35f,.36f,.35f),gold);
        }

        static void FallingBlock(Transform room, Vector3 position)
        {
            var block = Box(room,"Осыпавшаяся плита свода",position,new Vector3(1.8f,.88f,1.56f),stone);
            Hazard(block);
            block.AddComponent<FallingDungeonBlock>().triggerDistance = 2.15f;
        }

        static Vector3 FaceUp(GravitySide side)
        {
            return side switch
            {
                GravitySide.LeftWall => Vector3.right,
                GravitySide.RightWall => Vector3.left,
                GravitySide.Ceiling => Vector3.down,
                _ => Vector3.up
            };
        }

        static Vector3 FacePoint(GravitySide side, float z)
        {
            return side switch
            {
                GravitySide.LeftWall => new Vector3(-4.96f,4.24f,z),
                GravitySide.RightWall => new Vector3(4.96f,4.24f,z),
                GravitySide.Ceiling => new Vector3(0f,9.58f,z),
                _ => new Vector3(0f,0f,z)
            };
        }

        static void BuildGravity(Transform room, int index, bool timed)
        {
            float[] stations = { 8.65f,12.33f,16f,19.75f,23.45f,27.25f,30.85f,34.6f,38.25f,42f,45.75f,49.5f };
            GravitySide first = index%3 == 1 ? GravitySide.RightWall : GravitySide.LeftWall;
            GravitySide second = index%3 == 0 ? first : index%3 == 1 ? GravitySide.Ceiling : GravitySide.RightWall;
            var side = GravitySide.Floor;
            for (int i = 0; i < stations.Length; i++)
            {
                float z = stations[i];
                Quaternion face = Quaternion.LookRotation(Vector3.forward,FaceUp(side));
                bool icy = (index%5 == 2 || index%5 == 3) && i > 2 && i < 8;
                WorldPresence phase = WorldPresence.Both;
                if (i == 3 || i == 4 || i == 8) phase = WorldPresence.Dark;
                if (i == 5 && !timed || i == 7 || i == 10 && !timed) phase = WorldPresence.Light;
                float offset = i == 3 || i == 8 ? .54f : i == 4 || i == 7 ? -.54f : 0f;
                Vector3 surface = FacePoint(side,z) + face * Vector3.right * offset;
                var step = Deck(room,"Плита на грани",surface,new Vector3(3.08f,.45f,2.85f),phase,
                    icy ? ice : platformTile,face);
                if (icy) AddIce(step);
                if (i == 5 && index%4 == 1)
                    Blade(room,surface + FaceUp(side)*.53f,face,1.35f,68f);
                if (i == 5 && index%4 == 3)
                {
                    var mover = step.AddComponent<MovingDungeonPlatform>();
                    mover.travel = room.TransformDirection(face * Vector3.right) * .76f;
                    mover.period = 4.2f;
                }
                GravitySide next = i == 2 ? first : i == 6 ? second : i == 9 ? GravitySide.Floor : side;
                if (next == side) continue;

                var targetFace = Quaternion.LookRotation(Vector3.forward,FaceUp(next));
                Deck(room,"Приёмная плита",FacePoint(next,z),new Vector3(3.74f,.45f,2.95f),
                    WorldPresence.Both,floor,targetFace);
                bool needsWorld = i == 2 || (i == 6 && !timed && index%2==1);
                WorldKind world = i == 6 ? WorldKind.Light : WorldKind.Dark;
                GravitySeal(room, side, next, z, needsWorld, world, (index+i)%3 != 0);
                side = next;
            }

            KillVolume(room,"Под храмом",new Vector3(0f,-2.28f,29.9f),new Vector3(13f,1.7f,45.5f));
            KillVolume(room,"Выше потолка",new Vector3(0f,10.45f,29.9f),new Vector3(13f,.72f,45.5f));
            KillVolume(room,"За левой гранью",new Vector3(-5.94f,4.2f,29.9f),new Vector3(.88f,12.2f,45.5f));
            KillVolume(room,"За правой гранью",new Vector3(5.94f,4.2f,29.9f),new Vector3(.88f,12.2f,45.5f));
        }

        static void GravitySeal(Transform room, GravitySide side, GravitySide next, float z,
            bool needsWorld, WorldKind world, bool barrier)
        {
            var stamp = Empty(room,barrier?"Гравитационный барьер":"Гравитационная плита",FacePoint(side,z));
            stamp.localRotation = Quaternion.LookRotation(Vector3.forward,FaceUp(side));
            var pad = stamp.gameObject.AddComponent<GravityPad>();
            pad.source = side;
            pad.destination = next;
            pad.needsWorld = needsWorld;
            pad.requiredWorld = world;
            pad.barrier = barrier;
            pad.landingPosition = room.TransformPoint(FacePoint(next,z) + FaceUp(next)*.08f);
            pad.landingUp = room.TransformDirection(FaceUp(next));
            pad.routeForward = room.forward;
            var rune = !needsWorld || world == WorldKind.Dark ? cyan : gold;
            Decoration(stamp,"Печать",new Vector3(0f,.029f,.12f),new Vector3(2.85f,.047f,1.74f),rune);
            if (barrier)
            {
                Exhibit(stamp,"Dungeon_Custom_Border_Arched",new Vector3(0f,1.47f,.1f),new Vector3(3.94f,3.0f,.46f));
                for (int sideSign=-1; sideSign<=1; sideSign+=2)
                    Decoration(stamp,"Линия барьера",new Vector3(sideSign*1.43f,1.17f,.11f),
                        new Vector3(.083f,2.16f,.08f),rune);
            }
            Glow(stamp,"Свет печати",new Vector3(0f,1.26f,.15f),
                world==WorldKind.Dark?new Color(.26f,.95f,.86f):new Color(1f,.58f,.18f),1.55f,5.0f);
        }
    }
}
