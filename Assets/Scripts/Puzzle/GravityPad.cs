using UnityEngine;

namespace EchoIslands
{
    public sealed class GravityPad : MonoBehaviour
    {
        public GravitySide source, destination;
        public bool needsWorld, barrier;
        public WorldKind requiredWorld;
        public Vector3 landingPosition, landingUp, routeForward;
        public static GravityPad Nearby { get; private set; }
        public bool Ready
        {
            get
            {
                var state = WorldState.Instance;
                return !needsWorld || (state != null && state.Current == requiredWorld && state.Target == requiredWorld);
            }
        }

        void OnDisable() { if (Nearby == this) Nearby = null; }

        void Update()
        {
            var flow = LevelFlow.Instance;
            var fps = flow != null ? flow.Controller : null;
            if (fps == null || !fps.isActiveAndEnabled || fps.IsTurningGravity || fps.GravityOrientation != source ||
                (fps.transform.position-transform.position).sqrMagnitude > 12f)
            {
                if (Nearby == this) Nearby = null;
                return;
            }

            Vector3 feet = transform.InverseTransformPoint(fps.transform.position);
            bool near = Mathf.Abs(feet.x) < 1.57f && Mathf.Abs(feet.z) < 2.25f && feet.y > -.18f && feet.y < 1.7f;
            if (near) Nearby = this;
            else if (Nearby == this) Nearby = null;
            if (near && Ready && Mathf.Abs(feet.z) < (barrier ? .71f : .81f) && feet.y < (barrier ? 1.55f : .26f))
                fps.SetGravitySide(destination,landingPosition,landingUp,routeForward);
        }
    }
}
