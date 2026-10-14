using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using TMPro;

/// <summary>
/// Quản lý phân cảnh Lính hộ tống Lý Thông chầu Vua trong Chapter 4 (Chapter4_LythongCuopCong):
/// 1. Khóa điều khiển WASD của người chơi trên nhân vật Lý Thông.
/// 2. Lính dẫn đường đi qua các Waypoint đến trước mặt Nhà vua và Công chúa.
/// 3. Lý Thông tự động đi theo sau lưng người lính với khoảng cách 1.5 - 2.5m.
/// 4. Đồng bộ chuyển động Walk khi di chuyển và Idle khi dừng lại cho tất cả nhân vật.
/// 5. Đảm bảo Lý Thông không vượt lính và không đứng yên khi lính đang bước đi.
/// 6. Khi đến nơi, Nhà vua và Công chúa tự động quay mặt về phía Lý Thông.
/// 7. Lý Thông quay mặt về phía Nhà vua. Lính dẫn đường đứng nghiêm.
/// 8. Tự động kích hoạt hội thoại 7 câu (không cần bấm E), chuyển câu bằng Enter hoặc Click chuột trái.
/// </summary>
public class Chapter4SoldierEscort : MonoBehaviour
{
    public static Chapter4SoldierEscort Instance { get; private set; }

    public enum EscortState
    {
        Initializing,
        Escorting,
        ArrivedAtKing,
        InDialogue,
        Completed
    }

    [Header("Trạng thái hiện tại")]
    public EscortState currentState = EscortState.Initializing;

    [Header("Nhân vật tham chiếu")]
    [Tooltip("Nhân vật Lý Thông (để trống script sẽ tự tìm theo tên/tag)")]
    public Transform lyThong;

    [Tooltip("Lính dẫn đường (để trống script sẽ tự tìm lính gần Lý Thông nhất)")]
    public Transform escortSoldier;

    [Tooltip("Nhà vua (để trống script sẽ tự tìm)")]
    public Transform king;

    [Tooltip("Công chúa (để trống script sẽ tự tìm)")]
    public Transform princess;

    [Header("Đường đi (Waypoints)")]
    [Tooltip("Các mốc đường đi dẫn từ vị trí xuất phát đến trước ngai vàng")]
    public Transform[] waypoints;
    public int currentWaypointIndex = 0;

    [Header("Thông số di chuyển")]
    [Tooltip("Tốc độ bước đi của lính")]
    public float soldierSpeed = 2.8f;

    [Tooltip("Khoảng cách Lý Thông theo sau lính (1.5 - 2.5 đơn vị)")]
    [Range(1.5f, 2.5f)]
    public float followDistance = 2.0f;

    [Tooltip("Tốc độ xoay người khi chuyển hướng")]
    public float rotationSpeed = 6.0f;

    [Tooltip("Bán kính đến gần mốc Waypoint")]
    public float waypointTolerance = 1.0f;

    [Header("Camera")]
    public Camera mainCamera;
    [Tooltip("Vị trí & góc nhìn Camera khi hội kiến trước ngai vàng")]
    public Transform dialogueCameraPoint;
    public float cameraTransitionSpeed = 2.0f;

    [Header("UI Hội thoại (Tùy chọn)")]
    public TextMeshProUGUI dialogueText;

    [Header("UI Hội thoại Canvas (uGUI)")]
    [Tooltip("Panel khung thoại Canvas (RectTransform ở bottom-center)")]
    public GameObject dialoguePanel;
    [Tooltip("Text tên người nói")]
    public TextMeshProUGUI speakerText;
    [Tooltip("Text nội dung lời thoại")]
    public TextMeshProUGUI contentText;
    [Tooltip("Text hướng dẫn bấm phím chuyển thoại")]
    public TextMeshProUGUI hintText;

    // Component nội bộ
    private CharacterController lyThongCC;
    private CharacterController soldierCC;
    private Animator lyThongAnim;
    private Animator soldierAnim;
    private LyThongPlayerController lyThongInputController;
    private CameraFollow camFollow;

    // Cache các parameter tồn tại trên Animator để tránh gây cảnh báo Parameter Does Not Exist
    private string soldierSpeedParam = null;
    private string soldierWalkingBoolParam = null;
    private string lyThongSpeedParam = null;
    private string lyThongWalkingBoolParam = null;
    private bool animParamsCached = false;

    // Cờ đánh dấu kích hoạt đến nơi đúng 1 lần
    private bool hasTriggeredArrival = false;

