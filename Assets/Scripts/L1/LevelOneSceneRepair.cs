using System.Collections.Generic;
using UnityEngine;

namespace EchoIslands
{
    /// <summary>
    /// Level 1 crossing: the river is impassable in Light, while the wooden bridge
    /// is both VISIBLE and WALKABLE in Dark. Keeps the original Polytope bridge mesh
    /// and materials; only the invisible collision geometry is generated here.
    /// </summary>
    public sealed class LevelOneSceneRepair : MonoBehaviour
    {
        // These dimensions correspond to the PT_Wooden_Bridge_02 prefab used in Main.unity.
        // The bridge is oriented along local +X, with its deck centred ~1.3m from its pivot.
        const float RiverWallLength = 170f;
        const float RiverWallHeight = 35f;
        const float HalfCrossingWidth = 2.8f;
        const float DeckCenterX = 1.30f;
        const float DeckCenterZ = -0.11f;
        const float DeckLength = 5.10f;
        const float DeckWidth = 1.90f;
        const float DeckThickness = 0.22f;

        readonly List<Collider> darkBridgeColliders = new List<Collider>();
        BoxCollider lightOnlyGate;
        bool lastDark;
        bool setupFinished;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void EnsureInstalled()
        {
            var marker = GameObject.Find("L1_BridgeBarrier_LightOnly");
            if (marker == null || GameObject.Find("__REPAIRED_L1_CROSSING") != null) return;

            var root = new GameObject("__REPAIRED_L1_CROSSING");
            root.transform.SetParent(marker.transform.parent, false);
            root.AddComponent<LevelOneSceneRepair>();
        }

        void Start()
        {
            var bridge = GameObject.Find("PT_Wooden_Bridge_02");
            if (bridge == null)
            {
                Debug.LogWarning("Level 1: PT_Wooden_Bridge_02 is missing; river collision not configured.");
                return;
            }

            // The authored bridge is under L1_DarkBridge, whose WorldVariant used
            // to shrink the WHOLE parent to 4% in Light. That also shifted the
            // bridge's reported WORLD position towards (0,0,0). The old river
            // repair then placed the permanent banks across the real bridge!
            // Keep the parent at its original 1:1 scale, animate the renderers
            // through PhaseDissolve instead of scaling the entire scene branch.
            var bridgeVisualRoot = bridge.transform.parent;
            var bridgePhase = bridgeVisualRoot != null
                ? bridgeVisualRoot.GetComponent<WorldVariant>() : null;
            if (bridgePhase != null && bridgePhase.presence == WorldPresence.Dark)
            {
                bridgePhase.scaleAnimMaxExtent = 0.01f;
                bridgePhase.useTransitionWave = false;
                bridgePhase.useSoftFade = true;
                bridgePhase.affectColliders = false;
                bridgeVisualRoot.localScale = Vector3.one;
                bridgePhase.Refresh();
            }

            // The former short obstacle and its WorldVariant must not continue to
            // block the new gate in Dark (or reactivate their colliders later).
            var oldGate = GameObject.Find("L1_BridgeBarrier_LightOnly");
            if (oldGate != null)
            {
                foreach (var variant in oldGate.GetComponentsInChildren<WorldVariant>(true))
                    variant.enabled = false;
                foreach (var col in oldGate.GetComponentsInChildren<Collider>(true))
                    col.enabled = false;
            }

            // An earlier level-one bootstrap can also create a two-panel invisible
            // barrier. Its sliding panels do not reliably clear the actual doorway.
            var oldSlidingGate = GameObject.Find("L1_BridgeWorldBarrier");
            if (oldSlidingGate != null) Destroy(oldSlidingGate);

            Vector3 center = new Vector3(
                oldGate != null ? oldGate.transform.position.x : bridge.transform.position.x,
                10f,
                oldGate != null ? oldGate.transform.position.z : bridge.transform.position.z);

            // Continuous banks make going around the crossing impossible. Only the
            // centre doorway changes worlds, not these long flanking river walls.
            CreateBarrier("North river bank", center + Vector3.forward * (HalfCrossingWidth + RiverWallLength * 0.5f),
                new Vector3(1.2f, RiverWallHeight, RiverWallLength));
            CreateBarrier("South river bank", center - Vector3.forward * (HalfCrossingWidth + RiverWallLength * 0.5f),
                new Vector3(1.2f, RiverWallHeight, RiverWallLength));

            lightOnlyGate = CreateBarrier("Light world: closed river crossing", center,
                new Vector3(1.2f, RiverWallHeight, HalfCrossingWidth * 2f + 0.1f));

            CreateDarkBridgeCollision(bridge.transform);
            setupFinished = true;
            UpdateWorldCollision(force: true);

            // Preserve the positioning and VFX repairs from the preceding update.
            var orb = GameObject.Find("L1_DarkWorldOrb");
            if (orb != null)
            {
                var pos = orb.transform.position;
                pos.y = GroundHeight(pos) + 1.55f;
                orb.transform.position = pos;
                EchoVisualOverhaul.ReskinOrb(orb.GetComponent<Orb>());
            }
            var portal = GameObject.Find("L1_ExitPortal");
            if (portal != null)
            {
                var pos = portal.transform.position;
                pos.y = GroundHeight(pos) + 0.16f;
                portal.transform.position = pos;
                portal.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                var socket = portal.transform.Find("OrbSocket");
                if (socket != null) socket.localPosition = new Vector3(2.55f, 1.25f, -0.95f);
                var frame = portal.GetComponent<PortalFrame>();
                if (frame != null) EchoVisualOverhaul.ReskinPortal(frame);
            }

            // The open-air tutorial now has the same safe respawn system as the
            // dungeons: staff, far river bank, spectral orb and final gateway.
            InstallFirstLevelCheckpoints(bridge.transform, orb, portal);
        }

