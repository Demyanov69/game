using System;
using System.Collections.Generic;
using UnityEngine;

namespace EchoIslands
{
    public static partial class DungeonCampaignBuilder
    {
        const int MazeSize = 11;
        const float Cell = 8.4f;
        const float MazeHeight = 5.35f;
        static readonly Vector2Int[] MazeDirections =
        {
            new(0,1), new(1,0), new(0,-1), new(-1,0)
        };

        sealed class MazePlan
        {
            public readonly bool[,,] passages = new bool[MazeSize,MazeSize,4];
            public readonly bool[,,] shortcut = new bool[MazeSize,MazeSize,4];
            public readonly List<Vector2Int> route = new();
        }

        static bool InMaze(Vector2Int cell) => cell.x >= 0 && cell.x < MazeSize && cell.y >= 0 && cell.y < MazeSize;

        static MazePlan PlanMaze()
        {
            var rng = new System.Random(83);
            var cells = new Stack<Vector2Int>();
            var parents = new Vector2Int[MazeSize,MazeSize];
            var depth = new int[MazeSize,MazeSize];
            var visited = new bool[MazeSize,MazeSize];
            var plan = new MazePlan();
            var end = Vector2Int.zero;
            cells.Push(Vector2Int.zero);
            visited[0,0] = true;
            while (cells.Count > 0)
            {
                var current = cells.Peek();
                var directions = new List<int>(4);
                for (int i=0; i<4; i++)
                {
                    Vector2Int neighbor = current + MazeDirections[i];
                    if (InMaze(neighbor) && !visited[neighbor.x,neighbor.y]) directions.Add(i);
                }
                if (directions.Count == 0)
                {
                    cells.Pop();
                    continue;
                }

                int direction = directions[rng.Next(directions.Count)];
                Vector2Int next = current + MazeDirections[direction];
                plan.passages[current.x,current.y,direction] = true;
                plan.passages[next.x,next.y,(direction+2)%4] = true;
                visited[next.x,next.y] = true;
                parents[next.x,next.y] = current;
                depth[next.x,next.y] = depth[current.x,current.y] + 1;
                if (depth[next.x,next.y] > depth[end.x,end.y]) end = next;
                cells.Push(next);
            }
            for (var step = end; step != Vector2Int.zero; step = parents[step.x,step.y])
                plan.route.Add(step);
            plan.route.Add(Vector2Int.zero);
            plan.route.Reverse();

            // Short secret loops make the wings navigable without bypassing their world gates.
            int added = 0;
            var routeSet = new HashSet<Vector2Int>(plan.route);
            for (int x=0; x<MazeSize && added<11; x++)
                for (int z=0; z<MazeSize && added<11; z++)
                    for (int dir=0; dir<2 && added<11; dir++)
                    {
                        Vector2Int cell = new(x,z);
                        Vector2Int next = cell + MazeDirections[dir];
                        if (!InMaze(next) || plan.passages[x,z,dir] || routeSet.Contains(cell) ||
                            routeSet.Contains(next) || Math.Abs(depth[x,z]-depth[next.x,next.y]) > 7) continue;
                        plan.passages[x,z,dir] = true;
                        plan.passages[next.x,next.y,(dir+2)%4] = true;
                        plan.shortcut[x,z,dir] = true;
                        added++;
                    }
            return plan;
        }

        static Vector3 CellCenter(Vector2Int cell) => new((cell.x - (MazeSize-1)*.5f)*Cell,0f,cell.y*Cell);

        static Vector3 MazeForward(Vector2Int direction) => new(direction.x,0f,direction.y);

