using UnityEngine;

namespace EchoIslands
{
    /// <summary>Refreshes the existing art after the forest scene has finished loading.</summary>
    public sealed class EchoVisualOverhaulHook : MonoBehaviour
    {
        float elapsed;
        int passes;
        void Update()
        {
            elapsed += Time.deltaTime;
            if(elapsed < .15f)return;
            elapsed=0f;
            passes++;
            EchoVisualOverhaul.ReskinAll();
            if(passes>=8)Destroy(gameObject);
        }
    }
}
