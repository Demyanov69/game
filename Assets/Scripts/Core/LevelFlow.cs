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

        public const float FinalTime = 600f;
        public FirstPersonController Controller { get; private set; }
        public bool HasBegun { get; private set; }
        public float Level5Remaining { get; private set; } = FinalTime;
        CheckpointTrigger checkpoint;
        GameObject generatedDungeon;
        GameHUD hud;
        float lastDeath = -10f;
        bool finished;

        public static readonly string[] LevelNames = {
            "", "Берег двух миров", "Залы испытаний", "Перевёрнутый храм",
            "Лабиринт отражений", "Предел времени"
        };

        void Awake()
        {
            Instance = this;
            if (levelRoots == null || levelRoots.Length < 5) System.Array.Resize(ref levelRoots,5);
            if (spawns == null || spawns.Length < 5) System.Array.Resize(ref spawns,5);
            Controller = player != null ? player.GetComponent<FirstPersonController>() : null;
            LevelOneSceneRepair.EnsureInstalled();
        }

        void Start()
        {
            hud = FindAnyObjectByType<GameHUD>();
            for (int i=1; i<levelRoots.Length; i++)
                if (levelRoots[i] != null) levelRoots[i].SetActive(false);
        }

        void Update()
        {
            if (!HasBegun || finished || Time.timeScale <= 0f) return;
            if (current > 1 && player != null && player.position.y < -18f) Respawn();
            if (current != 5) return;
            Level5Remaining = Mathf.Max(0f,Level5Remaining-Time.deltaTime);
            if (Level5Remaining > 0f) return;
            Begin(5);
            if (hud != null) hud.Notify("Время вышло. Хронометр возвращает вас к началу.",4.5f);
        }

        void MovePlayer(Vector3 position, Quaternion rotation)
        {
            if (player == null) return;
            var cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            player.SetPositionAndRotation(position,rotation);
            if (Controller != null) Controller.ResetGravity();
            if (cc != null) cc.enabled = true;
        }

        void MovePlayer(Transform target)
        {
            if (target != null) MovePlayer(target.position,target.rotation);
        }

        public void Respawn()
        {
            if (!HasBegun || finished || Time.time - lastDeath < .25f) return;
            lastDeath = Time.time;
            MovePlayer(checkpoint != null ? checkpoint.checkpoint : spawns[current-1]);
            if (tools != null) tools.CancelClock();
            if (WorldState.Instance != null) WorldState.Instance.SnapTo(WorldKind.Light);
            if (hud != null) hud.Notify("Возрождение у контрольной точки",2.2f);
        }

        public void SetCheckpoint(CheckpointTrigger next)
        {
            if (!HasBegun || next == null || next.checkpoint == null ||
                (checkpoint != null && checkpoint.id >= next.id)) return;
            checkpoint = next;
            SaveCurrent();
            if (hud != null) hud.ShowRoom(next.roomTitle,next.roomHint);
        }

        public void SaveCurrent()
        {
            if (!HasBegun || player == null || finished) return;
            Transform anchor = checkpoint != null ? checkpoint.checkpoint : spawns[current-1];
            var root = levelRoots[current-1];
            var portal = root != null ? root.GetComponentInChildren<PortalFrame>() : null;
            var orbs = FindObjectsByType<Orb>(FindObjectsSortMode.None);
            bool carriedOrb = false;
            foreach (var orb in orbs)
                if (orb.carried) { carriedOrb = true; break; }
            GameProgress.Save(current,anchor != null ? anchor.position : player.position,
                tools != null && tools.staff,tools != null && tools.clock,
                current != 5 && WorldState.Instance != null ? WorldState.Instance.Target : WorldKind.Light,
                carriedOrb || (portal != null && portal.IsAbsorbing),Level5Remaining,checkpoint != null ? checkpoint.id : 0,
                portal != null && portal.HasOrb,portal != null && portal.HasStaff);
        }

        public void Begin(int level, bool restore = false)
        {
            if (generatedDungeon != null)
            {
                generatedDungeon.SetActive(false);
                Destroy(generatedDungeon);
                generatedDungeon = null;
            }
            foreach (var orb in FindObjectsByType<Orb>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                if (orb != null && orb.carried)
                {
                    orb.gameObject.SetActive(false);
                    Destroy(orb.gameObject);
                }

            current = Mathf.Clamp(level,1,5);
            HasBegun = true;
            finished = false;
            checkpoint = null;
            lastDeath = -10f;
            Level5Remaining = FinalTime;

            if (tools != null) tools.ClearTools();
            var world = WorldState.Instance;
            if (world != null)
            {
                world.debugToggleEnabled = false;
                world.SnapTo(WorldKind.Light);
            }

            if (current > 1)
            {
                generatedDungeon = DungeonCampaignBuilder.Build(current,out var spawn);
                levelRoots[current-1] = generatedDungeon;
                spawns[current-1] = spawn;
            }

            for (int i=0; i<levelRoots.Length; i++)
                if (levelRoots[i] != null) levelRoots[i].SetActive(i == current-1);
            MovePlayer(spawns[current-1]);

            if (tools != null && current >= 2 && current <= 4) tools.Give(ToolKind.Staff);
            if (tools != null && current == 5) tools.clockDuration = 7.5f;

            if (hud == null) hud = FindAnyObjectByType<GameHUD>();
            if (hud != null) hud.SetLevel(current);

            if (restore && GameProgress.HasSave && GameProgress.SavedLevel == current)
                Restore();
            else SaveCurrent();
        }

        void Restore()
        {
            // Old dungeon layouts used a single corridor and their coordinates cannot
            // identify a safe spot in the new rooms. Keep the chapter and its unlocks.
            if (!GameProgress.HasCheckpointFormat && current > 1)
            {
                SaveCurrent();
                return;
            }

            var root = levelRoots[current-1];
            if (root == null) return;
            if (GameProgress.SavedCheckpoint > 0)
            {
                foreach (var candidate in root.GetComponentsInChildren<CheckpointTrigger>())
                    if (candidate.id == GameProgress.SavedCheckpoint)
                    {
                        checkpoint = candidate;
                        MovePlayer(candidate.checkpoint);
                        if (hud != null) hud.ShowRoom(candidate.roomTitle,candidate.roomHint);
                        break;
                    }
            }
            else if (current == 1 && !GameProgress.HasCheckpointFormat)
                MovePlayer(GameProgress.SavedPosition,spawns[0] != null ? spawns[0].rotation : Quaternion.identity);

            if (tools != null)
            {
                if (current != 5 && GameProgress.SavedStaff && !tools.staff) tools.Give(ToolKind.Staff);
                if (current == 5 && GameProgress.SavedClock) tools.Give(ToolKind.Clock);
                foreach (var pickup in root.GetComponentsInChildren<ToolPickup>())
                    if (pickup.kind == ToolKind.Staff && tools.staff || pickup.kind == ToolKind.Clock && tools.clock)
                        pickup.gameObject.SetActive(false);
            }

            if (current == 5) Level5Remaining = Mathf.Clamp(GameProgress.SavedLevel5Time,1f,FinalTime);
            var portal = root.GetComponentInChildren<PortalFrame>();
            var orb = root.GetComponentInChildren<Orb>();
            if (portal != null) portal.RestoreRequirements(GameProgress.SavedDepositedOrb,GameProgress.SavedDepositedStaff);
            if (orb != null && GameProgress.SavedDepositedOrb) orb.gameObject.SetActive(false);
            else if (orb != null && GameProgress.SavedOrb && Camera.main != null)
                orb.Pick(Camera.main.transform);

            if (WorldState.Instance != null)
                WorldState.Instance.SnapTo(current == 5 || GameProgress.SavedDepositedStaff ? WorldKind.Light : GameProgress.SavedWorld);
        }

        public void CompleteLevel(int next, Transform spawnOverride)
        {
            if (finished) return;
            if (next > 5)
            {
                finished = true;
                GameProgress.Finish();
                if (hud != null) hud.ShowWin();
                return;
            }
            GameProgress.Unlock(next);
            Begin(next);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
