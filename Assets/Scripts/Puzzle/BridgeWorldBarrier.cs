using UnityEngine;

namespace EchoIslands
{
    /// <summary>
    /// Invisible Light-world wall at the river crossing.
    /// In Dark, the two halves slide apart exactly where the bridge is.
    /// </summary>
    public class BridgeWorldBarrier : MonoBehaviour
    {
        public float openingHalfWidth = 1.25f;
        public float openingSlide = 1.35f;
        public float wallLength = 8f;
        public float wallHeight = 7f;
        public float wallThickness = 0.8f;

        Transform panelA;
        Transform panelB;
        Vector3 closedA;
        Vector3 closedB;
        Vector3 openDirection;
        WorldState state;
        bool configured;

        public void ConfigureForBridge(Transform bridge)
        {
            if (bridge == null) return;

            transform.position = bridge.position;
            transform.rotation = bridge.rotation;

            bool longOnX = EstimateBridgeLongAxisX(bridge);
            Vector3 longAxis = longOnX ? Vector3.right : Vector3.forward;
            Vector3 shortAxis = longOnX ? Vector3.forward : Vector3.right;
            float longHalf = EstimateBridgeHalfLength(bridge, longOnX);
            Vector3 worldLong = bridge.TransformDirection(longAxis).normalized;

            transform.position = bridge.position - worldLong * (longHalf + 0.55f);
            transform.position += Vector3.down * 2.5f;
            openDirection = shortAxis;

            CreatePanels(longOnX, shortAxis);
            configured = true;
            state = WorldState.Instance;
        }

        void Awake()
        {
            state = WorldState.Instance;
        }

        void CreatePanels(bool longOnX, Vector3 shortAxis)
        {
            if (panelA != null || panelB != null) return;

            float halfPanelSpan = Mathf.Max(0.5f, (wallLength - openingHalfWidth * 2f) * 0.5f);
            Vector3 localPanelSize = longOnX
                ? new Vector3(wallThickness, wallHeight, halfPanelSpan)
                : new Vector3(halfPanelSpan, wallHeight, wallThickness);

            panelA = CreatePanel("LightBarrier_A", localPanelSize);
            panelB = CreatePanel("LightBarrier_B", localPanelSize);

            Vector3 lateralAxis = shortAxis.normalized;
            float closedOffset = halfPanelSpan * 0.5f;
            closedA = lateralAxis * -closedOffset + Vector3.up * (wallHeight * 0.5f);
            closedB = lateralAxis * closedOffset + Vector3.up * (wallHeight * 0.5f);

            panelA.localPosition = closedA;
            panelB.localPosition = closedB;
        }

        Transform CreatePanel(string name, Vector3 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var collider = go.AddComponent<BoxCollider>();
            collider.size = size;
            collider.isTrigger = false;
            return go.transform;
        }

        bool EstimateBridgeLongAxisX(Transform bridge)
        {
            float x = 0f;
            float z = 0f;
            var meshes = bridge.GetComponentsInChildren<MeshFilter>(true);
            for (int i = 0; i < meshes.Length; i++)
            {
                if (meshes[i] == null || meshes[i].sharedMesh == null) continue;
                var size = meshes[i].sharedMesh.bounds.size;
                Vector3 scale = meshes[i].transform.localScale;
                x = Mathf.Max(x, Mathf.Abs(size.x * scale.x));
                z = Mathf.Max(z, Mathf.Abs(size.z * scale.z));
            }
            if (x <= 0.001f && z <= 0.001f)
                return true;
            return x >= z;
        }

        float EstimateBridgeHalfLength(Transform bridge, bool longOnX)
        {
            float max = 2f;
            var meshes = bridge.GetComponentsInChildren<MeshFilter>(true);
            for (int i = 0; i < meshes.Length; i++)
            {
                if (meshes[i] == null || meshes[i].sharedMesh == null) continue;
                var size = meshes[i].sharedMesh.bounds.size;
                Vector3 scale = meshes[i].transform.localScale;
                float full = longOnX ? Mathf.Abs(size.x * scale.x) : Mathf.Abs(size.z * scale.z);
                if (full > max) max = full;
            }
            return max * 0.5f;
        }

        void Update()
        {
            if (!configured) return;
            if (state == null) state = WorldState.Instance;
            float blend = state == null ? 0f : Mathf.Clamp01(state.blend);
            blend = blend * blend * (3f - 2f * blend);

            Vector3 travel = openDirection.normalized * openingSlide * blend;
            if (panelA != null) panelA.localPosition = closedA - travel;
            if (panelB != null) panelB.localPosition = closedB + travel;
        }
    }
}
