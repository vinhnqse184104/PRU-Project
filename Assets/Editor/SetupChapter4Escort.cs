#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Animations;
using System.IO;
using TMPro;

[InitializeOnLoad]
public class SetupChapter4Escort
{
    private const string SCENE_PATH = "Assets/Scenes/Chapter4_LythongCuopCong.unity";
    private const string SOLDIER_FBX_PATH = "Assets/nhanvat/lính/lính1/Meshy_AI_Mongol_meelle_biped_Animation_01a0d464-3ba4-7709-b908-7ce4a2ae12f4_withSkin.fbx";
    private const string SOLDIER_CONTROLLER_PATH = "Assets/nhanvat/lính/LinhAnim.controller";

    static SetupChapter4Escort()
    {
        EditorApplication.delayCall += () =>
        {
            ExecuteSetup(false);
        };
    }

    [MenuItem("Tools/PRU/Thiết lập Lính hộ tống Chapter 4")]
    public static void ExecuteSetupMenu()
    {
        ExecuteSetup(true);
    }

    public static void ExecuteSetup(bool showDialog)
    {
        var activeScene = EditorSceneManager.GetActiveScene();
        if (!activeScene.name.Equals("Chapter4_LythongCuopCong", System.StringComparison.OrdinalIgnoreCase))
        {
            activeScene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
        }

        Debug.Log("[SetupChapter4Escort] Bắt đầu thiết lập hệ thống Lính hộ tống Lý Thông...");

        // 1. TẠO / CẤU HÌNH ANIMATOR CONTROLLER CHO LÍNH NẾU CHƯA CÓ
        RuntimeAnimatorController soldierCtrl = CreateOrLoadSoldierController();

        // 2. TÌM LY THONG, KING, PRINCESS
        GameObject lyThongObj = GameObject.Find("LyThong") ?? GameObject.Find("lythong");
        if (lyThongObj == null)
        {
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
            foreach (var p in players)
            {
                if (p.name.ToLower().Contains("lythong") || p.name.ToLower().Contains("sage"))
                {
                    lyThongObj = p;
                    break;
                }
            }
        }

        GameObject kingObj = GameObject.Find("Meshy_AI_Dragon_Emperor_Rising_biped_Animation_01a0d469-8153-7436-8709-d393703fc126_withSkin") ??
                             GameObject.Find("vua") ?? GameObject.Find("King");

        // 2b. TÌM VÀ CẤU HÌNH CÔNG CHÚA (TẠO GAMEOBJECT CHA ĐỂ QUAY TRỤC Y, GIỮ MODEL CON ĐỨNG THẲNG X = -90)
        GameObject princessObj = GameObject.Find("Congchua") ?? GameObject.Find("congchua") ?? GameObject.Find("Princess");
        GameObject princessRoot = null;

        if (princessObj != null)
        {
            Transform existingChildModel = princessObj.transform.Find("Congchua_Model");
            if (existingChildModel != null)
            {
                // princessObj chính là GameObject cha
                princessRoot = princessObj;
                existingChildModel.localPosition = Vector3.zero;
                existingChildModel.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            }
            else if (princessObj.name == "Congchua_Model" && princessObj.transform.parent != null)
            {
                // princessObj là model con
                princessRoot = princessObj.transform.parent.gameObject;
                princessObj.transform.localPosition = Vector3.zero;
                princessObj.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            }
            else
            {
                // Tạo GameObject cha Congchua để điều khiển quay trục Y, model con giữ nguyên trục đứng X = -90 độ
                Vector3 worldPos = princessObj.transform.position;
                Vector3 worldScale = princessObj.transform.localScale;

                princessRoot = new GameObject("Congchua");
                princessRoot.transform.position = worldPos;
                princessRoot.transform.rotation = Quaternion.Euler(0f, -83f, 0f);
                princessRoot.transform.localScale = Vector3.one;
                Undo.RegisterCreatedObjectUndo(princessRoot, "Tạo Congchua Root");

                princessObj.name = "Congchua_Model";
                Undo.SetTransformParent(princessObj.transform, princessRoot.transform, "Gán Congchua_Model vào cha");
                princessObj.transform.localPosition = Vector3.zero;
                princessObj.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                princessObj.transform.localScale = worldScale;

                Debug.Log($"[SetupChapter4Escort] Đã tạo GameObject cha 'Congchua' tại {worldPos} và giữ model con 'Congchua_Model' tại LocalRotation X = -90 độ.");
            }
        }

        // 3. TÌM ĐÚNG LÍNH DẪN ĐƯỜNG (GẦN LÝ THÔNG NHẤT)
        GameObject escortSoldierObj = GameObject.Find("Linh_EscortLeader");
        if (escortSoldierObj == null && lyThongObj != null)
        {
            GameObject[] allObjs = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            float minDistance = float.MaxValue;
            foreach (var go in allObjs)
            {
                if (go.name == "Linh" || go.name == "Linh_EscortLeader")
                {
                    float dist = Vector3.Distance(go.transform.position, lyThongObj.transform.position);
                    if (dist < minDistance && dist < 12.0f)
                    {
                        minDistance = dist;
                        escortSoldierObj = go;
                    }
                }
            }
        }

        if (escortSoldierObj != null)
        {
            Undo.RecordObject(escortSoldierObj, "Cấu hình Lính dẫn đường");
            // Đặt tên riêng phân biệt trong Hierarchy để dễ theo dõi
            escortSoldierObj.name = "Linh_EscortLeader";

            // Cấu hình CharacterController cho lính
            CharacterController soldierCC = escortSoldierObj.GetComponent<CharacterController>();
            if (soldierCC == null)
            {
                soldierCC = escortSoldierObj.AddComponent<CharacterController>();
            }
            soldierCC.height = 1.8f;
            soldierCC.radius = 0.35f;
            soldierCC.center = new Vector3(0f, 0.9f, 0f);
            soldierCC.stepOffset = 0.3f;
            soldierCC.slopeLimit = 45f;

            // Gán Animator Controller cho lính
            Animator soldierAnim = escortSoldierObj.GetComponent<Animator>();
            if (soldierAnim == null) soldierAnim = escortSoldierObj.AddComponent<Animator>();
            if (soldierCtrl != null && soldierAnim.runtimeAnimatorController == null)
            {
                soldierAnim.runtimeAnimatorController = soldierCtrl;
            }
            soldierAnim.applyRootMotion = false;

            Debug.Log($"[SetupChapter4Escort] Đã cấu hình Lính dẫn đường: '{escortSoldierObj.name}' tại {escortSoldierObj.transform.position:F2}.");
        }

        // 4. TẠO / CẬP NHẬT CỤM WAYPOINT
        GameObject waypointsRoot = GameObject.Find("Chapter4_EscortWaypoints");
        if (waypointsRoot == null)
        {
            waypointsRoot = new GameObject("Chapter4_EscortWaypoints");
            Undo.RegisterCreatedObjectUndo(waypointsRoot, "Tạo Chapter4_EscortWaypoints");
        }

        var wpData = new (string name, Vector3 pos)[]
        {
            ("Escort_Start", new Vector3(-95.94f, -10.04f, -30.95f)),
            ("Escort_Waypoint_01", new Vector3(-95.94f, -10.04f, 22.10f)),
            ("Escort_Waypoint_02", new Vector3(-70.00f, -10.04f, 22.10f)),
            ("Escort_Destination", new Vector3(-51.00f, -10.04f, 22.20f))
        };

        Transform[] wpTransforms = new Transform[wpData.Length];
        for (int i = 0; i < wpData.Length; i++)
        {
            Transform existingWp = waypointsRoot.transform.Find(wpData[i].name);
            GameObject wpObj;
            if (existingWp == null)
            {
                wpObj = new GameObject(wpData[i].name);
                wpObj.transform.parent = waypointsRoot.transform;
                Undo.RegisterCreatedObjectUndo(wpObj, "Tạo Waypoint " + wpData[i].name);
            }
            else
            {
                wpObj = existingWp.gameObject;
            }

            wpObj.transform.position = wpData[i].pos;
            wpTransforms[i] = wpObj.transform;
        }

        // 5. TẠO / CẬP NHẬT ĐIỂM CAMERA HỘI KIẾN
        GameObject camPointObj = GameObject.Find("Chapter4_DialogueCameraPoint");
        if (camPointObj == null)
        {
            camPointObj = new GameObject("Chapter4_DialogueCameraPoint");
            Undo.RegisterCreatedObjectUndo(camPointObj, "Tạo Chapter4_DialogueCameraPoint");
        }
        camPointObj.transform.position = new Vector3(-51.5f, -8.2f, 17.5f);
        camPointObj.transform.rotation = Quaternion.Euler(10f, 32f, 0f);

        // 6. GẮN COMPONENT Chapter4SoldierEscort VÀO Chapter4PalaceManager
        GameObject palaceMgrObj = GameObject.Find("Chapter4PalaceManager");
        if (palaceMgrObj == null)
        {
            palaceMgrObj = new GameObject("Chapter4PalaceManager");
            Undo.RegisterCreatedObjectUndo(palaceMgrObj, "Tạo Chapter4PalaceManager");
        }

        Chapter4PalaceManager palaceMgr = palaceMgrObj.GetComponent<Chapter4PalaceManager>();
        if (palaceMgr == null) palaceMgr = palaceMgrObj.AddComponent<Chapter4PalaceManager>();

        Chapter4SoldierEscort escort = palaceMgrObj.GetComponent<Chapter4SoldierEscort>();
        if (escort == null) escort = palaceMgrObj.AddComponent<Chapter4SoldierEscort>();

        Undo.RecordObject(escort, "Cấu hình Chapter4SoldierEscort");
        if (lyThongObj != null) escort.lyThong = lyThongObj.transform;
        if (escortSoldierObj != null) escort.escortSoldier = escortSoldierObj.transform;
        if (kingObj != null) escort.king = kingObj.transform;
        if (princessRoot != null) escort.princess = princessRoot.transform;
        else if (princessObj != null) escort.princess = princessObj.transform;
        escort.waypoints = wpTransforms;
        escort.dialogueCameraPoint = camPointObj.transform;
        escort.followDistance = 2.0f;
        escort.soldierSpeed = 2.8f;
        escort.rotationSpeed = 6.0f;
        escort.waypointTolerance = 0.8f;

        // Vô hiệu hóa WASD trên Lý Thông
        if (lyThongObj != null)
        {
            LyThongPlayerController ltCtrl = lyThongObj.GetComponent<LyThongPlayerController>();
            if (ltCtrl != null)
            {
                Undo.RecordObject(ltCtrl, "Tắt WASD Lý Thông");
                ltCtrl.enabled = false;
            }
        }

        // 7. TẠO / CẤU HÌNH CANVAS HỘI THOẠI (Chapter4_DialogueCanvas & DialoguePanel)
        GameObject canvasObj = GameObject.Find("Chapter4_DialogueCanvas");
        if (canvasObj == null)
        {
            canvasObj = new GameObject("Chapter4_DialogueCanvas");
            Undo.RegisterCreatedObjectUndo(canvasObj, "Tạo Chapter4_DialogueCanvas");
        }

        Canvas canvas = canvasObj.GetComponent<Canvas>();
        if (canvas == null) canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GraphicRaycaster raycaster = canvasObj.GetComponent<GraphicRaycaster>();
        if (raycaster == null) raycaster = canvasObj.AddComponent<GraphicRaycaster>();

        // EventSystem
        UnityEngine.EventSystems.EventSystem evtSys = Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (evtSys == null)
        {
            GameObject evtObj = new GameObject("EventSystem");
            evtObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            evtObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            Undo.RegisterCreatedObjectUndo(evtObj, "Tạo EventSystem");
        }

        // Tạo / Cấu hình DialoguePanel
        Transform panelTrans = canvasObj.transform.Find("DialoguePanel");
        GameObject panelObj;
        if (panelTrans == null)
        {
            panelObj = new GameObject("DialoguePanel");
            panelObj.transform.SetParent(canvasObj.transform, false);
            Undo.RegisterCreatedObjectUndo(panelObj, "Tạo DialoguePanel");
        }
        else
        {
            panelObj = panelTrans.gameObject;
        }

        RectTransform panelRT = panelObj.GetComponent<RectTransform>();
        if (panelRT == null) panelRT = panelObj.AddComponent<RectTransform>();
        Undo.RecordObject(panelRT, "Chỉnh RectTransform DialoguePanel");

        // Thiết lập RectTransform: vị trí Bottom-Center, kích thước chuẩn thống nhất 1100 x 215
        panelRT.anchorMin = new Vector2(0.5f, 0f);
        panelRT.anchorMax = new Vector2(0.5f, 0f);
        panelRT.pivot = new Vector2(0.5f, 0f);
        panelRT.anchoredPosition = new Vector2(0f, 30f); // Cách đáy 30 pixel
        panelRT.sizeDelta = new Vector2(1100f, 215f);

        // Nền khung thoại: màu đen mờ sang trọng (translucent slate, alpha 0.72)
        Image panelImg = panelObj.GetComponent<Image>();
        if (panelImg == null) panelImg = panelObj.AddComponent<Image>();
        Undo.RecordObject(panelImg, "Chỉnh màu nền DialoguePanel");
        panelImg.color = new Color(0.06f, 0.08f, 0.12f, 0.72f);

        // Load font TMP LiberationSans SDF
        TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");

        // 1. SpeakerText
        Transform speakerTrans = panelObj.transform.Find("SpeakerText");
        GameObject speakerObj;
        if (speakerTrans == null)
        {
            speakerObj = new GameObject("SpeakerText");
            speakerObj.transform.SetParent(panelObj.transform, false);
            Undo.RegisterCreatedObjectUndo(speakerObj, "Tạo SpeakerText");
        }
        else
        {
            speakerObj = speakerTrans.gameObject;
        }
        RectTransform speakerRT = speakerObj.GetComponent<RectTransform>();
        if (speakerRT == null) speakerRT = speakerObj.AddComponent<RectTransform>();
        speakerRT.anchorMin = new Vector2(0f, 1f);
        speakerRT.anchorMax = new Vector2(1f, 1f);
        speakerRT.pivot = new Vector2(0f, 1f);
        speakerRT.anchoredPosition = new Vector2(35f, -18f);
        speakerRT.sizeDelta = new Vector2(-70f, 44f);

        TextMeshProUGUI speakerTMP = speakerObj.GetComponent<TextMeshProUGUI>();
        if (speakerTMP == null) speakerTMP = speakerObj.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) speakerTMP.font = fontAsset;
        speakerTMP.fontSize = 36f;
        speakerTMP.fontStyle = FontStyles.Bold;
        speakerTMP.alignment = TextAlignmentOptions.TopLeft;
        speakerTMP.color = new Color(0.4f, 0.8f, 1.0f);
        speakerTMP.text = "LÍNH:";

