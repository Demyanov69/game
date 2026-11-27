using UnityEngine;

namespace EchoIslands
{
    public sealed class GravityPad : MonoBehaviour
    {
        public GravitySide source, destination;
        public WorldKind requiredWorld;
        public Vector3 landingPosition;
        public static GravityPad Nearby { get; private set; }

        void OnDisable() { if (Nearby == this) Nearby = null; }
        void Update()
        {
            var flow = LevelFlow.Instance;
            if (flow == null || flow.player == null || Time.timeScale <= 0f) return;
            var fps = flow.player.GetComponent<FirstPersonController>();
            if (fps == null || fps.GravityOrientation != source ||
                (fps.transform.position-transform.position).sqrMagnitude > 3.5f)
            {
                if (Nearby == this) Nearby = null;
                return;
            }
            Nearby = this;
            if (Input.GetKeyDown(KeyCode.G) && WorldState.Instance.Target == requiredWorld)
                fps.SetGravitySide(destination,landingPosition);
        }
    }
}