    // Hội thoại
    public struct DialogueEntry
    {
        public string speaker;
        public string message;
        public Color speakerColor;

        public DialogueEntry(string speaker, string message, Color speakerColor)
        {
            this.speaker = speaker;
            this.message = message;
            this.speakerColor = speakerColor;
        }
    }

    private readonly DialogueEntry[] scriptLines = new DialogueEntry[]
    {
        new DialogueEntry("LÍNH", "Bẩm bệ hạ! Người có công tiêu diệt Chằn Tinh đã đến!", new Color(0.4f, 0.8f, 1.0f)),
        new DialogueEntry("NHÀ VUA", "Cho hắn tiến lên!", new Color(1.0f, 0.84f, 0.0f)),
        new DialogueEntry("LÝ THÔNG", "Muôn tâu bệ hạ! Chính thần đã tiêu diệt Chằn Tinh, trừ hại cho dân!", new Color(1.0f, 0.72f, 0.2f)),
        new DialogueEntry("NHÀ VUA", "Ngươi quả là người tài giỏi! Trẫm phong cho ngươi làm Quận công!", new Color(1.0f, 0.84f, 0.0f)),
        new DialogueEntry("LÝ THÔNG", "Thần xin đa tạ bệ hạ!", new Color(1.0f, 0.72f, 0.2f)),
        new DialogueEntry("CÔNG CHÚA", "Phụ vương, mong rằng từ nay dân chúng sẽ được bình yên.", new Color(1.0f, 0.6f, 0.75f)),
        new DialogueEntry("NHÀ VUA", "Truyền ban thưởng cho Lý Thông!", new Color(1.0f, 0.84f, 0.0f))
    };

