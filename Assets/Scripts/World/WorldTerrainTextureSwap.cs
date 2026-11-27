using System;
using System.Collections.Generic;
using UnityEngine;

namespace EchoIslands
{
    /// <summary>
    /// Changes the actual ground diffuse textures of the original Polytope level-1
    /// terrains on world switch. Original TerrainData / TerrainLayer assets are never
    /// edited: independent play-mode copies are installed and safely restored.
    /// </summary>
    [DefaultExecutionOrder(-40)]
    public sealed class WorldTerrainTextureSwap : MonoBehaviour
    {
        struct Binding
        {
            public Terrain terrain;
            public TerrainCollider collider;
            public TerrainData original;
            public TerrainData runtimeCopy;
        }

        readonly List<Binding> bindings = new List<Binding>();
        readonly Dictionary<TerrainData, TerrainData> runtimeCopies =
            new Dictionary<TerrainData, TerrainData>();
        readonly Dictionary<TerrainData, TerrainLayer[]> dayLayers =
            new Dictionary<TerrainData, TerrainLayer[]>();
        readonly Dictionary<TerrainData, TerrainLayer[]> nightLayers =
            new Dictionary<TerrainData, TerrainLayer[]>();
        readonly List<TerrainLayer> instantiatedLayers = new List<TerrainLayer>();
        readonly Dictionary<TerrainData, TreePrototype[]> deadTrees = new Dictionary<TerrainData, TreePrototype[]>();
        GameObject pine, fruit, shrub;
        GameObject DeadModel(string name)
        {
            if (name.StartsWith("PT_Pine_Tree") && name.Contains("green")) return pine;
            if (name.StartsWith("PT_Fruit_Tree") && !name.Contains("dead") && !name.Contains("stump")) return fruit;
            if (name.StartsWith("PT_Generic_Shrub") && !name.Contains("dead")) return shrub;
            return null;
        }
        void PrepareTrees()
        {
            pine = Resources.Load<GameObject>("EchoL1/PT_Pine_Tree_03_dead");
            fruit = Resources.Load<GameObject>("EchoL1/PT_Fruit_Tree_01_dead");
            shrub = Resources.Load<GameObject>("EchoL1/PT_Generic_Shrub_01_dead");
            var flow = LevelFlow.Instance;
            if (flow == null || flow.levelRoots[0] == null) return;
            foreach (var tree in flow.levelRoots[0].GetComponentsInChildren<Transform>())
            {
                var model = DeadModel(tree.name);
                if (model == null || tree.GetComponentInParent<DungeonRuin>() != null) continue;
                var group = new GameObject("Seasonal grove").transform;
                group.SetParent(tree.parent,false);
                group.localPosition = tree.localPosition;
                group.localRotation = tree.localRotation;
                group.localScale = tree.localScale;
                tree.SetParent(group,false);
                tree.localPosition = Vector3.zero;
                tree.localRotation = Quaternion.identity;
                tree.localScale = Vector3.one;
                var dead = Instantiate(model,group,false);
                dead.transform.localPosition = Vector3.zero;
                dead.transform.localRotation = Quaternion.identity;
                dead.transform.localScale = Vector3.one;
                var swap = group.gameObject.AddComponent<DungeonRuin>();
                swap.intact = tree.gameObject;
                swap.ruined = dead;
                dead.SetActive(false);
            }
        }
        Texture2D darkGrass;
        Texture2D darkGround;
        bool initialized;
        bool lastDark;

        void Start()
        {
            PrepareTrees();
            darkGrass = Resources.Load<Texture2D>("EchoWorldGround/DarkGrass");
            darkGround = Resources.Load<Texture2D>("EchoWorldGround/DarkGround");
            if (darkGrass == null || darkGround == null) return;

            var terrains = Terrain.activeTerrains;
            for (int i = 0; i < terrains.Length; i++)
            {
                Terrain terrain = terrains[i];
                if (terrain == null || terrain.terrainData == null) continue;
                TerrainData source = terrain.terrainData;
                TerrainData copy;
                if (!runtimeCopies.TryGetValue(source, out copy))
                {
                    copy = Instantiate(source);
                    copy.name = source.name + " (world-skin runtime only)";
                    copy.hideFlags = HideFlags.DontSave;
                    runtimeCopies.Add(source, copy);

                    TerrainLayer[] originalLayers = source.terrainLayers;
                    var light = new TerrainLayer[originalLayers.Length];
                    var dark = new TerrainLayer[originalLayers.Length];
                    for (int j = 0; j < originalLayers.Length; j++)
                    {
                        var layer = originalLayers[j];
                        if (layer == null) continue;
                        light[j] = Instantiate(layer);
                        dark[j] = Instantiate(layer);
                        light[j].hideFlags = HideFlags.DontSave;
                        dark[j].hideFlags = HideFlags.DontSave;
                        instantiatedLayers.Add(light[j]);
                        instantiatedLayers.Add(dark[j]);
                        Texture2D diffuse = layer.diffuseTexture;
                        bool grass = diffuse != null &&
                            diffuse.name.IndexOf("Grass", StringComparison.OrdinalIgnoreCase) >= 0;
                        dark[j].diffuseTexture = grass ? darkGrass : darkGround;
                        dark[j].name = layer.name + " / spectral night";
                    }
                    var prototypes = source.treePrototypes;
                    bool changed = false;
                    for (int k=0; k<prototypes.Length; k++)
                    {
                        var prototype = prototypes[k];
                        if (prototype.prefab == null) continue;
                        var dead = DeadModel(prototype.prefab.name);
                        if (dead == null) continue;
                        prototypes[k] = new TreePrototype { prefab = dead, bendFactor = prototype.bendFactor };
                        changed = true;
                    }
                    if (changed) deadTrees.Add(copy,prototypes);
                    dayLayers.Add(copy, light);
                    nightLayers.Add(copy, dark);
                    copy.terrainLayers = light;
                }

                var collider = terrain.GetComponent<TerrainCollider>();
                bindings.Add(new Binding { terrain = terrain, collider = collider,
                    original = source, runtimeCopy = copy });
                terrain.terrainData = copy;
                if (collider != null) collider.terrainData = copy;
            }
            initialized = bindings.Count > 0;
            if (initialized)
                Apply(WorldState.Instance != null && WorldState.Instance.Current == WorldKind.Dark);
        }

        void LateUpdate()
        {
            if (!initialized || WorldState.Instance == null) return;
            bool nowDark = WorldState.Instance.Current == WorldKind.Dark;
            if (nowDark != lastDark) Apply(nowDark);
        }

        void Apply(bool dark)
        {
            foreach (var pair in runtimeCopies)
            {
                TerrainData data = pair.Value;
                if (deadTrees.TryGetValue(data,out var trees))
                {
                    data.treePrototypes = dark ? trees : pair.Key.treePrototypes;
                    data.RefreshPrototypes();
                }
                data.terrainLayers = dark ? nightLayers[data] : dayLayers[data];
            }
            lastDark = dark;
        }

        void OnDestroy()
        {
            // Restoring references first prevents Editor play-mode from holding
            // dangling runtime TerrainData while the original assets stay intact.
            foreach (var binding in bindings)
            {
                if (binding.terrain != null)
                    binding.terrain.terrainData = binding.original;
                if (binding.collider != null)
                    binding.collider.terrainData = binding.original;
            }
            foreach (var pair in runtimeCopies)
                if (pair.Value != null) Destroy(pair.Value);
            foreach (var layer in instantiatedLayers)
                if (layer != null) Destroy(layer);
        }
    }
}
