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

        public const float FinalTime = 390f;
        public FirstPersonController Controller { get; private set; }
        public bool HasBegun { get; private set; }
        public float Level5Remaining { get; private set; } = FinalTime;
        CheckpointTrigger checkpoint;
        GameObject generatedDungeon;
        DungeonVisibility visibility;
        PortalFrame levelPortal;
        Orb levelOrb;
        Camera playCamera;
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
            Application.targetFrameRate = 60;
            if (levelRoots == null || levelRoots.Length < 5) System.Array.Resize(ref levelRoots,5);
            if (spawns == null || spawns.Length < 5) System.Array.Resize(ref spawns,5);
            Controller = player != null ? player.GetComponent<FirstPersonController>() : null;
            LevelOneSceneRepair.EnsureInstalled();
        }

        void Start()
        {
            hud = FindAnyObjectByType<GameHUD>();
            playCamera = Camera.main;
            if (playCamera != null)
            {
                playCamera.allowHDR = false;
                playCamera.allowMSAA = false;
                playCamera.farClipPlane = 115f;
            }
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
            if (visibility != null) visibility.Refresh(position);
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
            if (hud != null && !string.IsNullOrEmpty(next.hint)) hud.Notify(next.hint,3.8f);
        }

        public void SaveCurrent()
        {
            if (!HasBegun || player == null || finished) return;
            Transform anchor = checkpoint != null ? checkpoint.checkpoint : spawns[current-1];
            bool carriedOrb = levelOrb != null && levelOrb.carried;
            GameProgress.Save(current,anchor != null ? anchor.position : player.position,
                tools != null && tools.staff,tools != null && tools.clock,
                current != 5 && WorldState.Instance != null ? WorldState.Instance.Target : WorldKind.Light,
                carriedOrb || (levelPortal != null && levelPortal.IsAbsorbing),Level5Remaining,checkpoint != null ? checkpoint.id : 0,
                levelPortal != null && levelPortal.HasOrb,levelPortal != null && levelPortal.HasStaff);
        }

        public void Begin(int level, bool restore = false)
        {
            if (generatedDungeon != null)
            {
                generatedDungeon.SetActive(false);
                Destroy(generatedDungeon);
                generatedDungeon = null;
                visibility = null;
            }
            foreach (var orb in FindObjectsByType<Orb>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                if (orb != null && orb.carried)
                {
                    orb.gameObject.SetActive(false);
                    Destroy(orb.gameObject);
                }

            current = Mathf.Clamp(level,1,5);
            if (Controller != null) Controller.SetDungeonMovement(current > 1);
            if (playCamera != null) playCamera.farClipPlane = current == 1 ? 115f : current == 4 ? 26f : 35f;
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
                generatedDungeon = DungeonCampaignBuilder.Build(current,player,out var spawn);
                visibility = generatedDungeon.GetComponent<DungeonVisibility>();
                levelRoots[current-1] = generatedDungeon;
                spawns[current-1] = spawn;
            }

            for (int i=0; i<levelRoots.Length; i++)
                if (levelRoots[i] != null) levelRoots[i].SetActive(i == current-1);
            var root = levelRoots[current-1];
            levelPortal = root != null ? root.GetComponentInChildren<PortalFrame>(true) : null;
            levelOrb = root != null ? root.GetComponentInChildren<Orb>(true) : null;
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
            // Checkpoints from earlier floor plans no longer map to safe landings.
            // The saved chapter and campaign unlocks still apply.
            if (current > 1 && !GameProgress.HasCurrentDungeonLayout)
            {
                SaveCurrent();
                return;
            }

            var root = levelRoots[current-1];
            if (root == null) return;
            if (GameProgress.SavedCheckpoint > 0)
            {
                foreach (var candidate in root.GetComponentsInChildren<CheckpointTrigger>(true))
                    if (candidate.id == GameProgress.SavedCheckpoint)
                    {
                        checkpoint = candidate;
                        MovePlayer(candidate.checkpoint);
                        break;
                    }
            }
            else if (current == 1 && !GameProgress.HasCheckpointFormat)
                MovePlayer(GameProgress.SavedPosition,spawns[0] != null ? spawns[0].rotation : Quaternion.identity);

            if (tools != null)
            {
                if (current != 5 && GameProgress.SavedStaff && !tools.staff) tools.Give(ToolKind.Staff);
                if (current == 5 && GameProgress.SavedClock) tools.Give(ToolKind.Clock);
                foreach (var pickup in root.GetComponentsInChildren<ToolPickup>(true))
                    if (pickup.kind == ToolKind.Staff && tools.staff || pickup.kind == ToolKind.Clock && tools.clock)
                        pickup.gameObject.SetActive(false);
            }

            if (current == 5) Level5Remaining = Mathf.Clamp(GameProgress.SavedLevel5Time,1f,FinalTime);
            if (levelPortal != null)
                levelPortal.RestoreRequirements(GameProgress.SavedDepositedOrb,GameProgress.SavedDepositedStaff);
            if (levelOrb != null && GameProgress.SavedDepositedOrb) levelOrb.gameObject.SetActive(false);
            else if (levelOrb != null && GameProgress.SavedOrb && playCamera != null)
            {
                if (!levelOrb.gameObject.activeInHierarchy) levelOrb.transform.SetParent(playCamera.transform,true);
                levelOrb.Pick(playCamera.transform);
            }

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
