using UnityEngine;

namespace EchoIslands
{
    /// <summary>
    /// Runtime setup for Level 1 on the Polytope Environment_Free map.
    /// The terrain stays shared; only the bridge and added puzzle objects switch worlds.
    /// </summary>
    public class EnvironmentFreeL1Bootstrap : MonoBehaviour
    {
        public Transform player;
        public Transform staffSpawn;
        public Transform portalSpawn;
        public Transform orbSpawn;
        public float staffGroundOffset = 0.9f;
        public float portalGroundOffset = 0f;
        public float orbGroundOffset = 0.8f;
        public float placementRayHeight = 250f;
        public float placementRayDistance = 500f;

        ToolPickup staffPickup;
        Orb orb;
        PortalFrame portal;
        bool staffTaken;

        static readonly Color OrbColor = new Color(0.02f, 0.16f, 0.15f, 1f);
        static readonly Color Infection = new Color(0.05f, 0.55f, 0.48f, 1f);

        void Awake()
        {
            ToolPickup.Picked += OnToolPicked;
        }

        void Start()
        {
            if (WorldState.Instance != null)
                WorldState.Instance.SetWorld(WorldKind.Light);

            SetupBridge();
            SpawnStaff();
            SpawnOrb();
            SpawnPortal();
            EchoVisualOverhaul.ReskinAll();
        }

        void OnDestroy()
        {
            ToolPickup.Picked -= OnToolPicked;
        }

        void SetupBridge()
        {
            var bridge = GameObject.Find("PT_Wooden_Bridge_02");
            if (bridge == null)
                return;

            var variant = bridge.GetComponent<WorldVariant>();
            if (variant == null)
                variant = bridge.AddComponent<WorldVariant>();
            variant.presence = WorldPresence.Dark;
            variant.affectColliders = true;
            variant.affectLights = true;
            variant.useTransitionWave = true;
            variant.useSoftFade = true;
            variant.Refresh();

            var oldBarrier = bridge.transform.parent != null ? bridge.transform.parent.Find("L1_BridgeWorldBarrier") : null;
            if (oldBarrier != null)
                return;

            var barrierObject = new GameObject("L1_BridgeWorldBarrier");
            var barrier = barrierObject.AddComponent<BridgeWorldBarrier>();
            barrier.ConfigureForBridge(bridge.transform);
        }

        void SpawnStaff()
        {
            if (staffPickup != null || staffSpawn == null)
                return;

            var prefab = Resources.Load<GameObject>("EchoL1/AirStaff");
            if (prefab == null)
                return;

            var go = Instantiate(prefab, GroundPosition(staffSpawn.position, staffGroundOffset), Quaternion.identity);
            go.name = "L1_StaffPickup_Air";
            go.transform.rotation = Quaternion.Euler(0f, 175f, 0f);
            go.transform.localScale = Vector3.one * 1.08f;
            StaffSurface.Apply(go);

            var pickupCollider = go.GetComponent<Collider>();
            if (pickupCollider == null)
            {
                var capsule = go.AddComponent<CapsuleCollider>();
                capsule.direction = 1;
                capsule.center = new Vector3(0f, 0.05f, 0f);
                capsule.height = 1.9f;
                capsule.radius = 0.2f;
                pickupCollider = capsule;
            }
            pickupCollider.isTrigger = true;

            var pickup = go.AddComponent<ToolPickup>();
            pickup.kind = ToolKind.Staff;
            pickup.hint = "E — взять посох";
            pickup.pickupDuration = 0.55f;
            pickup.animatePickup = true;
            staffPickup = pickup;
        }

        void SpawnOrb()
        {
            if (orb != null)
                return;

            Vector3 marker;
            if (orbSpawn != null)
            {
                marker = orbSpawn.position;
            }
            else if (staffSpawn != null && portalSpawn != null)
            {
                marker = Vector3.Lerp(staffSpawn.position, portalSpawn.position, 0.42f) + new Vector3(5f, 0f, 0f);
            }
            else if (portalSpawn != null)
            {
                marker = portalSpawn.position + new Vector3(-4f, 0f, 0f);
            }
            else
            {
                return;
            }

            var go = new GameObject("L1_DarkWorldOrb");
            go.transform.position = GroundPosition(marker, orbGroundOffset);
            BuildOrbVisual(go.transform);

            var orbCollider = go.AddComponent<SphereCollider>();
            orbCollider.isTrigger = true;
            orbCollider.radius = 0.34f;

            orb = go.AddComponent<Orb>();
            orb.home = WorldKind.Dark;

            var variant = go.AddComponent<WorldVariant>();
            variant.presence = WorldPresence.Dark;
            variant.affectColliders = true;
            variant.useTransitionWave = true;
            variant.useSoftFade = true;
            variant.Refresh();
        }

        void SpawnPortal()
        {
            if (portal != null || portalSpawn == null)
                return;

            var root = new GameObject("L1_ExitPortal");
            root.transform.position = GroundPosition(portalSpawn.position, portalGroundOffset);
            root.transform.rotation = portalSpawn.rotation;

            portal = root.AddComponent<PortalFrame>();
            portal.requiredOrbs = 1;
            portal.nextLevel = 2;
            portal.completeOnEnter = true;
            portal.activeVisuals = new Renderer[0];

            var socketRoot = new GameObject("PortalOrbSocket");
            socketRoot.transform.SetParent(root.transform, false);
            socketRoot.transform.localPosition = new Vector3(0f, 0.42f, -0.85f);
            var socket = socketRoot.AddComponent<OrbSocket>();
            socket.socket = socketRoot.transform;
            socket.targetPortal = portal;

            BuildPortalVisual(root.transform);
            var dish = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            dish.name = "SocketDish";
            dish.transform.SetParent(socketRoot.transform, false);
            dish.transform.localPosition = Vector3.zero;
            dish.transform.localScale = new Vector3(0.42f, 0.05f, 0.42f);
            SetMaterial(dish, CreateMaterial(new Color(0.08f, 0.09f, 0.09f), 0f));
            Destroy(dish.GetComponent<Collider>());
        }