        // 2. ContentText
        Transform contentTrans = panelObj.transform.Find("ContentText");
        GameObject contentObj;
        if (contentTrans == null)
        {
            contentObj = new GameObject("ContentText");
            contentObj.transform.SetParent(panelObj.transform, false);
            Undo.RegisterCreatedObjectUndo(contentObj, "Tạo ContentText");
        }
        else
        {
            contentObj = contentTrans.gameObject;
        }
        RectTransform contentRT = contentObj.GetComponent<RectTransform>();
        if (contentRT == null) contentRT = contentObj.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot = new Vector2(0f, 1f);
        contentRT.anchoredPosition = new Vector2(35f, -68f);
        contentRT.sizeDelta = new Vector2(-70f, 96f);

        TextMeshProUGUI contentTMP = contentObj.GetComponent<TextMeshProUGUI>();
        if (contentTMP == null) contentTMP = contentObj.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) contentTMP.font = fontAsset;
        contentTMP.fontSize = 28f;
        contentTMP.alignment = TextAlignmentOptions.TopLeft;
        contentTMP.textWrappingMode = TextWrappingModes.Normal;
        contentTMP.color = Color.white;
        contentTMP.text = "\"Bẩm bệ hạ! Người có công tiêu diệt Chằn Tinh đã đến!\"";

        // 3. HintText
        Transform hintTrans = panelObj.transform.Find("HintText");
        GameObject hintObj;
        if (hintTrans == null)
        {
            hintObj = new GameObject("HintText");
            hintObj.transform.SetParent(panelObj.transform, false);
            Undo.RegisterCreatedObjectUndo(hintObj, "Tạo HintText");
        }
        else
        {
            hintObj = hintTrans.gameObject;
        }
        RectTransform hintRT = hintObj.GetComponent<RectTransform>();
        if (hintRT == null) hintRT = hintObj.AddComponent<RectTransform>();
        hintRT.anchorMin = new Vector2(0f, 0f);
        hintRT.anchorMax = new Vector2(1f, 0f);
        hintRT.pivot = new Vector2(0.5f, 0f);
        hintRT.anchoredPosition = new Vector2(0f, 14f);
        hintRT.sizeDelta = new Vector2(-70f, 32f);

        TextMeshProUGUI hintTMP = hintObj.GetComponent<TextMeshProUGUI>();
        if (hintTMP == null) hintTMP = hintObj.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) hintTMP.font = fontAsset;
        hintTMP.fontSize = 20f;
        hintTMP.fontStyle = FontStyles.Italic;
        hintTMP.alignment = TextAlignmentOptions.Center;
        hintTMP.color = new Color(1.0f, 0.84f, 0.0f, 0.95f);
        hintTMP.text = "(Bấm Enter hoặc Chuột trái để tiếp tục)";

        // Khung thoại mặc định ẩn khi mới vào Scene cho đến khi Lý Thông tới nơi
        panelObj.SetActive(false);

        // Gán tham chiếu Canvas vào Chapter4SoldierEscort
        escort.dialoguePanel = panelObj;
        escort.speakerText = speakerTMP;
        escort.contentText = contentTMP;
        escort.hintText = hintTMP;

        // 8. LƯU SCENE
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);
        AssetDatabase.SaveAssets();

        Debug.Log("[SetupChapter4Escort] HOÀN TẤT THIẾT LẬP HỆ THỐNG LÍNH HỘ TỐNG VÀ GIAO DIỆN HỘI THOẠI CHAPTER 4 THÀNH CÔNG!");

        if (showDialog)
        {
            EditorUtility.DisplayDialog("Thành công",
                "Đã hoàn tất thiết lập hệ thống Lính hộ tống và giao diện hội thoại Chapter 4:\n" +
                "- Canvas: Chapter4_DialogueCanvas\n" +
                "- DialoguePanel: RectTransform bottom-center (800x120), nền bán trong suốt alpha 0.65\n" +
                "- SpeakerText: font size 18 (Bold)\n" +
                "- ContentText: font size 16 (Auto wrap)\n" +
                "- HintText: font size 12 (Vàng kim)\n" +
                "- Không che khuất bối cảnh ngai vàng và các nhân vật.",
                "Đóng");
        }
    }

    private static RuntimeAnimatorController CreateOrLoadSoldierController()
    {
        RuntimeAnimatorController existing = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(SOLDIER_CONTROLLER_PATH);
        if (existing != null) return existing;

        AnimationClip walkClip = null;
        Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(SOLDIER_FBX_PATH);
        foreach (var sa in subAssets)
        {
            if (sa is AnimationClip clip && !clip.name.StartsWith("__preview__"))
            {
                walkClip = clip;
                break;
            }
        }

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(SOLDIER_CONTROLLER_PATH);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsWalking", AnimatorControllerParameterType.Bool);

        var rootStateMachine = controller.layers[0].stateMachine;

        // State Idle
        var idleState = rootStateMachine.AddState("Idle");
        idleState.motion = null; // Stance cố định
        idleState.speed = 0f;
        rootStateMachine.defaultState = idleState;

        // State Walk
        var walkState = rootStateMachine.AddState("Walk");
        walkState.motion = walkClip;
        walkState.speed = 1f;

        // Idle -> Walk
        var toWalk = idleState.AddTransition(walkState);
        toWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        toWalk.duration = 0.15f;
        toWalk.hasExitTime = false;

        // Walk -> Idle
        var toIdle = walkState.AddTransition(idleState);
        toIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        toIdle.duration = 0.15f;
        toIdle.hasExitTime = false;

        AssetDatabase.SaveAssets();
        Debug.Log("[SetupChapter4Escort] Đã tạo mới LinhAnim.controller cho nhân vật lính.");
        return controller;
    }
}
#endif
