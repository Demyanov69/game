using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace EchoIslands
{
    [DefaultExecutionOrder(-40)]
    public sealed class WorldTerrainTextureSwap : MonoBehaviour
    {
        readonly Dictionary<Terrain, Material> originals = new();
        Material renderingMaterial;

        void Start()
        {
            var shader = Shader.Find("Echo/Terrain");
            var darkGround = Resources.Load<Texture2D>("EchoWorldGround/DarkGround");
            if (shader != null && shader.isSupported && darkGround != null)
            {
                renderingMaterial = new Material(shader) { name = "Echo Islands terrain", enableInstancing = true };
                Shader.SetGlobalTexture("_EchoTerrainDark", darkGround);
            }

            foreach (var terrain in Terrain.activeTerrains)
            {
                terrain.drawInstanced = true;
                terrain.heightmapPixelError = Mathf.Max(terrain.heightmapPixelError, 9f);
                terrain.basemapDistance = Mathf.Min(terrain.basemapDistance, 30f);
                terrain.treeDistance = Mathf.Min(terrain.treeDistance, 74f);
                terrain.treeBillboardDistance = Mathf.Min(terrain.treeBillboardDistance, 24f);
                terrain.treeMaximumFullLODCount = Mathf.Min(terrain.treeMaximumFullLODCount, 24);
                terrain.detailObjectDistance = Mathf.Min(terrain.detailObjectDistance, 28f);
                terrain.detailObjectDensity = Mathf.Min(terrain.detailObjectDensity, .5f);
                terrain.shadowCastingMode = ShadowCastingMode.Off;
                if (renderingMaterial == null) continue;
                originals.Add(terrain, terrain.materialTemplate);
                terrain.materialTemplate = renderingMaterial;
            }

            PrepareNearbyTrees();
        }

        static void PrepareNearbyTrees()
        {
            var flow = LevelFlow.Instance;
            if (flow == null || flow.levelRoots[0] == null) return;
            var pine = Resources.Load<GameObject>("EchoL1/PT_Pine_Tree_03_dead");
            var fruit = Resources.Load<GameObject>("EchoL1/PT_Fruit_Tree_01_dead");
            var shrub = Resources.Load<GameObject>("EchoL1/PT_Generic_Shrub_01_dead");
            var course = flow.spawns[0] != null ? flow.spawns[0].position + Vector3.right * 13f : Vector3.zero;
            foreach (var tree in flow.levelRoots[0].GetComponentsInChildren<Transform>())
            {
                Vector3 offset = tree.position - course;
                if (offset.x*offset.x + offset.z*offset.z > 42f*42f) continue;
                string name = tree.name;
                GameObject deadModel = name.StartsWith("PT_Pine_Tree") && name.Contains("green") ? pine :
                    name.StartsWith("PT_Fruit_Tree") && !name.Contains("dead") && !name.Contains("stump") ? fruit :
                    name.StartsWith("PT_Generic_Shrub") && !name.Contains("dead") ? shrub : null;
                if (deadModel == null || tree.GetComponentInParent<DungeonRuin>() != null) continue;
                var group = new GameObject("Seasonal grove").transform;
                group.SetParent(tree.parent,false);
                group.SetLocalPositionAndRotation(tree.localPosition, tree.localRotation);
                group.localScale = tree.localScale;
                tree.SetParent(group,false);
                tree.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                tree.localScale = Vector3.one;
                var dead = Instantiate(deadModel,group,false);
                dead.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                dead.transform.localScale = Vector3.one;
                dead.SetActive(false);
                var swap = group.gameObject.AddComponent<DungeonRuin>();
                swap.Configure(tree.gameObject, dead);
            }
        }

        void OnDestroy()
        {
            foreach (var pair in originals)
                if (pair.Key != null) pair.Key.materialTemplate = pair.Value;
            if (renderingMaterial != null) Destroy(renderingMaterial);
        }
    }
}
