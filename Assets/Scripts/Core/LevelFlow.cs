using UnityEngine;

namespace EchoIslands
{
    public class LevelFlow : MonoBehaviour
    {
        public static LevelFlow Instance { get; private set; }
        public Transform player;
        public ToolController tools;
        public GameObject[] levelRoots;
        public Transform[] spawns;
        public int current = 1;
        Transform checkpoint;
        int checkpoints;
        public int Checkpoints => checkpoints;
        public float Level5Remaining { get; private set; } = 600f;
        void Update()
        {
            if(current!=5 || Time.timeScale<=0f)return;
            Level5Remaining=Mathf.Max(0f,Level5Remaining-Time.deltaTime);
            if(Level5Remaining<=0f)
            {
                Begin(5);
                var hud=FindAnyObjectByType<GameHUD>();
                if(hud!=null)hud.Notify("Время вышло! Испытание началось снова",4.5f);
            }
        }
        public static readonly string[] LevelNames = {
            "", "Берег двух миров", "Залы испытаний", "Перевёрнутый храм",
            "Лабиринт отражений", "Предел времени" };

        void Awake()
        {
            Instance=this;
            if(levelRoots==null || levelRoots.Length<5) System.Array.Resize(ref levelRoots,5);
            if(spawns==null || spawns.Length<5) System.Array.Resize(ref spawns,5);
        }
        void Start()
        {
            // Scene-owned level 1 is the original forest; old placeholder dungeon roots stay intact but disabled.
            for(int i=1;i<levelRoots.Length;i++)
                if(levelRoots[i]!=null) levelRoots[i].SetActive(false);
        }
        void EnsureDungeon(int level)
        {
            if(level<2 || level>5) return;
            // Rebuild on restart/timeout: otherwise collected orbs, opened portals
            // and one-time checkpoints would keep their previous-run state.
            if(levelRoots[level-1]!=null)
            {
                var previous=levelRoots[level-1];
                previous.SetActive(false);
                if(previous.name.Contains("_COMPLETE_DUNGEON")) Destroy(previous);
            }
            Transform spawn;
            levelRoots[level-1]=DungeonCampaignBuilder.Build(level,out spawn);
            spawns[level-1]=spawn;
        }
        void MovePlayer(Transform target)
        {
            if(player==null||target==null)return;
            var cc=player.GetComponent<CharacterController>();
            if(cc!=null)cc.enabled=false;
            player.SetPositionAndRotation(target.position,target.rotation);
            var fps=player.GetComponent<FirstPersonController>();
            if(fps!=null)fps.ResetGravity();
            if(cc!=null)cc.enabled=true;
        }
        void MovePlayer(Vector3 pos)
        {
            if(player==null)return;
            var cc=player.GetComponent<CharacterController>();
            if(cc!=null)cc.enabled=false;
            player.position=pos;
            var fps=player.GetComponent<FirstPersonController>();
            if(fps!=null)fps.ResetGravity();
            if(cc!=null)cc.enabled=true;
        }
        public void Respawn()
        {
            Transform target=checkpoint;
            if(target==null && spawns!=null && current-1<spawns.Length)target=spawns[current-1];
            MovePlayer(target);
            if(tools!=null)tools.CancelClock();
            if(WorldState.Instance!=null)WorldState.Instance.SetWorld(WorldKind.Light);
            var hud=FindAnyObjectByType<GameHUD>();
            if(hud!=null)hud.Notify("Возрождение у контрольной точки",2.3f);
        }
        public void SetCheckpoint(Transform target)
        {
            if(target==null)return;
            checkpoint=target;
            checkpoints++;
            SaveCurrent();
            var hud=FindAnyObjectByType<GameHUD>();
            if(hud!=null)hud.Notify("Контрольная точка · прогресс сохранён",3f);
        }
        public void SaveCurrent()
        {
            if(player==null)return;
            Vector3 pos=checkpoint!=null?checkpoint.position:player.position;
            WorldKind world=WorldState.Instance!=null?WorldState.Instance.Target:WorldKind.Light;
            if (current == 5) world = WorldKind.Light; // Clock mode never grants permanent dark-world access.
            bool hasOrb=false;
            var activeOrbs=FindObjectsByType<Orb>(FindObjectsSortMode.None);
            for(int i=0;i<activeOrbs.Length;i++)
                if(activeOrbs[i]!=null && activeOrbs[i].carried){hasOrb=true;break;}
            GameProgress.Save(current,pos,tools!=null&&tools.staff,tools!=null&&tools.clock,world,hasOrb,Level5Remaining);
        }
        public void Begin(int level){Begin(level,false);}
        public void Begin(int level,bool restore)
        {
            // An orb carried from a prior chapter must never activate a new gate.
            foreach(var oldOrb in FindObjectsByType<Orb>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                if(oldOrb!=null && oldOrb.carried) Destroy(oldOrb.gameObject);
            current=Mathf.Clamp(level,1,5);
            EnsureDungeon(current);
            checkpoint=null;
            checkpoints=0;
            Level5Remaining=600f;
            ShowOnly(current);
            if(spawns!=null&&current-1<spawns.Length)MovePlayer(spawns[current-1]);
            if(tools!=null)
            {
                tools.ClearTools();
                if(current>=2&&current<=4)tools.Give(ToolKind.Staff);
                if(current==5)tools.clockDuration=7.5f;
            }
            if(WorldState.Instance!=null)
            {
                WorldState.Instance.debugToggleEnabled=false;
                WorldState.Instance.SetWorld(WorldKind.Light);
            }
            if(restore && GameProgress.HasSave && GameProgress.SavedLevel==current)
            {
                Level5Remaining = current==5 ? Mathf.Clamp(GameProgress.SavedLevel5Time,1f,600f) : 600f;
                if (current == 3)
                {
                    // Old saves can point to free space in the redesigned temple.
                    // Resume on the latest safe floor seal behind the saved station.
                    Transform best = spawns[2];
                    foreach (var pad in levelRoots[2].GetComponentsInChildren<CheckpointTrigger>())
                        if (pad.checkpoint != null && pad.checkpoint.position.z <= GameProgress.SavedPosition.z+.1f &&
                            pad.checkpoint.position.z > best.position.z) best = pad.checkpoint;
                    checkpoint = best;
                    MovePlayer(best);
                }
                else MovePlayer(GameProgress.SavedPosition);
                if(tools!=null)
                {
                    if(current!=5&&GameProgress.SavedStaff&&!tools.staff)tools.Give(ToolKind.Staff);
                    if(current==5&&GameProgress.SavedClock&&!tools.clock)tools.Give(ToolKind.Clock);
                }
                if(WorldState.Instance!=null)WorldState.Instance.SetWorld(current==5?WorldKind.Light:GameProgress.SavedWorld);
                if(GameProgress.SavedOrb && Camera.main!=null)
                {
                    var orbs=FindObjectsByType<Orb>(FindObjectsSortMode.None);
                    for(int i=0;i<orbs.Length;i++)
                    {
                        if(orbs[i]!=null&&!orbs[i].carried){orbs[i].Pick(Camera.main.transform);break;}
                    }
                }
            }
            else SaveCurrent();
            var hud=FindAnyObjectByType<GameHUD>();
            if(hud!=null)hud.SetLevel(current);
        }
        public void CompleteLevel(int next,Transform spawnOverride)
        {
            if(next>5)
            {
                GameProgress.Finish();
                var hud=FindAnyObjectByType<GameHUD>();
                if(hud!=null)hud.ShowWin();
                return;
            }
            GameProgress.Unlock(next);
            Begin(next);
        }
        void ShowOnly(int level)
        {
            if(levelRoots==null)return;
            for(int i=0;i<levelRoots.Length;i++)
                if(levelRoots[i]!=null)levelRoots[i].SetActive(i==level-1);
        }
    }
}