        void BuildPortalVisual(Transform root)
        {
            var shader = Shader.Find("Echo/InfectedPortal");
            Material rift = shader != null ? new Material(shader) : CreateTransparentMaterial(new Color(0.08f, 0.55f, 0.48f, 0.9f));
            if (rift.HasProperty("_Color")) rift.SetColor("_Color", new Color(0.12f, 0.78f, 0.66f, 1f));
            if (rift.HasProperty("_Core")) rift.SetColor("_Core", new Color(0.0f, 0.012f, 0.012f, 1f));

            var tear = GameObject.CreatePrimitive(PrimitiveType.Quad);
            tear.name = "RiftTear";
            tear.transform.SetParent(root, false);
            tear.transform.localPosition = new Vector3(0f, 1.7f, 0f);
            tear.transform.localScale = new Vector3(1.7f, 3.3f, 1f);
            Destroy(tear.GetComponent<Collider>());
            tear.GetComponent<Renderer>().sharedMaterial = rift;

            var halo = GameObject.CreatePrimitive(PrimitiveType.Quad);
            halo.name = "RiftHalo";
            halo.transform.SetParent(root, false);
            halo.transform.localPosition = new Vector3(0f, 1.7f, -0.04f);
            halo.transform.localScale = new Vector3(2.3f, 3.9f, 1f);
            Destroy(halo.GetComponent<Collider>());
            var haloMat = new Material(rift);
            if (haloMat.HasProperty("_Color")) haloMat.SetColor("_Color", new Color(0.05f, 0.35f, 0.30f, 1f));
            halo.GetComponent<Renderer>().sharedMaterial = haloMat;

            var pool = GameObject.CreatePrimitive(PrimitiveType.Quad);
            pool.name = "RiftPool";
            pool.transform.SetParent(root, false);
            pool.transform.localPosition = new Vector3(0f, 0.04f, 0.2f);
            pool.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            pool.transform.localScale = new Vector3(2.2f, 1.3f, 1f);
            Destroy(pool.GetComponent<Collider>());
            pool.GetComponent<Renderer>().sharedMaterial = rift;

            var lightGo = new GameObject("RiftLight");
            lightGo.transform.SetParent(root, false);
            lightGo.transform.localPosition = new Vector3(0f, 1.6f, 0.3f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.15f, 0.7f, 0.62f);
            light.intensity = 0.7f;
            light.range = 4.5f;
            light.shadows = LightShadows.None;
        }

        void BuildOrbVisual(Transform root)
        {
            // Mesh sphere looks like a flat turquoise ball if the custom shader is missing.
            // Particle energy shell is built by EchoVisualOverhaul.ReskinOrb.
            var orb = root.GetComponent<Orb>();
            if (orb != null)
                EchoVisualOverhaul.ReskinOrb(orb);
        }


        void OnToolPicked(ToolKind kind)
        {
            if (kind != ToolKind.Staff || staffTaken)
                return;
            staffTaken = true;
        }

        Vector3 GroundPosition(Vector3 marker, float offset)
        {
            var terrains = FindObjectsByType<Terrain>(FindObjectsSortMode.None);
            for (int i = 0; i < terrains.Length; i++)
            {
                var terrain = terrains[i];
                if (terrain == null || terrain.terrainData == null) continue;
                Vector3 origin = terrain.transform.position;
                Vector3 size = terrain.terrainData.size;
                if (marker.x < origin.x || marker.x > origin.x + size.x || marker.z < origin.z || marker.z > origin.z + size.z)
                    continue;
                float y = terrain.SampleHeight(new Vector3(marker.x, 0f, marker.z)) + origin.y;
                return new Vector3(marker.x, y + offset, marker.z);
            }

            Vector3 rayStart = new Vector3(marker.x, placementRayHeight, marker.z);
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, placementRayDistance, ~0, QueryTriggerInteraction.Ignore))
                return hit.point + Vector3.up * offset;
            return marker + Vector3.up * offset;
        }

        static Material CreateMaterial(Color color, float emission)
        {
            var shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            var mat = new Material(shader);
            if (mat.HasProperty("_Color")) mat.color = color;
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * emission);
            }
            return mat;
        }

        static Material CreateTransparentMaterial(Color color)
        {
            var mat = CreateMaterial(color, 2f);
            if (mat.HasProperty("_Mode")) mat.SetFloat("_Mode", 3f);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
            mat.color = color;
            return mat;
        }

        static void SetMaterial(GameObject go, Material material)
        {
            var renderers = go.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                    renderers[i].sharedMaterial = material;
            }
        }
    }

    public class OrbShardSpin : MonoBehaviour
    {
        public float speed = 30f;

        void Update()
        {
            transform.Rotate(Vector3.up, speed * Time.deltaTime, Space.World);
            var p = transform.localPosition;
            float a = speed * Time.deltaTime * Mathf.Deg2Rad;
            float x = p.x * Mathf.Cos(a) - p.z * Mathf.Sin(a);
            float z = p.x * Mathf.Sin(a) + p.z * Mathf.Cos(a);
            p.x = x;
            p.z = z;
            p.y = Mathf.Sin(Time.time * 1.6f + speed) * 0.04f;
            transform.localPosition = p;
        }
    }
}
