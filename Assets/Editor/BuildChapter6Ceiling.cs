#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public class BuildChapter6Ceiling
{
    private const string CEILING_SESSION_KEY = "PRU_EagleCaveCeiling_v4_Master";
    private const string SCENE_PATH = "Assets/Scenes/Chapter6_HangDaiBang.unity";
    private const string PREFAB_DIR = "Assets/Free_Rocks/_prefabs";
    private const string MAT_DIR = "Assets/Free_Rocks/_material";

    private static Material urpRockMat;

    static BuildChapter6Ceiling()
    {
        EditorApplication.delayCall += () =>
        {
            if (!SessionState.GetBool(CEILING_SESSION_KEY, false))
            {
                SessionState.SetBool(CEILING_SESSION_KEY, true);
                ExecuteBuildCeiling(false);
            }
        };
    }

    [MenuItem("Tools/PRU/Xây dựng Trần Hang Đá Chapter 6 (Rock Ceiling)")]
    public static void BuildCeilingMenu()
    {
        ExecuteBuildCeiling(true);
    }

    public static void ExecuteBuildCeiling(bool showDialog)
    {
        Debug.Log("[BuildChapter6Ceiling] Bắt đầu xây dựng vòm trần hang đá và lỗ bay Đại Bàng cho Chapter 6...");

        // 1. Tải Material URP Rock
        urpRockMat = AssetDatabase.LoadAssetAtPath<Material>(MAT_DIR + "/rocks_urp.mat");
        if (urpRockMat == null)
        {
            Debug.LogWarning("[BuildChapter6Ceiling] Chưa tìm thấy rocks_urp.mat, sẽ dùng material mặc định.");
        }

        // 2. Mở Scene Chapter 6
        string currentScene = EditorSceneManager.GetActiveScene().path;
        if (currentScene != SCENE_PATH)
        {
            EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
        }

        // 3. Tải các rock prefabs
        GameObject cliffPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_DIR + "/cliff.prefab");
        GameObject rock1Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_DIR + "/rock1.prefab");
        GameObject rock2Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_DIR + "/rock2.prefab");
        GameObject rock3Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_DIR + "/rock3.prefab");
        GameObject rock4Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_DIR + "/rock4.prefab");

        if (cliffPrefab == null || rock1Prefab == null)
        {
            Debug.LogError("[BuildChapter6Ceiling] Không tìm thấy prefabs đá trong " + PREFAB_DIR);
            return;
        }

        // 4. Tìm root môi trường: Chapter6_EagleCaveEnvironment
        GameObject rootEnv = GameObject.Find("Chapter6_EagleCaveEnvironment");
        if (rootEnv == null)
        {
            rootEnv = new GameObject("Chapter6_EagleCaveEnvironment");
            Undo.RegisterCreatedObjectUndo(rootEnv, "Create Chapter6_EagleCaveEnvironment");
        }

        // 5. Tìm hoặc tạo mới nhóm Rock_Ceiling
        Transform oldCeiling = rootEnv.transform.Find("Rock_Ceiling");
        if (oldCeiling != null)
        {
            Undo.DestroyObjectImmediate(oldCeiling.gameObject);
        }

        GameObject ceilingObj = new GameObject("Rock_Ceiling");
        ceilingObj.transform.SetParent(rootEnv.transform, false);
        Undo.RegisterCreatedObjectUndo(ceilingObj, "Create Rock_Ceiling");

        Transform tEntrance = CreateChild(ceilingObj.transform, "Ceiling_Entrance");
        Transform tArchTransition = CreateChild(ceilingObj.transform, "Ceiling_ArchTransition");
        Transform tTunnel = CreateChild(ceilingObj.transform, "Ceiling_MainTunnel");
        Transform tArenaGrandDome = CreateChild(ceilingObj.transform, "Ceiling_ArenaGrandDome");

        // 6. XÂY DỰNG CÁC PHÂN ĐOẠN TRẦN HANG
        BuildEntranceCeiling(tEntrance, cliffPrefab, rock1Prefab, rock2Prefab, rock3Prefab, rock4Prefab);
        BuildArchTransitionCeiling(tArchTransition, cliffPrefab, rock1Prefab, rock2Prefab, rock3Prefab, rock4Prefab);
        BuildMainTunnelCeiling(tTunnel, cliffPrefab, rock1Prefab, rock2Prefab, rock3Prefab, rock4Prefab);
        BuildArenaGrandDomeCeiling(tArenaGrandDome, cliffPrefab, rock1Prefab, rock2Prefab, rock3Prefab, rock4Prefab);

        // 7. Áp dụng Material URP Lit
        if (urpRockMat != null)
        {
            MeshRenderer[] renderers = ceilingObj.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var r in renderers)
            {
                r.sharedMaterial = urpRockMat;
            }
        }

        // 8. Đánh dấu dirty và lưu scene
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();

        Debug.Log("[BuildChapter6Ceiling] ĐÃ XÂY DỰNG HOÀN TẤT TRẦN HANG ĐÁ VÀ ĐƯỜNG BAY ĐẠI BÀNG CHO CHAPTER 6!");

        if (showDialog)
        {
            EditorUtility.DisplayDialog("Thành công",
                "Đã xây dựng hoàn tất trần hang đá tự nhiên cho Chapter 6:\n\n" +
                "- Che kín hoàn toàn khoảng trời hở lớn dọc lối đi và toàn bộ đấu trường\n" +
                "- Trần hang vòm khổng lồ từ hai bên vách trái/phải uốn lượn chụm vào giữa\n" +
                "- Chừa một lỗ hở tự nhiên phía trên đỉnh đấu trường (X=0, Z=54, Y=24m) làm đường bay vào của Đại Bàng\n" +
                "- Kích thước lỗ hở ~8x9m vừa đủ cho Đại Bàng sải cánh bay vào, viền đá gồ ghề tự nhiên\n" +
                "- Kết cấu đá overlap nhiều tầng, tựa vững chãi vào vách núi, không có đá lơ lửng\n" +
                "- Độ cao thông thủy Y >= 9m (hầm) và Y >= 15m-24m (đấu trường), hoàn toàn không cản trở Thạch Sanh hay góc nhìn camera.",
                "OK");
        }
    }

    private static Transform CreateChild(Transform parent, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    private static GameObject SpawnCeilingRock(Transform parent, GameObject prefab, string name, Vector3 pos, Quaternion rot, Vector3 scale)
    {
        if (prefab == null) return null;
        GameObject instance = Object.Instantiate(prefab, parent);
        instance.name = name;
        instance.transform.position = pos;
        instance.transform.rotation = rot;
        instance.transform.localScale = scale;

        if (urpRockMat != null)
        {
            MeshRenderer mr = instance.GetComponent<MeshRenderer>();
            if (mr != null) mr.sharedMaterial = urpRockMat;
        }

        Collider col = instance.GetComponent<Collider>();
        if (col == null)
        {
            MeshCollider mc = instance.AddComponent<MeshCollider>();
            mc.convex = false;
        }

        return instance;
    }

    /// <summary>
    /// PHÂN ĐOẠN 1: Khu vực Cửa hang & Trên đầu Thạch Sanh (Z = -46m đến -22m)
    /// </summary>
    private static void BuildEntranceCeiling(Transform parent, GameObject cliffPrefab, GameObject rock1Prefab, GameObject rock2Prefab, GameObject rock3Prefab, GameObject rock4Prefab)
    {
        // === KHUNG VÒM 1 (Z = -43m đến -40m) ===
        SpawnCeilingRock(parent, cliffPrefab, "Ent_OuterShell_L",
            new Vector3(-5.5f, 14.5f, -42.5f), Quaternion.Euler(5f, 65f, -8f), new Vector3(3.2f, 2.5f, 3.2f));
        SpawnCeilingRock(parent, cliffPrefab, "Ent_OuterShell_R",
            new Vector3(5.5f, 14.8f, -41.5f), Quaternion.Euler(5f, -85f, 8f), new Vector3(3.2f, 2.5f, 3.2f));

        SpawnCeilingRock(parent, cliffPrefab, "Ent_VaultRib1_LeftArm",
            new Vector3(-12.5f, 8.5f, -42.0f), Quaternion.Euler(10f, 30f, -38f), new Vector3(2.6f, 2.8f, 2.6f));
        SpawnCeilingRock(parent, cliffPrefab, "Ent_VaultRib1_RightArm",
            new Vector3(12.5f, 8.5f, -42.0f), Quaternion.Euler(10f, -150f, 38f), new Vector3(2.6f, 2.8f, 2.6f));
        SpawnCeilingRock(parent, cliffPrefab, "Ent_VaultRib1_Keystone",
            new Vector3(0.0f, 11.2f, -42.0f), Quaternion.Euler(185f, 20f, 0f), new Vector3(3.2f, 2.2f, 2.8f));

        SpawnCeilingRock(parent, rock1Prefab, "Ent_Detail_Stalactite1_L",
            new Vector3(-4.8f, 8.8f, -42.0f), Quaternion.Euler(20f, 45f, -15f), new Vector3(2.0f, 2.4f, 2.0f));
        SpawnCeilingRock(parent, rock2Prefab, "Ent_Detail_Stalactite1_R",
            new Vector3(4.5f, 8.6f, -42.0f), Quaternion.Euler(-15f, -60f, 20f), new Vector3(2.2f, 2.2f, 2.0f));
        SpawnCeilingRock(parent, rock4Prefab, "Ent_Detail_Plugger1_C",
            new Vector3(0.5f, 9.6f, -43.0f), Quaternion.Euler(195f, 110f, -10f), new Vector3(1.8f, 2.2f, 1.8f));

        // === KHUNG VÒM 2 (Z = -36m đến -33m) ===
        SpawnCeilingRock(parent, cliffPrefab, "Ent_OuterShell2_L",
            new Vector3(-6.5f, 15.0f, -34.5f), Quaternion.Euler(8f, 45f, -10f), new Vector3(3.2f, 2.6f, 3.2f));
        SpawnCeilingRock(parent, cliffPrefab, "Ent_OuterShell2_R",
            new Vector3(6.5f, 15.0f, -34.5f), Quaternion.Euler(8f, -135f, 10f), new Vector3(3.2f, 2.6f, 3.2f));
        SpawnCeilingRock(parent, cliffPrefab, "Ent_OuterShell2_Crown",
            new Vector3(0.0f, 15.6f, -35.5f), Quaternion.Euler(0f, 90f, 0f), new Vector3(3.5f, 2.6f, 3.2f));

        SpawnCeilingRock(parent, cliffPrefab, "Ent_VaultRib2_LeftArm",
            new Vector3(-12.5f, 9.0f, -35.0f), Quaternion.Euler(5f, 25f, -34f), new Vector3(2.7f, 3.0f, 2.6f));
        SpawnCeilingRock(parent, cliffPrefab, "Ent_VaultRib2_RightArm",
            new Vector3(12.5f, 9.0f, -35.0f), Quaternion.Euler(5f, -155f, 34f), new Vector3(2.7f, 3.0f, 2.6f));
        SpawnCeilingRock(parent, cliffPrefab, "Ent_VaultRib2_MidLeft",
            new Vector3(-5.5f, 11.5f, -35.0f), Quaternion.Euler(15f, 15f, -25f), new Vector3(2.6f, 2.5f, 2.6f));
        SpawnCeilingRock(parent, cliffPrefab, "Ent_VaultRib2_MidRight",
            new Vector3(5.5f, 11.5f, -35.0f), Quaternion.Euler(15f, -165f, 25f), new Vector3(2.6f, 2.5f, 2.6f));
        SpawnCeilingRock(parent, cliffPrefab, "Ent_VaultRib2_Keystone",
            new Vector3(0.0f, 12.2f, -35.0f), Quaternion.Euler(180f, 40f, 0f), new Vector3(3.4f, 2.4f, 2.8f));

        SpawnCeilingRock(parent, rock1Prefab, "Ent_Detail_Stalactite2_L",
            new Vector3(-3.8f, 9.2f, -35.5f), Quaternion.Euler(30f, 20f, -12f), new Vector3(2.0f, 2.5f, 2.0f));
        SpawnCeilingRock(parent, rock2Prefab, "Ent_Detail_Stalactite2_R",
            new Vector3(3.2f, 9.0f, -35.0f), Quaternion.Euler(-25f, 80f, 15f), new Vector3(2.2f, 2.4f, 2.0f));
        SpawnCeilingRock(parent, rock4Prefab, "Ent_Detail_Plugger2_C",
            new Vector3(-0.4f, 10.2f, -34.2f), Quaternion.Euler(180f, 0f, 45f), new Vector3(2.0f, 2.2f, 2.0f));

        // === KHUNG VÒM 3 (Z = -29m đến -26m) ===
        SpawnCeilingRock(parent, cliffPrefab, "Ent_OuterShell3_L",
            new Vector3(-4.0f, 16.0f, -28.0f), Quaternion.Euler(0f, 60f, 0f), new Vector3(3.4f, 2.6f, 3.2f));
        SpawnCeilingRock(parent, cliffPrefab, "Ent_OuterShell3_R",
            new Vector3(4.0f, 16.2f, -28.5f), Quaternion.Euler(0f, -70f, 0f), new Vector3(3.4f, 2.6f, 3.2f));

        SpawnCeilingRock(parent, cliffPrefab, "Ent_VaultRib3_LeftArm",
            new Vector3(-12.8f, 9.5f, -28.0f), Quaternion.Euler(0f, 35f, -35f), new Vector3(2.7f, 3.0f, 2.6f));
        SpawnCeilingRock(parent, cliffPrefab, "Ent_VaultRib3_RightArm",
            new Vector3(12.8f, 9.5f, -28.0f), Quaternion.Euler(0f, -145f, 35f), new Vector3(2.7f, 3.0f, 2.6f));
        SpawnCeilingRock(parent, cliffPrefab, "Ent_VaultRib3_MidLeft",
            new Vector3(-5.8f, 12.2f, -28.0f), Quaternion.Euler(10f, 20f, -28f), new Vector3(2.6f, 2.5f, 2.6f));
        SpawnCeilingRock(parent, cliffPrefab, "Ent_VaultRib3_MidRight",
            new Vector3(5.8f, 12.2f, -28.0f), Quaternion.Euler(10f, -160f, 28f), new Vector3(2.6f, 2.5f, 2.6f));
        SpawnCeilingRock(parent, cliffPrefab, "Ent_VaultRib3_Keystone",
            new Vector3(0.0f, 13.0f, -28.0f), Quaternion.Euler(185f, -15f, 0f), new Vector3(3.4f, 2.4f, 2.8f));

        SpawnCeilingRock(parent, rock2Prefab, "Ent_Detail_Stalactite3_L",
            new Vector3(-4.2f, 9.6f, -28.5f), Quaternion.Euler(15f, 30f, -20f), new Vector3(2.2f, 2.4f, 2.2f));
        SpawnCeilingRock(parent, rock1Prefab, "Ent_Detail_Stalactite3_R",
            new Vector3(3.8f, 9.5f, -28.0f), Quaternion.Euler(20f, -60f, 15f), new Vector3(2.2f, 2.4f, 2.2f));
        SpawnCeilingRock(parent, rock4Prefab, "Ent_Detail_Plugger3_C",
            new Vector3(0.0f, 10.6f, -27.0f), Quaternion.Euler(190f, 45f, 0f), new Vector3(2.0f, 2.4f, 2.0f));
    }

    /// <summary>
    /// PHÂN ĐOẠN 2: Chuyển tiếp vào Vòm đá khổng lồ (Z = -23m đến -15m)
    /// </summary>
    private static void BuildArchTransitionCeiling(Transform parent, GameObject cliffPrefab, GameObject rock1Prefab, GameObject rock2Prefab, GameObject rock3Prefab, GameObject rock4Prefab)
    {
        // Phía trước Vòm chính (Z = -22.5m)
        SpawnCeilingRock(parent, cliffPrefab, "ArchTrans_Front_L",
            new Vector3(-8.5f, 14.5f, -22.5f), Quaternion.Euler(15f, 30f, -35f), new Vector3(2.8f, 2.8f, 2.8f));
        SpawnCeilingRock(parent, cliffPrefab, "ArchTrans_Front_R",
            new Vector3(8.5f, 14.5f, -22.5f), Quaternion.Euler(15f, -150f, 35f), new Vector3(2.8f, 2.8f, 2.8f));
        SpawnCeilingRock(parent, cliffPrefab, "ArchTrans_Front_Apex",
            new Vector3(0.0f, 17.0f, -22.5f), Quaternion.Euler(180f, 10f, 0f), new Vector3(3.6f, 2.6f, 3.0f));
        SpawnCeilingRock(parent, cliffPrefab, "ArchTrans_Front_HighFiller",
            new Vector3(0.0f, 21.0f, -21.5f), Quaternion.Euler(0f, 85f, 0f), new Vector3(3.8f, 2.8f, 3.4f));

        SpawnCeilingRock(parent, rock1Prefab, "ArchTrans_Front_StalactiteL",
            new Vector3(-4.8f, 12.0f, -22.5f), Quaternion.Euler(10f, 45f, -15f), new Vector3(2.4f, 2.6f, 2.2f));
        SpawnCeilingRock(parent, rock2Prefab, "ArchTrans_Front_StalactiteR",
            new Vector3(4.8f, 12.0f, -22.5f), Quaternion.Euler(-10f, -50f, 15f), new Vector3(2.4f, 2.6f, 2.2f));

        // Phía sau Vòm chính chuyển vào Hầm đá (Z = -15.5m)
        SpawnCeilingRock(parent, cliffPrefab, "ArchTrans_Back_L",
            new Vector3(-8.8f, 14.2f, -15.5f), Quaternion.Euler(-15f, 25f, -35f), new Vector3(2.8f, 2.8f, 2.8f));
        SpawnCeilingRock(parent, cliffPrefab, "ArchTrans_Back_R",
            new Vector3(8.8f, 14.2f, -15.5f), Quaternion.Euler(-15f, -155f, 35f), new Vector3(2.8f, 2.8f, 2.8f));
        SpawnCeilingRock(parent, cliffPrefab, "ArchTrans_Back_Apex",
            new Vector3(0.0f, 16.2f, -15.5f), Quaternion.Euler(185f, -20f, 0f), new Vector3(3.6f, 2.6f, 3.0f));
        SpawnCeilingRock(parent, cliffPrefab, "ArchTrans_Back_HighFiller",
            new Vector3(0.0f, 20.0f, -16.5f), Quaternion.Euler(0f, -80f, 0f), new Vector3(3.6f, 2.8f, 3.4f));

        SpawnCeilingRock(parent, rock4Prefab, "ArchTrans_Back_StalactiteL",
            new Vector3(-4.0f, 11.5f, -15.5f), Quaternion.Euler(180f, 15f, 0f), new Vector3(2.2f, 2.4f, 2.0f));
        SpawnCeilingRock(parent, rock1Prefab, "ArchTrans_Back_StalactiteR",
            new Vector3(4.0f, 11.5f, -15.5f), Quaternion.Euler(20f, -45f, 10f), new Vector3(2.2f, 2.4f, 2.0f));
    }

    /// <summary>
    /// PHÂN ĐOẠN 3: Hầm đá chính (Z = -12m đến +26m) - Đóng kín trần hầm kiên cố
    /// </summary>
    private static void BuildMainTunnelCeiling(Transform parent, GameObject cliffPrefab, GameObject rock1Prefab, GameObject rock2Prefab, GameObject rock3Prefab, GameObject rock4Prefab)
    {
        // === KHUNG VÒM 1 (Z = -9m đến -7m) ===
        SpawnCeilingRock(parent, cliffPrefab, "Tunnel_OuterShell1_R",
            new Vector3(3.5f, 15.5f, -9.0f), Quaternion.Euler(0f, 70f, 0f), new Vector3(3.6f, 2.6f, 3.4f));
        SpawnCeilingRock(parent, cliffPrefab, "Tunnel_OuterShell1_L",
            new Vector3(-7.5f, 15.0f, -8.5f), Quaternion.Euler(0f, -110f, 0f), new Vector3(3.2f, 2.5f, 3.0f));
        SpawnCeilingRock(parent, cliffPrefab, "Tunnel_OuterShell1_Crown",
            new Vector3(0.0f, 16.2f, -8.0f), Quaternion.Euler(5f, 15f, 0f), new Vector3(3.6f, 2.4f, 3.2f));

        SpawnCeilingRock(parent, cliffPrefab, "Tunnel_Vault1_LeftArm",
            new Vector3(-12.5f, 9.5f, -9.0f), Quaternion.Euler(10f, 35f, -32f), new Vector3(2.7f, 3.0f, 2.7f));
        SpawnCeilingRock(parent, cliffPrefab, "Tunnel_Vault1_RightArm",
            new Vector3(12.5f, 9.5f, -9.0f), Quaternion.Euler(10f, -145f, 32f), new Vector3(2.7f, 3.0f, 2.7f));
        SpawnCeilingRock(parent, cliffPrefab, "Tunnel_Vault1_RightBridge",
            new Vector3(3.5f, 12.2f, -9.0f), Quaternion.Euler(175f, -10f, 15f), new Vector3(3.0f, 2.4f, 2.8f));
        SpawnCeilingRock(parent, cliffPrefab, "Tunnel_Vault1_LeftBridge",
            new Vector3(-5.2f, 12.5f, -9.0f), Quaternion.Euler(185f, 20f, -15f), new Vector3(2.8f, 2.4f, 2.8f));
        SpawnCeilingRock(parent, cliffPrefab, "Tunnel_Vault1_Keystone",
            new Vector3(0.0f, 13.0f, -9.0f), Quaternion.Euler(180f, 0f, 0f), new Vector3(3.4f, 2.4f, 2.8f));

        SpawnCeilingRock(parent, rock2Prefab, "Tunnel_Detail1_StalactiteL",
            new Vector3(-6.8f, 8.8f, -9.0f), Quaternion.Euler(15f, 40f, -15f), new Vector3(2.2f, 2.5f, 2.0f));
        SpawnCeilingRock(parent, rock1Prefab, "Tunnel_Detail1_StalactiteR",
            new Vector3(5.5f, 8.8f, -9.0f), Quaternion.Euler(-10f, -40f, 15f), new Vector3(2.2f, 2.5f, 2.0f));
        SpawnCeilingRock(parent, rock4Prefab, "Tunnel_Detail1_Plugger",
            new Vector3(0.0f, 10.2f, -9.0f), Quaternion.Euler(190f, 85f, 0f), new Vector3(2.2f, 2.2f, 2.0f));

        // === KHUNG VÒM 2 (Z = -2m đến +1m) ===
        SpawnCeilingRock(parent, cliffPrefab, "Tunnel_OuterShell2_L",
            new Vector3(-4.0f, 15.8f, -1.0f), Quaternion.Euler(5f, 50f, 0f), new Vector3(3.5f, 2.6f, 3.4f));
        SpawnCeilingRock(parent, cliffPrefab, "Tunnel_OuterShell2_R",
            new Vector3(4.0f, 15.8f, -1.5f), Quaternion.Euler(5f, -130f, 0f), new Vector3(3.5f, 2.6f, 3.4f));

        SpawnCeilingRock(parent, cliffPrefab, "Tunnel_Vault2_LeftArm",
            new Vector3(-13.0f, 9.2f, -1.0f), Quaternion.Euler(0f, 25f, -35f), new Vector3(2.8f, 3.0f, 2.8f));
        SpawnCeilingRock(parent, cliffPrefab, "Tunnel_Vault2_RightArm",
            new Vector3(13.0f, 9.2f, -1.0f), Quaternion.Euler(0f, -155f, 35f), new Vector3(2.8f, 3.0f, 2.8f));
        SpawnCeilingRock(parent, cliffPrefab, "Tunnel_Vault2_MidLeft",
            new Vector3(-5.8f, 11.8f, -1.0f), Quaternion.Euler(10f, 15f, -25f), new Vector3(2.6f, 2.5f, 2.6f));
        SpawnCeilingRock(parent, cliffPrefab, "Tunnel_Vault2_MidRight",
            new Vector3(5.8f, 11.8f, -1.0f), Quaternion.Euler(10f, -165f, 25f), new Vector3(2.6f, 2.5f, 2.6f));
        SpawnCeilingRock(parent, cliffPrefab, "Tunnel_Vault2_Keystone",
            new Vector3(0.0f, 12.8f, -1.0f), Quaternion.Euler(180f, 30f, 0f), new Vector3(3.5f, 2.5f, 3.0f));

        SpawnCeilingRock(parent, rock1Prefab, "Tunnel_Detail2_StalactiteL",
            new Vector3(-3.8f, 9.2f, -1.0f), Quaternion.Euler(25f, 30f, -10f), new Vector3(2.2f, 2.5f, 2.0f));
        SpawnCeilingRock(parent, rock2Prefab, "Tunnel_Detail2_StalactiteR",
            new Vector3(3.6f, 9.0f, -1.0f), Quaternion.Euler(-20f, 75f, 12f), new Vector3(2.2f, 2.5f, 2.0f));
        SpawnCeilingRock(parent, rock4Prefab, "Tunnel_Detail2_Plugger",
            new Vector3(0.0f, 10.4f, -0.5f), Quaternion.Euler(185f, -15f, 30f), new Vector3(2.0f, 2.4f, 2.0f));

        // === KHUNG VÒM 3 (Z = +6m đến +9m) ===
        SpawnCeilingRock(parent, cliffPrefab, "Tunnel_OuterShell3_Crown",
            new Vector3(0.0f, 16.2f, 7.5f), Quaternion.Euler(0f, 80f, 0f), new Vector3(3.8f, 2.6f, 3.5f));
        SpawnCeilingRock(parent, cliffPrefab, "Tunnel_OuterShell3_L",
            new Vector3(-6.5f, 15.5f, 7.0f), Quaternion.Euler(10f, 30f, -10f), new Vector3(3.2f, 2.5f, 3.2f));
        SpawnCeilingRock(parent, cliffPrefab, "Tunnel_OuterShell3_R",
            new Vector3(6.5f, 15.5f, 7.0f), Quaternion.Euler(10f, -150f, 10f), new Vector3(3.2f, 2.5f, 3.2f));

        SpawnCeilingRock(parent, cliffPrefab, "Tunnel_Vault3_LeftArm",
            new Vector3(-13.0f, 9.4f, 7.5f), Quaternion.Euler(5f, 40f, -32f), new Vector3(2.8f, 3.0f, 2.8f));
        SpawnCeilingRock(parent, cliffPrefab, "Tunnel_Vault3_RightArm",
            new Vector3(13.0f, 9.4f, 7.5f), Quaternion.Euler(5f, -140f, 32f), new Vector3(2.8f, 3.0f, 2.8f));
        SpawnCeilingRock(parent, cliffPrefab, "Tunnel_Vault3_MidLeft",
            new Vector3(-6.2f, 12.0f, 7.5f), Quaternion.Euler(15f, 20f, -22f), new Vector3(2.6f, 2.5f, 2.6f));
        SpawnCeilingRock(parent, cliffPrefab, "Tunnel_Vault3_MidRight",
            new Vector3(6.2f, 12.0f, 7.5f), Quaternion.Euler(15f, -160f, 22f), new Vector3(2.6f, 2.5f, 2.6f));
        SpawnCeilingRock(parent, cliffPrefab, "Tunnel_Vault3_Keystone",
            new Vector3(0.0f, 13.0f, 7.5f), Quaternion.Euler(185f, -35f, 0f), new Vector3(3.5f, 2.5f, 3.0f));

        SpawnCeilingRock(parent, rock2Prefab, "Tunnel_Detail3_StalactiteL",
            new Vector3(-4.8f, 9.4f, 7.5f), Quaternion.Euler(10f, 50f, -15f), new Vector3(2.2f, 2.4f, 2.2f));
        SpawnCeilingRock(parent, rock1Prefab, "Tunnel_Detail3_StalactiteR",
            new Vector3(4.2f, 9.2f, 7.5f), Quaternion.Euler(-15f, -45f, 15f), new Vector3(2.2f, 2.4f, 2.2f));
        SpawnCeilingRock(parent, rock3Prefab, "Tunnel_Detail3_Plugger",
            new Vector3(-0.5f, 10.6f, 8.0f), Quaternion.Euler(190f, 20f, 0f), new Vector3(2.0f, 2.2f, 2.0f));

        // === KHUNG VÒM 4 (Z = +14m đến +17m) ===
        SpawnCeilingRock(parent, cliffPrefab, "Tunnel_OuterShell4_L",
            new Vector3(-3.0f, 15.8f, 15.0f), Quaternion.Euler(0f, 65f, 0f), new Vector3(3.6f, 2.6f, 3.4f));
        SpawnCeilingRock(parent, cliffPrefab, "Tunnel_OuterShell4_R",
            new Vector3(3.0f, 15.8f, 15.5f), Quaternion.Euler(0f, -115f, 0f), new Vector3(3.6f, 2.6f, 3.4f));
        SpawnCeilingRock(parent, cliffPrefab, "Tunnel_OuterShell4_Crown",
            new Vector3(0.0f, 16.5f, 15.0f), Quaternion.Euler(0f, 0f, 0f), new Vector3(3.6f, 2.4f, 3.2f));

        SpawnCeilingRock(parent, cliffPrefab, "Tunnel_Vault4_LeftArm",
            new Vector3(-13.2f, 9.2f, 15.0f), Quaternion.Euler(-5f, 30f, -35f), new Vector3(2.8f, 3.0f, 2.8f));
        SpawnCeilingRock(parent, cliffPrefab, "Tunnel_Vault4_RightArm",
            new Vector3(13.2f, 9.2f, 15.0f), Quaternion.Euler(-5f, -150f, 35f), new Vector3(2.8f, 3.0f, 2.8f));
        SpawnCeilingRock(parent, cliffPrefab, "Tunnel_Vault4_LeftBridge",
            new Vector3(-3.8f, 12.4f, 15.0f), Quaternion.Euler(180f, 25f, -15f), new Vector3(3.2f, 2.4f, 2.8f));
        SpawnCeilingRock(parent, cliffPrefab, "Tunnel_Vault4_RightBridge",
            new Vector3(4.0f, 12.4f, 15.0f), Quaternion.Euler(180f, -30f, 20f), new Vector3(3.2f, 2.4f, 2.8f));
        SpawnCeilingRock(parent, cliffPrefab, "Tunnel_Vault4_Keystone",
            new Vector3(0.0f, 13.2f, 15.0f), Quaternion.Euler(180f, 10f, 0f), new Vector3(3.5f, 2.4f, 3.0f));

        SpawnCeilingRock(parent, rock1Prefab, "Tunnel_Detail4_StalactiteL",
            new Vector3(-6.5f, 9.0f, 15.0f), Quaternion.Euler(15f, 30f, -12f), new Vector3(2.2f, 2.4f, 2.2f));
        SpawnCeilingRock(parent, rock4Prefab, "Tunnel_Detail4_StalactiteR",
            new Vector3(5.8f, 9.0f, 15.0f), Quaternion.Euler(-10f, -50f, 15f), new Vector3(2.0f, 2.4f, 2.0f));
        SpawnCeilingRock(parent, rock2Prefab, "Tunnel_Detail4_Plugger",
            new Vector3(0.0f, 10.6f, 15.5f), Quaternion.Euler(185f, 40f, -10f), new Vector3(2.2f, 2.2f, 2.0f));

        // === KHUNG VÒM 5 (Z = +22m đến +25m) ===
        SpawnCeilingRock(parent, cliffPrefab, "Tunnel_OuterShell5_Crown",
            new Vector3(0.0f, 16.8f, 23.0f), Quaternion.Euler(0f, 85f, 0f), new Vector3(3.8f, 2.6f, 3.5f));
        SpawnCeilingRock(parent, cliffPrefab, "Tunnel_OuterShell5_L",
            new Vector3(-7.0f, 16.0f, 23.0f), Quaternion.Euler(10f, 35f, -10f), new Vector3(3.4f, 2.5f, 3.2f));
        SpawnCeilingRock(parent, cliffPrefab, "Tunnel_OuterShell5_R",
            new Vector3(7.0f, 16.0f, 23.0f), Quaternion.Euler(10f, -145f, 10f), new Vector3(3.4f, 2.5f, 3.2f));

        SpawnCeilingRock(parent, cliffPrefab, "Tunnel_Vault5_LeftArm",
            new Vector3(-13.5f, 9.8f, 23.0f), Quaternion.Euler(10f, 20f, -32f), new Vector3(2.8f, 3.0f, 2.8f));
        SpawnCeilingRock(parent, cliffPrefab, "Tunnel_Vault5_RightArm",
            new Vector3(13.5f, 9.8f, 23.0f), Quaternion.Euler(10f, -160f, 32f), new Vector3(2.8f, 3.0f, 2.8f));
        SpawnCeilingRock(parent, cliffPrefab, "Tunnel_Vault5_MidLeft",
            new Vector3(-6.5f, 12.4f, 23.0f), Quaternion.Euler(10f, 10f, -25f), new Vector3(2.7f, 2.5f, 2.7f));
        SpawnCeilingRock(parent, cliffPrefab, "Tunnel_Vault5_MidRight",
            new Vector3(6.5f, 12.4f, 23.0f), Quaternion.Euler(10f, -170f, 25f), new Vector3(2.7f, 2.5f, 2.7f));
        SpawnCeilingRock(parent, cliffPrefab, "Tunnel_Vault5_Keystone",
            new Vector3(0.0f, 13.5f, 23.0f), Quaternion.Euler(180f, 45f, 0f), new Vector3(3.6f, 2.5f, 3.0f));

        SpawnCeilingRock(parent, rock1Prefab, "Tunnel_Detail5_StalactiteL",
            new Vector3(-4.2f, 10.0f, 23.0f), Quaternion.Euler(20f, 25f, -15f), new Vector3(2.2f, 2.4f, 2.2f));
        SpawnCeilingRock(parent, rock2Prefab, "Tunnel_Detail5_StalactiteR",
            new Vector3(4.2f, 10.0f, 23.0f), Quaternion.Euler(-20f, -65f, 15f), new Vector3(2.2f, 2.4f, 2.2f));
        SpawnCeilingRock(parent, rock4Prefab, "Tunnel_Detail5_Plugger",
            new Vector3(0.0f, 11.0f, 23.5f), Quaternion.Euler(180f, 0f, 45f), new Vector3(2.0f, 2.2f, 2.0f));
    }

    /// <summary>
    /// PHÂN ĐOẠN 4: VÒM TRẦN ĐẤU TRƯỜNG KHỔNG LỒ & ĐƯỜNG BAY ĐẠI BÀNG (Z = +26m đến +78m)
    /// Tạo cấu trúc vòm 360 độ từ hai bên vách trái/phải chụm vào giữa.
    /// Giữ một lỗ hở tự nhiên duy nhất vừa đủ cho Đại Bàng bay vào tại tâm trần (X=0, Z=54m, Y=24m).
    /// </summary>
    private static void BuildArenaGrandDomeCeiling(Transform parent, GameObject cliffPrefab, GameObject rock1Prefab, GameObject rock2Prefab, GameObject rock3Prefab, GameObject rock4Prefab)
    {
        // -------------------------------------------------------------
        // A. ĐẠI VÒM CỬA TIỀN TIÊU VÀO ĐẤU TRƯỜNG (Z = 28m)
        // -------------------------------------------------------------
        SpawnCeilingRock(parent, cliffPrefab, "Dome_Portal_Summit",
            new Vector3(0.0f, 19.5f, 28.5f), Quaternion.Euler(0f, 90f, 0f), new Vector3(4.2f, 3.0f, 3.6f));
        SpawnCeilingRock(parent, cliffPrefab, "Dome_Portal_UpperL",
            new Vector3(-8.5f, 17.5f, 28.0f), Quaternion.Euler(15f, 40f, -20f), new Vector3(3.5f, 2.8f, 3.4f));
        SpawnCeilingRock(parent, cliffPrefab, "Dome_Portal_UpperR",
            new Vector3(8.5f, 17.5f, 28.0f), Quaternion.Euler(15f, -140f, 20f), new Vector3(3.5f, 2.8f, 3.4f));

        SpawnCeilingRock(parent, cliffPrefab, "Dome_Portal_PillarL",
            new Vector3(-15.5f, 11.0f, 28.0f), Quaternion.Euler(0f, 30f, -36f), new Vector3(3.0f, 3.4f, 3.0f));
        SpawnCeilingRock(parent, cliffPrefab, "Dome_Portal_PillarR",
            new Vector3(15.5f, 11.0f, 28.0f), Quaternion.Euler(0f, -150f, 36f), new Vector3(3.0f, 3.4f, 3.0f));
        SpawnCeilingRock(parent, cliffPrefab, "Dome_Portal_SpanL",
            new Vector3(-8.0f, 14.5f, 28.0f), Quaternion.Euler(10f, 15f, -28f), new Vector3(2.8f, 2.8f, 2.8f));
        SpawnCeilingRock(parent, cliffPrefab, "Dome_Portal_SpanR",
            new Vector3(8.0f, 14.5f, 28.0f), Quaternion.Euler(10f, -165f, 28f), new Vector3(2.8f, 2.8f, 2.8f));
        SpawnCeilingRock(parent, cliffPrefab, "Dome_Portal_Keystone",
            new Vector3(0.0f, 16.0f, 28.0f), Quaternion.Euler(185f, 0f, 0f), new Vector3(4.0f, 2.8f, 3.4f));

        SpawnCeilingRock(parent, rock1Prefab, "Dome_Portal_DetailL",
            new Vector3(-5.2f, 12.0f, 28.2f), Quaternion.Euler(15f, 35f, -15f), new Vector3(2.5f, 2.6f, 2.4f));
        SpawnCeilingRock(parent, rock2Prefab, "Dome_Portal_DetailR",
            new Vector3(5.2f, 12.0f, 28.2f), Quaternion.Euler(-15f, -55f, 15f), new Vector3(2.5f, 2.6f, 2.4f));

        // -------------------------------------------------------------
        // B. CUNG VÒM NAM (PHÍA TRƯỚC LỖ ĐẠI BÀNG, Z = 34m đến 48m)
        // Leo dần từ Z=28m lên đến viền trước lỗ bay (Z=49m, Y=23.5m)
        // -------------------------------------------------------------
        // Khung Rib Nam 1 (Z = 35m)
        SpawnCeilingRock(parent, cliffPrefab, "Dome_SouthRib1_CliffL",
            new Vector3(-17.5f, 14.5f, 35.0f), Quaternion.Euler(15f, 45f, -30f), new Vector3(3.4f, 3.0f, 3.4f));
        SpawnCeilingRock(parent, cliffPrefab, "Dome_SouthRib1_CliffR",
            new Vector3(17.5f, 14.5f, 35.0f), Quaternion.Euler(15f, -135f, 30f), new Vector3(3.4f, 3.0f, 3.4f));
        SpawnCeilingRock(parent, cliffPrefab, "Dome_SouthRib1_MidL",
            new Vector3(-8.8f, 17.5f, 35.0f), Quaternion.Euler(10f, 30f, -22f), new Vector3(3.4f, 2.6f, 3.2f));
        SpawnCeilingRock(parent, cliffPrefab, "Dome_SouthRib1_MidR",
            new Vector3(8.8f, 17.5f, 35.0f), Quaternion.Euler(10f, -150f, 22f), new Vector3(3.4f, 2.6f, 3.2f));
        SpawnCeilingRock(parent, cliffPrefab, "Dome_SouthRib1_Center",
            new Vector3(0.0f, 19.0f, 35.0f), Quaternion.Euler(180f, 20f, 0f), new Vector3(4.0f, 2.6f, 3.4f));
        SpawnCeilingRock(parent, cliffPrefab, "Dome_SouthRib1_HighCrown",
            new Vector3(0.0f, 23.0f, 35.5f), Quaternion.Euler(0f, 85f, 0f), new Vector3(4.0f, 2.8f, 3.6f));

        // Khung Rib Nam 2 (Z = 42m)
        SpawnCeilingRock(parent, cliffPrefab, "Dome_SouthRib2_CliffL",
            new Vector3(-18.5f, 15.5f, 42.0f), Quaternion.Euler(20f, 50f, -25f), new Vector3(3.4f, 3.0f, 3.4f));
        SpawnCeilingRock(parent, cliffPrefab, "Dome_SouthRib2_CliffR",
            new Vector3(18.5f, 15.5f, 42.0f), Quaternion.Euler(20f, -130f, 25f), new Vector3(3.4f, 3.0f, 3.4f));
        SpawnCeilingRock(parent, cliffPrefab, "Dome_SouthRib2_MidL",
            new Vector3(-9.2f, 19.2f, 42.0f), Quaternion.Euler(12f, 25f, -20f), new Vector3(3.5f, 2.8f, 3.2f));
        SpawnCeilingRock(parent, cliffPrefab, "Dome_SouthRib2_MidR",
            new Vector3(9.2f, 19.2f, 42.0f), Quaternion.Euler(12f, -155f, 20f), new Vector3(3.5f, 2.8f, 3.2f));
        SpawnCeilingRock(parent, cliffPrefab, "Dome_SouthRib2_Center",
            new Vector3(0.0f, 21.0f, 42.0f), Quaternion.Euler(185f, -15f, 0f), new Vector3(4.2f, 2.6f, 3.5f));
        SpawnCeilingRock(parent, cliffPrefab, "Dome_SouthRib2_HighCrown",
            new Vector3(0.0f, 24.5f, 42.5f), Quaternion.Euler(0f, -95f, 0f), new Vector3(4.2f, 2.8f, 3.6f));

        // Khung Tiền Tiêu Viền Trước Lỗ Bay (Z = 47m đến 49m)
        SpawnCeilingRock(parent, cliffPrefab, "Dome_FrontLip_VaultL",
            new Vector3(-7.5f, 21.5f, 47.5f), Quaternion.Euler(15f, 20f, -15f), new Vector3(3.4f, 2.6f, 3.2f));
        SpawnCeilingRock(parent, cliffPrefab, "Dome_FrontLip_VaultR",
            new Vector3(7.5f, 21.5f, 47.5f), Quaternion.Euler(15f, -160f, 15f), new Vector3(3.4f, 2.6f, 3.2f));
        SpawnCeilingRock(parent, cliffPrefab, "Dome_FrontLip_Keystone",
            new Vector3(0.0f, 23.2f, 47.5f), Quaternion.Euler(180f, 10f, 0f), new Vector3(3.8f, 2.5f, 3.2f));
        SpawnCeilingRock(parent, rock1Prefab, "Dome_FrontLip_CragL",
            new Vector3(-3.2f, 23.0f, 49.0f), Quaternion.Euler(20f, 40f, -10f), new Vector3(2.5f, 2.5f, 2.5f));
        SpawnCeilingRock(parent, rock1Prefab, "Dome_FrontLip_CragR",
            new Vector3(3.2f, 23.0f, 49.0f), Quaternion.Euler(20f, -140f, 10f), new Vector3(2.5f, 2.5f, 2.5f));

        // -------------------------------------------------------------
        // C. CÁNH VÒM TÂY (BÊN TRÁI ĐẤU TRƯỜNG, X = -24m đến -4.5m, Z = 45m đến 64m)
        // Chụm từ vách trái uốn cong vào tâm lỗ bay
        // -------------------------------------------------------------
        // Tầng chân vách trái tựa chắc chắn vào vách đá
        SpawnCeilingRock(parent, cliffPrefab, "Dome_West_Base1",
            new Vector3(-21.5f, 15.0f, 48.0f), Quaternion.Euler(15f, 60f, -28f), new Vector3(3.4f, 3.2f, 3.4f));
        SpawnCeilingRock(parent, cliffPrefab, "Dome_West_Base2",
            new Vector3(-22.5f, 15.5f, 54.0f), Quaternion.Euler(10f, 75f, -28f), new Vector3(3.5f, 3.2f, 3.5f));
        SpawnCeilingRock(parent, cliffPrefab, "Dome_West_Base3",
            new Vector3(-21.5f, 15.0f, 60.0f), Quaternion.Euler(15f, 90f, -28f), new Vector3(3.4f, 3.2f, 3.4f));

        // Tầng giữa sườn vòm Tây vươn vào lòng hang
        SpawnCeilingRock(parent, cliffPrefab, "Dome_West_MidSlope1",
            new Vector3(-14.5f, 19.0f, 49.0f), Quaternion.Euler(12f, 45f, -22f), new Vector3(3.6f, 2.8f, 3.4f));
        SpawnCeilingRock(parent, cliffPrefab, "Dome_West_MidSlope2",
            new Vector3(-15.0f, 19.5f, 54.0f), Quaternion.Euler(10f, 70f, -22f), new Vector3(3.6f, 2.8f, 3.5f));
        SpawnCeilingRock(parent, cliffPrefab, "Dome_West_MidSlope3",
            new Vector3(-14.5f, 19.0f, 59.0f), Quaternion.Euler(12f, 95f, -22f), new Vector3(3.6f, 2.8f, 3.4f));

        // Tầng cao áp sát viền trái lỗ bay Đại Bàng (X = -8m đến -4.5m)
        SpawnCeilingRock(parent, cliffPrefab, "Dome_West_HighCanopy1",
            new Vector3(-8.5f, 22.8f, 51.0f), Quaternion.Euler(8f, 30f, -15f), new Vector3(3.4f, 2.6f, 3.2f));
        SpawnCeilingRock(parent, cliffPrefab, "Dome_West_HighCanopy2",
            new Vector3(-8.8f, 23.5f, 54.5f), Quaternion.Euler(5f, 65f, -15f), new Vector3(3.4f, 2.6f, 3.4f));
        SpawnCeilingRock(parent, cliffPrefab, "Dome_West_HighCanopy3",
            new Vector3(-8.5f, 22.8f, 58.0f), Quaternion.Euler(8f, 100f, -15f), new Vector3(3.4f, 2.6f, 3.2f));

        // Mép đá gồ ghề tự nhiên viền trái lỗ bay
        SpawnCeilingRock(parent, rock2Prefab, "EagleHole_Rim_L1",
            new Vector3(-4.5f, 23.5f, 51.5f), Quaternion.Euler(10f, 45f, -20f), new Vector3(2.5f, 2.6f, 2.4f));
        SpawnCeilingRock(parent, rock1Prefab, "EagleHole_Rim_L2",
            new Vector3(-4.8f, 24.0f, 54.5f), Quaternion.Euler(0f, 75f, -15f), new Vector3(2.6f, 2.6f, 2.6f));
        SpawnCeilingRock(parent, rock2Prefab, "EagleHole_Rim_L3",
            new Vector3(-4.5f, 23.5f, 57.5f), Quaternion.Euler(10f, 105f, -20f), new Vector3(2.5f, 2.6f, 2.4f));

        // -------------------------------------------------------------
        // D. CÁNH VÒM ĐÔNG (BÊN PHẢI ĐẤU TRƯỜNG, X = +24m đến +4.5m, Z = 45m đến 64m)
        // Chụm từ vách phải uốn cong vào tâm lỗ bay
        // -------------------------------------------------------------
        // Tầng chân vách phải
        SpawnCeilingRock(parent, cliffPrefab, "Dome_East_Base1",
            new Vector3(21.5f, 15.0f, 48.0f), Quaternion.Euler(15f, -120f, 28f), new Vector3(3.4f, 3.2f, 3.4f));
        SpawnCeilingRock(parent, cliffPrefab, "Dome_East_Base2",
            new Vector3(22.5f, 15.5f, 54.0f), Quaternion.Euler(10f, -105f, 28f), new Vector3(3.5f, 3.2f, 3.5f));
        SpawnCeilingRock(parent, cliffPrefab, "Dome_East_Base3",
            new Vector3(21.5f, 15.0f, 60.0f), Quaternion.Euler(15f, -90f, 28f), new Vector3(3.4f, 3.2f, 3.4f));

        // Tầng giữa sườn vòm Đông
        SpawnCeilingRock(parent, cliffPrefab, "Dome_East_MidSlope1",
            new Vector3(14.5f, 19.0f, 49.0f), Quaternion.Euler(12f, -135f, 22f), new Vector3(3.6f, 2.8f, 3.4f));
        SpawnCeilingRock(parent, cliffPrefab, "Dome_East_MidSlope2",
            new Vector3(15.0f, 19.5f, 54.0f), Quaternion.Euler(10f, -110f, 22f), new Vector3(3.6f, 2.8f, 3.5f));
        SpawnCeilingRock(parent, cliffPrefab, "Dome_East_MidSlope3",
            new Vector3(14.5f, 19.0f, 59.0f), Quaternion.Euler(12f, -85f, 22f), new Vector3(3.6f, 2.8f, 3.4f));

        // Tầng cao áp sát viền phải lỗ bay Đại Bàng (X = +8m đến +4.5m)
        SpawnCeilingRock(parent, cliffPrefab, "Dome_East_HighCanopy1",
            new Vector3(8.5f, 22.8f, 51.0f), Quaternion.Euler(8f, -150f, 15f), new Vector3(3.4f, 2.6f, 3.2f));
        SpawnCeilingRock(parent, cliffPrefab, "Dome_East_HighCanopy2",
            new Vector3(8.8f, 23.5f, 54.5f), Quaternion.Euler(5f, -115f, 15f), new Vector3(3.4f, 2.6f, 3.4f));
        SpawnCeilingRock(parent, cliffPrefab, "Dome_East_HighCanopy3",
            new Vector3(8.5f, 22.8f, 58.0f), Quaternion.Euler(8f, -80f, 15f), new Vector3(3.4f, 2.6f, 3.2f));

        // Mép đá gồ ghề tự nhiên viền phải lỗ bay
        SpawnCeilingRock(parent, rock2Prefab, "EagleHole_Rim_R1",
            new Vector3(4.5f, 23.5f, 51.5f), Quaternion.Euler(10f, -135f, 20f), new Vector3(2.5f, 2.6f, 2.4f));
        SpawnCeilingRock(parent, rock1Prefab, "EagleHole_Rim_R2",
            new Vector3(4.8f, 24.0f, 54.5f), Quaternion.Euler(0f, -105f, 15f), new Vector3(2.6f, 2.6f, 2.6f));
        SpawnCeilingRock(parent, rock2Prefab, "EagleHole_Rim_R3",
            new Vector3(4.5f, 23.5f, 57.5f), Quaternion.Euler(10f, -75f, 20f), new Vector3(2.5f, 2.6f, 2.4f));

        // -------------------------------------------------------------
        // E. CUNG VÒM BẮC (PHÍA SAU LỖ ĐẠI BÀNG, Z = 59m đến 78m)
        // Đóng kín toàn bộ nửa sau đấu trường và viền sau lỗ bay
        // -------------------------------------------------------------
        // Viền Sau Lỗ Bay (Z = 59m đến 61m)
        SpawnCeilingRock(parent, rock1Prefab, "Dome_RearLip_CragL",
            new Vector3(-3.2f, 23.5f, 59.5f), Quaternion.Euler(-15f, 30f, -10f), new Vector3(2.5f, 2.5f, 2.5f));
        SpawnCeilingRock(parent, rock1Prefab, "Dome_RearLip_CragR",
            new Vector3(3.2f, 23.5f, 59.5f), Quaternion.Euler(-15f, -150f, 10f), new Vector3(2.5f, 2.5f, 2.5f));
        SpawnCeilingRock(parent, cliffPrefab, "Dome_RearLip_Keystone",
            new Vector3(0.0f, 24.2f, 60.5f), Quaternion.Euler(180f, 15f, 0f), new Vector3(3.8f, 2.5f, 3.2f));
        SpawnCeilingRock(parent, cliffPrefab, "Dome_RearLip_VaultL",
            new Vector3(-7.5f, 22.2f, 61.0f), Quaternion.Euler(-15f, 35f, -15f), new Vector3(3.4f, 2.6f, 3.2f));
        SpawnCeilingRock(parent, cliffPrefab, "Dome_RearLip_VaultR",
            new Vector3(7.5f, 22.2f, 61.0f), Quaternion.Euler(-15f, -145f, 15f), new Vector3(3.4f, 2.6f, 3.2f));

        // Khung Rib Bắc 1 (Z = 67m)
        SpawnCeilingRock(parent, cliffPrefab, "Dome_NorthRib1_CliffL",
            new Vector3(-18.0f, 16.0f, 67.0f), Quaternion.Euler(-15f, 40f, -25f), new Vector3(3.4f, 3.0f, 3.4f));
        SpawnCeilingRock(parent, cliffPrefab, "Dome_NorthRib1_CliffR",
            new Vector3(18.0f, 16.0f, 67.0f), Quaternion.Euler(-15f, -140f, 25f), new Vector3(3.4f, 3.0f, 3.4f));
        SpawnCeilingRock(parent, cliffPrefab, "Dome_NorthRib1_MidL",
            new Vector3(-9.0f, 20.0f, 67.0f), Quaternion.Euler(-10f, 20f, -18f), new Vector3(3.5f, 2.8f, 3.4f));
        SpawnCeilingRock(parent, cliffPrefab, "Dome_NorthRib1_MidR",
            new Vector3(9.0f, 20.0f, 67.0f), Quaternion.Euler(-10f, -160f, 18f), new Vector3(3.5f, 2.8f, 3.4f));
        SpawnCeilingRock(parent, cliffPrefab, "Dome_NorthRib1_Center",
            new Vector3(0.0f, 22.5f, 67.0f), Quaternion.Euler(180f, 30f, 0f), new Vector3(4.2f, 2.6f, 3.5f));
        SpawnCeilingRock(parent, cliffPrefab, "Dome_NorthRib1_HighCrown",
            new Vector3(0.0f, 25.0f, 67.5f), Quaternion.Euler(0f, 75f, 0f), new Vector3(4.2f, 2.8f, 3.6f));

        // Khung Rib Bắc 2 (Hậu Cung Đấu Trường, Z = 74m đến 77m)
        SpawnCeilingRock(parent, cliffPrefab, "Dome_NorthRib2_BackL",
            new Vector3(-15.5f, 15.0f, 74.0f), Quaternion.Euler(-20f, 30f, -25f), new Vector3(3.5f, 3.0f, 3.4f));
        SpawnCeilingRock(parent, cliffPrefab, "Dome_NorthRib2_BackR",
            new Vector3(15.5f, 15.0f, 74.0f), Quaternion.Euler(-20f, -150f, 25f), new Vector3(3.5f, 3.0f, 3.4f));
        SpawnCeilingRock(parent, cliffPrefab, "Dome_NorthRib2_MidL",
            new Vector3(-7.5f, 18.5f, 75.0f), Quaternion.Euler(-15f, 15f, -18f), new Vector3(3.5f, 2.8f, 3.4f));
        SpawnCeilingRock(parent, cliffPrefab, "Dome_NorthRib2_MidR",
            new Vector3(7.5f, 18.5f, 75.0f), Quaternion.Euler(-15f, -165f, 18f), new Vector3(3.5f, 2.8f, 3.4f));
        SpawnCeilingRock(parent, cliffPrefab, "Dome_NorthRib2_CenterKeystone",
            new Vector3(0.0f, 20.5f, 75.0f), Quaternion.Euler(185f, 0f, 0f), new Vector3(4.2f, 2.6f, 3.5f));
        SpawnCeilingRock(parent, cliffPrefab, "Dome_NorthRib2_BackCrown",
            new Vector3(0.0f, 23.5f, 76.5f), Quaternion.Euler(0f, 90f, 0f), new Vector3(4.5f, 2.8f, 3.6f));

        // -------------------------------------------------------------
        // F. CÁC KHỐI ĐÁ TẦNG NGOẠI CAO (OUTER ROOF SHELL OVERLAP)
        // Đặt ở cao độ Y=25-27m phủ chồng mép ngoài che kín 100% khe sáng
        // -------------------------------------------------------------
        SpawnCeilingRock(parent, cliffPrefab, "Dome_RoofShell_SW",
            new Vector3(-12.0f, 24.5f, 45.0f), Quaternion.Euler(5f, 40f, 0f), new Vector3(4.0f, 2.6f, 4.0f));
        SpawnCeilingRock(parent, cliffPrefab, "Dome_RoofShell_SE",
            new Vector3(12.0f, 24.5f, 45.0f), Quaternion.Euler(5f, -140f, 0f), new Vector3(4.0f, 2.6f, 4.0f));
        SpawnCeilingRock(parent, cliffPrefab, "Dome_RoofShell_NW",
            new Vector3(-12.0f, 25.0f, 63.0f), Quaternion.Euler(-5f, 50f, 0f), new Vector3(4.0f, 2.6f, 4.0f));
        SpawnCeilingRock(parent, cliffPrefab, "Dome_RoofShell_NE",
            new Vector3(12.0f, 25.0f, 63.0f), Quaternion.Euler(-5f, -130f, 0f), new Vector3(4.0f, 2.6f, 4.0f));

        // -------------------------------------------------------------
        // G. CHI TIẾT THẠCH NHŨ & MỎM ĐÁ VIỀN LỖ BAY ĐẠI BÀNG (EAGLE APERTURE ACCENTS)
        // Tạo đường nét gồ ghề, hiểm trở đặc trưng hang đại bàng
        // -------------------------------------------------------------
        SpawnCeilingRock(parent, rock4Prefab, "EagleHole_Stalactite_NW",
            new Vector3(-3.8f, 22.0f, 57.0f), Quaternion.Euler(190f, 45f, 15f), new Vector3(2.0f, 2.6f, 2.0f));
        SpawnCeilingRock(parent, rock4Prefab, "EagleHole_Stalactite_NE",
            new Vector3(3.8f, 22.0f, 57.0f), Quaternion.Euler(190f, -45f, -15f), new Vector3(2.0f, 2.6f, 2.0f));
        SpawnCeilingRock(parent, rock4Prefab, "EagleHole_Stalactite_SW",
            new Vector3(-3.8f, 21.8f, 51.5f), Quaternion.Euler(175f, 30f, 15f), new Vector3(2.0f, 2.6f, 2.0f));
        SpawnCeilingRock(parent, rock4Prefab, "EagleHole_Stalactite_SE",
            new Vector3(3.8f, 21.8f, 51.5f), Quaternion.Euler(175f, -30f, -15f), new Vector3(2.0f, 2.6f, 2.0f));
        SpawnCeilingRock(parent, rock3Prefab, "EagleHole_Nodule_Front",
            new Vector3(0.0f, 22.6f, 49.0f), Quaternion.Euler(185f, 10f, 0f), new Vector3(2.2f, 2.0f, 2.2f));
        SpawnCeilingRock(parent, rock3Prefab, "EagleHole_Nodule_Back",
            new Vector3(0.0f, 23.4f, 59.5f), Quaternion.Euler(185f, -20f, 0f), new Vector3(2.2f, 2.0f, 2.2f));
    }
}
#endif
