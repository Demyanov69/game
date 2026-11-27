using UnityEngine;
namespace EchoIslands
{
    public sealed class OrbVisualAnimator : MonoBehaviour
    {
        public float rotationSpeed = 12f;
        Vector3 initialPosition;
        void Awake() { initialPosition = transform.localPosition; }
        void Update()
        {
            transform.Rotate(Vector3.up,rotationSpeed * Time.deltaTime,Space.Self);
            transform.localPosition = initialPosition + Vector3.up *
                (Mathf.Sin(Time.time * 2.0f) * .075f);
        }
    }
}
