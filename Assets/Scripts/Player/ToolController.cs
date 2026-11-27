using UnityEngine;

namespace EchoIslands
{
    public class ToolController : MonoBehaviour
    {
        public bool staff;
        public bool flashlight;
        public bool clock;

        public float flashlightRadius = 4.5f;
        public float clockDuration = 4.5f;
        public float useDistance = 3.2f;

        public Transform maskOrigin;

        int flashSlot = -1;
        float clockLeft;
        public float ClockRemaining => clockRunning ? clockLeft : 0f;
        WorldKind clockReturn;
        bool clockRunning;
        GameObject heldStaffVisual;
        GameObject heldClockVisual;
        GameObject heldFlashlightVisual;

        void Update()
        {
            // Restore held models even when a saved tool is granted before the camera
            // has finished initializing. All tool models use right-hand camera space.
            RefreshHeldTool();
            if (WorldState.Instance == null || Time.timeScale <= 0f) return;

            if (staff && Input.GetMouseButtonDown(0))
                WorldState.Instance.Toggle();

            UpdateFlashlight();
            UpdateClock();

            if (Input.GetKeyDown(KeyCode.E))
                TryUse();
        }

        void UpdateFlashlight()
        {
            bool held = flashlight && Input.GetMouseButton(1);
            if (!held)
            {
                if (flashSlot >= 0)
                {
                    WorldState.Instance.ClearMask(flashSlot);
                    flashSlot = -1;
                }
                return;
            }

            if (flashSlot < 0) flashSlot = WorldState.Instance.AllocMask();
            if (flashSlot < 0) return;
            Vector3 origin = maskOrigin != null ? maskOrigin.position : transform.position + Vector3.up * 1.4f;
            float opposite = WorldState.Instance.Target == WorldKind.Dark ? 0f : 1f;
            WorldState.Instance.SetMask(flashSlot, origin, flashlightRadius, opposite);
        }

        public void CancelClock()
        {
            if (clockRunning && WorldState.Instance != null)
                WorldState.Instance.SetWorld(clockReturn);
            clockRunning = false;
            clockLeft = 0f;
        }

        void UpdateClock()
        {
            if (clock && !clockRunning && Input.GetKeyDown(KeyCode.Q))
            {
                clockReturn = WorldState.Instance.Target;
                WorldState.Instance.Toggle();
                clockLeft = clockDuration;
                clockRunning = true;
            }
            if (!clockRunning) return;
            clockLeft -= Time.deltaTime;
            if (clockLeft <= 0f)
            {
                WorldState.Instance.SetWorld(clockReturn);
                clockRunning = false;
            }
        }

        void TryUse()
        {
            Vector3 origin = maskOrigin != null ? maskOrigin.position : transform.position + Vector3.up * 1.5f;
            Vector3 dir = maskOrigin != null ? maskOrigin.forward : transform.forward;
            if (!Physics.Raycast(origin, dir, out RaycastHit hit, useDistance, ~0, QueryTriggerInteraction.Collide)) return;

            var pickup = hit.collider.GetComponentInParent<ToolPickup>();
            if (pickup != null)
            {
                pickup.Take(this);
                return;
            }

            var orb = hit.collider.GetComponentInParent<Orb>();
            if (orb != null && !orb.carried && WorldState.Instance.IsReal(orb.home, orb.transform.position))
            {
                var cam = Camera.main;
                if (cam != null)
                {
                    orb.Pick(cam.transform);
                    if (LevelFlow.Instance != null) LevelFlow.Instance.SaveCurrent();
                }
                return;
            }

            var beacon = hit.collider.GetComponentInParent<Beacon>();
            if (beacon != null)
            {
                beacon.Toggle();
                return;
            }

            var portal = hit.collider.GetComponentInParent<PortalFrame>();
            if (portal != null)
            {
                portal.TryInsertHeld();
                return;
            }

            var socket = hit.collider.GetComponentInParent<OrbSocket>();
            if (socket != null)
            {
                if (socket.targetPortal != null) socket.targetPortal.TryInsertHeld();
                else socket.TryInsertHeld();
            }
        }

        public void ClearTools()
        {
            if (heldStaffVisual != null)
            {
                Destroy(heldStaffVisual);
                heldStaffVisual = null;
            }

            if (heldClockVisual != null) Destroy(heldClockVisual);
            if (heldFlashlightVisual != null) Destroy(heldFlashlightVisual);
            heldClockVisual = null;
            heldFlashlightVisual = null;

            staff = false;
            flashlight = false;
            clock = false;
            CancelClock();
            if (flashSlot >= 0 && WorldState.Instance != null)
                WorldState.Instance.ClearMask(flashSlot);
            flashSlot = -1;
        }

        public void Give(ToolKind kind)
        {
            if (kind == ToolKind.Staff)
            {
                staff = true;
                AttachHeldStaff();
            }
            if (kind == ToolKind.Flashlight) flashlight = true;
            if (kind == ToolKind.Clock) clock = true;
            RefreshHeldTool();
        }

        void RefreshHeldTool()
        {
            // A single right hand: the staff has priority over the watch/torch.
            // The aether orb is owned separately by Orb, on negative local X.
            if (staff && heldStaffVisual == null) AttachHeldStaff();
            if (!staff && clock && heldClockVisual == null) AttachHeldClock();
            if (!staff && !clock && flashlight && heldFlashlightVisual == null)
                AttachHeldFlashlight();

            if (heldStaffVisual != null) heldStaffVisual.SetActive(staff);
            if (heldClockVisual != null) heldClockVisual.SetActive(!staff && clock);
            if (heldFlashlightVisual != null) heldFlashlightVisual.SetActive(!staff && !clock && flashlight);
        }

