#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

/// <summary>
/// Tool Editor: Thiết lập hoàn chỉnh Scene Chapter 4 (Chapter4_Lythongcuopcong):
/// 1. Tạo và cấu hình Unity Terrain bao quanh cung điện Seosipjagak Complex.
/// 2. Bổ sung đường dẫn đá, hàng cây, cỏ hoa và đuốc dẫn từ ngoài vào cổng cung điện.
/// 3. Định vị nhân vật Lý Thông tại điểm xuất phát ngoài cung điện trên mặt đất.
/// 4. Cấu hình LyThongPlayerController (WASD, xoay mượt, Animator Speed).
/// 5. Loại bỏ NavMeshAgent trên Lý Thông để loại bỏ cảnh báo "Failed to create agent because there is no valid NavMesh".
/// 6. Gắn NavMeshSurface lên Terrain/Môi trường và nướng NavMesh cho AI/NPC.
/// 7. Cấu hình CameraFollow bám sát Lý Thông và nhìn hướng vào cổng cung điện.
/// 8. Đặt vị trí tượng trưng cho Vua / Triều đình bên trong sảnh chính để Lý Thông dâng nộp đầu Chằn Tinh.
/// </summary>
public class Chapter4SceneSetup : MonoBehaviour
{
    private const string SCENE_PATH = "Assets/Scenes/Chapter4_Lythongcuopcong.unity";
    private const string MODEL_PATH = "Assets/nhanvat/lythong/dung/Meshy_AI_The_Wayfarer_Sage_biped_Animation_01a0d469-8153-7436-8709-d393703fc126_withSkin.fbx";
    private const string ANIM_CONTROLLER_PATH = "Assets/nhanvat/lythong/LyThongAnim.controller";
    private const string TERRAIN_DATA_GUID = "b313b495b8f6108489709cc7c446562b"; // TerrainData sẵn có trong Project

    [MenuItem("Tools/PRU/Thiết lập hoàn chỉnh Chapter 4 (Lý Thông & Terrain)")]
    public static void SetupChapter4SceneMenu()
    {
        ExecuteSetup(true);
    }

    public static void SetupChapter4SceneBatch()
    {
        ExecuteSetup(false);
    }

    public static void ExecuteSetup(bool showDialog)
    {
        Debug.Log("[Chapter4SceneSetup] Bắt đầu thiết lập hoàn chỉnh Scene Chapter 4...");

        // 1. Mở Scene Chapter 4
        var scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);

