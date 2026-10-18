#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

[InitializeOnLoad]
public class BuildChapter6EagleCave
{
    private const string BUILD_SESSION_KEY = "PRU_EagleCaveBuilt_v1";
    private const string SCENE_PATH = "Assets/Scenes/Chapter6_HangDaiBang.unity";
    private const string ROCKS_DIR = "Assets/Free_Rocks";
    private const string PREFAB_DIR = ROCKS_DIR + "/_prefabs";
    private const string MODEL_DIR = ROCKS_DIR + "/_models";
    private const string MAT_DIR = ROCKS_DIR + "/_material";
    private const string TEX_DIR = ROCKS_DIR + "/_textures";

    private static Material urpRockMat;
    private static Material urpGroundMat;

    static BuildChapter6EagleCave()
    {
        EditorApplication.delayCall += () =>
        {
            if (!SessionState.GetBool(BUILD_SESSION_KEY, false))
            {
                SessionState.SetBool(BUILD_SESSION_KEY, true);
                ExecuteBuild(false);
            }
        };
    }

    [MenuItem("Tools/PRU/Xây dựng Hang Đại Bàng Chapter 6 (Free_Rocks)")]
    public static void BuildEagleCaveMenu()
    {
        ExecuteBuild(true);
    }

    [MenuItem("Tools/PRU/Mở và Test Scene Chapter 6 - Hang Đại Bàng")]
    public static void OpenAndTestScene()
    {
        ExecuteBuild(false);
        var activeScene = EditorSceneManager.GetActiveScene();
        if (activeScene.path != SCENE_PATH)
        {
            EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
        }
        EditorApplication.isPlaying = true;
    }