    private int currentDialogueStep = 0;
    private bool isTransitioningCamera = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) { Destroy(this); return; }

        FindReferences();
        LockPlayerInput();
        EnsureCharacterControllers();
        EnsureAnimators();
    }

    void Start()
    {
        FindReferences();
        LockPlayerInput();
        EnsureCharacterControllers();
        EnsureAnimators();

        StartCoroutine(StartEscortSequence());
    }

    /// <summary>
    /// Tìm kiếm và gán các tham chiếu cần thiết trong Scene nếu chưa được kéo thả trong Inspector
    /// </summary>
    public void FindReferences()
    {
        // 1. Tìm Lý Thông
        if (lyThong == null)
        {
            GameObject ltObj = GameObject.Find("LyThong") ?? GameObject.Find("lythong");
            if (ltObj == null)
            {
                GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
                foreach (var p in players)
                {
                    if (p.name.ToLower().Contains("lythong") || p.name.ToLower().Contains("sage"))
                    {
                        ltObj = p;
                        break;
                    }
                }
            }
            if (ltObj != null) lyThong = ltObj.transform;
        }

        // 2. Tìm đúng Lính dẫn đường (Lính ở gần Lý Thông nhất)
        if (escortSoldier == null && lyThong != null)
        {
            GameObject leaderNamed = GameObject.Find("Linh_EscortLeader");
            if (leaderNamed != null)
            {
                escortSoldier = leaderNamed.transform;
            }
            else
            {
                GameObject[] allObjs = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                float minDistance = float.MaxValue;
                GameObject closestSoldier = null;

                foreach (var go in allObjs)
                {
                    if (go.name == "Linh" || go.name == "Linh_EscortLeader")
                    {
                        float dist = Vector3.Distance(go.transform.position, lyThong.position);
                        if (dist < minDistance && dist < 12.0f)
                        {
                            minDistance = dist;
                            closestSoldier = go;
                        }
                    }
                }

                if (closestSoldier != null)
                {
                    escortSoldier = closestSoldier.transform;
                    Debug.Log($"[Chapter4SoldierEscort] Đã tự động gắn Lính dẫn đường: '{escortSoldier.name}' tại {escortSoldier.position:F2}.");
                }
            }
        }

        // 3. Tìm Nhà vua
        if (king == null)
        {
            GameObject kObj = GameObject.Find("Meshy_AI_Dragon_Emperor_Rising_biped_Animation_01a0d469-8153-7436-8709-d393703fc126_withSkin") ??
                              GameObject.Find("vua") ?? GameObject.Find("King");
            if (kObj != null) king = kObj.transform;
        }

        // 4. Tìm Công chúa
        if (princess == null)
        {
            GameObject pObj = GameObject.Find("Congchua") ?? GameObject.Find("congchua") ?? GameObject.Find("Princess");
            if (pObj != null)
            {
                if (pObj.name == "Congchua_Model" && pObj.transform.parent != null)
                {
                    princess = pObj.transform.parent;
                }
                else
                {
                    princess = pObj.transform;
                }
            }
        }

        // 5. Tìm Camera
        if (mainCamera == null)
        {
            mainCamera = Camera.main ?? Object.FindAnyObjectByType<Camera>();
        }
        if (mainCamera != null)
        {
            camFollow = mainCamera.GetComponent<CameraFollow>();
        }

        // 6. Tìm Waypoints nếu chưa gắn
        if (waypoints == null || waypoints.Length == 0)
        {
            GameObject wpGroup = GameObject.Find("Chapter4_EscortWaypoints");
            if (wpGroup != null && wpGroup.transform.childCount > 0)
            {
                waypoints = new Transform[wpGroup.transform.childCount];
                for (int i = 0; i < wpGroup.transform.childCount; i++)
                {
                    waypoints[i] = wpGroup.transform.GetChild(i);
                }
                Debug.Log($"[Chapter4SoldierEscort] Đã nạp {waypoints.Length} Waypoint từ '{wpGroup.name}'.");
            }
        }

        // 7. Tìm Dialogue Camera Point
        if (dialogueCameraPoint == null)
        {
            GameObject dcp = GameObject.Find("Chapter4_DialogueCameraPoint");
            if (dcp != null) dialogueCameraPoint = dcp.transform;
        }

        // 8. Tự động tìm Canvas và DialoguePanel nếu có trong Scene
        if (dialoguePanel == null)
        {
            GameObject panelObj = GameObject.Find("DialoguePanel");
            if (panelObj != null)
            {
                dialoguePanel = panelObj;
            }
            else
            {
                GameObject canvasObj = GameObject.Find("Chapter4_DialogueCanvas");
                if (canvasObj != null)
                {
                    Transform tPanel = canvasObj.transform.Find("DialoguePanel");
                    if (tPanel != null) dialoguePanel = tPanel.gameObject;
                }
            }
        }

        if (dialoguePanel != null)
        {
            if (speakerText == null)
            {
                Transform sObj = dialoguePanel.transform.Find("SpeakerText");
                if (sObj != null) speakerText = sObj.GetComponent<TextMeshProUGUI>();
            }
            if (contentText == null)
            {
                Transform cObj = dialoguePanel.transform.Find("ContentText");
                if (cObj != null) contentText = cObj.GetComponent<TextMeshProUGUI>();
            }
            if (hintText == null)
            {
                Transform hObj = dialoguePanel.transform.Find("HintText");
                if (hObj != null) hintText = hObj.GetComponent<TextMeshProUGUI>();
            }
        }
    }

    /// <summary>
    /// Vô hiệu hóa script điều khiển WASD của Lý Thông
    /// </summary>
    private void LockPlayerInput()
    {
        if (lyThong != null)
        {
            lyThongInputController = lyThong.GetComponent<LyThongPlayerController>();
            if (lyThongInputController != null && lyThongInputController.enabled)
            {
                lyThongInputController.enabled = false;
                Debug.Log("[Chapter4SoldierEscort] Đã vô hiệu hóa LyThongPlayerController (khóa di chuyển WASD).");
            }
        }
    }

    private void EnsureCharacterControllers()
    {
        if (lyThong != null)
        {
            lyThongCC = lyThong.GetComponent<CharacterController>();
            if (lyThongCC == null)
            {
                lyThongCC = lyThong.gameObject.AddComponent<CharacterController>();
                lyThongCC.height = 1.8f;
                lyThongCC.radius = 0.35f;
                lyThongCC.center = new Vector3(0f, 0.9f, 0f);
            }
        }

        if (escortSoldier != null)
        {
            soldierCC = escortSoldier.GetComponent<CharacterController>();
            if (soldierCC == null)
            {
                soldierCC = escortSoldier.gameObject.AddComponent<CharacterController>();
                soldierCC.height = 1.8f;
                soldierCC.radius = 0.35f;
                soldierCC.center = new Vector3(0f, 0.9f, 0f);
                soldierCC.stepOffset = 0.3f;
                soldierCC.slopeLimit = 45f;
            }
        }
    }

    private static readonly string[] SPEED_FLOAT_CANDIDATES = new string[] { "Speed", "MoveSpeed", "moveSpeed", "speed" };
    private static readonly string[] WALKING_BOOL_CANDIDATES = new string[] { "IsWalking", "isWalking", "Walking", "walking", "Walk", "walk" };

    private static string FindParameterName(Animator anim, string[] candidates, AnimatorControllerParameterType type)
    {
        if (anim == null || anim.runtimeAnimatorController == null) return null;
        try
        {
            foreach (var p in anim.parameters)
            {
                if (p.type == type)
                {
                    foreach (var candidate in candidates)
                    {
                        if (string.Equals(p.name, candidate, System.StringComparison.OrdinalIgnoreCase))
                        {
                            return p.name;
                        }
                    }
                }
            }
        }
        catch { }
        return null;
    }

    public void CacheAnimatorParameters()
    {
        if (soldierAnim != null && soldierAnim.runtimeAnimatorController != null)
        {
            soldierSpeedParam = FindParameterName(soldierAnim, SPEED_FLOAT_CANDIDATES, AnimatorControllerParameterType.Float);
            soldierWalkingBoolParam = FindParameterName(soldierAnim, WALKING_BOOL_CANDIDATES, AnimatorControllerParameterType.Bool);
        }

        if (lyThongAnim != null && lyThongAnim.runtimeAnimatorController != null)
        {
            lyThongSpeedParam = FindParameterName(lyThongAnim, SPEED_FLOAT_CANDIDATES, AnimatorControllerParameterType.Float);
            lyThongWalkingBoolParam = FindParameterName(lyThongAnim, WALKING_BOOL_CANDIDATES, AnimatorControllerParameterType.Bool);
        }

        animParamsCached = (soldierAnim != null && soldierAnim.runtimeAnimatorController != null) &&
                           (lyThongAnim != null && lyThongAnim.runtimeAnimatorController != null);
    }

    private void EnsureAnimators()
    {
        if (lyThong != null)
        {
            lyThongAnim = lyThong.GetComponent<Animator>();
            if (lyThongAnim == null) lyThongAnim = lyThong.GetComponentInChildren<Animator>();
            if (lyThongAnim != null) lyThongAnim.applyRootMotion = false;
        }

        if (escortSoldier != null)
        {
            soldierAnim = escortSoldier.GetComponent<Animator>();
            if (soldierAnim == null) soldierAnim = escortSoldier.GetComponentInChildren<Animator>();
            if (soldierAnim != null) soldierAnim.applyRootMotion = false;
        }

        CacheAnimatorParameters();
    }

    private IEnumerator StartEscortSequence()
    {
        // Chờ 0.5s đầu tiên cho scene và physics nạp ổn định
        yield return new WaitForSeconds(0.5f);

        LockPlayerInput();

        if (camFollow != null && lyThong != null)
        {
            camFollow.target = lyThong;
            camFollow.distance = 4.5f;
            camFollow.heightOffset = 1.8f;
            camFollow.enabled = true;
        }

        currentWaypointIndex = 0;
        if (waypoints != null && waypoints.Length > 1 && escortSoldier != null)
        {
            float distToStart = Vector3.Distance(escortSoldier.position, waypoints[0].position);
            if (distToStart < 2.0f)
            {
                currentWaypointIndex = 1;
            }
        }

        hasTriggeredArrival = false;
        currentState = EscortState.Escorting;
        Debug.Log("[Chapter4SoldierEscort] Bắt đầu quá trình dẫn đường tới nhà vua!");
    }

    void Update()
    {
        // Khóa tuyệt đối WASD trong suốt quá trình
        if (lyThongInputController != null && lyThongInputController.enabled)
        {
            lyThongInputController.enabled = false;
        }

        switch (currentState)
        {
            case EscortState.Escorting:
                UpdateEscortMovement();
                break;

            case EscortState.ArrivedAtKing:
                UpdateArrivedFacing();
                break;

            case EscortState.InDialogue:
                UpdateDialogueInput();
                MaintainDialogueFacing();
                break;
        }
    }

    /// <summary>
    /// Điều khiển người lính đi theo Waypoint và Lý Thông bám sát phía sau
    /// </summary>
    private void UpdateEscortMovement()
    {
        if (escortSoldier == null || lyThong == null || waypoints == null || waypoints.Length == 0) return;

        // 1. DI CHUYỂN NGƯỜI LÍNH (Được bảo vệ nghiêm ngặt chống tràn chỉ mục mảng)
        bool soldierReachedDest = (currentWaypointIndex >= waypoints.Length);

        if (!soldierReachedDest)
        {
            Transform targetWaypoint = waypoints[currentWaypointIndex];
            Vector3 soldierPos = escortSoldier.position;
            Vector3 targetPos = targetWaypoint.position;

            Vector3 soldierMoveDir = (targetPos - soldierPos);
            soldierMoveDir.y = 0f;
            float distToWaypoint = soldierMoveDir.magnitude;

            if (distToWaypoint > waypointTolerance)
            {
                soldierMoveDir.Normalize();

                // Xoay lính về hướng đi
                Quaternion targetRot = Quaternion.LookRotation(soldierMoveDir);
                escortSoldier.rotation = Quaternion.Slerp(escortSoldier.rotation, targetRot, rotationSpeed * Time.deltaTime);

                // Bước đi
                Vector3 moveVelocity = escortSoldier.forward * soldierSpeed;
                moveVelocity.y = -9.81f; // Trọng lực mặt đất

                if (soldierCC != null && soldierCC.enabled)
                {
                    soldierCC.Move(moveVelocity * Time.deltaTime);
                }
                else
                {
                    escortSoldier.position += moveVelocity * Time.deltaTime;
                }

                // Animation Walk cho lính
                SetSoldierWalking(true);
            }
            else
            {
                // Đã chạm waypoint hiện tại -> chuyển sang waypoint kế tiếp
                currentWaypointIndex++;
                if (currentWaypointIndex >= waypoints.Length)
                {
                    // ĐÃ ĐẾN MỐC CUỐI CÙNG (Trước mặt nhà vua)
                    soldierReachedDest = true;
                    SetSoldierWalking(false);
                }
            }
        }
        else
        {
            SetSoldierWalking(false);
        }

        // 2. LÝ THÔNG TỰ ĐỘNG ĐI THEO SAU LƯNG LÍNH
        Vector3 lyThongPos = lyThong.position;
        Vector3 behindSoldierPos = escortSoldier.position - (escortSoldier.forward * followDistance);
        behindSoldierPos.y = lyThongPos.y;

        Vector3 toSoldier = (escortSoldier.position - lyThongPos);
        toSoldier.y = 0f;
        float actualDistToSoldier = toSoldier.magnitude;

        Vector3 lyThongMoveDir = (behindSoldierPos - lyThongPos);
        lyThongMoveDir.y = 0f;
        float distToFollowSpot = lyThongMoveDir.magnitude;

        bool lyThongIsMoving = false;
        bool lyThongArrived = soldierReachedDest && (actualDistToSoldier <= (followDistance + 0.45f) || distToFollowSpot <= 0.4f);

        // Nếu chưa đến điểm dừng và còn cách xa lính, tiếp tục bước theo
        if (!lyThongArrived && (actualDistToSoldier > (followDistance * 0.9f) || distToFollowSpot > 0.3f))
        {
            if (lyThongMoveDir.sqrMagnitude > 0.001f)
            {
                lyThongMoveDir.Normalize();

                // Xoay mặt theo hướng đi
                Quaternion ltTargetRot = Quaternion.LookRotation(lyThongMoveDir);
                lyThong.rotation = Quaternion.Slerp(lyThong.rotation, ltTargetRot, rotationSpeed * Time.deltaTime);

                // Điều chỉnh tốc độ: nếu bị tụt lại xa thì đi nhanh hơn để bắt kịp
                float speedMultiplier = 1.0f;
                if (actualDistToSoldier > followDistance * 1.3f) speedMultiplier = 1.25f;
                else if (actualDistToSoldier < followDistance) speedMultiplier = 0.85f;

                Vector3 ltVelocity = lyThong.forward * (soldierSpeed * speedMultiplier);
                ltVelocity.y = -9.81f;

                if (lyThongCC != null && lyThongCC.enabled)
                {
                    lyThongCC.Move(ltVelocity * Time.deltaTime);
                }
                else
                {
                    lyThong.position += ltVelocity * Time.deltaTime;
                }

                lyThongIsMoving = true;
            }
        }

        // Cập nhật Animation cho Lý Thông (Speed > 0.1 -> Chạy/Bước, <= 0.1 -> Đứng Idle)
        SetLyThongWalking(lyThongIsMoving);

        // 3. KIỂM TRA HOÀN TẤT HÀNH TRÌNH (Chỉ kích hoạt DUY NHẤT một lần)
        if (soldierReachedDest && (!lyThongIsMoving || lyThongArrived) && !hasTriggeredArrival)
        {
            hasTriggeredArrival = true;
            OnBothArrivedAtDestination();
        }
    }

    private void SetSoldierWalking(bool walking)
    {
        if (soldierAnim != null)
        {
            if (!animParamsCached) CacheAnimatorParameters();

            soldierAnim.speed = 1.0f;
            if (!string.IsNullOrEmpty(soldierSpeedParam))
            {
                soldierAnim.SetFloat(soldierSpeedParam, walking ? 1.0f : 0.0f);
            }
            if (!string.IsNullOrEmpty(soldierWalkingBoolParam))
            {
                soldierAnim.SetBool(soldierWalkingBoolParam, walking);
            }
        }
    }

    private void SetLyThongWalking(bool walking)
    {
        if (lyThongAnim != null)
        {
            if (!animParamsCached) CacheAnimatorParameters();

            if (!string.IsNullOrEmpty(lyThongSpeedParam))
            {
                lyThongAnim.SetFloat(lyThongSpeedParam, walking ? 1.0f : 0.0f);
            }
            if (!string.IsNullOrEmpty(lyThongWalkingBoolParam))
            {
                lyThongAnim.SetBool(lyThongWalkingBoolParam, walking);
            }
        }
    }

    private void OnBothArrivedAtDestination()
    {
        SetSoldierWalking(false);
        SetLyThongWalking(false);

        currentState = EscortState.ArrivedAtKing;
        Debug.Log("[Chapter4SoldierEscort] Đã đến trước mặt Nhà vua và Công chúa! Đang xoay nhân vật đối diện...");

        StartCoroutine(ArrivedAndPrepareDialogueRoutine());
    }

    /// <summary>
    /// Xoay Nhà vua & Công chúa về phía Lý Thông, Lý Thông & Lính về phía Nhà vua
    /// </summary>
    private void UpdateArrivedFacing()
    {
        RotateCharactersFacing();

        // Chuyển mượt Camera nếu đang chuyển cảnh
        if (isTransitioningCamera && mainCamera != null && dialogueCameraPoint != null)
        {
            mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, dialogueCameraPoint.position, cameraTransitionSpeed * Time.deltaTime);
            mainCamera.transform.rotation = Quaternion.Slerp(mainCamera.transform.rotation, dialogueCameraPoint.rotation, cameraTransitionSpeed * Time.deltaTime);
        }
    }

    private void MaintainDialogueFacing()
    {
        RotateCharactersFacing();
    }

    private void RotateCharactersFacing()
    {
        if (lyThong == null) return;
        Vector3 lyThongPos = lyThong.position;

        // 1. NHÀ VUA tự quay mặt về phía Lý Thông
        if (king != null)
        {
            Vector3 dirK = (lyThongPos - king.position);
            dirK.y = 0f;
            if (dirK.sqrMagnitude > 0.01f)
            {
                Quaternion rotK = Quaternion.LookRotation(dirK.normalized);
                king.rotation = Quaternion.Slerp(king.rotation, rotK, rotationSpeed * Time.deltaTime);
            }
        }

        // 2. CÔNG CHÚA tự quay mặt về phía Lý Thông (bảo toàn góc đứng X = -90 độ)
        if (princess != null)
        {
            Vector3 dirP = (lyThongPos - princess.position);
            dirP.y = 0f;
            if (dirP.sqrMagnitude > 0.01f)
            {
                Quaternion yawRot = Quaternion.LookRotation(dirP.normalized);

                // Kiểm tra xem princess là GameObject cha (có con là model) hay là model FBX trực tiếp
                Transform modelChild = princess.Find("Congchua_Model");
                if (modelChild != null)
                {
                    // Trường hợp có GameObject cha:
                    // 1. Quay GameObject cha chỉ theo trục Y để mặt hướng về Lý Thông
                    princess.rotation = Quaternion.Slerp(princess.rotation, yawRot, rotationSpeed * Time.deltaTime);

                    // 2. Cố định model con luôn giữ nguyên góc nghiêng X = -90 độ để đứng thẳng
                    modelChild.localPosition = Vector3.zero;
                    modelChild.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                }
                else
                {
                    // Trường hợp princess là chính model FBX (chưa có cha):
                    // Bù thêm góc nghiêng X = -90 độ vào Quaternion để mô hình luôn đứng thẳng
                    Quaternion targetRotP = yawRot * Quaternion.Euler(-90f, 0f, 0f);
                    princess.rotation = Quaternion.Slerp(princess.rotation, targetRotP, rotationSpeed * Time.deltaTime);
                }
            }
        }

        // 3. LÝ THÔNG quay mặt về phía Nhà vua
        if (king != null)
        {
            Vector3 dirL = (king.position - lyThong.position);
            dirL.y = 0f;
            if (dirL.sqrMagnitude > 0.01f)
            {
                Quaternion rotL = Quaternion.LookRotation(dirL.normalized);
                lyThong.rotation = Quaternion.Slerp(lyThong.rotation, rotL, rotationSpeed * Time.deltaTime);
            }
        }

        // 4. LÍNH DẪN ĐƯỜNG đứng nghiêm quay mặt về phía Nhà vua
        if (escortSoldier != null && king != null)
        {
            Vector3 dirS = (king.position - escortSoldier.position);
            dirS.y = 0f;
            if (dirS.sqrMagnitude > 0.01f)
            {
                Quaternion rotS = Quaternion.LookRotation(dirS.normalized);
                escortSoldier.rotation = Quaternion.Slerp(escortSoldier.rotation, rotS, rotationSpeed * Time.deltaTime);
            }
        }
    }

    private IEnumerator ArrivedAndPrepareDialogueRoutine()
    {
        // 1. Tắt CameraFollow để máy quay cố định vào góc đại điện
        if (camFollow != null) camFollow.enabled = false;

        // Đảm bảo dialogueCameraPoint đã sẵn sàng
        if (dialogueCameraPoint == null && king != null && lyThong != null)
        {
            GameObject dcp = GameObject.Find("Chapter4_DialogueCameraPoint");
            if (dcp == null)
            {
                dcp = new GameObject("Chapter4_DialogueCameraPoint");
                dcp.transform.position = new Vector3(-51.5f, -8.2f, 17.5f);
                dcp.transform.rotation = Quaternion.Euler(10f, 32f, 0f);
            }
            dialogueCameraPoint = dcp.transform;
        }

        isTransitioningCamera = true;

        // Chờ 1.2 giây để camera chuyển và các nhân vật xoay mặt hoàn tất
        yield return new WaitForSeconds(1.2f);

        isTransitioningCamera = false;
        if (mainCamera != null && dialogueCameraPoint != null)
        {
            mainCamera.transform.position = dialogueCameraPoint.position;
            mainCamera.transform.rotation = dialogueCameraPoint.rotation;
        }

        // Mở khóa con trỏ chuột để người chơi tiện click chuột chuyển thoại
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Đảm bảo hoạt ảnh Idle tự nhiên cho Lý Thông và Lính
        SetSoldierWalking(false);
        SetLyThongWalking(false);

        // TỰ ĐỘNG BẮT ĐẦU HỘI THOẠI (Không cần người chơi bấm E)
        currentDialogueStep = 0;
        currentState = EscortState.InDialogue;
        UpdateDialogueUI();
        Debug.Log("[Chapter4SoldierEscort] TỰ ĐỘNG KÍCH HOẠT HỘI THOẠI CƯỚP CÔNG THÀNH CÔNG!");
    }

    /// <summary>
    /// Nhận phím Enter / Space / Click chuột trái để chuyển câu thoại
    /// </summary>
    private void UpdateDialogueInput()
    {
        bool nextPressed = false;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && (Keyboard.current.enterKey.wasPressedThisFrame ||
                                         Keyboard.current.numpadEnterKey.wasPressedThisFrame ||
                                         Keyboard.current.spaceKey.wasPressedThisFrame))
        {
            nextPressed = true;
        }
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            nextPressed = true;
        }
