#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[InitializeOnLoad]
public class CaveSceneSetup : EditorWindow
{
    static CaveSceneSetup()
    {
        EditorApplication.delayCall += () =>
        {
            SetupCaveInCurrentScene();
        };
    }

    [MenuItem("Thach Sanh/Setup Hang Chan Tinh Cave")]
    public static void SetupCaveInCurrentScene()
    {
        string scenePath = "Assets/Scenes/Chapter3_MieuChanTinh.unity";
        var activeScene = EditorSceneManager.GetActiveScene();
        if (activeScene.path != scenePath)
        {
            return; // Only run when Chapter3_MieuChanTinh is opened
        }

        // 1. Ensure Outside Spawn Point near CaveTeleportTrigger
        GameObject outsideSpawn = GameObject.Find("OutsideSpawnPoint");
        if (outsideSpawn == null)
        {
            outsideSpawn = new GameObject("OutsideSpawnPoint");
            outsideSpawn.transform.position = new Vector3(100.5f, 8.0f, 88.0f);
            outsideSpawn.transform.rotation = Quaternion.Euler(0, 45f, 0);
        }

        // 2. Root GameObject for the entire cave (Yêu cầu 10 & 11)
        GameObject caveArea = GameObject.Find("ChanTinhCaveArea");
        if (caveArea == null)
        {
            caveArea = new GameObject("ChanTinhCaveArea");
        }
        caveArea.transform.position = Vector3.zero;

        // 3. Setup exact Hierarchy requested in Requirement 10:
        // ChanTinhCaveArea
        //   - CaveStructure
        //   - CaveGround
        //   - CaveWalls
        //   - CaveCeiling
        //   - CaveProps
        //   - CaveLights
        Transform caveStructure = GetOrCreateChild(caveArea.transform, "CaveStructure");
        Transform caveGround = GetOrCreateChild(caveArea.transform, "CaveGround");
        Transform caveWalls = GetOrCreateChild(caveArea.transform, "CaveWalls");
        Transform caveCeiling = GetOrCreateChild(caveArea.transform, "CaveCeiling");
        Transform caveProps = GetOrCreateChild(caveArea.transform, "CaveProps");
        Transform caveLights = GetOrCreateChild(caveArea.transform, "CaveLights");

        // 4. Cave Spawn Point
        GameObject caveSpawn = GameObject.Find("ChanTinhCaveSpawnPoint");
        if (caveSpawn == null)
        {
            caveSpawn = new GameObject("ChanTinhCaveSpawnPoint");
        }
        caveSpawn.transform.SetParent(caveArea.transform, true);
        caveSpawn.transform.position = new Vector3(290.0f, 0.5f, 282.0f);
        caveSpawn.transform.rotation = Quaternion.Euler(0, 0, 0);

        // 5. Cave Exit Trigger inside cave
        GameObject caveExitTrigger = GameObject.Find("CaveExitTrigger");
        if (caveExitTrigger == null)
        {
            caveExitTrigger = new GameObject("CaveExitTrigger");
        }
        caveExitTrigger.transform.SetParent(caveArea.transform, true);
        caveExitTrigger.transform.position = new Vector3(288.0f, 1.5f, 276.0f);
        BoxCollider exitCol = caveExitTrigger.GetComponent<BoxCollider>();
        if (exitCol == null) exitCol = caveExitTrigger.AddComponent<BoxCollider>();
        exitCol.isTrigger = true;
        exitCol.size = new Vector3(6f, 4f, 4f);

        CaveFadeTeleport exitTeleport = caveExitTrigger.GetComponent<CaveFadeTeleport>();
        if (exitTeleport == null) exitTeleport = caveExitTrigger.AddComponent<CaveFadeTeleport>();
        exitTeleport.isEnteringCave = false;
        exitTeleport.targetSpawnPoint = outsideSpawn.transform;
        exitTeleport.countdownTime = 5.0f;
        exitTeleport.fadeDuration = 2.0f;

        // 6. Outside Teleport Trigger
        GameObject outsideTrigger = GameObject.Find("CaveTeleportTrigger");
        if (outsideTrigger != null)
        {
            CaveFadeTeleport enterTeleport = outsideTrigger.GetComponent<CaveFadeTeleport>();
            if (enterTeleport == null) enterTeleport = outsideTrigger.AddComponent<CaveFadeTeleport>();
            enterTeleport.isEnteringCave = true;
            enterTeleport.targetSpawnPoint = caveSpawn.transform;
            enterTeleport.countdownTime = 5.0f;
            enterTeleport.fadeDuration = 2.0f;
        }

        // 7. Ensure Lighting Controller
        CaveLightingController lightingCtrl = Object.FindAnyObjectByType<CaveLightingController>();
        if (lightingCtrl == null)
        {
            GameObject ctrlObj = new GameObject("CaveLightingController");
            lightingCtrl = ctrlObj.AddComponent<CaveLightingController>();
        }

        lightingCtrl.caveSunIntensity = 0.0f;
        lightingCtrl.caveAmbientColor = new Color(0.008f, 0.012f, 0.025f);
        lightingCtrl.caveCameraBackgroundColor = new Color(0.005f, 0.01f, 0.02f);
        lightingCtrl.enableCaveFog = true;
        lightingCtrl.caveFogColor = new Color(0.01f, 0.03f, 0.06f);
        lightingCtrl.caveFogDensity = 0.04f;

        // 8. Build 3D Cave using MinesAndCaveSet prefabs
        BuildCaveWithMinesAndCaveSet(caveStructure, caveGround, caveWalls, caveCeiling, caveProps, caveLights);

        // 9. Mark dirty & save scene
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);
        Debug.Log("[CaveSceneSetup] Hang Chan Tinh Cave assembled with MinesAndCaveSet prefabs!");
    }

    private static Transform GetOrCreateChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child == null)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            child = go.transform;
        }
        return child;
    }

    private static void BuildCaveWithMinesAndCaveSet(Transform structure, Transform ground, Transform walls, Transform ceiling, Transform props, Transform lights)
    {
        // Path base for MinesAndCaveSet prefabs
        string basePath = "Assets/LoafbrrAssets/MInesAndCaveSet/prefabs/";

        // Load Prefabs from MinesAndCaveSet
        GameObject groundPrefabA = AssetDatabase.LoadAssetAtPath<GameObject>(basePath + "Ground/Ground_Caves_A.prefab");
        GameObject groundPrefabB = AssetDatabase.LoadAssetAtPath<GameObject>(basePath + "Ground/Ground_Caves_B.prefab");
        GameObject groundPrefabEntrance = AssetDatabase.LoadAssetAtPath<GameObject>(basePath + "Ground/Ground_Caves_Entrance_A.prefab");

        GameObject wallPrefabA = AssetDatabase.LoadAssetAtPath<GameObject>(basePath + "Wall/Wall_Caves_A.prefab");
        GameObject wallPrefabB = AssetDatabase.LoadAssetAtPath<GameObject>(basePath + "Wall/Wall_Caves_B.prefab");
        GameObject wallPrefabC = AssetDatabase.LoadAssetAtPath<GameObject>(basePath + "Wall/Wall_Caves_C.prefab");
        GameObject tunnelPrefabA = AssetDatabase.LoadAssetAtPath<GameObject>(basePath + "Wall/Tunnel_A.prefab");
        GameObject tunnelPrefabB = AssetDatabase.LoadAssetAtPath<GameObject>(basePath + "Wall/Tunnel_B.prefab");
        GameObject wallPlanesA = AssetDatabase.LoadAssetAtPath<GameObject>(basePath + "Wall/Wall_Planes_A.prefab");

        GameObject postPrefabA = AssetDatabase.LoadAssetAtPath<GameObject>(basePath + "Posts/Post_Single_A.prefab");
        GameObject postPrefabB = AssetDatabase.LoadAssetAtPath<GameObject>(basePath + "Posts/Posts_Caves_A.prefab");

        // Torch visual prefab
        GameObject torchPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Dungeon/URP/Prefabs/Other/NGF_Light_Torch_01.prefab");
        if (torchPrefab == null)
        {
            torchPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Dungeon/Build-In/Prefabs/Misc/Light/NGF_Light_Torch_01.prefab");
        }

        Material stoneMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Dungeon/URP/Materials/Dungeon URP.mat");

        // --- 1. CAVE GROUND (Ground_Caves prefabs) ---
        Vector3[] groundPositions = new Vector3[]
        {
            new Vector3(288, 0, 276),
            new Vector3(288, 0, 282),
            new Vector3(290, 0, 288),
            new Vector3(293, 0, 294),
            new Vector3(296, 0, 300),
            new Vector3(299, 0, 306),
            new Vector3(300, 0, 312),
            new Vector3(300, 0, 318),
            new Vector3(300, 0, 324)
        };

        for (int i = 0; i < groundPositions.Length; i++)
        {
            Transform existingG = ground.Find($"CaveGroundTile_{i + 1}");
            if (existingG == null)
            {
                GameObject gObj;
                GameObject pToUse = (i % 2 == 0) ? groundPrefabA : groundPrefabB;
                if (i == 0 && groundPrefabEntrance != null) pToUse = groundPrefabEntrance;

                if (pToUse != null)
                {
                    gObj = (GameObject)PrefabUtility.InstantiatePrefab(pToUse, ground);
                }
                else
                {
                    gObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    gObj.transform.SetParent(ground, false);
                    gObj.transform.localScale = new Vector3(8f, 0.4f, 8f);
                    if (stoneMat != null) gObj.GetComponent<Renderer>().sharedMaterial = stoneMat;
                }
                gObj.name = $"CaveGroundTile_{i + 1}";
                gObj.transform.position = groundPositions[i];

                // Add Collider if missing
                if (gObj.GetComponent<Collider>() == null && gObj.GetComponentInChildren<Collider>() == null)
                {
                    BoxCollider box = gObj.AddComponent<BoxCollider>();
                    box.size = new Vector3(8f, 0.4f, 8f);
                }
            }
        }

        // --- 2. CAVE WALLS (Wall_Caves & Tunnel prefabs) ---
        Vector3[] wallPositions = new Vector3[]
        {
            // Left Tunnel Wall
            new Vector3(282, 0, 276), new Vector3(282, 0, 284), new Vector3(285, 0, 292), new Vector3(289, 0, 300),
            // Right Tunnel Wall
            new Vector3(294, 0, 276), new Vector3(294, 0, 284), new Vector3(299, 0, 292), new Vector3(307, 0, 300),
            // Main Chamber Back/Left/Right Enclosures
            new Vector3(286, 0, 308), new Vector3(286, 0, 316), new Vector3(286, 0, 324),
            new Vector3(314, 0, 308), new Vector3(314, 0, 316), new Vector3(314, 0, 324),
            new Vector3(292, 0, 328), new Vector3(300, 0, 328), new Vector3(308, 0, 328)
        };

        for (int i = 0; i < wallPositions.Length; i++)
        {
            Transform existingW = walls.Find($"CaveWallTile_{i + 1}");
            if (existingW == null)
            {
                GameObject wObj;
                GameObject pToUse = (i % 3 == 0) ? wallPrefabA : ((i % 3 == 1) ? wallPrefabB : wallPrefabC);

                if (pToUse != null)
                {
                    wObj = (GameObject)PrefabUtility.InstantiatePrefab(pToUse, walls);
                }
                else
                {
                    wObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    wObj.transform.SetParent(walls, false);
                    wObj.transform.localScale = new Vector3(6f, 14f, 10f);
                    if (stoneMat != null) wObj.GetComponent<Renderer>().sharedMaterial = stoneMat;
                }
                wObj.name = $"CaveWallTile_{i + 1}";
                wObj.transform.position = wallPositions[i];
            }
        }

        // Tunnel archways for ceiling connection
        if (tunnelPrefabA != null && walls.Find("CaveTunnelArch_1") == null)
        {
            GameObject t1 = (GameObject)PrefabUtility.InstantiatePrefab(tunnelPrefabA, walls);
            t1.name = "CaveTunnelArch_1";
            t1.transform.position = new Vector3(288, 0, 280);

            if (tunnelPrefabB != null)
            {
                GameObject t2 = (GameObject)PrefabUtility.InstantiatePrefab(tunnelPrefabB, walls);
                t2.name = "CaveTunnelArch_2";
                t2.transform.position = new Vector3(294, 0, 296);
            }
        }

        // --- 3. CAVE CEILING (Roof slabs) ---
        Vector3[] ceilingPositions = new Vector3[]
        {
            new Vector3(288, 9, 278),
            new Vector3(291, 9, 288),
            new Vector3(296, 9, 298),
            new Vector3(300, 9, 308),
            new Vector3(300, 9, 318),
            new Vector3(300, 9, 326)
        };

        for (int i = 0; i < ceilingPositions.Length; i++)
        {
            Transform existingC = ceiling.Find($"CaveCeilingRoof_{i + 1}");
            if (existingC == null)
            {
                GameObject cObj;
                if (wallPlanesA != null)
                {
                    cObj = (GameObject)PrefabUtility.InstantiatePrefab(wallPlanesA, ceiling);
                    cObj.transform.rotation = Quaternion.Euler(90, 0, 0);
                    cObj.transform.localScale = new Vector3(3f, 3f, 3f);
                }
                else
                {
                    cObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cObj.transform.SetParent(ceiling, false);
                    cObj.transform.localScale = new Vector3(28f, 2.0f, 28f);
                    if (stoneMat != null) cObj.GetComponent<Renderer>().sharedMaterial = stoneMat;
                }
                cObj.name = $"CaveCeilingRoof_{i + 1}";
                cObj.transform.position = ceilingPositions[i];
            }
        }

        // --- 4. CAVE PROPS (Posts_Caves & Torches) ---
        Vector3[] postPositions = new Vector3[]
        {
            new Vector3(285f, 0, 280f),
            new Vector3(291f, 0, 280f),
            new Vector3(288f, 0, 294f),
            new Vector3(296f, 0, 294f)
        };

        for (int i = 0; i < postPositions.Length; i++)
        {
            Transform existingP = props.Find($"CavePostProp_{i + 1}");
            if (existingP == null && postPrefabA != null)
            {
                GameObject pObj = (GameObject)PrefabUtility.InstantiatePrefab(postPrefabA, props);
                pObj.name = $"CavePostProp_{i + 1}";
                pObj.transform.position = postPositions[i];
            }
        }

        // --- 5. CAVE LIGHTS (Torches Warm Orange + Cool Blue Portal) ---
        Vector3[] torchPositions = new Vector3[]
        {
            new Vector3(284.5f, 2.6f, 278f),
            new Vector3(291.5f, 2.6f, 278f),
            new Vector3(287.0f, 2.8f, 290f),
            new Vector3(297.0f, 2.8f, 290f),
            new Vector3(289.5f, 3.0f, 304f),
            new Vector3(308.5f, 3.0f, 304f),
            new Vector3(288.0f, 3.2f, 316f),
            new Vector3(312.0f, 3.2f, 316f)
        };

        for (int i = 0; i < torchPositions.Length; i++)
        {
            Transform existingTorch = props.Find($"TorchProp_{i + 1}");
            if (existingTorch == null && torchPrefab != null)
            {
                GameObject torchObj = (GameObject)PrefabUtility.InstantiatePrefab(torchPrefab, props);
                torchObj.name = $"TorchProp_{i + 1}";
                torchObj.transform.position = torchPositions[i];
            }

            Transform existingLight = lights.Find($"TorchPointLight_{i + 1}");
            GameObject lightObj;
            if (existingLight != null)
            {
                lightObj = existingLight.gameObject;
            }
            else
            {
                lightObj = new GameObject($"TorchPointLight_{i + 1}");
                lightObj.transform.SetParent(lights, false);
            }
            lightObj.transform.position = torchPositions[i] + Vector3.up * 0.8f;

            Light pLight = lightObj.GetComponent<Light>();
            if (pLight == null) pLight = lightObj.AddComponent<Light>();
            pLight.type = LightType.Point;
            pLight.color = new Color(1.0f, 0.55f, 0.12f); // Warm Torch Orange
            pLight.intensity = 3.5f;
            pLight.range = 10.0f;
            pLight.shadows = LightShadows.Soft;

            LightFlickerEffect flicker = lightObj.GetComponent<LightFlickerEffect>();
            if (flicker == null) flicker = lightObj.AddComponent<LightFlickerEffect>();
            flicker.minIntensity = 2.5f;
            flicker.maxIntensity = 4.2f;
            flicker.smoothing = 7;
        }

        // Cool Cyan/Blue Light deep in cave
        Transform coolLightTr = lights.Find("CaveDeepCoolLight");
        GameObject coolLightObj;
        if (coolLightTr != null)
        {
            coolLightObj = coolLightTr.gameObject;
        }
        else
        {
            coolLightObj = new GameObject("CaveDeepCoolLight");
            coolLightObj.transform.SetParent(lights, false);
        }
        coolLightObj.transform.position = new Vector3(300.0f, 4.5f, 324.0f);

        Light cLight = coolLightObj.GetComponent<Light>();
        if (cLight == null) cLight = coolLightObj.AddComponent<Light>();
        cLight.type = LightType.Point;
        cLight.color = new Color(0.12f, 0.55f, 0.85f); // Cool Cyan/Teal glow
        cLight.intensity = 3.0f;
        cLight.range = 22.0f;
        cLight.shadows = LightShadows.Soft;
    }
}
#endif
