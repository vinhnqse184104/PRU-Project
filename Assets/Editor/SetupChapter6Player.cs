#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

[InitializeOnLoad]
public class SetupChapter6Player
{
    private const string SETUP_SESSION_KEY = "PRU_Chapter6PlayerSetup_v5";
    private const string SCENE_PATH = "Assets/Scenes/Chapter6_HangDaiBang.unity";
    private const string TS_FBX_PATH = "Assets/nhanvat/thachsanh/hanhdong/dungyen/Meshy_AI_Axe_Bearing_Wanderer_biped_Animation_01a0d469-8153-7436-8709-d393703fc126_withSkin.fbx";
    private const string CONTROLLER_PATH = "Assets/nhanvat/thachsanh/hanhdong/chay.controller";

    static SetupChapter6Player()
    {
        EditorApplication.delayCall += () =>
        {
            if (!SessionState.GetBool(SETUP_SESSION_KEY, false))
            {
                SessionState.SetBool(SETUP_SESSION_KEY, true);
                ExecuteSetup(false);
            }
        };
    }

    [MenuItem("Tools/PRU/Thiết lập nhân vật Thạch Sanh Chapter 6")]
    public static void ManualSetupMenu()
    {
        ExecuteSetup(true);
    }

    [MenuItem("Tools/PRU/Khôi phục thanh máu đỏ Thạch Sanh Chapter 6")]
    public static void RemoveThachSanhHpLabelMenu()
    {
        ExecuteSetup(true);
    }

    [MenuItem("Tools/PRU/Mở và Test Thạch Sanh trong Chapter 6")]
    public static void OpenAndTestPlayer()
    {
        ExecuteSetup(false);
        var activeScene = EditorSceneManager.GetActiveScene();
        if (activeScene.path != SCENE_PATH)
        {
            EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
        }
        EditorApplication.isPlaying = true;
    }

    public static void ExecuteSetup(bool showDialog)
    {
        Debug.Log("[SetupChapter6Player] Bắt đầu thiết lập nhân vật Thạch Sanh trong Scene Chapter6_HangDaiBang...");

        // 1. Mở Scene Chapter 6
        string currentScene = EditorSceneManager.GetActiveScene().path;
        if (currentScene != SCENE_PATH)
        {
            EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
        }

        // 2. Tạo hoặc liên kết GameObject Chapter6_StartPoint
        GameObject startPoint = GameObject.Find("Chapter6_StartPoint");
        if (startPoint == null)
        {
            startPoint = new GameObject("Chapter6_StartPoint");
            Undo.RegisterCreatedObjectUndo(startPoint, "Create Chapter6_StartPoint");
        }
        // Tọa độ cửa hang trên mặt đất vững chắc, hướng vào trong hang (+Z)
        startPoint.transform.position = new Vector3(0f, 0.05f, -38.0f);
        startPoint.transform.rotation = Quaternion.Euler(0f, 0f, 0f);

        // Đồng bộ điểm ThachSanh_SpawnPoint
        GameObject oldSpawn = GameObject.Find("ThachSanh_SpawnPoint");
        if (oldSpawn != null)
        {
            oldSpawn.transform.position = startPoint.transform.position;
            oldSpawn.transform.rotation = startPoint.transform.rotation;
        }

        // 3. Đảm bảo CHỈ CÓ DUY NHẤT 1 instance Thạch Sanh trong Scene
        GameObject[] allPlayers = GameObject.FindGameObjectsWithTag("Player");
        GameObject playerInstance = null;

        List<GameObject> toDestroy = new List<GameObject>();
        foreach (var p in allPlayers)
        {
            if (playerInstance == null)
            {
                playerInstance = p;
            }
            else
            {
                toDestroy.Add(p);
            }
        }

        // Kiểm tra thêm theo tên "thachsanh"
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var obj in allObjects)
        {
            if (obj.name.ToLower() == "thachsanh" || obj.name.ToLower() == "thach_sanh")
            {
                if (playerInstance == null)
                {
                    playerInstance = obj;
                }
                else if (obj != playerInstance && !toDestroy.Contains(obj))
                {
                    toDestroy.Add(obj);
                }
            }
        }

        // Xóa các bản sao trùng lặp nếu có
        foreach (var d in toDestroy)
        {
            Debug.Log($"[SetupChapter6Player] Đã xóa instance Thạch Sanh trùng lặp: {d.name}");
            Undo.DestroyObjectImmediate(d);
        }