    public static void ExecuteBuild(bool showDialog)
    {
        Debug.Log("[BuildChapter6EagleCave] Bắt đầu xây dựng môi trường Hang Đại Bàng trong Scene Chapter6_HangDaiBang...");

        // 1. Chuẩn bị Materials URP
        EnsureUrpMaterials();

        // 2. Mở Scene Chapter 6
        string currentScene = EditorSceneManager.GetActiveScene().path;
        if (currentScene != SCENE_PATH)
        {
            EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
        }

        // 3. Tải các asset đá từ Free_Rocks
        GameObject cliffPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_DIR + "/cliff.prefab");
        GameObject rock1Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_DIR + "/rock1.prefab");
        GameObject rock2Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_DIR + "/rock2.prefab");
        GameObject rock3Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_DIR + "/rock3.prefab");
        GameObject rock4Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_DIR + "/rock4.prefab");
        GameObject groundModel = AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_DIR + "/ground.fbx");

        if (cliffPrefab == null || rock1Prefab == null)
        {
            Debug.LogError("[BuildChapter6EagleCave] Không tìm thấy đầy đủ prefabs trong Assets/Free_Rocks/_prefabs!");
            return;
        }

        // 4. Tìm hoặc tạo GameObject cha: Chapter6_EagleCaveEnvironment
        GameObject rootEnv = GameObject.Find("Chapter6_EagleCaveEnvironment");
        if (rootEnv != null)
        {
            Undo.DestroyObjectImmediate(rootEnv);
        }
        rootEnv = new GameObject("Chapter6_EagleCaveEnvironment");
        Undo.RegisterCreatedObjectUndo(rootEnv, "Create Chapter6_EagleCaveEnvironment");
        rootEnv.transform.position = Vector3.zero;
        rootEnv.transform.rotation = Quaternion.identity;

        // 5. Tạo 7 nhóm con chuẩn theo yêu cầu:
        Transform tEntrance = CreateChild(rootEnv.transform, "Rock_Entrance");
        Transform tArch = CreateChild(rootEnv.transform, "Rock_Arch");
        Transform tLeftCliffs = CreateChild(rootEnv.transform, "Rock_LeftCliffs");
        Transform tRightCliffs = CreateChild(rootEnv.transform, "Rock_RightCliffs");
        Transform tPillars = CreateChild(rootEnv.transform, "Rock_Pillars");
        Transform tGround = CreateChild(rootEnv.transform, "Rock_Ground");
        Transform tDecorations = CreateChild(rootEnv.transform, "Rock_Decorations");

        // Nhóm điểm đánh dấu và gameplay
        Transform tGameplay = CreateChild(rootEnv.transform, "Gameplay_Markers");
        Transform tLights = CreateChild(rootEnv.transform, "Cave_Lights");
        Transform tBoundaries = CreateChild(rootEnv.transform, "Cave_BoundaryColliders");

        // 6. XÂY DỰNG NỀN ĐÁ (Rock_Ground)
        BuildRockGround(tGround, groundModel, cliffPrefab);

        // 7. XÂY DỰNG VÒM ĐÁ KHỔNG LỒ (Rock_Arch) - Mô phỏng ảnh mẫu
        BuildGiantRockArch(tArch, cliffPrefab, rock1Prefab, rock2Prefab, rock4Prefab);

        // 8. XÂY DỰNG CỬA HANG (Rock_Entrance)
        BuildEntranceArea(tEntrance, cliffPrefab, rock1Prefab, rock2Prefab);

        // 9. XÂY DỰNG VÁCH ĐÁ TRÁI VÀ PHẢI (Rock_LeftCliffs, Rock_RightCliffs)
        BuildFlankCliffs(tLeftCliffs, tRightCliffs, cliffPrefab, rock1Prefab, rock2Prefab);

        // 10. XÂY DỰNG CỘT ĐÁ CAO DỰNG ĐỨNG (Rock_Pillars)
        BuildTallPillars(tPillars, cliffPrefab, rock1Prefab, rock2Prefab);

        // 11. XÂY DỰNG ĐẤU TRƯỜNG ĐẠI BÀNG TINH (Khu vực C)
        BuildBossArenaCavern(tLeftCliffs, tRightCliffs, tDecorations, cliffPrefab, rock1Prefab, rock2Prefab);

        // 12. BỐ TRÍ ĐÁ TRANG TRÍ (Rock_Decorations)
        BuildRockDecorations(tDecorations, rock1Prefab, rock2Prefab, rock3Prefab, rock4Prefab);

        // 13. TẠO CÁC ĐIỂM ĐÁNH DẤU GAMEPLAY (Khu A, B, C)
        BuildGameplayMarkers(tGameplay, rock1Prefab);

        // 14. BỐ TRÍ ÁNH SÁNG & ĐUỐC HANG ĐỘNG
        BuildLightingAndTorches(tLights);

        // 15. TẠO BOUNDARY COLLIDERS (Chặn mép vực)
        BuildBoundaryColliders(tBoundaries);

        // 16. THIẾT LẬP THẠCH SANH VÀ CAMERA
        SetupPlayerAndCamera();

        // 17. Áp dụng Material URP Lit cho tất cả các MeshRenderer trong môi trường
        ApplyUrpMaterialsToEnvironment(rootEnv);

        // 18. Lưu Scene
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();

        Debug.Log("[BuildChapter6EagleCave] XÂY DỰNG HANG ĐẠI BÀNG CHAPTER 6 HOÀN TẤT THÀNH CÔNG!");

        if (showDialog)
        {
            EditorUtility.DisplayDialog("Xây dựng thành công",
                "Đã xây dựng hoàn tất Hang Đại Bàng Chapter 6 theo đúng kiến trúc ảnh mẫu:\n\n" +
                "1. Vòm đá khổng lồ (Rock_Arch) cao 28m đóng khung bầu trời.\n" +
                "2. Hai vách đá đồ sộ (Rock_LeftCliffs, Rock_RightCliffs) cao 25-35m.\n" +
                "3. Các cột đá cao vút (Rock_Pillars) sừng sững phía sau.\n" +
                "4. Nền đá vững chắc (Rock_Ground) có MeshCollider đầy đủ.\n" +
                "5. Khu A (Cửa hang + SpawnPoint Thạch Sanh).\n" +
                "6. Khu B (Đường hầm đá + Ánh sáng ấm).\n" +
                "7. Khu C (Hang Boss Đại Bàng Tinh + Điểm cứu Công Chúa).\n" +
                "8. Material URP Lit tương thích 100% Unity 6.",
                "OK");
        }
    }

    private static Transform CreateChild(Transform parent, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    /// <summary>
    /// Đảm bảo Material URP Lit chuẩn chất lượng cao cho đá và nền
    /// </summary>
    private static void EnsureUrpMaterials()
    {
        string rockMatPath = MAT_DIR + "/rocks_urp.mat";
        string groundMatPath = MAT_DIR + "/ground_urp.mat";

        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) urpLit = Shader.Find("URP/Lit");
        if (urpLit == null) urpLit = Shader.Find("Standard");

        // 1. Material Đá (rocks_urp)
        urpRockMat = AssetDatabase.LoadAssetAtPath<Material>(rockMatPath);
        if (urpRockMat == null)
        {
            urpRockMat = new Material(urpLit);
            urpRockMat.name = "rocks_urp";
            AssetDatabase.CreateAsset(urpRockMat, rockMatPath);
        }

        Texture2D rockAlbedo = AssetDatabase.LoadAssetAtPath<Texture2D>(TEX_DIR + "/rocks_diff_spec.PNG");
        Texture2D rockNormal = AssetDatabase.LoadAssetAtPath<Texture2D>(TEX_DIR + "/rocks_nm.PNG");

        FixTextureType(rockNormal, TextureImporterType.NormalMap);

        if (rockAlbedo != null)
        {
            urpRockMat.SetTexture("_BaseMap", rockAlbedo);
            urpRockMat.SetTexture("_MainTex", rockAlbedo);
        }
        if (rockNormal != null)
        {
            urpRockMat.SetTexture("_BumpMap", rockNormal);
            urpRockMat.EnableKeyword("_NORMALMAP");
        }
        urpRockMat.SetFloat("_Smoothness", 0.25f);
        urpRockMat.SetFloat("_Metallic", 0.05f);
        EditorUtility.SetDirty(urpRockMat);

        // 2. Material Nền (ground_urp)
        urpGroundMat = AssetDatabase.LoadAssetAtPath<Material>(groundMatPath);
        if (urpGroundMat == null)
        {
            urpGroundMat = new Material(urpLit);
            urpGroundMat.name = "ground_urp";
            AssetDatabase.CreateAsset(urpGroundMat, groundMatPath);
        }

        Texture2D groundAlbedo = AssetDatabase.LoadAssetAtPath<Texture2D>(TEX_DIR + "/ground_diff_spec.PNG");
        Texture2D groundNormal = AssetDatabase.LoadAssetAtPath<Texture2D>(TEX_DIR + "/ground_nm.PNG");

        FixTextureType(groundNormal, TextureImporterType.NormalMap);

        if (groundAlbedo != null)
        {
            urpGroundMat.SetTexture("_BaseMap", groundAlbedo);
            urpGroundMat.SetTexture("_MainTex", groundAlbedo);
        }
        if (groundNormal != null)
        {
            urpGroundMat.SetTexture("_BumpMap", groundNormal);
            urpGroundMat.EnableKeyword("_NORMALMAP");
        }
        urpGroundMat.SetFloat("_Smoothness", 0.18f);
        urpGroundMat.SetFloat("_Metallic", 0.0f);
        EditorUtility.SetDirty(urpGroundMat);
    }

    private static void FixTextureType(Texture2D tex, TextureImporterType type)
    {
        if (tex == null) return;
        string path = AssetDatabase.GetAssetPath(tex);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null && importer.textureType != type)
        {
            importer.textureType = type;
            importer.SaveAndReimport();
        }
    }

    /// <summary>
    /// Xây dựng nền đá liên tục vững chắc, không bao giờ rơi xuyên map
    /// </summary>
    private static void BuildRockGround(Transform parent, GameObject groundModel, GameObject cliffPrefab)
    {
        // 1. Tấm nền chính khu vực Cửa hang & Hẻm núi (Z từ -50 đến +25)
        for (float z = -46f; z <= 26f; z += 14f)
        {
            for (float x = -14f; x <= 14f; x += 14f)
            {
                CreateGroundPlate(parent, $"Ground_EntranceTunnel_{x:F0}_{z:F0}", new Vector3(x, 0f, z), new Vector3(15f, 1.2f, 15f));
            }
        }

        // 2. Tấm nền chính khu vực Đấu trường Đại Bàng Tinh (Z từ +25 đến +80, bán kính rộng)
        for (float z = 30f; z <= 78f; z += 14f)
        {
            for (float x = -21f; x <= 21f; x += 14f)
            {
                CreateGroundPlate(parent, $"Ground_Arena_{x:F0}_{z:F0}", new Vector3(x, 0.2f, z), new Vector3(15.5f, 1.2f, 15.5f));
            }
        }

        // 3. Các mảng đá sàn gồ ghề tự nhiên từ Free_Rocks (groundModel nếu có)
        if (groundModel != null)
        {
            for (int i = 0; i < 6; i++)
            {
                float gz = -30f + i * 18f;
                GameObject g = Object.Instantiate(groundModel, parent);
                g.name = $"NaturalGroundMesh_{i + 1}";
                g.transform.position = new Vector3((i % 2 == 0 ? -2.5f : 2.5f), 0.05f, gz);
                g.transform.rotation = Quaternion.Euler(0, (i * 67) % 360, 0);
                g.transform.localScale = new Vector3(1.4f, 0.8f, 1.4f);

                MeshCollider mc = g.GetComponent<MeshCollider>();
                if (mc == null) mc = g.AddComponent<MeshCollider>();
            }
        }
    }

    private static void CreateGroundPlate(Transform parent, string name, Vector3 pos, Vector3 size)
    {
        GameObject plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        plate.name = name;
        plate.transform.SetParent(parent, false);
        plate.transform.position = pos - new Vector3(0, size.y * 0.5f - 0.02f, 0);
        plate.transform.localScale = size;

        if (urpGroundMat != null)
        {
            plate.GetComponent<Renderer>().sharedMaterial = urpGroundMat;
        }

        BoxCollider col = plate.GetComponent<BoxCollider>();
        if (col != null)
        {
            col.size = Vector3.one;
        }
    }

    /// <summary>
    /// Xây dựng Vòm đá tự nhiên khổng lồ (Rock_Arch) phỏng theo ảnh mẫu
    /// </summary>
    private static void BuildGiantRockArch(Transform parent, GameObject cliffPrefab, GameObject rock1Prefab, GameObject rock2Prefab, GameObject rock4Prefab)
    {
        // Vòm đá nằm ở Z = -20m, vươn cao 28m, nhịp vòm rộng từ X = -8m đến +8m
        float archZ = -19.0f;

        // --- CỘT TRỤ CHÂN VÒM BÊN TRÁI ---
        SpawnRock(parent, cliffPrefab, "Arch_PillarLeft_Base",
            new Vector3(-13.0f, 0f, archZ), Quaternion.Euler(-5f, 35f, 0f), new Vector3(2.8f, 3.2f, 2.8f));
        SpawnRock(parent, cliffPrefab, "Arch_PillarLeft_Mid",
            new Vector3(-11.5f, 9.5f, archZ + 0.5f), Quaternion.Euler(0f, 20f, -8f), new Vector3(2.5f, 3.0f, 2.5f));
        SpawnRock(parent, cliffPrefab, "Arch_PillarLeft_High",
            new Vector3(-9.5f, 17.5f, archZ + 0.2f), Quaternion.Euler(0f, 15f, -22f), new Vector3(2.3f, 2.8f, 2.4f));

        // --- CỘT TRỤ CHÂN VÒM BÊN PHẢI ---
        SpawnRock(parent, cliffPrefab, "Arch_PillarRight_Base",
            new Vector3(13.0f, 0f, archZ), Quaternion.Euler(-5f, -145f, 0f), new Vector3(2.8f, 3.2f, 2.8f));
        SpawnRock(parent, cliffPrefab, "Arch_PillarRight_Mid",
            new Vector3(11.5f, 9.5f, archZ + 0.5f), Quaternion.Euler(0f, -160f, 8f), new Vector3(2.5f, 3.0f, 2.5f));
        SpawnRock(parent, cliffPrefab, "Arch_PillarRight_High",
            new Vector3(9.5f, 17.5f, archZ + 0.2f), Quaternion.Euler(0f, -165f, 22f), new Vector3(2.3f, 2.8f, 2.4f));

        // --- NHỊP VÒM CONG BẮC NGANG (ARCH SPAN) ---
        // Cánh cong trái uốn vào giữa
        SpawnRock(parent, cliffPrefab, "Arch_Span_LeftArm",
            new Vector3(-5.5f, 23.5f, archZ - 0.2f), Quaternion.Euler(10f, 10f, -38f), new Vector3(2.4f, 2.4f, 2.4f));
        // Cánh cong phải uốn vào giữa
        SpawnRock(parent, cliffPrefab, "Arch_Span_RightArm",
            new Vector3(5.5f, 23.5f, archZ - 0.2f), Quaternion.Euler(10f, -170f, 38f), new Vector3(2.4f, 2.4f, 2.4f));

        // Đỉnh chóp vòm (Keystone / Apex) nằm ngang vắt qua hai bên
        SpawnRock(parent, cliffPrefab, "Arch_Span_Keystone",
            new Vector3(0f, 26.8f, archZ - 0.5f), Quaternion.Euler(180f, 0f, 0f), new Vector3(3.2f, 2.2f, 2.6f));

        // Khối đá nóc đỉnh vươn cao ngất trời như đỉnh núi trong ảnh mẫu
        SpawnRock(parent, cliffPrefab, "Arch_Summit_TopCrown",
            new Vector3(0f, 32.0f, archZ + 1.5f), Quaternion.Euler(0f, 80f, 0f), new Vector3(3.5f, 3.0f, 3.2f));

        // Các mảng đá phụ lèn kín khe hở tự nhiên quanh vòm
        SpawnRock(parent, rock1Prefab, "Arch_Detail_LeftShoulder",
            new Vector3(-7.5f, 21.0f, archZ - 1.2f), Quaternion.Euler(25f, 45f, -15f), new Vector3(2.2f, 2.5f, 2.2f));
        SpawnRock(parent, rock1Prefab, "Arch_Detail_RightShoulder",
            new Vector3(7.5f, 21.0f, archZ - 1.2f), Quaternion.Euler(25f, -135f, 15f), new Vector3(2.2f, 2.5f, 2.2f));
        SpawnRock(parent, rock2Prefab, "Arch_Detail_CenterSupport",
            new Vector3(0f, 24.5f, archZ + 1.2f), Quaternion.Euler(-15f, 90f, 0f), new Vector3(2.8f, 2.0f, 2.2f));
    }

    /// <summary>
    /// Xây dựng khu vực Cửa hang (Rock_Entrance)
    /// </summary>
    private static void BuildEntranceArea(Transform parent, GameObject cliffPrefab, GameObject rock1Prefab, GameObject rock2Prefab)
    {
        // Cửa đón phía ngoài (Z = -45 đến -30)
        // Vách đá tiền sảnh bên trái
        SpawnRock(parent, cliffPrefab, "Entrance_OuterCliff_L1",
            new Vector3(-19.0f, 0f, -38.0f), Quaternion.Euler(0f, 45f, 0f), new Vector3(2.5f, 3.2f, 2.5f));
        SpawnRock(parent, cliffPrefab, "Entrance_OuterCliff_L2",
            new Vector3(-16.0f, 0f, -28.0f), Quaternion.Euler(0f, 30f, 0f), new Vector3(2.6f, 3.0f, 2.6f));

        // Vách đá tiền sảnh bên phải
        SpawnRock(parent, cliffPrefab, "Entrance_OuterCliff_R1",
            new Vector3(19.0f, 0f, -38.0f), Quaternion.Euler(0f, -135f, 0f), new Vector3(2.5f, 3.2f, 2.5f));
        SpawnRock(parent, cliffPrefab, "Entrance_OuterCliff_R2",
            new Vector3(16.0f, 0f, -28.0f), Quaternion.Euler(0f, -150f, 0f), new Vector3(2.6f, 3.0f, 2.6f));

        // Mỏm đá gồ ghề tự nhiên ở hai rìa lối vào
        SpawnRock(parent, rock1Prefab, "Entrance_FrontRock_L",
            new Vector3(-8.5f, 0f, -42.0f), Quaternion.Euler(0f, 20f, 0f), new Vector3(2.2f, 2.0f, 2.2f));
        SpawnRock(parent, rock1Prefab, "Entrance_FrontRock_R",
            new Vector3(8.5f, 0f, -42.0f), Quaternion.Euler(0f, 160f, 0f), new Vector3(2.2f, 2.0f, 2.2f));
    }

    /// <summary>
    /// Xây dựng vách đá sừng sững hai bên đường hầm (Khu vực B)
    /// </summary>
    private static void BuildFlankCliffs(Transform leftParent, Transform rightParent, GameObject cliffPrefab, GameObject rock1Prefab, GameObject rock2Prefab)
    {
        // Chiều dài đường hầm từ Z = -15 đến Z = +30
        float[] zSteps = new float[] { -14f, -4f, 6f, 16f, 26f };

        // --- VÁCH TRÁI (Rock_LeftCliffs) ---
        for (int i = 0; i < zSteps.Length; i++)
        {
            float z = zSteps[i];
            float rotY = (i * 35f) % 90f;
            SpawnRock(leftParent, cliffPrefab, $"LeftCliff_Wall_{i + 1}",
                new Vector3(-13.5f, 0f, z), Quaternion.Euler(0f, rotY, 0f), new Vector3(2.5f, 3.2f, 2.6f));

            // Vách tầng cao chồng lên tạo độ hùng vĩ
            SpawnRock(leftParent, cliffPrefab, $"LeftCliff_Upper_{i + 1}",
                new Vector3(-16.0f, 12f, z + 1f), Quaternion.Euler(5f, rotY + 15f, -5f), new Vector3(2.8f, 3.0f, 2.8f));
        }

        // --- VÁCH PHẢI (Rock_RightCliffs) ---
        for (int i = 0; i < zSteps.Length; i++)
        {
            float z = zSteps[i];
            float rotY = 180f + (i * 35f) % 90f;
            SpawnRock(rightParent, cliffPrefab, $"RightCliff_Wall_{i + 1}",
                new Vector3(13.5f, 0f, z), Quaternion.Euler(0f, rotY, 0f), new Vector3(2.5f, 3.2f, 2.6f));

            // Vách tầng cao chồng lên
            SpawnRock(rightParent, cliffPrefab, $"RightCliff_Upper_{i + 1}",
                new Vector3(16.0f, 12f, z + 1f), Quaternion.Euler(5f, rotY - 15f, 5f), new Vector3(2.8f, 3.0f, 2.8f));
        }
    }

    /// <summary>
    /// Xây dựng các cột đá cao dựng đứng sừng sững (Rock_Pillars)
    /// </summary>
    private static void BuildTallPillars(Transform parent, GameObject cliffPrefab, GameObject rock1Prefab, GameObject rock2Prefab)
    {
        // Các cột đá cao vươn thẳng lên trời giống ảnh mẫu (scale Y từ 3.8 đến 5.2)
        Vector3[] pillarPositions = new Vector3[]
        {
            new Vector3(-20f, 0f, -16f), // Cột cao sau vòm trái
            new Vector3(20f, 0f, -16f),  // Cột cao sau vòm phải
            new Vector3(-18f, 0f, 10f),   // Cột giữa hầm trái
            new Vector3(18f, 0f, 10f),    // Cột giữa hầm phải
            new Vector3(-25f, 0f, 45f),   // Cột rìa đấu trường trái
            new Vector3(25f, 0f, 45f),    // Cột rìa đấu trường phải
            new Vector3(0f, 0f, 85f),     // Cột khổng lồ phía sau lưng Boss
            new Vector3(-15f, 0f, 80f),   // Cột góc sau trái
            new Vector3(15f, 0f, 80f)     // Cột góc sau phải
        };

        for (int i = 0; i < pillarPositions.Length; i++)
        {
            Vector3 pos = pillarPositions[i];
            float heightScale = 4.0f + (i % 3) * 0.6f;
            SpawnRock(parent, cliffPrefab, $"GiantPillar_{i + 1}",
                pos, Quaternion.Euler(0f, (i * 53) % 360, 0f), new Vector3(2.2f, heightScale, 2.2f));

            // Đặt thêm tảng đá chân cột củng cố nền vững chắc
            SpawnRock(parent, rock1Prefab, $"PillarBase_{i + 1}",
                pos + new Vector3(1f, 0f, 0f), Quaternion.Euler(0f, (i * 37) % 360, 0f), new Vector3(2.5f, 2.0f, 2.5f));
        }
    }

    /// <summary>
    /// Xây dựng Đấu trường Đại Bàng Tinh (Khu vực C - Z từ +30 đến +80)
    /// </summary>
    private static void BuildBossArenaCavern(Transform leftParent, Transform rightParent, Transform decorParent, GameObject cliffPrefab, GameObject rock1Prefab, GameObject rock2Prefab)
    {
        // Đấu trường hình bán nguyệt rộng mở, khép lại ở phía sau (Z = 75 đến 85)
        // Vòng vách đá cung tròn bao bọc đấu trường
        int arcSegments = 9;
        float radius = 26f;
        Vector3 arenaCenter = new Vector3(0f, 0f, 55f);

        for (int i = 0; i < arcSegments; i++)
        {
            float angleDeg = -80f + i * (160f / (arcSegments - 1)); // từ -80 độ đến +80 độ
            float angleRad = angleDeg * Mathf.Deg2Rad;

            float x = arenaCenter.x + Mathf.Sin(angleRad) * radius;
            float z = arenaCenter.z + Mathf.Cos(angleRad) * radius;

            Vector3 pos = new Vector3(x, 0f, z);
            Vector3 lookAtCenter = (arenaCenter - pos).normalized;
            Quaternion rot = Quaternion.LookRotation(lookAtCenter);

            Transform p = (x < 0) ? leftParent : rightParent;
            SpawnRock(p, cliffPrefab, $"ArenaWallArc_{i + 1}",
                pos, rot, new Vector3(2.6f, 3.4f, 2.6f));

            // Tầng cao của vách đấu trường
            SpawnRock(p, cliffPrefab, $"ArenaWallArc_High_{i + 1}",
                pos + new Vector3(0, 10f, 0), rot * Quaternion.Euler(5f, 10f, 0f), new Vector3(2.8f, 3.0f, 2.8f));
        }
    }

    /// <summary>
    /// Rải các tảng đá trang trí tự nhiên (Rock_Decorations)
    /// </summary>
    private static void BuildRockDecorations(Transform parent, GameObject rock1Prefab, GameObject rock2Prefab, GameObject rock3Prefab, GameObject rock4Prefab)
    {
        // Các vị trí ven rìa tường, hoàn toàn không chặn lối đi
        Vector3[] decorSpots = new Vector3[]
        {
            new Vector3(-6.8f, 0f, -32f),
            new Vector3(6.8f, 0f, -30f),
            new Vector3(-6.2f, 0f, -10f),
            new Vector3(6.5f, 0f, -8f),
            new Vector3(-7.0f, 0f, 12f),
            new Vector3(7.2f, 0f, 14f),
            new Vector3(-14f, 0f, 40f),
            new Vector3(14f, 0f, 42f),
            new Vector3(-16f, 0f, 65f),
            new Vector3(0f, 0f, 72f) // Sau lưng bệ Boss
        };

        GameObject[] prefabs = new GameObject[] { rock2Prefab, rock3Prefab, rock4Prefab, rock1Prefab };

        for (int i = 0; i < decorSpots.Length; i++)
        {
            GameObject p = prefabs[i % prefabs.Length];
            if (p != null)
            {
                SpawnRock(parent, p, $"RockDecor_{i + 1}",
                    decorSpots[i], Quaternion.Euler((i * 17) % 360, (i * 47) % 360, (i * 23) % 360),
                    new Vector3(1.4f, 1.3f, 1.4f));
            }
        }
    }

    /// <summary>
    /// Tạo các điểm đánh dấu Gameplay chính xác theo yêu cầu
    /// </summary>
    private static void BuildGameplayMarkers(Transform parent, GameObject rock1Prefab)
    {
        // 1. Điểm xuất hiện Thạch Sanh (Khu vực A)
        GameObject spawnPt = GameObject.Find("ThachSanh_SpawnPoint");
        if (spawnPt == null) spawnPt = new GameObject("ThachSanh_SpawnPoint");
        spawnPt.transform.SetParent(parent, false);
        spawnPt.transform.position = new Vector3(0f, 0.25f, -40f);
        spawnPt.transform.rotation = Quaternion.Euler(0f, 0f, 0f); // Hướng vào hang

        // 2. Vị trí Đại Bàng Tinh (Khu vực C)
        GameObject bossPt = GameObject.Find("DaiBangTinh_BossPoint");
        if (bossPt == null) bossPt = new GameObject("DaiBangTinh_BossPoint");
        bossPt.transform.SetParent(parent, false);
        bossPt.transform.position = new Vector3(0f, 0.5f, 55f);
        bossPt.transform.rotation = Quaternion.Euler(0f, 180f, 0f); // Hướng ra cửa đón Thạch Sanh

        // Bệ đá tự nhiên nâng đỡ Boss
        if (rock1Prefab != null)
        {
            GameObject bossRockAltar = Object.Instantiate(rock1Prefab, parent);
            bossRockAltar.name = "Boss_PerchRockPlatform";
            bossRockAltar.transform.position = new Vector3(0f, -0.4f, 55f);
            bossRockAltar.transform.rotation = Quaternion.Euler(0f, 45f, 0f);
            bossRockAltar.transform.localScale = new Vector3(3.5f, 1.0f, 3.5f);
        }

        // 3. Vị trí Công Chúa (Khu vực C)
        GameObject princessPt = GameObject.Find("Princess_RescuePoint");
        if (princessPt == null) princessPt = new GameObject("Princess_RescuePoint");
        princessPt.transform.SetParent(parent, false);
        princessPt.transform.position = new Vector3(14.0f, 1.8f, 62f);
        princessPt.transform.rotation = Quaternion.Euler(0f, -120f, 0f);

        // Bệ đá tự nhiên nâng đỡ Công Chúa ở góc cao an toàn
        if (rock1Prefab != null)
        {
            GameObject princessAltar = Object.Instantiate(rock1Prefab, parent);
            princessAltar.name = "Princess_SanctuaryPlatform";
            princessAltar.transform.position = new Vector3(14.0f, 0.4f, 62f);
            princessAltar.transform.rotation = Quaternion.Euler(0f, 10f, 0f);
            princessAltar.transform.localScale = new Vector3(2.4f, 1.4f, 2.4f);
        }
    }

    /// <summary>
    /// Bố trí hệ thống ánh sáng tự nhiên và đuốc hang động
    /// </summary>
    private static void BuildLightingAndTorches(Transform parent)
    {
        // 1. Ánh sáng Mặt Trời (Directional Light chiếu xiên qua Vòm đá hùng vĩ)
        Light sunLight = null;
        Light[] lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var l in lights)
        {
            if (l.type == LightType.Directional) { sunLight = l; break; }
        }

        if (sunLight == null)
        {
            GameObject sunGo = new GameObject("Directional Light");
            sunLight = sunGo.AddComponent<Light>();
            sunLight.type = LightType.Directional;
        }

        sunLight.name = "Sun_NaturalEntranceLight";
        sunLight.transform.rotation = Quaternion.Euler(22f, 35f, 0f); // Chiếu xiên qua vòm đá từ sau ra trước
        sunLight.color = new Color(1.0f, 0.94f, 0.82f); // Ánh nắng vàng ấm
        sunLight.intensity = 1.25f;
        sunLight.shadows = LightShadows.Soft;

        // 2. Các điểm sáng ấm nhẹ (Point Lights) dọc lối đi đường hầm (Khu B)
        Vector3[] torchPositions = new Vector3[]
        {
            new Vector3(-5.5f, 2.5f, -6f),
            new Vector3(5.5f, 2.5f, 4f),
            new Vector3(-5.5f, 2.5f, 16f),
            new Vector3(5.5f, 2.5f, 26f),
            // Đấu trường (Khu C)
            new Vector3(0f, 8.0f, 52f), // Đèn trần trung tâm đấu trường
            new Vector3(14.0f, 3.2f, 62f) // Đèn dịu khu vực công chúa
        };

        for (int i = 0; i < torchPositions.Length; i++)
        {
            GameObject ptLightGo = new GameObject($"CaveLight_{i + 1}");
            ptLightGo.transform.SetParent(parent, false);
            ptLightGo.transform.position = torchPositions[i];

            Light pl = ptLightGo.AddComponent<Light>();
            pl.type = LightType.Point;

            if (i == 4) // Đèn trần đấu trường
            {
                pl.color = new Color(1.0f, 0.88f, 0.70f);
                pl.range = 35f;
                pl.intensity = 2.2f;
                pl.shadows = LightShadows.Soft;
            }
            else if (i == 5) // Đèn khu công chúa
            {
                pl.color = new Color(0.9f, 0.95f, 1.0f); // Sáng thanh khiết
                pl.range = 10f;
                pl.intensity = 1.5f;
                pl.shadows = LightShadows.None;
            }
            else // Đuốc dọc đường hầm
            {
                pl.color = new Color(1.0f, 0.65f, 0.25f); // Vàng cam ấm
                pl.range = 14f;
                pl.intensity = 1.8f;
                pl.shadows = LightShadows.Soft;
            }
        }
    }

    /// <summary>
    /// Tạo các BoxCollider vô hình bao quanh mép hang để Thạch Sanh không bao giờ rơi ra ngoài map
    /// </summary>
    private static void BuildBoundaryColliders(Transform parent)
    {
        // Tường chặn phía sau lối vào (Z = -46)
        CreateInvisibleWall(parent, "Wall_BackEntrance", new Vector3(0, 5, -46), new Vector3(32, 12, 2));

        // Tường chặn sau lưng Boss (Z = +82)
        CreateInvisibleWall(parent, "Wall_BackArena", new Vector3(0, 10, 82), new Vector3(45, 20, 2));

        // Tường chặn rìa trái đấu trường (X = -27)
        CreateInvisibleWall(parent, "Wall_LeftArena", new Vector3(-27, 10, 55), new Vector3(2, 20, 55));

        // Tường chặn rìa phải đấu trường (X = +27)
        CreateInvisibleWall(parent, "Wall_RightArena", new Vector3(27, 10, 55), new Vector3(2, 20, 55));
    }

    private static void CreateInvisibleWall(Transform parent, string name, Vector3 pos, Vector3 size)
    {
        GameObject wall = new GameObject(name);
        wall.transform.SetParent(parent, false);
        wall.transform.position = pos;
        BoxCollider box = wall.AddComponent<BoxCollider>();
        box.size = size;
    }

    /// <summary>
    /// Thiết lập Thạch Sanh và CameraFollow
    /// </summary>
    private static void SetupPlayerAndCamera()
    {
        // Tìm hoặc cấu hình Thạch Sanh
        GameObject player = GameObject.FindWithTag("Player") ?? GameObject.Find("thachsanh") ?? GameObject.Find("ThachSanh");
        if (player == null)
        {
            // Tìm prefab Thạch Sanh
            string tsFbxPath = "Assets/nhanvat/thachsanh/hanhdong/dungyen/Meshy_AI_Axe_Bearing_Wanderer_biped_Animation_01a0d469-8153-7436-8709-d393703fc126_withSkin.fbx";
            GameObject tsFbx = AssetDatabase.LoadAssetAtPath<GameObject>(tsFbxPath);
            if (tsFbx != null)
            {
                player = Object.Instantiate(tsFbx);
                player.name = "thachsanh";
                player.tag = "Player";
            }
        }

        if (player != null)
        {
            player.tag = "Player";
            player.transform.position = new Vector3(0f, 0.25f, -40f);
            player.transform.rotation = Quaternion.Euler(0f, 0f, 0f);

            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc == null) cc = player.AddComponent<CharacterController>();
            cc.center = new Vector3(0, 0.9f, 0);
            cc.radius = 0.35f;
            cc.height = 1.8f;

            PlayerMovement pm = player.GetComponent<PlayerMovement>();
            if (pm == null) player.AddComponent<PlayerMovement>();

            Debug.Log($"[BuildChapter6EagleCave] Đã thiết lập Thạch Sanh tại vị trí cửa hang: {player.transform.position}");
        }

        // Thiết lập Main Camera
        Camera cam = Camera.main ?? Object.FindAnyObjectByType<Camera>();
        if (cam != null)
        {
            cam.name = "Main Camera";
            cam.tag = "MainCamera";
            cam.transform.position = new Vector3(0f, 2.5f, -44.5f);
            cam.transform.rotation = Quaternion.Euler(12f, 0f, 0f); // Hơi ngước nhìn lên vòm đá khổng lồ

            CameraFollow cf = cam.GetComponent<CameraFollow>();
            if (cf == null) cf = cam.gameObject.AddComponent<CameraFollow>();
            if (player != null)
            {
                cf.target = player.transform;
            }
            cf.distance = 4.5f;
            cf.heightOffset = 1.6f;

            Debug.Log("[BuildChapter6EagleCave] Đã cấu hình CameraFollow bám theo Thạch Sanh với góc nhìn hướng về Vòm đá.");
        }
    }

    /// <summary>
    /// Tạo instance đá với MeshCollider và gắn vào Transform cha
    /// </summary>
    private static GameObject SpawnRock(Transform parent, GameObject prefab, string name, Vector3 pos, Quaternion rot, Vector3 scale)
    {
        if (prefab == null) return null;

        GameObject instance = Object.Instantiate(prefab, parent);
        instance.name = name;
        instance.transform.position = pos;
        instance.transform.rotation = rot;
        instance.transform.localScale = scale;

        // Đảm bảo có Collider
        Collider col = instance.GetComponent<Collider>();
        if (col == null)
        {
            MeshFilter mf = instance.GetComponentInChildren<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                MeshCollider mc = instance.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
            }
        }

        return instance;
    }

    /// <summary>
    /// Gán Material URP Lit cho tất cả các MeshRenderer trong môi trường để không bị lỗi shader
    /// </summary>
    private static void ApplyUrpMaterialsToEnvironment(GameObject root)
    {
        if (urpRockMat == null && urpGroundMat == null) return;

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            string rName = r.gameObject.name.ToLower();
            string pName = r.transform.parent != null ? r.transform.parent.name.ToLower() : "";

            if (rName.Contains("ground") || pName.Contains("ground"))
            {
                if (urpGroundMat != null) r.sharedMaterial = urpGroundMat;
            }
            else
            {
                if (urpRockMat != null) r.sharedMaterial = urpRockMat;
            }
        }
    }
}
#endif