        void InstallFirstLevelCheckpoints(Transform bridge, GameObject orb, GameObject portal)
        {
            var staff = GameObject.Find("L1_StaffPickup_Air");
            if (staff != null)
                MakeFirstLevelCheckpoint(1,"Берег посоха","Наведите взгляд на парящий посох и нажмите E.",staff.transform.position);

            // The bank on the exit side of the repaired wooden bridge is solid
            // in both worlds. Never put the checkpoint on its disappearing deck.
            Vector3 farBank = bridge.TransformPoint(new Vector3(5.8f, 0f, DeckCenterZ));
            MakeFirstLevelCheckpoint(2,"Другой берег","Сфера видна в тёмном мире. Ищите частицы вдоль тропы.",farBank);

            if (orb != null)
                MakeFirstLevelCheckpoint(3,"Сфера эха","Нажмите E, чтобы забрать сферу.",orb.transform.position);
            if (portal != null)
                MakeFirstLevelCheckpoint(4,"Первый портал","Поднесите сферу к порталу и нажмите E.",
                    portal.transform.position - portal.transform.forward * 3.5f);
        }

        void MakeFirstLevelCheckpoint(int id, string title, string hint, Vector3 near)
        {
            if (transform.Find("L1_Checkpoint_" + id) != null) return;
            float ground = GroundHeight(near);
            Vector3 position = new Vector3(near.x, ground + 1.1f, near.z);
            var point = new GameObject("L1_Checkpoint_" + id);
            point.layer = 2;
            point.transform.SetParent(transform, true);
            point.transform.position = position;
            var range = point.AddComponent<SphereCollider>();
            range.isTrigger = true;
            range.radius = 1.8f;
            var body = point.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;

            var respawn = new GameObject("Respawn " + title).transform;
            respawn.SetParent(point.transform, false);
            respawn.localPosition = new Vector3(0f, .20f, 0f);
            var start = LevelFlow.Instance != null && LevelFlow.Instance.spawns.Length > 0 ? LevelFlow.Instance.spawns[0] : null;
            if (start != null) respawn.rotation = start.rotation;
            var trigger = point.AddComponent<CheckpointTrigger>();
            trigger.checkpoint = respawn;
            trigger.id = id;
            trigger.roomTitle = title;
            trigger.roomHint = hint;

            // Visual marker - textured bronze/iron disk, non-blocking for walking.
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "Ancient checkpoint marker";
            marker.transform.SetParent(point.transform, false);
            marker.transform.localPosition = new Vector3(0f, -1.07f, 0f);
            marker.transform.localScale = new Vector3(.94f, .045f, .94f);
            var markerCollider = marker.GetComponent<Collider>();
            if (markerCollider != null) Destroy(markerCollider);
            var r = marker.GetComponent<MeshRenderer>();
            var texture = Resources.Load<Texture2D>("EchoDungeon/Textures/RuneTiles");
            var shader = Shader.Find("Echo/DungeonTriplanar");
            if (r != null && shader != null && texture != null)
            {
                var material = new Material(shader) { name = "Checkpoint rune material" };
                material.SetTexture("_MainTex", texture);
                var dark = Resources.Load<Texture2D>("EchoDungeon/Textures/RuneTiles_Dark");
                if (dark != null) material.SetTexture("_DarkTex", dark);
                material.SetFloat("_TileScale", 1f);
                material.SetFloat("_DarkGlow", .2f);
                r.sharedMaterial = material;
            }
        }

        void LateUpdate()
        {
            if (setupFinished) UpdateWorldCollision(force: false);
        }

        void UpdateWorldCollision(bool force)
        {
            // Depend on the actual world, not on a delayed fade, renderer material,
            // or a moving barrier panel. This makes the visible dark bridge usable.
            bool dark = WorldState.Instance != null && WorldState.Instance.Current == WorldKind.Dark;
            if (!force && dark == lastDark) return;

            if (lightOnlyGate != null) lightOnlyGate.enabled = !dark;
            for (int i = 0; i < darkBridgeColliders.Count; i++)
                if (darkBridgeColliders[i] != null) darkBridgeColliders[i].enabled = dark;
            lastDark = dark;
        }

