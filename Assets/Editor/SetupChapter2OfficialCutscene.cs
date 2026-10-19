#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Animations;
using System.IO;

[InitializeOnLoad]
public class SetupChapter2OfficialCutscene
{
    private const string SETUP_SESSION_KEY = "PRU_OfficialCutsceneSetup_v1";
    private const string CH2_SCENE_PATH = "Assets/Scenes/Chapter2_LyThong.unity";
    private const string CH4_SCENE_PATH = "Assets/Scenes/Chapter4_LythongCuopCong.unity";

    private const string OFFICIAL_DIR = "Assets/nhanvat/lý thông được phong chức";
    private const string IDLE_DIR = OFFICIAL_DIR + "/đứng yên";
    private const string WALK_DIR = OFFICIAL_DIR + "/đi bộ";

    private const string MAT_PATH = OFFICIAL_DIR + "/maulythong_quan.mat";
    private const string CONTROLLER_PATH = OFFICIAL_DIR + "/LyThongQuanAnim.controller";

    private const string IDLE_FBX_PATH = IDLE_DIR + "/Meshy_AI_Imperial_Dragon_Warlo_biped_Animation_01a0d464-3ba4-7709-b908-7ce4a2ae12f4_withSkin.fbx";
    private const string WALK_FBX_PATH = WALK_DIR + "/Meshy_AI_Imperial_Dragon_Warlo_biped_Animation_Walking_withSkin.fbx";