        // Nếu chưa có, instantiate từ FBX Thạch Sanh gốc
        if (playerInstance == null)
        {
            GameObject tsFbx = AssetDatabase.LoadAssetAtPath<GameObject>(TS_FBX_PATH);
            if (tsFbx != null)
            {
                playerInstance = (GameObject)PrefabUtility.InstantiatePrefab(tsFbx);
                Undo.RegisterCreatedObjectUndo(playerInstance, "Instantiate ThachSanh");
            }
            else
            {
                Debug.LogError($"[SetupChapter6Player] Không tìm thấy model Thạch Sanh tại {TS_FBX_PATH}!");
                return;
            }
        }

        playerInstance.name = "thachsanh";
        playerInstance.tag = "Player";
        playerInstance.transform.position = startPoint.transform.position;
        playerInstance.transform.rotation = startPoint.transform.rotation;
        playerInstance.transform.localScale = Vector3.one;

        // 4. CẤU HÌNH CÁC COMPONENT THEO CHUẨN CHAPTER 3:
        // A. CharacterController
        CharacterController cc = playerInstance.GetComponent<CharacterController>();
        if (cc == null) cc = playerInstance.AddComponent<CharacterController>();
        cc.center = new Vector3(0f, 0.9f, 0f);
        cc.radius = 0.35f;
        cc.height = 1.8f;
        cc.stepOffset = 0.3f;
        cc.slopeLimit = 45f;
        cc.skinWidth = 0.08f;
        cc.minMoveDistance = 0.001f;

        // B. Animator
        Animator anim = playerInstance.GetComponent<Animator>();
        if (anim == null) anim = playerInstance.AddComponent<Animator>();
        RuntimeAnimatorController animCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(CONTROLLER_PATH);
        if (animCtrl != null)
        {
            anim.runtimeAnimatorController = animCtrl;
        }
        anim.applyRootMotion = false;

        // C. AttackPoint (Child GameObject cho tầm đánh chém/đá)
        Transform attackPointTr = playerInstance.transform.Find("AttackPoint");
        if (attackPointTr == null)
        {
            GameObject apObj = new GameObject("AttackPoint");
            apObj.transform.SetParent(playerInstance.transform, false);
            apObj.transform.localPosition = new Vector3(-0.024f, 0.97f, 0.5f);
            apObj.transform.localRotation = Quaternion.identity;
            attackPointTr = apObj.transform;
            Undo.RegisterCreatedObjectUndo(apObj, "Create AttackPoint");
        }

        // D. PlayerMovement
        PlayerMovement pm = playerInstance.GetComponent<PlayerMovement>();
        if (pm == null) pm = playerInstance.AddComponent<PlayerMovement>();
        pm.moveSpeed = 7f;
        pm.rotationSpeed = 10f;
        pm.gravity = -9.81f;
        pm.jumpForce = 5f;
        pm.attackPoint = attackPointTr;
        pm.attackRange = 1.5f;
        pm.attackDamage = 35;
        pm.kickDamage = 25;

        // E. PlayerCut (Phím E - Chém rìu)
        PlayerCut cut = playerInstance.GetComponent<PlayerCut>();
        if (cut == null) cut = playerInstance.AddComponent<PlayerCut>();

        // F. PlayerKick (Phím F - Đá chân)
        PlayerKick kick = playerInstance.GetComponent<PlayerKick>();
        if (kick == null) kick = playerInstance.AddComponent<PlayerKick>();

        // G. CapsuleCollider & Rigidbody (Khớp cấu hình Chapter 3)
        CapsuleCollider capCol = playerInstance.GetComponent<CapsuleCollider>();
        if (capCol == null) capCol = playerInstance.AddComponent<CapsuleCollider>();
        capCol.center = new Vector3(0f, 0.8f, 0f);
        capCol.radius = 0.35f;
        capCol.height = 1.8f;

        Rigidbody rb = playerInstance.GetComponent<Rigidbody>();
        if (rb == null) rb = playerInstance.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        // H. UI Canvas & Thanh máu (PlayerHealth)
        Slider healthBarSlider = EnsureHealthBarUI();

        PlayerHealth health = playerInstance.GetComponent<PlayerHealth>();
        if (health == null) health = playerInstance.AddComponent<PlayerHealth>();
        health.maxHealth = 100;
        health.currentHealth = 100;
        health.knockbackForce = 1.5f;
        health.knockbackDuration = 0.2f;
        if (healthBarSlider != null)
        {
            health.healthBar = healthBarSlider;
        }