        void AttachHeldStaff()
        {
            if (heldStaffVisual != null) return;
            var prefab = Resources.Load<GameObject>("EchoL1/AirStaff");
            var cam = Camera.main;
            if (prefab == null || cam == null) return;

            heldStaffVisual = Instantiate(prefab, cam.transform);
            heldStaffVisual.name = "HeldStaff";
            heldStaffVisual.transform.localPosition = new Vector3(0.42f, -0.34f, 0.62f);
            heldStaffVisual.transform.localRotation = Quaternion.Euler(18f, -28f, 10f);
            heldStaffVisual.transform.localScale = Vector3.one * 0.42f;
            StaffSurface.Apply(heldStaffVisual);

            var colliders = heldStaffVisual.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
                if (colliders[i] != null) colliders[i].enabled = false;

            var rigidbodies = heldStaffVisual.GetComponentsInChildren<Rigidbody>(true);
            for (int i = 0; i < rigidbodies.Length; i++)
                if (rigidbodies[i] != null) rigidbodies[i].isKinematic = true;
        }
        void AttachHeldClock()
        {
            var cam = Camera.main;
            if (cam == null) return;

            heldClockVisual = new GameObject("HeldChronometer_RightHand");
            var root = heldClockVisual.transform;
            root.SetParent(cam.transform, false);
            root.localPosition = new Vector3(.43f, -.33f, .69f);
            root.localRotation = Quaternion.Euler(12f, -16f, -12f);
            root.localScale = Vector3.one * .77f;

            // An ornamented pocket-watch, not an intrusive HUD plate.
            // The ring and ivory dial face the camera; the world-shift effect
            // itself is still controlled by the existing Q key logic.
            BuildWatchFace(root);
        }

        public static void BuildWatchFace(Transform root)
        {
            var rim = HeldMaterial("Watch - aged gold", new Color(.81f, .60f, .24f), .22f);
            var dial = HeldMaterial("Watch - ivory face", new Color(.82f, .86f, .82f), .08f);
            var ink = HeldMaterial("Watch - dark ticks", new Color(.10f, .20f, .22f), .04f);
            var gem = HeldMaterial("Watch - glowing heart", new Color(.08f, .82f, .77f), 1.8f);
            Quaternion faceRotation = Quaternion.Euler(-90f, 0f, 0f);

            HeldPiece(root, PrimitiveType.Cylinder, "Engraved gold bezel", Vector3.zero,
                new Vector3(.235f, .035f, .235f), faceRotation, rim);
            HeldPiece(root, PrimitiveType.Cylinder, "Pale clock dial", new Vector3(0f, 0f, -.035f),
                new Vector3(.193f, .015f, .193f), faceRotation, dial);
            for (int i = 0; i < 12; i++)
            {
                float angle = i * Mathf.PI / 6f;
                HeldPiece(root, PrimitiveType.Cube, "Hour mark " + (i + 1),
                    new Vector3(Mathf.Sin(angle) * .158f, Mathf.Cos(angle) * .158f, -.055f),
                    new Vector3(.014f, i % 3 == 0 ? .038f : .021f, .008f),
                    Quaternion.Euler(0f, 0f, -i * 30f), ink);
            }
            HeldPiece(root, PrimitiveType.Cube, "Hour hand", new Vector3(.016f, .045f, -.066f),
                new Vector3(.014f, .105f, .010f), Quaternion.Euler(0f, 0f, -18f), ink);
            HeldPiece(root, PrimitiveType.Cube, "Minute hand", new Vector3(-.04f, -.03f, -.067f),
                new Vector3(.011f, .129f, .010f), Quaternion.Euler(0f, 0f, -52f), ink);
            HeldPiece(root, PrimitiveType.Sphere, "Aether clock crystal", new Vector3(0f, 0f, -.078f),
                Vector3.one * .040f, Quaternion.identity, gem);
            HeldPiece(root, PrimitiveType.Cylinder, "Suspension knob", new Vector3(0f, .25f, 0f),
                new Vector3(.052f, .042f, .052f), Quaternion.identity, rim);
        }

        void AttachHeldFlashlight()
        {
            var cam = Camera.main;
            if (cam == null) return;
            // Reuse the original dungeon torch model, with its own materials.
            var prefab = Resources.Load<GameObject>("EchoDungeon/Torch");
            if (prefab == null) return;
            heldFlashlightVisual = Instantiate(prefab, cam.transform);
            heldFlashlightVisual.name = "HeldTorch_RightHand";
            var t = heldFlashlightVisual.transform;
            t.localPosition = new Vector3(.44f, -.46f, .72f);
            t.localRotation = Quaternion.Euler(16f, -24f, -14f);
            t.localScale = Vector3.one * .27f;
            foreach (var c in heldFlashlightVisual.GetComponentsInChildren<Collider>(true))
                if (c != null) c.enabled = false;
            foreach (var r in heldFlashlightVisual.GetComponentsInChildren<Rigidbody>(true))
                if (r != null) { r.isKinematic = true; r.useGravity = false; }
        }

        static Material HeldMaterial(string name, Color tint, float emission)
        {
            var shader = Shader.Find("Standard") ?? Shader.Find("Unlit/Color");
            var mat = new Material(shader) { name = name };
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", tint);
            if (emission > 0f && mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", tint * emission);
            }
            return mat;
        }

        static GameObject HeldPiece(Transform parent, PrimitiveType type, string name,
            Vector3 position, Vector3 size, Quaternion rotation, Material material)
        {
            var obj = GameObject.CreatePrimitive(type);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            obj.transform.localRotation = rotation;
            obj.transform.localScale = size;
            var renderer = obj.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;
            var collider = obj.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
            return obj;
        }
    }
}