    static SetupChapter2OfficialCutscene()
    {
        if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode) return;
        EditorApplication.delayCall += () =>
        {
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!SessionState.GetBool(SETUP_SESSION_KEY, false))
            {
                SessionState.SetBool(SETUP_SESSION_KEY, true);
                ExecuteSetup(false);
            }
        };
    }

    [MenuItem("Tools/PRU/Thiết lập phân cảnh Lý Thông Quận công Chapter 2")]
    public static void ManualSetupMenu()
    {
        ExecuteSetup(true);
    }

    [MenuItem("Tools/PRU/Test Chapter 2 - Lý Thông về nhà gọi mẹ (Bật trạng thái Quận công)")]
    public static void TestChapter2OfficialReturn()
    {
        StoryStateManager.LyThongReturnedAsOfficial = true;
        Debug.Log("[SetupChapter2OfficialCutscene] Đã kích hoạt StoryStateManager.LyThongReturnedAsOfficial = true.");

        var activeScene = EditorSceneManager.GetActiveScene();
        if (activeScene.path != CH2_SCENE_PATH)
        {
            EditorSceneManager.OpenScene(CH2_SCENE_PATH, OpenSceneMode.Single);
        }

        EditorApplication.isPlaying = true;
    }

    [MenuItem("Tools/PRU/Test Chapter 4 - Toàn bộ chu trình Phong Quận công -> Về nhà")]
    public static void TestFullFromChapter4()
    {
        StoryStateManager.StartNewGame();
        Debug.Log("[SetupChapter2OfficialCutscene] Đã khởi tạo mới StoryStateManager. Đang mở Chapter 4...");

        var activeScene = EditorSceneManager.GetActiveScene();
        if (activeScene.path != CH4_SCENE_PATH)
        {
            EditorSceneManager.OpenScene(CH4_SCENE_PATH, OpenSceneMode.Single);
        }

        EditorApplication.isPlaying = true;
    }

    [MenuItem("Tools/PRU/Reset Story State (Chơi mới)")]
    public static void ResetState()
    {
        StoryStateManager.StartNewGame();
        Debug.Log("[SetupChapter2OfficialCutscene] Đã xoá toàn bộ StoryStateManager về mặc định.");
        EditorUtility.DisplayDialog("Story State", "Đã reset toàn bộ tiến trình game (StoryStateManager) về ban đầu.", "OK");
    }

    public static void ExecuteSetup(bool showDialog)
    {
        Debug.Log("[SetupChapter2OfficialCutscene] Bắt đầu thiết lập tài nguyên và phân cảnh Lý Thông Quận công...");

        Material mat = EnsureMaterial();
        AnimatorController controller = EnsureAnimatorController();

        string currentScenePath = EditorSceneManager.GetActiveScene().path;
        bool isSceneOpened = false;

        if (currentScenePath != CH2_SCENE_PATH)
        {
            if (File.Exists(CH2_SCENE_PATH))
            {
                EditorSceneManager.OpenScene(CH2_SCENE_PATH, OpenSceneMode.Single);
                isSceneOpened = true;
            }
        }
        else
        {
            isSceneOpened = true;
        }

        if (isSceneOpened)
        {
            ConfigureChapter2Scene(mat, controller);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("[SetupChapter2OfficialCutscene] Đã lưu Scene Chapter2_LyThong thành công.");
        }

        AssetDatabase.SaveAssets();

        Debug.Log("[SetupChapter2OfficialCutscene] THIẾT LẬP HOÀN TẤT THÀNH CÔNG!");

        if (showDialog)
        {
            EditorUtility.DisplayDialog("Thiết lập thành công",
                "Đã hoàn tất cấu hình phân cảnh Lý Thông Quận công về nhà gọi mẹ:\n\n" +
                "- Material: " + MAT_PATH + "\n" +
                "- Animator Controller: " + CONTROLLER_PATH + "\n" +
                "- Tọa độ Lý Thông: (81.98, 0.2318305, 31.29)\n" +
                "- Điểm Mẹ bên trong: (82.0, 0.25, 25.0)\n" +
                "- Điểm Mẹ trước cửa: (81.98, 0.2318305, 29.5)\n" +
                "- Camera phân cảnh: (78.0, 1.45, 31.8), quay góc 112 độ\n" +
                "- Tự động mở cửa nhà và đối thoại 5 câu chuẩn Dialogue UI Chapter 4.",
                "OK");
        }
    }

    private static Material EnsureMaterial()
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(MAT_PATH);
        if (mat == null)
        {
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null) urpLit = Shader.Find("URP/Lit");
            if (urpLit == null) urpLit = Shader.Find("Standard");

            mat = new Material(urpLit);
            mat.name = "maulythong_quan";

            AssetDatabase.CreateAsset(mat, MAT_PATH);
            Debug.Log($"[SetupChapter2OfficialCutscene] Đã tạo Material mới tại: {MAT_PATH}");
        }

        // Gán textures
        Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(IDLE_DIR + "/Meshy_AI_Imperial_Dragon_Warlo_biped_texture_0.png");
        Texture2D metallic = AssetDatabase.LoadAssetAtPath<Texture2D>(IDLE_DIR + "/Meshy_AI_Imperial_Dragon_Warlo_biped_texture_0_metallic.png");
        Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(IDLE_DIR + "/Meshy_AI_Imperial_Dragon_Warlo_biped_texture_0_normal.png");

        if (normal != null)
        {
            string normalPath = AssetDatabase.GetAssetPath(normal);
            TextureImporter importer = AssetImporter.GetAtPath(normalPath) as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.NormalMap)
            {
                importer.textureType = TextureImporterType.NormalMap;
                importer.SaveAndReimport();
            }
        }

        if (albedo != null)
        {
            mat.SetTexture("_BaseMap", albedo);
            mat.SetTexture("_MainTex", albedo);
        }
        if (metallic != null)
        {
            mat.SetTexture("_MetallicGlossMap", metallic);
            mat.EnableKeyword("_METALLICSPECGLOSSMAP");
        }
        if (normal != null)
        {
            mat.SetTexture("_BumpMap", normal);
            mat.EnableKeyword("_NORMALMAP");
        }

        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static AnimatorController EnsureAnimatorController()
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(CONTROLLER_PATH);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(CONTROLLER_PATH);
            Debug.Log($"[SetupChapter2OfficialCutscene] Đã tạo Animator Controller mới tại: {CONTROLLER_PATH}");
        }

        // Thêm tham số Speed nếu chưa có
        bool hasSpeedParam = false;
        foreach (var p in controller.parameters)
        {
            if (p.name == "Speed") { hasSpeedParam = true; break; }
        }
        if (!hasSpeedParam)
        {
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        }

        // Trích xuất AnimationClip từ Idle và Walk FBX
        AnimationClip idleClip = ExtractFirstAnimationClip(IDLE_FBX_PATH);
        AnimationClip walkClip = ExtractFirstAnimationClip(WALK_FBX_PATH);

        var rootStateMachine = controller.layers[0].stateMachine;

        // Tìm hoặc tạo State Idle
        AnimatorState idleState = null;
        AnimatorState walkState = null;

        foreach (var childState in rootStateMachine.states)
        {
            if (childState.state.name == "Idle" || childState.state.name == "Standing") idleState = childState.state;
            if (childState.state.name == "Walk" || childState.state.name == "Walking") walkState = childState.state;
        }

        if (idleState == null)
        {
            idleState = rootStateMachine.AddState("Idle");
        }
        if (idleClip != null)
        {
            idleState.motion = idleClip;
        }
        rootStateMachine.defaultState = idleState;

        if (walkClip != null)
        {
            if (walkState == null)
            {
                walkState = rootStateMachine.AddState("Walk");
            }
            walkState.motion = walkClip;

            // Kiểm tra Transitions giữa Idle và Walk
            if (idleState.transitions.Length == 0)
            {
                var toWalk = idleState.AddTransition(walkState);
                toWalk.hasExitTime = false;
                toWalk.duration = 0.2f;
                toWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            }

            if (walkState.transitions.Length == 0)
            {
                var toIdle = walkState.AddTransition(idleState);
                toIdle.hasExitTime = false;
                toIdle.duration = 0.2f;
                toIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
            }
        }

        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static AnimationClip ExtractFirstAnimationClip(string fbxPath)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        if (assets == null) return null;

        foreach (var asset in assets)
        {
            if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
            {
                return clip;
            }
        }
        return null;
    }

    private static void ConfigureChapter2Scene(Material mat, AnimatorController controller)
    {
        // 1. Tìm hoặc tạo Chapter2OfficialReturnManager
        GameObject managerObj = GameObject.Find("Chapter2OfficialReturnManager");
        if (managerObj == null)
        {
            managerObj = new GameObject("Chapter2OfficialReturnManager");
            Undo.RegisterCreatedObjectUndo(managerObj, "Create Chapter2OfficialReturnManager");
        }

        Chapter2OfficialReturnCutscene cutscene = managerObj.GetComponent<Chapter2OfficialReturnCutscene>();
        if (cutscene == null)
        {
            cutscene = Undo.AddComponent<Chapter2OfficialReturnCutscene>(managerObj);
        }

        // 2. Điểm xuất hiện của Lý Thông: (81.98, 0.2318305, 31.29)
        GameObject lyThongPoint = GameObject.Find("LyThong_OfficialReturnPoint");
        if (lyThongPoint == null)
        {
            lyThongPoint = new GameObject("LyThong_OfficialReturnPoint");
            Undo.RegisterCreatedObjectUndo(lyThongPoint, "Create LyThong_OfficialReturnPoint");
        }
        lyThongPoint.transform.position = new Vector3(81.98f, 0.2318305f, 31.29f);
        lyThongPoint.transform.rotation = Quaternion.Euler(0f, 180f, 0f); // Quay vào cửa nhà
        cutscene.lyThongOfficialReturnPoint = lyThongPoint.transform;

        // 3. Điểm Mẹ bên trong nhà: (82.0, 0.25, 25.0)
        GameObject motherInside = GameObject.Find("Mother_InsidePoint");
        if (motherInside == null)
        {
            motherInside = new GameObject("Mother_InsidePoint");
            Undo.RegisterCreatedObjectUndo(motherInside, "Create Mother_InsidePoint");
        }
        motherInside.transform.position = new Vector3(82.0f, 0.25f, 25.0f);
        motherInside.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        cutscene.motherInsidePoint = motherInside.transform;

        // 4. Điểm Mẹ trước cửa nhà: (81.98, 0.2318305, 29.5) đối diện Lý Thông
        GameObject motherDoor = GameObject.Find("Mother_DoorPoint");
        if (motherDoor == null)
        {
            motherDoor = new GameObject("Mother_DoorPoint");
            Undo.RegisterCreatedObjectUndo(motherDoor, "Create Mother_DoorPoint");
        }
        motherDoor.transform.position = new Vector3(81.98f, 0.2318305f, 29.5f);
        motherDoor.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        cutscene.motherDoorPoint = motherDoor.transform;

        // 5. Điểm Camera phân cảnh: (78.0, 1.45, 31.8), quay góc (6.0, 112.0, 0)
        GameObject camPoint = GameObject.Find("Chapter2_OfficialReturnCamera");
        if (camPoint == null)
        {
            camPoint = new GameObject("Chapter2_OfficialReturnCamera");
            Undo.RegisterCreatedObjectUndo(camPoint, "Create Chapter2_OfficialReturnCamera");
        }
        camPoint.transform.position = new Vector3(78.0f, 1.45f, 31.8f);
        camPoint.transform.rotation = Quaternion.Euler(6.0f, 112.0f, 0f);
        cutscene.cutsceneCameraPoint = camPoint.transform;

        // 6. Mô hình Lý Thông Quận công (LyThong_Official)
        GameObject lyThongOfficial = GameObject.Find("LyThong_Official");
        if (lyThongOfficial == null)
        {
            GameObject fbxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(IDLE_FBX_PATH);
            if (fbxPrefab != null)
            {
                lyThongOfficial = PrefabUtility.InstantiatePrefab(fbxPrefab) as GameObject;
                if (lyThongOfficial != null)
                {
                    lyThongOfficial.name = "LyThong_Official";
                    Undo.RegisterCreatedObjectUndo(lyThongOfficial, "Instantiate LyThong_Official");
                }
            }
        }

        if (lyThongOfficial != null)
        {
            lyThongOfficial.transform.position = new Vector3(81.98f, 0.2318305f, 31.29f);
            lyThongOfficial.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            lyThongOfficial.transform.localScale = Vector3.one;

            // Gán Material
            if (mat != null)
            {
                Renderer[] renderers = lyThongOfficial.GetComponentsInChildren<Renderer>(true);
                foreach (var r in renderers)
                {
                    r.sharedMaterial = mat;
                }
            }

            // Gán Animator & Controller
            Animator anim = lyThongOfficial.GetComponent<Animator>();
            if (anim == null) anim = lyThongOfficial.AddComponent<Animator>();
            if (controller != null) anim.runtimeAnimatorController = controller;

            // Mặc định ẩn đối tượng khi chơi thông thường; Cutscene sẽ tự bật khi StoryStateManager.LyThongReturnedAsOfficial == true
            lyThongOfficial.SetActive(false);

            cutscene.lyThongOfficialInstance = lyThongOfficial;
        }

        // 7. Tìm Mẹ Lý Thông
        GameObject motherObj = GameObject.Find("Meshy_AI_Elder_Master_Stance_biped_Animation_01a0d44b-3352-70aa-af88-28f245645f42_withSkin");
        if (motherObj == null)
        {
            GameObject[] all = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var go in all)
            {
                string l = go.name.ToLower();
                if ((l.Contains("elder") || l.Contains("mother") || l.Contains("melythong")) &&
                    !l.Contains("point") && !l.Contains("camera"))
                {
                    motherObj = go;
                    break;
                }
            }
        }
        if (motherObj != null)
        {
            cutscene.motherInstance = motherObj;
        }

        // 8. Tìm Thạch Sanh
        GameObject tsObj = GameObject.Find("thachsanh") ?? GameObject.FindWithTag("Player");
        if (tsObj != null)
        {
            cutscene.thachSanhPlayer = tsObj;
        }

        // 9. Tìm Lý Thông cũ
        GameObject oldLt = GameObject.Find("Meshy_AI_The_Wayfarer_Sage_biped_Animation_01a0d469-8153-7436-8709-d393703fc126_withSkin") ??
                          GameObject.Find("LyThong");
        if (oldLt != null)
        {
            cutscene.oldLyThongNPC = oldLt;
        }

        // 10. Tìm DoorInteraction
        DoorInteraction door = Object.FindAnyObjectByType<DoorInteraction>();
        if (door != null)
        {
            cutscene.doorInteraction = door;
        }

        // 11. Tìm Camera chính
        Camera cam = Camera.main ?? Object.FindAnyObjectByType<Camera>();
        if (cam != null)
        {
            cutscene.mainCamera = cam;
        }

        EditorUtility.SetDirty(managerObj);
        EditorUtility.SetDirty(cutscene);
    }
}
#endif
