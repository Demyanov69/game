using UnityEngine;

namespace EchoIslands
{
    public sealed class SpinningDungeonHazard : MonoBehaviour
    {
        public float degreesPerSecond = 95f;
        public Vector3 axis = Vector3.up;
        void Update() { transform.Rotate(axis, degreesPerSecond * Time.deltaTime, Space.Self); }
    }
}
