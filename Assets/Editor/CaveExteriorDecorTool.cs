#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class CaveExteriorDecorTool : EditorWindow
{
    [MenuItem("Tools/Thach Sanh/Decorate Cave Exterior")]
    [MenuItem("Thach Sanh/Decorate Cave Exterior")]
    public static void DecorateCaveExterior()
    {
        AssetDatabase.Refresh();
        string scenePath = "Assets/Scenes/Chapter3_MieuChanTinh.unity";
        var activeScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        Terrain terrain = Terrain.activeTerrain;
        Vector3 terrainPos = terrain != null ? terrain.transform.position : Vector3.zero;

        // Path waypoints from Sea Edge to Cave Entrance
        Vector3 seaEdge = new Vector3(150.7683f, 0.5f, 116.6338f);
        Vector3 caveEntrance = new Vector3(105.7401f, 2.530818f, 97.22861f);

        // Find or create Root Decor Object
        GameObject decorRoot = GameObject.Find("CaveExteriorDecor");
        if (decorRoot != null)
        {
            Undo.DestroyObjectImmediate(decorRoot);
        }

        decorRoot = new GameObject("CaveExteriorDecor");
        Undo.RegisterCreatedObjectUndo(decorRoot, "Decorate Cave Exterior");

        Transform pathFolder = GetOrCreateFolder(decorRoot.transform, "CavePath");
        Transform rocksFolder = GetOrCreateFolder(decorRoot.transform, "CaveRocks");
        Transform grassFolder = GetOrCreateFolder(decorRoot.transform, "CaveGrass");
        Transform bushesFolder = GetOrCreateFolder(decorRoot.transform, "CaveBushes");
        Transform treesFolder = GetOrCreateFolder(decorRoot.transform, "CaveTrees");
        Transform sandFolder = GetOrCreateFolder(decorRoot.transform, "CaveSand");

        // Load Prefabs
        string staticPath = "Assets/Toby Fredson/The Toby Foliage Engine/(TTFE)_Demo/Prefabs/Prefabs_Static/";
        string vegPath = "Assets/Toby Fredson/The Toby Foliage Engine/(TTFE)_Demo/Prefabs/Prefabs_Vegetation/";

        GameObject cliffRockA = AssetDatabase.LoadAssetAtPath<GameObject>(staticPath + "CliffRockTTFEL_A.prefab");
        GameObject cliffRockB = AssetDatabase.LoadAssetAtPath<GameObject>(staticPath + "CliffRockTTFEL_B.prefab");
        GameObject rockA = AssetDatabase.LoadAssetAtPath<GameObject>(staticPath + "RocksTTFEL_A.prefab");
        GameObject rockB = AssetDatabase.LoadAssetAtPath<GameObject>(staticPath + "RocksTTFEL_B.prefab");
        GameObject rockC = AssetDatabase.LoadAssetAtPath<GameObject>(staticPath + "RocksTTFEL_C.prefab");
        GameObject rockD = AssetDatabase.LoadAssetAtPath<GameObject>(staticPath + "RocksTTFEL_D.prefab");
        GameObject rockE = AssetDatabase.LoadAssetAtPath<GameObject>(staticPath + "RocksTTFEL_E.prefab");

        GameObject grassMedA = AssetDatabase.LoadAssetAtPath<GameObject>(vegPath + "Vegetation_Plants/VP_Grass/GrassMedium_A.prefab");
        GameObject grassMedB = AssetDatabase.LoadAssetAtPath<GameObject>(vegPath + "Vegetation_Plants/VP_Grass/GrassMedium_B.prefab");
        GameObject grassShortA = AssetDatabase.LoadAssetAtPath<GameObject>(vegPath + "Vegetation_Plants/VP_Grass/GrassShort_A.prefab");
        GameObject grassShortB = AssetDatabase.LoadAssetAtPath<GameObject>(vegPath + "Vegetation_Plants/VP_Grass/GrassShort_B.prefab");

        GameObject bushB = AssetDatabase.LoadAssetAtPath<GameObject>(vegPath + "Vegetation_Trees/VTSpecies_Shrub/ShrubBush_B.prefab");
        GameObject bushC = AssetDatabase.LoadAssetAtPath<GameObject>(vegPath + "Vegetation_Trees/VTSpecies_Shrub/ShrubBush_C.prefab");
        GameObject treeC = AssetDatabase.LoadAssetAtPath<GameObject>(vegPath + "Vegetation_Trees/VTSpecies_Shrub/ShrubTree_C.prefab");
        GameObject treeD = AssetDatabase.LoadAssetAtPath<GameObject>(vegPath + "Vegetation_Trees/VTSpecies_Shrub/ShrubTree(NR)_D.prefab");

        // Fallback cave rocks if needed
        GameObject caveRockA = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LoafbrrAssets/MInesAndCaveSet/prefabs/Wall/Cave_Rocks_A.prefab");

        Random.InitState(12345); // Consistent natural seed

        Vector3 pathDir = (caveEntrance - seaEdge).normalized;
        Vector3 sideDir = new Vector3(-pathDir.z, 0, pathDir.x).normalized;

        // 1. Large Cliff Rocks around Cave Entrance (2-4 large rocks)
        float[] cliffOffsets = new float[] { -6.5f, 6.5f, -8.0f, 8.0f };
        float[] cliffProgress = new float[] { 0.92f, 0.95f, 0.88f, 0.98f };
        for (int i = 0; i < 4; i++)
        {
            GameObject pToUse = (i % 2 == 0) ? cliffRockA : cliffRockB;
            if (pToUse == null) pToUse = rockA ?? caveRockA;
            if (pToUse != null)
            {
                Vector3 pos = Vector3.Lerp(seaEdge, caveEntrance, cliffProgress[i]) + sideDir * cliffOffsets[i];
                pos.y = SampleGroundHeight(terrain, pos, terrainPos) - 0.2f;
                Quaternion rot = Quaternion.Euler(0, Random.Range(0, 360), 0);
                Vector3 scale = Vector3.one * Random.Range(1.2f, 1.8f);

                SpawnDecorInstance(pToUse, pos, rot, scale, rocksFolder);
            }
        }

        // 2. Medium & Small Rocks along path sides (8-12 rocks)
        GameObject[] smallRocks = new GameObject[] { rockA, rockB, rockC, rockD, rockE, caveRockA };
        for (int i = 0; i < 10; i++)
        {
            float t = Random.Range(0.12f, 0.85f);
            float side = (i % 2 == 0 ? 1f : -1f) * Random.Range(3.2f, 5.8f);
            Vector3 pos = Vector3.Lerp(seaEdge, caveEntrance, t) + sideDir * side;
            pos.y = SampleGroundHeight(terrain, pos, terrainPos) - 0.1f;
            Quaternion rot = Quaternion.Euler(Random.Range(-10, 10), Random.Range(0, 360), Random.Range(-10, 10));
            Vector3 scale = Vector3.one * Random.Range(0.7f, 1.3f);

            GameObject pToUse = GetRandomElement(smallRocks);
            if (pToUse != null)
            {
                SpawnDecorInstance(pToUse, pos, rot, scale, rocksFolder);
            }
        }

        // 3. Grass tufts along path edges (15-20 grass patches)
        GameObject[] grassPrefabs = new GameObject[] { grassMedA, grassMedB, grassShortA, grassShortB };
        for (int i = 0; i < 18; i++)
        {
            float t = Random.Range(0.08f, 0.92f);
            float side = (i % 2 == 0 ? 1f : -1f) * Random.Range(2.2f, 4.5f);
            Vector3 pos = Vector3.Lerp(seaEdge, caveEntrance, t) + sideDir * side;
            pos.y = SampleGroundHeight(terrain, pos, terrainPos);
            Quaternion rot = Quaternion.Euler(0, Random.Range(0, 360), 0);
            Vector3 scale = Vector3.one * Random.Range(0.9f, 1.4f);

            GameObject pToUse = GetRandomElement(grassPrefabs);
            if (pToUse != null)
            {
                SpawnDecorInstance(pToUse, pos, rot, scale, grassFolder);
            }
        }

        // 4. Bushes & Shrubs along path edges (5-8 bushes)
        GameObject[] bushPrefabs = new GameObject[] { bushB, bushC };
        for (int i = 0; i < 7; i++)
        {
            float t = Random.Range(0.15f, 0.82f);
            float side = (i % 2 == 0 ? 1f : -1f) * Random.Range(3.8f, 6.2f);
            Vector3 pos = Vector3.Lerp(seaEdge, caveEntrance, t) + sideDir * side;
            pos.y = SampleGroundHeight(terrain, pos, terrainPos);
            Quaternion rot = Quaternion.Euler(0, Random.Range(0, 360), 0);
            Vector3 scale = Vector3.one * Random.Range(0.8f, 1.2f);

            GameObject pToUse = GetRandomElement(bushPrefabs);
            if (pToUse != null)
            {
                SpawnDecorInstance(pToUse, pos, rot, scale, bushesFolder);
            }
        }

        // 5. Trees placed further back from path and camera (5-8 trees)
        GameObject[] treePrefabs = new GameObject[] { treeC, treeD, bushC };
        for (int i = 0; i < 7; i++)
        {
            float t = Random.Range(0.2f, 0.8f);
            float side = (i % 2 == 0 ? 1f : -1f) * Random.Range(7.5f, 12.0f);
            Vector3 pos = Vector3.Lerp(seaEdge, caveEntrance, t) + sideDir * side;
            pos.y = SampleGroundHeight(terrain, pos, terrainPos);
            Quaternion rot = Quaternion.Euler(0, Random.Range(0, 360), 0);
            Vector3 scale = Vector3.one * Random.Range(0.85f, 1.35f);

            GameObject pToUse = GetRandomElement(treePrefabs);
            if (pToUse != null)
            {
                SpawnDecorInstance(pToUse, pos, rot, scale, treesFolder);
            }
        }

        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[CaveExteriorDecorTool] Path decorated successfully! CaveExteriorDecor created with natural foliage, rocks, bushes, and trees.");
    }

    private static Transform GetOrCreateFolder(Transform parent, string folderName)
    {
        Transform child = parent.Find(folderName);
        if (child == null)
        {
            GameObject go = new GameObject(folderName);
            go.transform.SetParent(parent, false);
            child = go.transform;
        }
        return child;
    }

    private static float SampleGroundHeight(Terrain terrain, Vector3 worldPos, Vector3 terrainPos)
    {
        if (terrain != null)
        {
            return terrain.SampleHeight(worldPos) + terrainPos.y;
        }
        return worldPos.y;
    }

    private static GameObject GetRandomElement(GameObject[] list)
    {
        List<GameObject> valid = new List<GameObject>();
        foreach (var g in list)
        {
            if (g != null) valid.Add(g);
        }
        if (valid.Count == 0) return null;
        return valid[Random.Range(0, valid.Count)];
    }

    private static void SpawnDecorInstance(GameObject prefab, Vector3 pos, Quaternion rot, Vector3 scale, Transform parent)
    {
        GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        inst.transform.position = pos;
        inst.transform.rotation = rot;
        inst.transform.localScale = scale;
        Undo.RegisterCreatedObjectUndo(inst, "Decorate Instance");
    }
}
#endif