        // 5. THIẾT LẬP CAMERA FOLLOW CHUYÊN DỤNG CHO CHAPTER 6
        Camera cam = Camera.main ?? Object.FindAnyObjectByType<Camera>();
        if (cam != null)
        {
            cam.name = "Main Camera";
            cam.tag = "MainCamera";
            cam.transform.position = new Vector3(0f, 2.5f, -44.5f);
            cam.transform.rotation = Quaternion.Euler(12f, 0f, 0f);

            // Tắt CameraFollow cũ nếu có để tránh xung đột
            CameraFollow oldCf = cam.GetComponent<CameraFollow>();
            if (oldCf != null)
            {
                oldCf.enabled = false;
            }

            // Gán Chapter6CameraFollow chuyên dụng
            Chapter6CameraFollow c6cf = cam.GetComponent<Chapter6CameraFollow>();
            if (c6cf == null) c6cf = cam.gameObject.AddComponent<Chapter6CameraFollow>();
            c6cf.enabled = true;
            c6cf.target = playerInstance.transform;
            c6cf.autoFindPlayer = true;
            c6cf.distance = 4.5f;
            c6cf.heightOffset = 2.0f;
            c6cf.smoothSpeed = 7.0f;
            c6cf.rotationSpeed = 1.5f;
            c6cf.preventWallClipping = true;
            c6cf.minDistance = 1.2f;

            EditorUtility.SetDirty(cam.gameObject);
            EditorUtility.SetDirty(c6cf);
        }

        // 6. Lưu Scene & Asset
        EditorUtility.SetDirty(playerInstance);
        EditorUtility.SetDirty(startPoint);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();

        Debug.Log($"[SetupChapter6Player] ĐÃ CẤU HÌNH THẠCH SANH VÀ CAMERA HOÀN TẤT TẠI: {playerInstance.transform.position}");

        if (showDialog)
        {
            EditorUtility.DisplayDialog("Thành công",
                "Đã cấu hình nhân vật Thạch Sanh hoàn tất trong Chapter 6:\n\n" +
                "- Điểm xuất hiện: Chapter6_StartPoint (0, 0.05, -38)\n" +
                "- Điều khiển: WASD di chuyển, Space nhảy\n" +
                "- Animation: Idle, Walk, Run, Jump, Attack, Kick\n" +
                "- Chiến đấu: Phím E chém rìu, Phím F đá\n" +
                "- Máu: PlayerHealth (100 HP) + Thanh máu UI\n" +
                "- Camera: CameraFollow bám theo góc nhìn thứ ba\n" +
                "- Chỉ 1 instance Thạch Sanh duy nhất trong Scene.",
                "OK");
        }
    }

    private static Slider EnsureHealthBarUI()
    {
        // 1. XÓA BỎ HOÀN TOÀN Chapter6_Canvas VÀ THANH MÁU XANH DƯ THỪA NẾU CÒN TỒN TẠI
        GameObject redundantCanvas = GameObject.Find("Chapter6_Canvas");
        if (redundantCanvas != null)
        {
            Undo.DestroyObjectImmediate(redundantCanvas);
            Debug.Log("[SetupChapter6Player] Đã xóa Chapter6_Canvas dư thừa (thanh màu xanh).");
        }

        // 2. TÌM THANH MÁU ĐỎ NGUYÊN BẢN CỦA THẠCH SANH
        // Trong Chapter 6, Thạch Sanh có Canvas con chứa "ThanhMau_ThachSanh" nguyên bản (thanh máu đỏ)
        Slider redSlider = null;
        Slider[] allSliders = Object.FindObjectsByType<Slider>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var s in allSliders)
        {
            if (s != null && s.gameObject.name == "ThanhMau_ThachSanh")
            {
                redSlider = s;
                break;
            }
        }

        if (redSlider != null)
        {
            // Căn chỉnh vị trí thanh máu đỏ lên góc trên bên trái màn hình (Top-Left)
            RectTransform rt = redSlider.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(30f, -25f);
                rt.sizeDelta = new Vector2(200f, 20f);
            }

            // Thiết lập giá trị Slider chuẩn 100 HP
            redSlider.minValue = 0f;
            redSlider.maxValue = 100f;
            redSlider.value = 100f;

            // Đảm bảo không còn bất kỳ nhãn chữ "THẠCH SANH" hay Text nào trên thanh máu đỏ
            foreach (var tmp in redSlider.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true))
            {
                Undo.DestroyObjectImmediate(tmp.gameObject);
            }
            foreach (var t in redSlider.GetComponentsInChildren<Text>(true))
            {
                Undo.DestroyObjectImmediate(t.gameObject);
            }

            EditorUtility.SetDirty(redSlider.gameObject);
            Debug.Log("[SetupChapter6Player] Đã khôi phục và căn chỉnh thanh máu đỏ nguyên bản của Thạch Sanh tại góc trên bên trái.");
            return redSlider;
        }

        return null;
    }
}
#endif