        // 2. Thêm MeshCollider cho toàn bộ sàn và công trình cung điện
        MeshFilter[] allMeshFilters = Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None);
        int collidersAdded = 0;
        foreach (MeshFilter mf in allMeshFilters)
        {
            if (mf == null || mf.sharedMesh == null) continue;
            string objName = mf.gameObject.name.ToLower();
            if (objName.Contains("floor") || objName.Contains("ground") || objName.Contains("stone") ||
                objName.Contains("stairs") || objName.Contains("bridge") || objName.Contains("ssg"))
            {
                if (mf.GetComponent<Collider>() == null)
                {
                    MeshCollider mc = mf.gameObject.AddComponent<MeshCollider>();
                    mc.convex = false;
                    collidersAdded++;
                }
            }
        }
        Debug.Log($"[Chapter4SceneSetup] Đã thêm MeshCollider cho {collidersAdded} đối tượng sàn/kiến trúc.");

        // Thêm sàn bảo vệ an toàn dưới đáy cung điện
        GameObject safetyFloor = GameObject.Find("PalaceSafetyFloorCollider");
        if (safetyFloor == null)
        {
            safetyFloor = new GameObject("PalaceSafetyFloorCollider");
            BoxCollider box = safetyFloor.AddComponent<BoxCollider>();
            box.size = new Vector3(500f, 2f, 500f);
            safetyFloor.transform.position = new Vector3(-64f, -12.0f, 0f);
        }

        // 3. TẠO HOẶC CẤU HÌNH TERRAIN CHO CHAPTER 4
        SetupChapter4Terrain();

        // 4. TẠO ĐƯỜNG DẪN & CẢNH QUAN (CÂY CỎ, ĐÁ, ĐUỐC DẪN VÀO CUNG ĐIỆN)
        SetupPathwayAndEnvironment();

        // 5. CẤU HÌNH SPAWNPOINT VÀ NHÂN VẬT LÝ THÔNG
        GameObject lyThong = SetupLyThongPlayer();

        // 6. CẤU HÌNH CAMERA THEO LÝ THÔNG
        SetupCamera(lyThong);

        // 7. CẤU HÌNH KHU VỰC VUA / TRIỀU ĐÌNH BÊN TRONG CUNG ĐIỆN
        SetupKingCourtArea();

        // 8. CẤU HÌNH CHAPTER4PALACEMANAGER
        GameObject mgrObj = GameObject.Find("Chapter4PalaceManager");
        if (mgrObj == null)
        {
            mgrObj = new GameObject("Chapter4PalaceManager");
        }
        Chapter4PalaceManager mgr = mgrObj.GetComponent<Chapter4PalaceManager>();
        if (mgr == null) mgr = mgrObj.AddComponent<Chapter4PalaceManager>();
        mgr.lyThongObject = lyThong;
        GameObject spObj = GameObject.Find("Chapter4_StartPoint");
        if (spObj != null) mgr.chapter4SpawnPoint = spObj.transform;
        if (Camera.main != null) mgr.cameraFollow = Camera.main.GetComponent<CameraFollow>();

        // 9. BAKE NAVMESH NẾU CÓ THÀNH PHẦN AI CẦN DI CHUYỂN
        SetupNavMesh();

        // 10. Lưu Scene
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log("[Chapter4SceneSetup] HOÀN TẤT THIẾT LẬP TOÀN BỘ SCENE CHAPTER 4 THÀNH CÔNG!");

        if (showDialog)
        {
            EditorUtility.DisplayDialog("Thành công",
                "Đã hoàn tất thiết lập Scene Chapter 4:\n" +
                "- Đã tạo Unity Terrain bao quanh cung điện (cao độ Y = -10.55m khớp sàn cung điện).\n" +
                "- Đã tạo đường dẫn đá, hàng cây và đèn đuốc dẫn từ ngoài vào cổng cung điện.\n" +
                "- Lý Thông xuất hiện ngoài cung điện, đứng vững trên mặt đất (Y ~ -10.5f).\n" +
                "- Điều khiển WASD mượt mà, chuyển động Idle/Walk/Run theo Speed.\n" +
                "- Đã loại bỏ NavMeshAgent gây xung đột trên người chơi.\n" +
                "- NavMeshSurface đã sẵn sàng cho khu vực đi lại.\n" +
                "- Camera bám sát Lý Thông và nhìn hướng vào cung điện.", "OK");
        }
    }

    private static void SetupChapter4Terrain()
    {
        GameObject terrainObj = GameObject.Find("Chapter4_Terrain") ?? GameObject.Find("Terrain");
        if (terrainObj == null)
        {
            // Tải TerrainData sẵn có
            string terrainPath = AssetDatabase.GUIDToAssetPath(TERRAIN_DATA_GUID);
            TerrainData tData = AssetDatabase.LoadAssetAtPath<TerrainData>(terrainPath);
            if (tData == null)
            {
                // Thử tìm bất kỳ TerrainData nào
                string[] guids = AssetDatabase.FindAssets("t:TerrainData");
                if (guids.Length > 0)
                {
                    tData = AssetDatabase.LoadAssetAtPath<TerrainData>(AssetDatabase.GUIDToAssetPath(guids[0]));
                }
            }

            if (tData != null)
            {
                terrainObj = Terrain.CreateTerrainGameObject(tData);
                terrainObj.name = "Chapter4_Terrain";
            }
            else
            {
                terrainObj = new GameObject("Chapter4_Terrain");
                TerrainData newTData = new TerrainData();
                newTData.size = new Vector3(600f, 100f, 600f);
                string newAssetPath = "Assets/TerrainData_Chapter4.asset";
                AssetDatabase.CreateAsset(newTData, newAssetPath);
                Terrain t = terrainObj.AddComponent<Terrain>();
                TerrainCollider tc = terrainObj.AddComponent<TerrainCollider>();
                t.terrainData = newTData;
                tc.terrainData = newTData;
            }
        }

        // Đặt vị trí Terrain sao cho bao quanh cung điện
        // Cung điện sàn ở Y = -10.54, X trong khoảng [-160, 30], Z trong khoảng [-100, 100]
        // Đặt Terrain gốc tại X = -300, Y = -10.55, Z = -300 để mặt phẳng tự nhiên ôm sát sàn
        terrainObj.transform.position = new Vector3(-300f, -10.55f, -300f);

        Terrain terrainComp = terrainObj.GetComponent<Terrain>();
        if (terrainComp != null)
        {
            terrainComp.drawTreesAndFoliage = true;
            terrainComp.treeDistance = 1000f;
            terrainComp.detailObjectDistance = 100f;
        }

        TerrainCollider tCollider = terrainObj.GetComponent<TerrainCollider>();
        if (tCollider == null) terrainObj.AddComponent<TerrainCollider>();
    }

    private static void SetupPathwayAndEnvironment()
    {
        // Nhóm chứa toàn bộ cảnh quan Chapter 4
        GameObject envGroup = GameObject.Find("Chapter4_Environment");
        if (envGroup == null)
        {
            envGroup = new GameObject("Chapter4_Environment");
        }

        // Tải các prefab cây, cỏ, đá, đường đi có sẵn trong project
        GameObject rockPrefabA = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Toby Fredson/The Toby Foliage Engine/(TTFE)_Demo/Prefabs/Prefabs_Static/RocksTTFEL_A.prefab");
        GameObject rockPrefabB = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Toby Fredson/The Toby Foliage Engine/(TTFE)_Demo/Prefabs/Prefabs_Static/RocksTTFEL_B.prefab");
        GameObject rockFloorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval_house_lite/Prefabs/modular elements/floor_rock_4x1m.prefab")
                                    ?? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval_house_lite/Prefabs/modular elements/floor_rock_2x3m.prefab");
        GameObject shrubTreeC = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Toby Fredson/The Toby Foliage Engine/(TTFE)_Demo/Prefabs/Prefabs_Vegetation/Vegetation_Trees/VTSpecies_Shrub/ShrubTree_C.prefab");
        GameObject shrubBushB = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Toby Fredson/The Toby Foliage Engine/(TTFE)_Demo/Prefabs/Prefabs_Vegetation/Vegetation_Trees/VTSpecies_Shrub/ShrubBush_B.prefab");
        GameObject lamppostPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval_house_lite/Prefabs/modular elements/lamppost.prefab");
        GameObject torchPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval_house_lite/Prefabs/Fire/Fire_torch.prefab");
        GameObject grassPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Toby Fredson/The Toby Foliage Engine/(TTFE)_Demo/Prefabs/Prefabs_Vegetation/Vegetation_Plants/VP_Grass/GrassMedium_A.prefab");
        GameObject flowerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Toby Fredson/The Toby Foliage Engine/(TTFE)_Demo/Prefabs/Prefabs_Vegetation/Vegetation_Plants/VP_Flowers/FlowersWTTFE-A.prefab");

        // 1. TẠO CON ĐƯỜNG DẪN TỪ NGOÀI VÀO CỔNG CUNG ĐIỆN (Z từ -75m đến -15m, X từ -135m đến -134m)
        GameObject pathObj = GameObject.Find("Chapter4_PathToGate");
        if (pathObj == null)
        {
            pathObj = new GameObject("Chapter4_PathToGate");
            pathObj.transform.parent = envGroup.transform;

            for (float z = -75f; z <= -15f; z += 3.5f)
            {
                if (rockFloorPrefab != null)
                {
                    GameObject stone = (GameObject)PrefabUtility.InstantiatePrefab(rockFloorPrefab);
                    stone.transform.parent = pathObj.transform;
                    stone.transform.position = new Vector3(-134.5f, -10.50f, z);
                    stone.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                }

                // Cắm đèn/đuốc hai bên đường mỗi 12m
                if (((int)z) % 12 == 0)
                {
                    if (torchPrefab != null || lamppostPrefab != null)
                    {
                        GameObject lampL = (GameObject)PrefabUtility.InstantiatePrefab(torchPrefab != null ? torchPrefab : lamppostPrefab);
                        lampL.transform.parent = pathObj.transform;
                        lampL.transform.position = new Vector3(-138.5f, -10.5f, z);

                        GameObject lampR = (GameObject)PrefabUtility.InstantiatePrefab(torchPrefab != null ? torchPrefab : lamppostPrefab);
                        lampR.transform.parent = pathObj.transform;
                        lampR.transform.position = new Vector3(-130.5f, -10.5f, z);
                    }
                }
            }
        }

        // 2. HÀNG CÂY VÀ CẢNH QUAN 2 BÊN ĐƯỜNG DẪN (Tạo cảm giác không gian ngoài trời cổ tích)
        GameObject treesGroup = GameObject.Find("Chapter4_FoliageGroup");
        if (treesGroup == null)
        {
            treesGroup = new GameObject("Chapter4_FoliageGroup");
            treesGroup.transform.parent = envGroup.transform;

            for (float z = -80f; z <= -18f; z += 7f)
            {
                // Cây bên trái đường
                if (shrubTreeC != null)
                {
                    GameObject treeL = (GameObject)PrefabUtility.InstantiatePrefab(shrubTreeC);
                    treeL.transform.parent = treesGroup.transform;
                    treeL.transform.position = new Vector3(-142f - Random.Range(1f, 4f), -10.5f, z + Random.Range(-1.5f, 1.5f));
                    treeL.transform.localScale = Vector3.one * Random.Range(0.9f, 1.3f);
                }

                // Cây bên phải đường
                if (shrubBushB != null)
                {
                    GameObject bushR = (GameObject)PrefabUtility.InstantiatePrefab(shrubBushB);
                    bushR.transform.parent = treesGroup.transform;
                    bushR.transform.position = new Vector3(-127f + Random.Range(1f, 4f), -10.5f, z + Random.Range(-1.5f, 1.5f));
                    bushR.transform.localScale = Vector3.one * Random.Range(0.8f, 1.2f);
                }

                // Đá và khóm hoa dọc ven đường
                if (rockPrefabA != null && Random.value > 0.4f)
                {
                    GameObject rock = (GameObject)PrefabUtility.InstantiatePrefab(rockPrefabA);
                    rock.transform.parent = treesGroup.transform;
                    rock.transform.position = new Vector3(-139.5f, -10.5f, z + 2f);
                    rock.transform.localScale = Vector3.one * 0.7f;
                }

                if (flowerPrefab != null)
                {
                    GameObject fl = (GameObject)PrefabUtility.InstantiatePrefab(flowerPrefab);
                    fl.transform.parent = treesGroup.transform;
                    fl.transform.position = new Vector3(-136.5f, -10.45f, z);
                }

                if (grassPrefab != null)
                {
                    GameObject gr = (GameObject)PrefabUtility.InstantiatePrefab(grassPrefab);
                    gr.transform.parent = treesGroup.transform;
                    gr.transform.position = new Vector3(-132.5f, -10.45f, z + 1f);
                }
            }
        }
    }

    private static GameObject SetupLyThongPlayer()
    {
        // Điểm xuất hiện ngoài cung điện
        GameObject spawnPointObj = GameObject.Find("Chapter4_StartPoint");
        if (spawnPointObj == null)
        {
            spawnPointObj = new GameObject("Chapter4_StartPoint");
        }
        // Vị trí ngoài cung điện nhìn thẳng về cổng chính (-134.5f, -10.5f, -70.0f)
        Vector3 spawnPos = new Vector3(-134.5f, -10.5f, -70.0f);
        spawnPointObj.transform.position = spawnPos;
        spawnPointObj.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 0f, 1f)); // Nhìn hướng Z+ về phía cung điện

        GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_PATH);
        RuntimeAnimatorController animCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ANIM_CONTROLLER_PATH);

        // Tìm hoặc khởi tạo Lý Thông
        GameObject lyThong = GameObject.Find("LyThong") ?? GameObject.Find("Lythong");
        if (lyThong == null)
        {
            // Kiểm tra xem có prefab cũ trong scene với tên khác không
            GameObject[] allObjs = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var obj in allObjs)
            {
                if (obj.name.Contains("Meshy_AI_The_Wayfarer_Sage"))
                {
                    lyThong = obj;
                    break;
                }
            }
        }

        if (lyThong == null)
        {
            if (modelAsset != null)
            {
                lyThong = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset);
            }
            else
            {
                lyThong = new GameObject("LyThong");
            }
        }

        lyThong.name = "LyThong";
        lyThong.tag = "Player";

        // Vô hiệu hóa / gỡ bỏ script điều hướng AI cũ nếu có
        LyThongGuide oldGuide = lyThong.GetComponent<LyThongGuide>();
        if (oldGuide != null) Undo.DestroyObjectImmediate(oldGuide);

        // YÊU CẦU 3: Gỡ bỏ NavMeshAgent trên người chơi để tránh lỗi "Failed to create agent because there is no valid NavMesh"
        NavMeshAgent agent = lyThong.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            Undo.DestroyObjectImmediate(agent);
        }

        // Animator
        Animator anim = lyThong.GetComponent<Animator>();
        if (anim == null) anim = lyThong.AddComponent<Animator>();
        if (animCtrl != null) anim.runtimeAnimatorController = animCtrl;
        anim.applyRootMotion = false;

        // CharacterController
        CharacterController cc = lyThong.GetComponent<CharacterController>();
        if (cc == null) cc = lyThong.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.35f;
        cc.center = new Vector3(0f, 0.9f, 0f);
        cc.minMoveDistance = 0.001f;
        cc.slopeLimit = 45f;
        cc.stepOffset = 0.3f;

        // LyThongPlayerController (WASD)
        LyThongPlayerController pc = lyThong.GetComponent<LyThongPlayerController>();
        if (pc == null) pc = lyThong.AddComponent<LyThongPlayerController>();
        pc.moveSpeed = 5.0f;
        pc.rotationSpeed = 10.0f;
        pc.gravity = -9.81f;

        // Đặt vị trí
        cc.enabled = false;
        lyThong.transform.position = spawnPos;
        lyThong.transform.rotation = spawnPointObj.transform.rotation;
        cc.enabled = true;

        // Lưu bản prefab sao lưu
        string resDir = "Assets/Resources";
        if (!Directory.Exists(resDir)) Directory.CreateDirectory(resDir);
        PrefabUtility.SaveAsPrefabAsset(lyThong, "Assets/Resources/LyThong_Player.prefab");

        return lyThong;
    }

    private static void SetupCamera(GameObject target)
    {
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            CameraFollow camFollow = mainCam.GetComponent<CameraFollow>();
            if (camFollow == null) camFollow = mainCam.gameObject.AddComponent<CameraFollow>();
            camFollow.target = target.transform;
            camFollow.distance = 4.5f;
            camFollow.heightOffset = 1.6f;
            camFollow.rotationSpeed = 1.5f;

            // Đặt camera nhìn từ sau lưng Lý Thông hướng vào cung điện
            mainCam.transform.position = target.transform.position - (Vector3.forward * 4.5f) + (Vector3.up * 1.6f);
            mainCam.transform.LookAt(target.transform.position + Vector3.up * 1.2f);
        }
    }

    private static void SetupKingCourtArea()
    {
        GameObject courtArea = GameObject.Find("Chapter4_KingCourt");
        if (courtArea == null)
        {
            courtArea = new GameObject("Chapter4_KingCourt");
            // Đặt tại sảnh chính cung điện
            courtArea.transform.position = new Vector3(-134.5f, -8.3f, 4.0f);

            // Thêm Trigger tương tác kết thúc nhiệm vụ hoặc hội thoại với Vua
            SphereCollider sc = courtArea.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 3.0f;
        }
    }

    private static void SetupNavMesh()
    {
        // Gắn NavMeshSurface lên môi trường nếu cần điều hướng cho NPC / lính canh trong cung điện
        GameObject navSurfaceObj = GameObject.Find("NavMeshSurface_Palace");
        if (navSurfaceObj == null)
        {
            navSurfaceObj = new GameObject("NavMeshSurface_Palace");
            NavMeshSurface surface = navSurfaceObj.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            // Nướng NavMesh Surface tự động
            surface.BuildNavMesh();
            Debug.Log("[Chapter4SceneSetup] Đã nướng NavMesh cho khu vực cung điện.");
        }
    }
}
#endif