        static void BuildMaze(Transform root, DungeonVisibility visibility, out Transform spawn)
        {
            MazePlan plan = PlanMaze();
            var routePositions = new Dictionary<Vector2Int,int>();
            for (int i=0; i<plan.route.Count; i++) routePositions.Add(plan.route[i],i);
            var start = plan.route[0];
            var last = plan.route[^1];
            int orbIndex = plan.route.Count * 4 / 5;
            var orbCell = plan.route[orbIndex];

            spawn = Empty(root,"StartSpawn",CellCenter(start) + Vector3.up*.12f);
            spawn.localRotation = Quaternion.LookRotation(MazeForward(plan.route[1]-start));
            Box(root,"Свод лабиринта",new Vector3(0f,MazeHeight+.28f,(MazeSize-1)*Cell*.5f),
                new Vector3(MazeSize*Cell+.4f,.56f,MazeSize*Cell+.4f),stone);

            for (int x=0; x<MazeSize; x++)
                for (int z=0; z<MazeSize; z++)
                {
                    Vector2Int cell = new(x,z);
                    var room = Empty(root,"Ячейка " + x + "–" + z,CellCenter(cell));
                    bool mainRoute = routePositions.TryGetValue(cell,out int routeIndex);
                    int degree = 0, entryDir = 0;
                    for (int d=0; d<4; d++)
                        if (plan.passages[x,z,d]) { degree++; entryDir = d; }

                    bool trap = !mainRoute && degree == 1 && (x*7 + z*11)%5 == 1;
                    if (trap) MazeTrap(room,MazeForward(MazeDirections[entryDir]));
                    else Box(room,"Пол катакомб",new Vector3(0f,-.30f,0f),new Vector3(Cell+.02f,.60f,Cell+.02f),floor);

                    for (int d=0; d<4; d++)
                    {
                        var neighbor = cell + MazeDirections[d];
                        bool exterior = !InMaze(neighbor);
                        if (!exterior && d > 1) continue;
                        Vector3 direction = MazeForward(MazeDirections[d]);
                        var door = Empty(room,"Граница камеры",direction * Cell*.5f);
                        door.localRotation = Quaternion.LookRotation(direction);
                        if (exterior || !plan.passages[x,z,d])
                        {
                            Box(door,"Массивная кладка",Vector3.up * MazeHeight*.5f,
                                new Vector3(Cell+.10f,MazeHeight,.48f),stone);
                            if ((x*2+z+d)%5==0)
                                Alcove(door,new Vector3(0f,0f,-.48f),0f,x+z);
                            continue;
                        }

                        bool routeEdge = mainRoute && routePositions.TryGetValue(neighbor,out var nextIndex) &&
                            Math.Abs(nextIndex-routeIndex) == 1;
                        if (routeEdge && (Math.Min(routeIndex,routePositions[neighbor])+1)%5 != 0)
                        {
                            int order = Math.Min(routeIndex,routePositions[neighbor]);
                            MazeDoor(door,order%2==0?WorldPresence.Light:WorldPresence.Dark,false);
                        }
                        else if (plan.shortcut[x,z,d] || !routeEdge && (x*13+z*7+d)%4 != 0)
                        {
                            MazeDoor(door,(x+3*z+d)%2==0?WorldPresence.Light:WorldPresence.Dark,plan.shortcut[x,z,d]);
                        }
                        else MazeDoor(door,WorldPresence.Both,false);
                    }

                    if (mainRoute && routeIndex>0 && routeIndex<plan.route.Count-1 && routeIndex%10==0)
                    {
                        SavePad(room,Vector3.zero,routeIndex+1,
                            MazeForward(plan.route[routeIndex+1]-cell), 6.4f);
                    }

                    if ((x+z*2)%3 != 1 || mainRoute && routeIndex%5==0)
                    {
                        var sconce = new Vector3(-2.55f,2.44f,-2.8f);
                        Exhibit(room,"Torch_Wall",sconce,new Vector3(.38f,.86f,.48f));
                        Glow(room,"Масляный факел",sconce + Vector3.up*.43f,new Color(1f,.61f,.33f),2.65f,9.4f);
                    }

                    if (degree>=3)
                        Exhibit(room,(x+z)%2==0?"Pillar":"Cauldron_Full",new Vector3(0f,.52f,0f),
                            new Vector3(.65f,1.04f,.65f));
                    if (!mainRoute && degree==1 && !trap)
                        Exhibit(room,"Skull",new Vector3(1.75f,.22f,1.6f),new Vector3(.54f,.43f,.55f));
                    if (cell==orbCell)
                    {
                        PlaceOrb(room,new Vector3(-2.05f,1.51f,1.75f));
                        Exhibit(room,"Chandelier",new Vector3(0f,4.41f,0f),new Vector3(1.75f,1.28f,1.75f));
                        Glow(room,"Вход в крипту сферы",new Vector3(-1.8f,2.55f,1.5f),new Color(.68f,1f,.92f),2.8f,9f);
                    }
                    if (cell==last)
                    {
                        var altar = Empty(room,"Последний алтарь",Vector3.zero);
                        altar.localRotation = Quaternion.LookRotation(-MazeForward(MazeDirections[entryDir]));
                        SavePad(altar,new Vector3(0f,0f,-1.87f),plan.route.Count+1);
                        PlacePortal(altar,new Vector3(-.78f,0f,2.40f),5,true);
                        Glow(altar,"Алтарная жаровня",new Vector3(0f,3.5f,1.9f),new Color(1f,.78f,.50f),3.5f,11f);
                    }
                    visibility.Add(room, Vector3.zero, new Vector3(Cell, 0f, Cell));
                }
            Glow(root,"Вход в лабиринт",CellCenter(start)+Vector3.up*3.3f,new Color(1f,.74f,.46f),3.9f,10.7f);
        }