#endif

        if (!nextPressed)
        {
            try
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) ||
                    Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
                {
                    nextPressed = true;
                }
            }
            catch { }
        }

        if (nextPressed)
        {
            AdvanceDialogue();
        }
    }

    private void AdvanceDialogue()
    {
        currentDialogueStep++;
        if (currentDialogueStep < scriptLines.Length)
        {
            UpdateDialogueUI();
        }
        else
        {
            EndDialogue();
        }
    }

    private void UpdateDialogueUI()
    {
        if (currentDialogueStep < 0 || currentDialogueStep >= scriptLines.Length) return;

        DialogueEntry entry = scriptLines[currentDialogueStep];
        string hexColor = ColorUtility.ToHtmlStringRGB(entry.speakerColor);
        string formatted = $"<b><color=#{hexColor}>{entry.speaker}:</color></b>\n\"{entry.message}\"";

        // Cập nhật giao diện Canvas uGUI nếu có
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
            if (speakerText != null)
            {
                speakerText.text = entry.speaker + ":";
                speakerText.color = entry.speakerColor;
            }
            if (contentText != null)
            {
                contentText.text = $"\"{entry.message}\"";
            }
            if (hintText != null)
            {
                hintText.text = "(Bấm Enter hoặc Chuột trái để tiếp tục)";
            }
        }

        if (dialogueText != null)
        {
            dialogueText.text = formatted;
            dialogueText.gameObject.SetActive(true);
        }
    }

    private void EndDialogue()
    {
        currentState = EscortState.Completed;
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (dialogueText != null) dialogueText.text = "";

        Debug.Log("[Chapter4SoldierEscort] Cuộc hội thoại đã kết thúc thành công! Lý Thông được phong làm Quận công.");
    }

    private Texture2D boxBgTexture;

    private Texture2D GetBoxBackground()
    {
        if (boxBgTexture == null)
        {
            boxBgTexture = new Texture2D(1, 1);
            // Nền tối bán trong suốt (alpha = 0.65) để nhìn thấy bối cảnh phía sau
            boxBgTexture.SetPixel(0, 0, new Color(0.06f, 0.08f, 0.12f, 0.65f));
            boxBgTexture.Apply();
        }
        return boxBgTexture;
    }

    void OnGUI()
    {
        // Nếu đã có Canvas DialoguePanel đang hiển thị thì không vẽ OnGUI để tránh trùng lặp
        if (dialoguePanel != null && dialoguePanel.activeInHierarchy)
        {
            return;
        }

        // Vẽ đè lên trên cùng màn hình
        GUI.depth = -100;

        // Hiển thị khung hội thoại OnGUI (gọn gàng, bottom-center, nền mờ thấy bối cảnh)
        if (currentState == EscortState.InDialogue && currentDialogueStep < scriptLines.Length)
        {
            DialogueEntry line = scriptLines[currentDialogueStep];

            GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.normal.background = GetBoxBackground();
            boxStyle.wordWrap = true;
            boxStyle.alignment = TextAnchor.UpperLeft;
            boxStyle.padding = new RectOffset(35, 35, 18, 18);
            boxStyle.normal.textColor = Color.white;
            boxStyle.richText = true;

            // Kích thước chuẩn thống nhất: chiếm ~75-80% chiều rộng màn hình (tối đa 1100px), cao 215px, cách đáy 30px
            float width = Mathf.Min(Screen.width * 0.78f, 1100f);
            float height = 215f;
            float left = (Screen.width - width) / 2f;
            float top = Screen.height - height - 30f;

            string colorHex = ColorUtility.ToHtmlStringRGB(line.speakerColor);
            string fullMsg = $"<size=34><b><color=#{colorHex}>{line.speaker}:</color></b></size>\n" +
                             $"<size=28>\"{line.message}\"</size>\n\n" +
                             $"<color=#FFD700><size=20><i>(Bấm Enter hoặc Chuột trái để tiếp tục)</i></size></color>";

            GUI.Box(new Rect(left, top, width, height), fullMsg, boxStyle);
        }
        else if (currentState == EscortState.Completed)
        {
            GUIStyle completeStyle = new GUIStyle(GUI.skin.box);
            completeStyle.normal.background = GetBoxBackground();
            completeStyle.fontSize = 17;
            completeStyle.alignment = TextAnchor.MiddleCenter;
            completeStyle.normal.textColor = Color.yellow;
            completeStyle.richText = true;

            float width = 460f;
            float height = 60f;
            float left = (Screen.width - width) / 2f;
            float top = Screen.height - 100f;

            GUI.Box(new Rect(left, top, width, height),
                "<b><color=#00FF00>LÝ THÔNG ĐÃ ĐƯỢC PHONG LÀM QUẬN CÔNG!</color></b>\n<size=13>Hoàn thành phân cảnh bái kiến triều đình Chương 4</size>",
                completeStyle);
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (waypoints != null && waypoints.Length > 1)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < waypoints.Length - 1; i++)
            {
                if (waypoints[i] != null && waypoints[i + 1] != null)
                {
                    Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
                    Gizmos.DrawSphere(waypoints[i].position, 0.4f);
                }
            }
            if (waypoints[waypoints.Length - 1] != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(waypoints[waypoints.Length - 1].position, 0.6f);
            }
        }
    }
#endif
}