        void CreateDarkBridgeCollision(Transform bridge)
        {
            // The imported bridge has complex MeshColliders on both LODs. Depending
            // on their mesh and the WorldVariant fade they can act like a solid wall
            // or leave a gap under the player's feet. The visual mesh is retained;
            // we use a simple dedicated walkable collision surface instead.
            foreach (var variant in bridge.GetComponentsInParent<WorldVariant>(true))
                variant.affectColliders = false;
            foreach (var variant in bridge.GetComponentsInChildren<WorldVariant>(true))
                variant.affectColliders = false;
            foreach (var collider in bridge.GetComponentsInChildren<Collider>(true))
                collider.enabled = false;

            // World-space collision is kept OUTSIDE the animated bridge hierarchy:
            // soft-fade scaling must never shrink or move the player's floor.
            Vector3 leftBank = bridge.TransformPoint(new Vector3(-3.0f, 0f, DeckCenterZ));
            Vector3 rightBank = bridge.TransformPoint(new Vector3(5.65f, 0f, DeckCenterZ));
            float leftY = GroundHeight(leftBank);
            float rightY = GroundHeight(rightBank);
            float deckTop = Mathf.Max(bridge.position.y + 0.46f,
                                      Mathf.Max(leftY, rightY) + 0.08f);

            Vector3 deckCenter = bridge.TransformPoint(new Vector3(DeckCenterX, 0f, DeckCenterZ));
            deckCenter.y = deckTop - DeckThickness * 0.5f;
            CreateDarkBox("Dark bridge: walkable deck", deckCenter, bridge.rotation,
                new Vector3(DeckLength, DeckThickness, DeckWidth));

            float halfLength = DeckLength * 0.5f;
            CreateRamp("Dark bridge: west approach", bridge, -3.0f, DeckCenterX - halfLength + 0.06f,
                leftY + 0.06f, deckTop, DeckWidth);
            CreateRamp("Dark bridge: east approach", bridge, DeckCenterX + halfLength - 0.06f, 5.65f,
                deckTop, rightY + 0.06f, DeckWidth);

            // Guardrails are physical in Dark, too, so the player doesn't walk off
            // the invisible edge of the collision deck beside the original railing.
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 railPos = bridge.TransformPoint(new Vector3(DeckCenterX, 0f,
                    DeckCenterZ + side * (DeckWidth * 0.5f + 0.08f)));
                railPos.y = deckTop + 0.48f;
                CreateDarkBox(side < 0 ? "Dark bridge: south rail" : "Dark bridge: north rail",
                    railPos, bridge.rotation, new Vector3(DeckLength, 0.96f, 0.14f));
            }
        }

        void CreateRamp(string name, Transform bridge, float xA, float xB,
                        float yA, float yB, float width)
        {
            Vector3 a = bridge.TransformPoint(new Vector3(xA, 0f, DeckCenterZ));
            Vector3 b = bridge.TransformPoint(new Vector3(xB, 0f, DeckCenterZ));
            a.y = yA;
            b.y = yB;
            Vector3 along = (b - a).normalized;
            Vector3 across = bridge.forward;
            Vector3 up = Vector3.Cross(across, along).normalized;
            if (Vector3.Dot(up, Vector3.up) < 0f) up = -up;
            const float thickness = 0.20f;
            Vector3 center = (a + b) * 0.5f - up * (thickness * 0.5f);
            CreateDarkBox(name, center, Quaternion.LookRotation(across, up),
                new Vector3(Vector3.Distance(a, b), thickness, width));
        }

        BoxCollider CreateBarrier(string name, Vector3 position, Vector3 dimensions)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(transform, false);
            obj.transform.position = position;
            var collider = obj.AddComponent<BoxCollider>();
            collider.size = dimensions;
            collider.isTrigger = false;
            return collider;
        }

        void CreateDarkBox(string name, Vector3 position, Quaternion rotation, Vector3 dimensions)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(transform, false);
            obj.transform.SetPositionAndRotation(position, rotation);
            var collider = obj.AddComponent<BoxCollider>();
            collider.size = dimensions;
            collider.isTrigger = false;
            collider.enabled = false;
            darkBridgeColliders.Add(collider);
        }

        static float GroundHeight(Vector3 pos)
        {
            var terrains = Terrain.activeTerrains;
            for (int i = 0; i < terrains.Length; i++)
            {
                var t = terrains[i];
                if (t == null || t.terrainData == null) continue;
                Vector3 origin = t.transform.position;
                Vector3 size = t.terrainData.size;
                if (pos.x >= origin.x && pos.x <= origin.x + size.x &&
                    pos.z >= origin.z && pos.z <= origin.z + size.z)
                    return t.SampleHeight(pos) + origin.y;
            }
            if (Physics.Raycast(new Vector3(pos.x, 250f, pos.z), Vector3.down,
                out RaycastHit hit, 500f, ~0, QueryTriggerInteraction.Ignore))
                return hit.point.y;
            return pos.y;
        }
    }
}