        static void MazeDoor(Transform door, WorldPresence presence, bool secret)
        {
            const float opening = 3.8f;
            float edge = (Cell-opening)*.5f;
            for (int sign = -1; sign <= 1; sign += 2)
                Box(door,"Камень вокруг проёма",new Vector3(sign*(opening+edge)*.5f,MazeHeight*.5f,0f),
                    new Vector3(edge+.07f,MazeHeight,.54f),stone);
            Box(door,"Низкая перемычка",new Vector3(0f,4.26f,0f),new Vector3(opening+.14f,2.18f,.58f),stone);
            Exhibit(door,"Dungeon_Custom_Border_Arched",new Vector3(0f,1.53f,-.36f),new Vector3(4.45f,3.75f,.48f));

            if (presence == WorldPresence.Both) return;
            var phased = Empty(door,"Межмировая решётка",Vector3.zero);
            var collision = phased.gameObject.AddComponent<BoxCollider>();
            collision.center = new Vector3(0f,1.56f,0f);
            collision.size = new Vector3(opening+.13f,3.16f,.38f);
            var model = Exhibit(phased,secret?"Jail_Wall_broken":"Portcullis_Metal",new Vector3(0f,1.57f,0f),
                new Vector3(opening,3.12f,.31f));
            var visuals = model == null ? Array.Empty<Renderer>() : model.GetComponentsInChildren<Renderer>(true);
            phased.gameObject.AddComponent<DungeonPhase>().Configure(presence, collision, visuals);

            Decoration(door,"Руна закрытого мира",new Vector3(0f,3.41f,0f),
                new Vector3(.66f,.20f,.86f),presence==WorldPresence.Light?gold:cyan);
        }

        static void MazeTrap(Transform room, Vector3 entry)
        {
            var trap = Empty(room,"Осыпавшаяся камера",Vector3.zero);
            trap.localRotation = Quaternion.LookRotation(entry);
            Box(trap,"Безопасный край",new Vector3(0f,-.30f,2.60f),new Vector3(Cell+.02f,.60f,3.24f),floor);
            Box(trap,"Плита над ловушкой",new Vector3(0f,-.30f,-2.60f),new Vector3(Cell+.02f,.60f,3.24f),floor);
            Hazard(Box(trap,"Расплав под полом",new Vector3(0f,-1.55f,0f),new Vector3(Cell,.34f,2.15f),lava));
            ForgeSpike(trap,new Vector3(-1.65f,0f,-1.20f),.82f);
            Exhibit(trap,"Skull",new Vector3(1.25f,.25f,1.61f),new Vector3(.62f,.50f,.68f));
            Glow(trap,"Красная трещина",new Vector3(0f,-.35f,0f),new Color(1f,.20f,.05f),2.4f,8f);
        }
    }
}
